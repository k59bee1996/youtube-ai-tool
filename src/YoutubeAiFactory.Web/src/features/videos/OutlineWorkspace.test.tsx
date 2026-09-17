import { cleanup, fireEvent, render, screen } from "@testing-library/react"
import { afterEach, expect, test, vi } from "vitest"
import type { OutlineStatus, VideoOutline } from "../../services/api"
import { OutlineWorkspace } from "./OutlineWorkspace"

afterEach(() => {
  cleanup()
  vi.useRealTimers()
  vi.restoreAllMocks()
  vi.unstubAllGlobals()
})

test("shows the ResearchReady empty state and queues generation", async () => {
  let generated = false
  const fetchMock = vi.fn((input: RequestInfo | URL, init?: RequestInit) => {
    const url = String(input)
    const method = init?.method ?? "GET"
    if (url.endsWith("/outlines") && method === "GET") return json([])
    if (url.endsWith("/outline:generate") && method === "POST") {
      generated = true
      return json({ jobId: "job-1", status: "Queued" }, 202)
    }
    if (url.endsWith("/outline/latest")) return json(generated
      ? status({ latestOutline: null, activeJob: { id: "job-1", status: "Queued", failureReason: null }, canGenerate: false })
      : status({ latestOutline: null, activeJob: null, canGenerate: true }))
    throw new Error(`Unexpected request: ${method} ${url}`)
  })
  vi.stubGlobal("fetch", fetchMock)

  render(<OutlineWorkspace projectId="project-1" videoProjectId="video-1" revisionKey="r1" onWorkflowStatusChange={vi.fn()} />)

  fireEvent.click(await screen.findByRole("button", { name: "Generate Outline" }))
  expect(await screen.findByText(/Generating outline.*Queued/)).toBeVisible()
  expect(fetchMock).toHaveBeenCalledWith(expect.stringContaining("/outline:generate"), expect.objectContaining({ method: "POST" }))
})

test("renders evidence traceability, conflicts, gaps, and explicitly approves a ready outline", async () => {
  const outline = readyOutline()
  const approved = { ...outline, status: "Approved", approvedAt: "2026-09-16T00:02:00Z" }
  const onStatus = vi.fn()
  vi.stubGlobal("fetch", vi.fn((input: RequestInfo | URL, init?: RequestInit) => {
    const url = String(input)
    const method = init?.method ?? "GET"
    if (url.endsWith("/outline/latest")) return json(status({ latestOutline: outline, activeJob: null, canGenerate: true }))
    if (url.endsWith("/outlines") && method === "GET") return json([{ id: outline.id, version: 1, status: "Ready", researchReportVersion: 1, structureType: "Explainer", isStale: false, createdAt: outline.createdAt, approvedAt: null }])
    if (url.endsWith(`/${outline.id}:approve`) && method === "POST") return json(approved)
    throw new Error(`Unexpected request: ${method} ${url}`)
  }))

  render(<OutlineWorkspace projectId="project-1" videoProjectId="video-1" revisionKey="r1" onWorkflowStatusChange={onStatus} />)

  expect(await screen.findByText("What made ownership costly?")).toBeVisible()
  expect(screen.getByText(/Conflicted evidence:/)).toBeVisible()
  expect(screen.getByText(/Research gap:/)).toBeVisible()
  fireEvent.click(screen.getAllByText(/Inspect supporting claims/)[0])
  expect(screen.getAllByText("Ownership required continuing staffing obligations.")[0]).toBeVisible()
  expect(screen.getAllByText("Castle accounts")[0]).toBeVisible()

  fireEvent.click(screen.getByRole("button", { name: "Approve Outline" }))
  expect(await screen.findByText(/Outline Approved/)).toBeVisible()
  expect(screen.queryByRole("button", { name: "Move up" })).not.toBeInTheDocument()
  expect(onStatus).toHaveBeenCalledWith("OutlineApproved")
})

test("uses a persisted Vietnamese reading overlay without changing outline identity", async () => {
  const outline = readyOutline()
  const localizationUrl = `/api/projects/project-1/video-projects/video-1/outlines/${outline.id}/localizations/vi`
  const fetchMock = vi.fn((input: RequestInfo | URL, init?: RequestInit) => {
    const url = String(input)
    const method = init?.method ?? "GET"
    if (url.endsWith("/outline/latest")) return json(status({ latestOutline: outline, activeJob: null, canGenerate: true }))
    if (url.endsWith("/outlines") && method === "GET") return json([])
    if (url === localizationUrl && method === "GET") return json({ content: {
      canonicalContentFingerprint: "a".repeat(64),
      coreQuestion: "Điều gì khiến quyền sở hữu trở nên tốn kém?",
      coreTension: "Uy tín đi cùng nghĩa vụ định kỳ.", openingHookConcept: "Mở bằng sự tương phản.",
      viewerPromise: "Hiểu chi phí sở hữu thực tế.", narrativeProgression: "Từ bối cảnh đến cơ chế.",
      payoff: "Hiểu hệ thống nghĩa vụ.", pacingStrategy: "Tăng dần đến phần tổng hợp.",
      howOutlineImplementsExperiment: "Giữ nguyên tiền đề đóng gói.", risksToExperimentIntegrity: [], warnings: [],
      sections: outline.sections.map(section => ({ sectionId: section.id, heading: `VI ${section.heading}`,
        objective: `VI ${section.objective}`, summary: `VI ${section.summary}`, viewerQuestion: section.viewerQuestion,
        transitionIntent: section.transitionIntent })),
    }, activeJob: null, latestJob: null })
    throw new Error(`Unexpected request: ${method} ${url}`)
  })
  vi.stubGlobal("fetch", fetchMock)

  render(<OutlineWorkspace projectId="project-1" videoProjectId="video-1" revisionKey="r1" onWorkflowStatusChange={vi.fn()} />)
  fireEvent.click(await screen.findByRole("button", { name: "VI" }))

  expect(await screen.findByText("Điều gì khiến quyền sở hữu trở nên tốn kém?")).toBeVisible()
  expect(fetchMock).toHaveBeenCalledWith(localizationUrl, expect.anything())
  expect(fetchMock.mock.calls.filter(([input, init]) => String(input) === localizationUrl && (init as RequestInit | undefined)?.method === "POST")).toHaveLength(0)
})

test("continues polling a queued Vietnamese overlay until it is available", async () => {
  const outline = readyOutline()
  const localizationUrl = `/api/projects/project-1/video-projects/video-1/outlines/${outline.id}/localizations/vi`
  let localizationGets = 0
  vi.stubGlobal("fetch", vi.fn((input: RequestInfo | URL, init?: RequestInit) => {
    const url = String(input)
    const method = init?.method ?? "GET"
    if (url.endsWith("/outline/latest")) return json(status({ latestOutline: outline, activeJob: null, canGenerate: true }))
    if (url.endsWith("/outlines") && method === "GET") return json([])
    if (url === localizationUrl && method === "POST") return json({ jobId: "translation-1", status: "Queued" }, 202)
    if (url === localizationUrl && method === "GET") {
      localizationGets++
      if (localizationGets < 3) return json({ content: null, activeJob: { id: "translation-1", status: "Running", failureReason: null }, latestJob: null })
      return json({ content: localizedContent(outline), activeJob: null, latestJob: { id: "translation-1", status: "Completed", failureReason: null } })
    }
    throw new Error(`Unexpected request: ${method} ${url}`)
  }))

  render(<OutlineWorkspace projectId="project-1" videoProjectId="video-1" revisionKey="r1" onWorkflowStatusChange={vi.fn()} />)
  const toggle = await screen.findByRole("button", { name: "VI" })
  vi.useFakeTimers()
  fireEvent.click(toggle)
  await Promise.resolve()
  await vi.advanceTimersByTimeAsync(2000)

  expect(screen.getByText("VI What made ownership costly?")).toBeVisible()
  expect(localizationGets).toBeGreaterThanOrEqual(3)
})

test("shows stale outline warning and withholds edit reorder and approval actions", async () => {
  const outline = { ...readyOutline(), isStale: true }
  vi.stubGlobal("fetch", vi.fn((input: RequestInfo | URL) => {
    const url = String(input)
    if (url.endsWith("/outline/latest")) return json(status({ latestOutline: outline, canGenerate: false,
      blockReason: "The current research is outdated." }))
    if (url.endsWith("/outlines")) return json([])
    throw new Error(`Unexpected request: ${url}`)
  }))

  render(<OutlineWorkspace projectId="project-1" videoProjectId="video-1" revisionKey="r1" onWorkflowStatusChange={vi.fn()} />)

  expect(await screen.findByText(/This outline is outdated/)).toBeVisible()
  expect(screen.queryByRole("button", { name: "Approve Outline" })).not.toBeInTheDocument()
  expect(screen.queryByRole("button", { name: "Edit sections" })).not.toBeInTheDocument()
  expect(screen.queryByRole("button", { name: "Move up" })).not.toBeInTheDocument()
})

function status(overrides: Partial<OutlineStatus>): OutlineStatus {
  return { latestOutline: null, activeJob: null, latestJob: null, canGenerate: false, blockReason: null, ...overrides }
}

function readyOutline(): VideoOutline {
  const source = { id: "source-1", url: "https://archive.example/castle", canonicalUrl: "https://archive.example/castle",
    domain: "archive.example", title: "Castle accounts", publisher: "Archive", publishedAt: null,
    retrievedAt: "2026-09-16T00:00:00Z", category: "Primary", fetchStatus: "Fetched" }
  const claim = { id: "claim-1", statement: "Ownership required continuing staffing obligations.", type: "Factual",
    supportStatus: "Conflicted", confidence: 82, isCritical: true, usageRole: "Core",
    evidence: [{ id: "evidence-1", fact: "Staffing was recurring.", supportingExcerpt: "Accounts list household roles.",
      sourceLocator: "ledger 12", type: "Fact", confidence: 90, source }] }
  const baseSection = { heading: "The contradiction", purpose: "Context", objective: "Establish the central tension.",
    summary: "Use the supported obligation claim.", viewerQuestion: "What did ownership require?",
    transitionIntent: "Move into the economic mechanism.", estimatedSeconds: 60, claims: [claim], conflicts: [], researchGaps: [] }
  return {
    id: "outline-1", projectId: "project-1", videoProjectId: "video-1", researchReportId: "report-1",
    researchReportVersion: 1, version: 1, status: "Ready", outlineAlgorithmVersion: "outline-engine:v1",
    promptKey: "outline-generation", promptVersion: 1, provider: "fake", model: "reasoning-model",
    structureType: "Explainer", coreQuestion: "What made ownership costly?", coreTension: "Prestige carried recurring obligations.",
    openingHookConcept: "Open on visible prestige versus hidden obligations.", viewerPromise: "Understand the real ownership cost.",
    narrativeProgression: "Move from context through the recurring-cost mechanism.", payoff: "Ownership was an ongoing economic system.",
    pacingStrategy: "Build steadily toward synthesis.", experimentAlignment: { experimentType: "Packaging",
      variableBeingTested: "Hidden-cost framing", controlStrategy: "Keep storytelling comparable",
      howOutlineImplementsExperiment: "Preserves the packaging premise.", risksToExperimentIntegrity: [] },
    totalEstimatedSeconds: 240, transitionsRequireReview: false, isStale: false, warnings: [],
    sections: [
      { ...baseSection, id: "section-1", sequence: 1 },
      { ...baseSection, id: "section-2", sequence: 2, heading: "Conflicting estimates", purpose: "Conflict",
        conflicts: [{ id: "conflict-1", claimId: claim.id, explanation: "Sources disagree on one annual figure.", isResolved: false }] },
      { ...baseSection, id: "section-3", sequence: 3, heading: "What remains unknown", purpose: "Counterpoint",
        researchGaps: [{ index: 0, description: "No universal annual figure was found.", claimIds: [claim.id] }] },
      { ...baseSection, id: "section-4", sequence: 4, heading: "The payoff", purpose: "Conclusion" },
    ],
    createdAt: "2026-09-16T00:00:00Z", updatedAt: "2026-09-16T00:00:00Z", approvedAt: null,
  }
}

function localizedContent(outline: VideoOutline) {
  return {
    canonicalContentFingerprint: "a".repeat(64), coreQuestion: `VI ${outline.coreQuestion}`,
    coreTension: `VI ${outline.coreTension}`, openingHookConcept: `VI ${outline.openingHookConcept}`,
    viewerPromise: `VI ${outline.viewerPromise}`, narrativeProgression: `VI ${outline.narrativeProgression}`,
    payoff: `VI ${outline.payoff}`, pacingStrategy: `VI ${outline.pacingStrategy}`,
    howOutlineImplementsExperiment: `VI ${outline.experimentAlignment.howOutlineImplementsExperiment}`,
    risksToExperimentIntegrity: [], warnings: [],
    sections: outline.sections.map(section => ({ sectionId: section.id, heading: `VI ${section.heading}`,
      objective: `VI ${section.objective}`, summary: `VI ${section.summary}`, viewerQuestion: section.viewerQuestion,
      transitionIntent: section.transitionIntent })),
  }
}

function json(value: unknown, statusCode = 200): Promise<Response> {
  return Promise.resolve(new Response(JSON.stringify(value), { status: statusCode, headers: { "Content-Type": "application/json" } }))
}

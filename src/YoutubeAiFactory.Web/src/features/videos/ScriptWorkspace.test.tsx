import { cleanup, fireEvent, render, screen } from "@testing-library/react"
import { afterEach, expect, test, vi } from "vitest"
import type { ScriptStatus, VideoScript } from "../../services/api"
import { ScriptWorkspace } from "./ScriptWorkspace"

afterEach(() => {
  cleanup()
  vi.restoreAllMocks()
  vi.unstubAllGlobals()
})

test("queues Script generation from an approved Outline", async () => {
  let generated = false
  const fetchMock = vi.fn((input: RequestInfo | URL, init?: RequestInit) => {
    const url = String(input)
    const method = init?.method ?? "GET"
    if (url.endsWith("/scripts") && method === "GET") return json([])
    if (url.endsWith("/script:generate") && method === "POST") {
      generated = true
      return json({ jobId: "job-1", status: "Queued", existing: false }, 202)
    }
    if (url.endsWith("/script/latest")) return json(generated
      ? status({ activeJob: { id: "job-1", status: "Queued", operation: "Generate", failureReason: null } })
      : status({ canGenerate: true }))
    throw new Error(`Unexpected request: ${method} ${url}`)
  })
  vi.stubGlobal("fetch", fetchMock)

  render(<ScriptWorkspace projectId="project-1" videoProjectId="video-1" revisionKey="r1" onWorkflowStatusChange={vi.fn()} />)

  fireEvent.click(await screen.findByRole("button", { name: "Generate Script" }))
  expect(await screen.findByText(/Generating Script from the approved Outline.*Queued/)).toBeVisible()
  expect(fetchMock).toHaveBeenCalledWith(expect.stringContaining("/script:generate"),
    expect.objectContaining({ method: "POST" }))
})

test("renders narration with claim and source traceability and approves a grounded Script", async () => {
  const script = readyScript()
  const approved = { ...script, status: "Approved", approvedAt: "2026-09-18T00:02:00Z" }
  const onStatus = vi.fn()
  vi.stubGlobal("fetch", vi.fn((input: RequestInfo | URL, init?: RequestInit) => {
    const url = String(input)
    const method = init?.method ?? "GET"
    if (url.endsWith("/script/latest")) return json(status({ latestScript: script, canGenerate: true }))
    if (url.endsWith("/scripts") && method === "GET") return json([history(script)])
    if (url.endsWith(`/${script.id}:approve`) && method === "POST") return json(approved)
    throw new Error(`Unexpected request: ${method} ${url}`)
  }))

  render(<ScriptWorkspace projectId="project-1" videoProjectId="video-1" revisionKey="r1" onWorkflowStatusChange={onStatus} />)

  expect(await screen.findByText("Castle ownership required recurring staffing obligations.")).toBeVisible()
  fireEvent.click(screen.getByText("Supporting Claims (1)"))
  expect(screen.getByText((_, element) => element?.tagName === "P" &&
    element.textContent?.includes("Ownership required continuing staffing obligations.") === true)).toBeVisible()
  fireEvent.click(screen.getByText("Castle accounts"))
  expect(screen.getByText("Accounts list recurring household roles.")).toBeVisible()
  fireEvent.click(screen.getByRole("button", { name: "Approve Script" }))

  expect(await screen.findByText(/Script Approved/)).toBeVisible()
  expect(screen.queryByRole("button", { name: "Edit narration" })).not.toBeInTheDocument()
  expect(onStatus).toHaveBeenCalledWith("ScriptApproved")
})

test("an edit changes grounding to Pending and exposes explicit revalidation", async () => {
  const script = readyScript()
  const pending = { ...script, groundingStatus: "Pending", totalWordCount: 9,
    sections: script.sections.map(section => ({ ...section, blocks: section.blocks.map(block => ({ ...block,
      text: "Castle ownership required recurring staffing obligations, with qualifications.", wordCount: 9 })) })) }
  vi.stubGlobal("fetch", vi.fn((input: RequestInfo | URL, init?: RequestInit) => {
    const url = String(input)
    const method = init?.method ?? "GET"
    if (url.endsWith("/script/latest")) return json(status({ latestScript: script, canGenerate: true }))
    if (url.endsWith("/scripts") && method === "GET") return json([history(script)])
    if (url.endsWith(`/${script.id}`) && method === "PATCH") return json(pending)
    throw new Error(`Unexpected request: ${method} ${url}`)
  }))

  render(<ScriptWorkspace projectId="project-1" videoProjectId="video-1" revisionKey="r1" onWorkflowStatusChange={vi.fn()} />)
  fireEvent.click(await screen.findByRole("button", { name: "Edit narration" }))
  fireEvent.change(screen.getByRole("textbox", { name: "Narration block 1" }),
    { target: { value: "Castle ownership required recurring staffing obligations, with qualifications." } })
  fireEvent.click(screen.getByRole("button", { name: "Save narration edits" }))

  expect(await screen.findByText("Pending")).toBeVisible()
  expect(screen.getByRole("button", { name: "Validate Script" })).toBeVisible()
  expect(screen.queryByRole("button", { name: "Approve Script" })).not.toBeInTheDocument()
})

function status(overrides: Partial<ScriptStatus>): ScriptStatus {
  return { latestScript: null, activeJob: null, latestJob: null, canGenerate: false,
    blockReason: "Approve an Outline before generating a Script.", ...overrides }
}

function history(script: VideoScript) {
  return { id: script.id, version: script.version, status: script.status,
    groundingStatus: script.groundingStatus, videoOutlineVersion: script.videoOutlineVersion,
    researchReportVersion: script.researchReportVersion, isStale: script.isStale,
    createdAt: script.createdAt, approvedAt: script.approvedAt }
}

function readyScript(): VideoScript {
  const source = { id: "source-1", url: "https://archive.example/castle",
    canonicalUrl: "https://archive.example/castle", domain: "archive.example", title: "Castle accounts",
    publisher: "Archive", publishedAt: null, retrievedAt: "2026-09-18T00:00:00Z",
    category: "Primary", fetchStatus: "Fetched" }
  const claim = { id: "claim-1", statement: "Ownership required continuing staffing obligations.",
    type: "Factual", supportStatus: "Supported", confidence: 92, isCritical: true,
    evidence: [{ id: "evidence-1", fact: "Staffing was recurring.",
      supportingExcerpt: "Accounts list recurring household roles.", sourceLocator: "ledger 12",
      type: "Fact", confidence: 92, source }] }
  return {
    id: "script-1", projectId: "project-1", videoProjectId: "video-1", videoOutlineId: "outline-1",
    videoOutlineVersion: 1, researchReportId: "report-1", researchReportVersion: 1, version: 1,
    status: "Ready", groundingStatus: "Passed", scriptEngineVersion: "script-engine:v1",
    promptKey: "script-generation", promptVersion: 1, provider: "fake", model: "premium-model",
    contentLanguage: "English", totalWordCount: 7, estimatedDurationSeconds: 3, isStale: false,
    warnings: [], groundingIssues: [], sections: [{ id: "script-section-1", outlineSectionId: "section-1",
      sequence: 1, heading: "The operating burden", wordCount: 7, estimatedDurationSeconds: 3,
      blocks: [{ id: "block-1", sequence: 1, type: "FactualNarration",
        text: "Castle ownership required recurring staffing obligations.", wordCount: 7,
        claims: [claim], conflicts: [] }] }], createdAt: "2026-09-18T00:00:00Z",
    updatedAt: "2026-09-18T00:00:00Z", approvedAt: null,
  }
}

function json(value: unknown, statusCode = 200): Promise<Response> {
  return Promise.resolve(new Response(JSON.stringify(value),
    { status: statusCode, headers: { "Content-Type": "application/json" } }))
}

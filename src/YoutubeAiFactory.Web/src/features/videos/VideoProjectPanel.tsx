import { type ReactNode, useCallback, useEffect, useMemo, useState } from "react"
import { api, type ResearchJob, type ResearchLocalizationStatus, type ResearchReport, type ResearchStatus, type VideoProject, type VideoProjectListItem } from "../../services/api"
import { messageFrom } from "../../lib/errors"
import { AnalysisLanguageToggle, type AnalysisLocale } from "../localization/AnalysisLanguageToggle"
import { OutlineWorkspace } from "./OutlineWorkspace"

export function VideoProjectPanel({ projectId, onOpen }: { projectId: string; onOpen: (videoProject: VideoProject) => void }) {
  const [items, setItems] = useState<VideoProjectListItem[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const load = useCallback(async () => {
    try { setItems(await api.listVideoProjects(projectId)); setError(null) }
    catch (requestError) { setError(messageFrom(requestError)) }
    finally { setLoading(false) }
  }, [projectId])
  useEffect(() => { void Promise.resolve().then(load) }, [load])
  async function open(item: VideoProjectListItem) {
    try { onOpen(await api.getVideoProject(projectId, item.id)) }
    catch (requestError) { setError(messageFrom(requestError)) }
  }
  return <section className="analysis-section panel">
    <div className="section-heading"><div><span className="eyebrow">Execution</span><h2>Video Projects</h2><p className="section-copy">Start production deliberately from an approved pilot experiment.</p></div></div>
    {loading ? <p className="empty-state">Loading video projects...</p> : error ? <p className="error-copy" role="alert">{error}</p> : items.length === 0 ? <p className="empty-state">No videos have entered production yet. Create a Video Project from an approved Pilot.</p> : <div className="analysis-report">{items.map((item) => <article className="pilot-card" key={item.id}><div className="section-heading"><div><strong>{item.workingTitle}</strong><p>Slot {item.pilotSequence} · {item.experimentType} experiment · {item.status}</p></div><button className="quiet-button" type="button" onClick={() => void open(item)}>Open Video Project</button></div></article>)}</div>}
  </section>
}

export function VideoProjectWorkspace({ project, onBack }: { project: VideoProject; onBack: () => void }) {
  const [workingTitle, setWorkingTitle] = useState(project.workingTitle)
  const [notes, setNotes] = useState(project.executionNotes ?? "")
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [current, setCurrent] = useState(project)
  const [research, setResearch] = useState<ResearchStatus | null>(null)
  const [researchError, setResearchError] = useState<string | null>(null)
  const [researchBusy, setResearchBusy] = useState(false)
  const updateWorkflowStatus = useCallback((status: string) => setCurrent(value => value.status === status ? value : { ...value, status }), [])
  const loadResearch = useCallback(async () => {
    try { setResearch(await api.getResearchStatus(project.projectId, project.id)); setResearchError(null) }
    catch (requestError) { setResearchError(messageFrom(requestError)) }
  }, [project.id, project.projectId])
  useEffect(() => { void Promise.resolve().then(loadResearch) }, [loadResearch])
  useEffect(() => {
    if (!research?.activeJob) return
    const timer = window.setTimeout(() => void loadResearch(), 2000)
    return () => window.clearTimeout(timer)
  }, [loadResearch, research?.activeJob])
  async function save() {
    setBusy(true)
    try { setCurrent(await api.updateVideoProject(current.projectId, current.id, workingTitle, notes || null)); setError(null) }
    catch (requestError) { setError(messageFrom(requestError)) }
    finally { setBusy(false) }
  }
  async function runResearch() {
    setResearchBusy(true)
    try { await api.runResearch(current.projectId, current.id); await loadResearch() }
    catch (requestError) { setResearchError(messageFrom(requestError)) }
    finally { setResearchBusy(false) }
  }
  const refreshFailure = research?.latestReport && research.latestJob?.status === "Failed" ? research.latestJob : null
  return <section className="analysis-section panel">
    <div className="section-heading"><div><span className="eyebrow">Video Project</span><h2>{current.workingTitle}</h2><p className="section-copy">Status: {current.status}</p></div><button className="quiet-button" type="button" onClick={onBack}>Back to Videos</button></div>
    {error ? <p className="error-copy" role="alert">{error}</p> : null}
    {current.sourceRequiresReview ? <p className="stale-note">Source strategy has changed since this Video Project was created. Its execution history remains intact.</p> : null}
    {current.sourceWarnings.map((warning) => <p className="stale-note" key={warning}>{warning}</p>)}
    <div className="confidence-grid"><div><span>Planning</span><strong>Complete</strong></div><div><span>Research</span><strong>{research?.activeJob ? research.activeJob.status : refreshFailure ? "Refresh failed" : research?.latestReport ? "Complete" : "Not started"}</strong></div><div><span>Outline</span><strong>{current.status.startsWith("Outline") ? current.status.replace("Outline", "") || "Generating" : "Locked"}</strong></div><div><span>Script</span><strong>Locked</strong></div><div><span>Production</span><strong>Locked</strong></div></div>
    <section className="analysis-block"><h3>Source context</h3><p><strong>Opportunity:</strong> {current.opportunityName}</p><p><strong>Idea score:</strong> {current.ideaScore}</p><p><strong>Pilot:</strong> Version {current.pilotVersion}, slot {current.pilotSequence} · {current.experimentType}</p><p><strong>Hypothesis:</strong> {current.pilotHypothesis}</p><p><strong>Variable:</strong> {current.variableBeingTested}</p><p><strong>Primary metric:</strong> {current.primaryMetric}</p><p><strong>Success signal:</strong> {current.successSignal}</p></section>
    <section className="analysis-block"><h3>Execution brief</h3><p><strong>Topic:</strong> {current.topic}</p><p><strong>Angle:</strong> {current.angle}</p><p><strong>Format:</strong> {current.contentFormat}</p><p><strong>Audience:</strong> {current.targetAudience}</p><p><strong>Viewer promise:</strong> {current.viewerPromise}</p><p><strong>Hook:</strong> {current.hookConcept}</p><p><strong>Thumbnail concept:</strong> {current.thumbnailConcept}</p></section>
    <section className="analysis-block"><h3>Working details</h3><label>Working title<input value={workingTitle} maxLength={300} onChange={(event) => setWorkingTitle(event.target.value)} /></label><label>Execution notes<textarea value={notes} maxLength={10000} onChange={(event) => setNotes(event.target.value)} /></label><button className="primary-button" type="button" disabled={busy} onClick={() => void save()}>{busy ? "Saving..." : "Save working details"}</button></section>
    <section className="analysis-block"><h3>Research</h3>{researchError ? <p className="error-copy" role="alert">{researchError}</p> : null}{research?.activeJob ? <><p>Research is running: {research.activeJob.status}. It plans questions, retrieves public sources, extracts evidence, checks conflicts, and builds a report.</p><button className="quiet-button" type="button" onClick={() => void loadResearch()}>Refresh research status</button></> : research?.latestReport ? <ResearchReportView key={research.latestReport.id} projectId={current.projectId} videoProjectId={current.id} report={research.latestReport} refreshFailure={refreshFailure} onRerun={runResearch} busy={researchBusy} /> : <><p className="section-copy">No research has been created for this video. Research gathers external sources, extracts evidence, and builds a fact-backed report.</p>{research?.latestJob?.failureReason ? <p className="error-copy" role="alert">Research failed: {research.latestJob.failureReason}</p> : null}<button className="primary-button" type="button" disabled={researchBusy} onClick={() => void runResearch()}>{researchBusy ? "Queuing research..." : research?.latestJob?.status === "Failed" ? "Retry Research" : "Run Research"}</button></>}</section>
    <OutlineWorkspace projectId={current.projectId} videoProjectId={current.id} revisionKey={`${current.updatedAt}:${research?.latestReport?.id ?? ""}:${research?.activeJob?.id ?? ""}`} onWorkflowStatusChange={updateWorkflowStatus} />
  </section>
}

function ResearchReportView({ projectId, videoProjectId, report, refreshFailure, onRerun, busy }: { projectId: string; videoProjectId: string; report: ResearchReport; refreshFailure: ResearchJob | null; onRerun: () => Promise<void>; busy: boolean }) {
  const sources = useMemo(() => new Map(report.sources.map((source) => [source.id, source])), [report.sources])
  const evidence = useMemo(() => new Map(report.evidence.map((item) => [item.id, item])), [report.evidence])
  const [locale, setLocale] = useState<AnalysisLocale>("en")
  const [localized, setLocalized] = useState<ResearchLocalizationStatus | null>(null)
  const [localizationBusy, setLocalizationBusy] = useState(false)
  const [localizationError, setLocalizationError] = useState<string | null>(null)
  const presentation = locale === "vi" ? localized?.content : null
  const localizedFindings = useMemo(() => new Map(presentation?.keyFindings.map((finding) => [finding.index, finding])), [presentation])
  const localizedGaps = useMemo(() => new Map(presentation?.gaps.map((gap) => [gap.index, gap])), [presentation])
  const warnings = presentation?.warnings.map((warning) => warning.text) ?? report.synthesis.warnings
  const limitations = presentation?.limitations.map((limitation) => limitation.text) ?? report.synthesis.limitations
  const loadLocalization = useCallback(async () => {
    const status = await api.getResearchLocalization(projectId, videoProjectId, report.id, "vi")
    setLocalized(status)
    if (status.content) { setLocalizationBusy(false); setLocalizationError(null); return }
    setLocalizationBusy(Boolean(status.activeJob))
    if (status.latestJob?.status === "Failed") setLocalizationError(status.latestJob.failureReason ?? "Vietnamese research translation failed. English remains available.")
  }, [projectId, report.id, videoProjectId])
  useEffect(() => {
    if (locale !== "vi" || !localizationBusy) return undefined
    const timer = window.setInterval(() => void loadLocalization(), 2000)
    return () => window.clearInterval(timer)
  }, [loadLocalization, locale, localizationBusy])
  async function selectLocale(nextLocale: AnalysisLocale) {
    setLocale(nextLocale)
    if (nextLocale === "en") return
    setLocalizationBusy(true)
    setLocalizationError(null)
    try {
      const status = await api.getResearchLocalization(projectId, videoProjectId, report.id, "vi")
      if (status.content) { setLocalized(status); setLocalizationBusy(false); return }
      await api.requestResearchLocalization(projectId, videoProjectId, report.id, "vi")
      await loadLocalization()
    } catch (requestError) { setLocalized(null); setLocalizationBusy(false); setLocalizationError(messageFrom(requestError)) }
  }
  return <div className="analysis-report"><div className="section-heading"><div><p className="eyebrow">Research report v{report.version}</p><p className="section-copy">{report.isStale ? "Research may be outdated because the video research brief changed." : "Research reflects the current video brief."}</p></div><div><AnalysisLanguageToggle locale={locale} onChange={(next) => void selectLocale(next)} disabled={localizationBusy} /><button className="quiet-button" type="button" disabled={busy} onClick={() => void onRerun()}>{busy ? "Queuing..." : "Run refreshed research"}</button></div></div>
    {refreshFailure ? <p className="error-copy" role="alert">The refreshed research failed: {refreshFailure.failureReason ?? "No failure reason was provided."} Report v{report.version} is the previous successful report and remains available.</p> : null}
    {locale === "vi" && !presentation && localizationBusy ? <p className="section-copy">Vietnamese reading aid is being prepared. Canonical English evidence remains available.</p> : null}{locale === "vi" && localizationError ? <p className="error-copy" role="alert">{localizationError}</p> : null}<p>{presentation?.executiveSummary ?? report.synthesis.executiveSummary}</p>
    {warnings.map((warning, index) => <p className="stale-note" key={`${warning}-${index}`}>{warning}</p>)}
    <div className="confidence-grid"><div><span>Confidence</span><strong>{report.confidence.level}</strong></div><div><span>Sources</span><strong>{report.metrics.relevantSourceCount}</strong></div><div><span>Evidence</span><strong>{report.metrics.evidenceCount}</strong></div><div><span>Claims</span><strong>{report.metrics.claimCount}</strong></div><div><span>Conflicts</span><strong>{report.metrics.conflictCount}</strong></div></div>
    <ResearchSection title="Key findings">{report.synthesis.keyFindings.map((finding, index) => <article className="pilot-card" key={`${finding.summary}-${index}`}><strong>{localizedFindings.get(index)?.category ?? finding.category}</strong><p>{localizedFindings.get(index)?.summary ?? finding.summary}</p><p className="section-copy">Claims: {finding.claimIds.length} · Evidence: {finding.evidenceIds.length}</p></article>)}</ResearchSection>
    <ResearchSection title="Claims & evidence">{report.claims.map((claim) => <article className="pilot-card" key={claim.id}><div className="section-heading"><strong>{claim.supportStatus}</strong>{claim.isCritical ? <span className="eyebrow">Critical</span> : null}</div><p>{claim.statement}</p>{claim.evidence.map((link) => { const item = evidence.get(link.evidenceId); const source = item ? sources.get(item.sourceId) : undefined; return <details key={`${claim.id}-${link.evidenceId}`}><summary>{link.stance}: {source?.title ?? source?.domain ?? "Unavailable source"}</summary>{item ? <><p>{item.supportingExcerpt}</p><p className="section-copy">{item.type} · {item.sourceLocator}</p>{source ? <SourceLink source={source.url} label="Open source" /> : null}</> : null}</details> })}</article>)}</ResearchSection>
    {report.conflicts.length > 0 ? <ResearchSection title="Conflicting evidence">{report.conflicts.map((conflict) => <article className="pilot-card" key={conflict.id}><p>{conflict.explanation}</p><p className="section-copy">{conflict.isResolved ? "Contextualized; retain the source distinction." : "Unresolved; do not present one definitive value."}</p></article>)}</ResearchSection> : null}
    {report.synthesis.gaps.length > 0 ? <ResearchSection title="Research gaps">{report.synthesis.gaps.map((gap, index) => <p className="stale-note" key={`${gap.description}-${index}`}>{localizedGaps.get(index)?.description ?? gap.description}</p>)}</ResearchSection> : null}
    {report.synthesis.limitations.length > 0 ? <ResearchSection title="Limitations">{limitations.map((limitation, index) => <p className="section-copy" key={`${limitation}-${index}`}>{limitation}</p>)}</ResearchSection> : null}
    <ResearchSection title="Sources">{report.sources.map((source) => <article className="pilot-card" key={source.id}><strong>{source.title ?? source.domain}</strong><p>{source.publisher ?? source.domain} · {source.category} · {source.fetchStatus}</p><p className="section-copy">Retrieved {new Date(source.retrievedAt).toLocaleDateString()}</p><SourceLink source={source.url} label="Open external source" /></article>)}</ResearchSection>
  </div>
}

function ResearchSection({ title, children }: { title: string; children: ReactNode }) { return <section className="analysis-block"><h3>{title}</h3>{children}</section> }

function SourceLink({ source, label }: { source: string; label: string }) {
  if (!/^https?:\/\//i.test(source)) return null
  return <a className="quiet-button" href={source} target="_blank" rel="noreferrer noopener">{label}</a>
}

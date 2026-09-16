import { useCallback, useEffect, useMemo, useState } from "react"
import { api, type LocalizedVideoOutlineContent, type OutlineLocalizationStatus, type OutlineSection, type OutlineStatus, type VideoOutline } from "../../services/api"
import { messageFrom } from "../../lib/errors"
import { AnalysisLanguageToggle, type AnalysisLocale } from "../localization/AnalysisLanguageToggle"

type OutlineDraftSection = Pick<OutlineSection, "id" | "heading" | "objective" | "summary" | "viewerQuestion" | "transitionIntent" | "estimatedSeconds">

export function OutlineWorkspace({ projectId, videoProjectId, revisionKey, onWorkflowStatusChange }: {
  projectId: string
  videoProjectId: string
  revisionKey: string
  onWorkflowStatusChange: (status: string) => void
}) {
  const [status, setStatus] = useState<OutlineStatus | null>(null)
  const [viewed, setViewed] = useState<VideoOutline | null>(null)
  const [history, setHistory] = useState<{ id: string; version: number; status: string }[]>([])
  const [loading, setLoading] = useState(true)
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [editing, setEditing] = useState(false)
  const [draftSections, setDraftSections] = useState<OutlineDraftSection[]>([])

  const load = useCallback(async () => {
    if (revisionKey.length === 0) return
    try {
      const [nextStatus, nextHistory] = await Promise.all([
        api.getOutlineStatus(projectId, videoProjectId),
        api.listOutlines(projectId, videoProjectId),
      ])
      setStatus(nextStatus)
      setHistory(nextHistory)
      setViewed(nextStatus.latestOutline)
      setError(null)
      if (nextStatus.activeJob) onWorkflowStatusChange("OutlineGenerating")
      else if (nextStatus.latestOutline?.status === "Approved") onWorkflowStatusChange("OutlineApproved")
      else if (nextStatus.latestOutline) onWorkflowStatusChange("OutlineReady")
    } catch (requestError) {
      setError(messageFrom(requestError))
    } finally {
      setLoading(false)
    }
  }, [onWorkflowStatusChange, projectId, revisionKey, videoProjectId])

  useEffect(() => { void Promise.resolve().then(load) }, [load])
  useEffect(() => {
    if (!status?.activeJob) return undefined
    const timer = window.setTimeout(() => void load(), 2000)
    return () => window.clearTimeout(timer)
  }, [load, status?.activeJob])

  async function generate() {
    setBusy(true)
    try { await api.generateOutline(projectId, videoProjectId); await load() }
    catch (requestError) { setError(messageFrom(requestError)) }
    finally { setBusy(false) }
  }

  async function selectVersion(outlineId: string) {
    if (outlineId === status?.latestOutline?.id) { setViewed(status.latestOutline); return }
    setBusy(true)
    try { setViewed(await api.getOutline(projectId, videoProjectId, outlineId)); setError(null) }
    catch (requestError) { setError(messageFrom(requestError)) }
    finally { setBusy(false) }
  }

  function beginEditing(outline: VideoOutline) {
    setDraftSections(outline.sections.map(section => ({
      id: section.id, heading: section.heading, objective: section.objective, summary: section.summary,
      viewerQuestion: section.viewerQuestion, transitionIntent: section.transitionIntent,
      estimatedSeconds: section.estimatedSeconds,
    })))
    setEditing(true)
  }

  function updateDraft(sectionId: string, field: keyof Omit<OutlineDraftSection, "id">, value: string | number | null) {
    setDraftSections(current => current.map(section => section.id === sectionId ? { ...section, [field]: value } : section))
  }

  async function saveEdits(outline: VideoOutline) {
    setBusy(true)
    try {
      const updated = await api.updateOutline(projectId, videoProjectId, outline.id,
        draftSections.map(section => ({ sectionId: section.id, heading: section.heading, objective: section.objective,
          summary: section.summary, viewerQuestion: section.viewerQuestion, transitionIntent: section.transitionIntent,
          estimatedSeconds: section.estimatedSeconds })))
      applyUpdatedOutline(updated)
      setEditing(false)
      setError(null)
    } catch (requestError) { setError(messageFrom(requestError)) }
    finally { setBusy(false) }
  }

  async function move(outline: VideoOutline, sectionId: string, direction: -1 | 1) {
    const ordered = outline.sections.toSorted((left, right) => left.sequence - right.sequence).map(section => section.id)
    const index = ordered.indexOf(sectionId)
    const target = index + direction
    if (index < 0 || target < 0 || target >= ordered.length) return
    ;[ordered[index], ordered[target]] = [ordered[target], ordered[index]]
    setBusy(true)
    try { applyUpdatedOutline(await api.reorderOutline(projectId, videoProjectId, outline.id, ordered)); setError(null) }
    catch (requestError) { setError(messageFrom(requestError)) }
    finally { setBusy(false) }
  }

  async function approve(outline: VideoOutline) {
    setBusy(true)
    try {
      const approved = await api.approveOutline(projectId, videoProjectId, outline.id)
      applyUpdatedOutline(approved)
      onWorkflowStatusChange("OutlineApproved")
      setError(null)
    } catch (requestError) { setError(messageFrom(requestError)) }
    finally { setBusy(false) }
  }

  function applyUpdatedOutline(outline: VideoOutline) {
    setViewed(outline)
    setStatus(current => current ? { ...current, latestOutline: outline } : current)
    setHistory(current => current.map(item => item.id === outline.id ? { ...item, status: outline.status } : item))
  }

  if (loading) return <section className="analysis-block"><h3>Outline</h3><p className="section-copy">Loading outline workspace...</p></section>
  if (error && !status) return <section className="analysis-block"><h3>Outline</h3><p className="error-copy" role="alert">{error}</p></section>

  return <section className="analysis-block outline-workspace">
    <div className="section-heading"><div><h3>Outline</h3><p className="section-copy">Narrative planning grounded only in the current ResearchReport.</p></div>
      {history.length > 0 ? <label className="idea-sort">View version<select value={viewed?.id ?? ""} disabled={busy || editing} onChange={event => void selectVersion(event.target.value)}>{history.map(item => <option key={item.id} value={item.id}>v{item.version} · {item.status}</option>)}</select></label> : null}
    </div>
    {error ? <p className="error-copy" role="alert">{error}</p> : null}
    {status?.activeJob ? <div className="analysis-state"><p>Generating outline... Status: {status.activeJob.status}. The worker is designing the narrative and validating evidence references.</p><button className="quiet-button" type="button" onClick={() => void load()}>Refresh outline status</button></div> : null}
    {!status?.activeJob && !viewed ? <div className="analysis-state"><p>{status?.blockReason ?? "Research is ready. Generate a structured narrative outline grounded in the current evidence."}</p>{status?.canGenerate ? <button className="primary-button" type="button" disabled={busy} onClick={() => void generate()}>{busy ? "Queuing outline..." : "Generate Outline"}</button> : null}</div> : null}
    {!status?.activeJob && status?.latestJob?.status === "Failed" ? <p className="error-copy" role="alert">Outline generation failed: {status.latestJob.failureReason ?? "No failure reason was provided."} {viewed ? "The previous validated outline remains available." : ""}</p> : null}
    {viewed ? <OutlineView key={`${viewed.id}-${viewed.updatedAt}`} outline={viewed} isLatest={viewed.id === status?.latestOutline?.id} canGenerate={Boolean(status?.canGenerate)} busy={busy} editing={editing} drafts={draftSections}
      onGenerate={generate} onBeginEdit={beginEditing} onCancelEdit={() => setEditing(false)} onDraftChange={updateDraft}
      onSave={saveEdits} onMove={move} onApprove={approve} projectId={projectId} videoProjectId={videoProjectId} /> : null}
  </section>
}

function OutlineView({ outline, isLatest, canGenerate, busy, editing, drafts, onGenerate, onBeginEdit,
  onCancelEdit, onDraftChange, onSave, onMove, onApprove, projectId, videoProjectId }: {
  outline: VideoOutline; isLatest: boolean; canGenerate: boolean; busy: boolean; editing: boolean
  drafts: OutlineDraftSection[]; onGenerate: () => Promise<void>; onBeginEdit: (outline: VideoOutline) => void
  onCancelEdit: () => void; onDraftChange: (sectionId: string, field: keyof Omit<OutlineDraftSection, "id">, value: string | number | null) => void
  onSave: (outline: VideoOutline) => Promise<void>; onMove: (outline: VideoOutline, sectionId: string, direction: -1 | 1) => Promise<void>
  onApprove: (outline: VideoOutline) => Promise<void>; projectId: string; videoProjectId: string
}) {
  const [locale, setLocale] = useState<AnalysisLocale>("en")
  const [localized, setLocalized] = useState<OutlineLocalizationStatus | null>(null)
  const [localizationBusy, setLocalizationBusy] = useState(false)
  const [localizationError, setLocalizationError] = useState<string | null>(null)
  const presentation = locale === "vi" ? localized?.content : null
  const sectionsById = useMemo(() => new Map(presentation?.sections.map(section => [section.sectionId, section])), [presentation])
  const risks = presentation?.risksToExperimentIntegrity.map(item => item.text) ?? outline.experimentAlignment.risksToExperimentIntegrity
  const warnings = presentation?.warnings.map(item => item.text) ?? outline.warnings
  const draftsById = useMemo(() => new Map(drafts.map(section => [section.id, section])), [drafts])

  const loadLocalization = useCallback(async () => {
    const next = await api.getOutlineLocalization(projectId, videoProjectId, outline.id, "vi")
    setLocalized(next)
    if (next.content) { setLocalizationBusy(false); setLocalizationError(null); return }
    setLocalizationBusy(Boolean(next.activeJob))
    if (next.latestJob?.status === "Failed") setLocalizationError(next.latestJob.failureReason ?? "Vietnamese outline translation failed. English remains available.")
  }, [outline.id, projectId, videoProjectId])

  useEffect(() => {
    if (locale !== "vi" || !localizationBusy) return undefined
    const timer = window.setTimeout(() => void loadLocalization(), 2000)
    return () => window.clearTimeout(timer)
  }, [loadLocalization, locale, localizationBusy])

  async function selectLocale(nextLocale: AnalysisLocale) {
    setLocale(nextLocale)
    if (nextLocale === "en") return
    setLocalizationBusy(true)
    setLocalizationError(null)
    try {
      const existing = await api.getOutlineLocalization(projectId, videoProjectId, outline.id, "vi")
      if (existing.content) { setLocalized(existing); setLocalizationBusy(false); return }
      await api.requestOutlineLocalization(projectId, videoProjectId, outline.id, "vi")
      await loadLocalization()
    } catch (requestError) { setLocalized(null); setLocalizationBusy(false); setLocalizationError(messageFrom(requestError)) }
  }

  const ready = outline.status === "Ready" && isLatest && canGenerate
  return <div className="analysis-report">
    <div className="section-heading"><div><p className="eyebrow">Outline v{outline.version} · {outline.status}</p><p className="section-copy">Research report v{outline.researchReportVersion} · {outline.isStale ? "Outdated" : "Current"}</p></div><div className="section-actions"><AnalysisLanguageToggle locale={locale} onChange={next => void selectLocale(next)} disabled={localizationBusy || editing} />{canGenerate && isLatest ? <button className="quiet-button" type="button" disabled={busy || editing} onClick={() => void onGenerate()}>Generate new version</button> : null}</div></div>
    {locale === "vi" && !presentation && localizationBusy ? <p className="section-copy">Vietnamese reading aid is being prepared. Canonical English remains available.</p> : null}
    {localizationError ? <p className="error-copy" role="alert">{localizationError}</p> : null}
    {outline.isStale ? <p className="stale-note">This outline is outdated because its VideoProject or current ResearchReport changed. It cannot be approved.</p> : null}
    {outline.transitionsRequireReview ? <p className="stale-note">Section order changed. Review and save transition intent before approval.</p> : null}
    {warnings.map((warning, index) => <p className="stale-note" key={`${index}-${warning}`}>{warning}</p>)}
    <section className="outline-strategy"><h4>Narrative Strategy</h4><dl><div><dt>Structure</dt><dd>{outline.structureType}</dd></div><div><dt>Core question</dt><dd>{presentation?.coreQuestion ?? outline.coreQuestion}</dd></div><div><dt>Core tension</dt><dd>{presentation?.coreTension ?? outline.coreTension}</dd></div><div><dt>Opening hook</dt><dd>{presentation?.openingHookConcept ?? outline.openingHookConcept}</dd></div><div><dt>Viewer promise</dt><dd>{presentation?.viewerPromise ?? outline.viewerPromise}</dd></div><div><dt>Progression</dt><dd>{presentation?.narrativeProgression ?? outline.narrativeProgression}</dd></div><div><dt>Payoff</dt><dd>{presentation?.payoff ?? outline.payoff}</dd></div><div><dt>Pacing</dt><dd>{presentation?.pacingStrategy ?? outline.pacingStrategy}</dd></div></dl></section>
    <section className="outline-strategy"><h4>Pilot Experiment</h4><p><strong>Type:</strong> {outline.experimentAlignment.experimentType}</p><p><strong>Variable:</strong> {outline.experimentAlignment.variableBeingTested}</p><p><strong>Control:</strong> {outline.experimentAlignment.controlStrategy}</p><p><strong>Outline alignment:</strong> {presentation?.howOutlineImplementsExperiment ?? outline.experimentAlignment.howOutlineImplementsExperiment}</p>{risks.length > 0 ? <ul>{risks.map((risk, index) => <li key={`${index}-${risk}`}>{risk}</li>)}</ul> : null}</section>
    <section><div className="section-heading"><h4>Ordered Sections</h4>{ready && !editing ? <button className="quiet-button" type="button" disabled={busy} onClick={() => onBeginEdit(outline)}>Edit sections</button> : null}</div>
      {outline.sections.toSorted((left, right) => left.sequence - right.sequence).map((section, index) => <OutlineSectionCard key={section.id} section={section} localized={sectionsById.get(section.id)} draft={draftsById.get(section.id)} editing={editing} canReorder={ready} busy={busy} first={index === 0} last={index === outline.sections.length - 1} onDraftChange={onDraftChange} onMove={(direction) => void onMove(outline, section.id, direction)} />)}
    </section>
    {ready && editing ? <div className="idea-actions"><button className="primary-button" type="button" disabled={busy} onClick={() => void onSave(outline)}>{busy ? "Saving..." : "Save section edits"}</button><button className="quiet-button" type="button" disabled={busy} onClick={onCancelEdit}>Cancel</button></div> : null}
    {ready && !editing ? <div className="idea-actions"><button className="primary-button" type="button" disabled={busy || outline.isStale || outline.transitionsRequireReview} onClick={() => void onApprove(outline)}>Approve Outline</button></div> : null}
    {outline.status === "Approved" ? <p className="idea-approval-summary">Outline Approved. This immutable version is the explicit Phase 10 source; no script was generated.</p> : null}
  </div>
}

function OutlineSectionCard({ section, localized, draft, editing, canReorder, busy, first, last, onDraftChange, onMove }: {
  section: OutlineSection; localized: LocalizedVideoOutlineContent["sections"][number] | undefined
  draft: OutlineDraftSection | undefined; editing: boolean; canReorder: boolean; busy: boolean; first: boolean; last: boolean
  onDraftChange: (sectionId: string, field: keyof Omit<OutlineDraftSection, "id">, value: string | number | null) => void
  onMove: (direction: -1 | 1) => void
}) {
  return <article className="outline-section-card"><div className="section-heading"><div><span className="eyebrow">Section {section.sequence} · {section.purpose}</span><h4>{localized?.heading ?? section.heading}</h4></div>{canReorder && !editing ? <div className="section-actions"><button className="quiet-button" type="button" disabled={busy || first} onClick={() => onMove(-1)}>Move up</button><button className="quiet-button" type="button" disabled={busy || last} onClick={() => onMove(1)}>Move down</button></div> : null}</div>
    {editing && draft ? <div className="outline-edit-grid"><label>Heading<input maxLength={500} value={draft.heading} onChange={event => onDraftChange(section.id, "heading", event.target.value)} /></label><label>Objective<textarea maxLength={1000} value={draft.objective} onChange={event => onDraftChange(section.id, "objective", event.target.value)} /></label><label>Summary<textarea maxLength={2000} value={draft.summary} onChange={event => onDraftChange(section.id, "summary", event.target.value)} /></label><label>Viewer question<textarea maxLength={1000} value={draft.viewerQuestion ?? ""} onChange={event => onDraftChange(section.id, "viewerQuestion", event.target.value || null)} /></label><label>Transition intent<textarea maxLength={1000} value={draft.transitionIntent ?? ""} onChange={event => onDraftChange(section.id, "transitionIntent", event.target.value || null)} /></label><label>Estimated seconds<input type="number" min={15} max={1800} value={draft.estimatedSeconds ?? ""} onChange={event => onDraftChange(section.id, "estimatedSeconds", event.target.value ? Number(event.target.value) : null)} /></label></div> : <><p><strong>Objective:</strong> {localized?.objective ?? section.objective}</p><p>{localized?.summary ?? section.summary}</p>{section.viewerQuestion ? <p><strong>Viewer question:</strong> {localized?.viewerQuestion ?? section.viewerQuestion}</p> : null}{section.transitionIntent ? <p><strong>Transition intent:</strong> {localized?.transitionIntent ?? section.transitionIntent}</p> : null}<p className="section-copy">{section.claims.length} claim(s){section.estimatedSeconds ? ` · about ${section.estimatedSeconds}s` : ""}</p></>}
    {section.conflicts.length > 0 ? section.conflicts.map(conflict => <p className="stale-note" key={conflict.id}><strong>Conflicted evidence:</strong> {conflict.explanation}</p>) : null}
    {section.researchGaps.length > 0 ? section.researchGaps.map(gap => <p className="stale-note" key={gap.index}><strong>Research gap:</strong> {gap.description}</p>) : null}
    {section.claims.length > 0 ? <details><summary>Inspect supporting claims ({section.claims.length})</summary>{section.claims.map(claim => <div className="outline-claim" key={`${claim.id}-${claim.usageRole}`}><p><strong>{claim.usageRole} · {claim.supportStatus}</strong></p><p>{claim.statement}</p>{claim.evidence.map(item => <details key={item.id}><summary>{item.source.title ?? item.source.domain}</summary><p>{item.supportingExcerpt}</p><p className="section-copy">{item.type} · {item.sourceLocator}</p><a className="quiet-button" href={item.source.url} target="_blank" rel="noreferrer noopener">Open source</a></details>)}</div>)}</details> : null}
  </article>
}

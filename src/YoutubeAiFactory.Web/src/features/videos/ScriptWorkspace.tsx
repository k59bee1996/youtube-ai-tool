import { useCallback, useEffect, useMemo, useState } from "react"
import { api, type ScriptBlock, type ScriptHistoryItem, type ScriptStatus, type VideoScript } from "../../services/api"
import { messageFrom } from "../../lib/errors"

type BlockDraft = { id: string; text: string }

export function ScriptWorkspace({ projectId, videoProjectId, revisionKey, onWorkflowStatusChange }: {
  projectId: string
  videoProjectId: string
  revisionKey: string
  onWorkflowStatusChange: (status: string) => void
}) {
  const [status, setStatus] = useState<ScriptStatus | null>(null)
  const [viewed, setViewed] = useState<VideoScript | null>(null)
  const [history, setHistory] = useState<ScriptHistoryItem[]>([])
  const [loading, setLoading] = useState(true)
  const [busy, setBusy] = useState(false)
  const [editing, setEditing] = useState(false)
  const [drafts, setDrafts] = useState<BlockDraft[]>([])
  const [error, setError] = useState<string | null>(null)

  const load = useCallback(async () => {
    if (revisionKey.length === 0) return
    try {
      const [nextStatus, nextHistory] = await Promise.all([
        api.getScriptStatus(projectId, videoProjectId),
        api.listScripts(projectId, videoProjectId),
      ])
      setStatus(nextStatus)
      setHistory(nextHistory)
      setViewed(nextStatus.latestScript)
      setError(null)
      if (nextStatus.activeJob?.operation === "Generate") onWorkflowStatusChange("ScriptGenerating")
      else if (nextStatus.latestScript?.status === "Approved") onWorkflowStatusChange("ScriptApproved")
      else if (nextStatus.latestScript) onWorkflowStatusChange("ScriptReady")
    } catch (requestError) { setError(messageFrom(requestError)) }
    finally { setLoading(false) }
  }, [onWorkflowStatusChange, projectId, revisionKey, videoProjectId])

  useEffect(() => { void Promise.resolve().then(load) }, [load])
  useEffect(() => {
    if (!status?.activeJob) return undefined
    const timer = window.setTimeout(() => void load(), 2000)
    return () => window.clearTimeout(timer)
  }, [load, status?.activeJob])

  async function generate() {
    setBusy(true)
    try { await api.generateScript(projectId, videoProjectId); await load() }
    catch (requestError) { setError(messageFrom(requestError)) }
    finally { setBusy(false) }
  }

  async function selectVersion(scriptId: string) {
    if (scriptId === status?.latestScript?.id) { setViewed(status.latestScript); return }
    setBusy(true)
    try { setViewed(await api.getScript(projectId, videoProjectId, scriptId)); setError(null) }
    catch (requestError) { setError(messageFrom(requestError)) }
    finally { setBusy(false) }
  }

  function beginEditing(script: VideoScript) {
    setDrafts(script.sections.flatMap(section => section.blocks.map(block => ({ id: block.id, text: block.text }))))
    setEditing(true)
  }

  function updateDraft(blockId: string, text: string) {
    setDrafts(current => current.map(block => block.id === blockId ? { ...block, text } : block))
  }

  async function save(script: VideoScript) {
    setBusy(true)
    try {
      applyUpdated(await api.updateScript(projectId, videoProjectId, script.id,
        drafts.map(block => ({ blockId: block.id, text: block.text }))))
      setEditing(false)
      setError(null)
    } catch (requestError) { setError(messageFrom(requestError)) }
    finally { setBusy(false) }
  }

  async function validate(script: VideoScript) {
    setBusy(true)
    try { await api.validateScript(projectId, videoProjectId, script.id); await load() }
    catch (requestError) { setError(messageFrom(requestError)) }
    finally { setBusy(false) }
  }

  async function approve(script: VideoScript) {
    setBusy(true)
    try {
      const approved = await api.approveScript(projectId, videoProjectId, script.id)
      applyUpdated(approved)
      onWorkflowStatusChange("ScriptApproved")
      setError(null)
    } catch (requestError) { setError(messageFrom(requestError)) }
    finally { setBusy(false) }
  }

  function applyUpdated(script: VideoScript) {
    setViewed(script)
    setStatus(current => current ? { ...current, latestScript: script } : current)
    setHistory(current => current.map(item => item.id === script.id
      ? { ...item, status: script.status, groundingStatus: script.groundingStatus }
      : item))
  }

  if (loading) return <section className="analysis-block"><h3>Script</h3><p className="section-copy">Loading Script workspace...</p></section>
  if (error && !status) return <section className="analysis-block"><h3>Script</h3><p className="error-copy" role="alert">{error}</p></section>

  return <section className="analysis-block script-workspace">
    <div className="section-heading"><div><h3>Script</h3><p className="section-copy">Viewer-facing narration grounded in the approved Outline and its ResearchClaims.</p></div>
      {history.length > 0 ? <label className="idea-sort">View version<select value={viewed?.id ?? ""} disabled={busy || editing} onChange={event => void selectVersion(event.target.value)}>{history.map(item => <option key={item.id} value={item.id}>v{item.version} · {item.status} · {item.groundingStatus}</option>)}</select></label> : null}
    </div>
    {error ? <p className="error-copy" role="alert">{error}</p> : null}
    {status?.activeJob ? <ScriptJobState operation={status.activeJob.operation} status={status.activeJob.status} onRefresh={load} /> : null}
    {!status?.activeJob && !viewed ? <div className="analysis-state"><p>{status?.blockReason ?? "The approved Outline is ready for Script generation."}</p>{status?.canGenerate ? <button className="primary-button" type="button" disabled={busy} onClick={() => void generate()}>{busy ? "Queuing Script..." : "Generate Script"}</button> : null}</div> : null}
    {!status?.activeJob && status?.latestJob?.status === "Failed" ? <p className="error-copy" role="alert">Script workflow failed: {status.latestJob.failureReason ?? "No failure reason was provided."} {viewed ? "The previous Script version remains available." : ""}</p> : null}
    {viewed ? <ScriptView script={viewed} isLatest={viewed.id === status?.latestScript?.id} canGenerate={Boolean(status?.canGenerate)} busy={busy} editing={editing} drafts={drafts}
      onGenerate={generate} onBeginEdit={beginEditing} onCancelEdit={() => setEditing(false)} onDraftChange={updateDraft}
      onSave={save} onValidate={validate} onApprove={approve} /> : null}
  </section>
}

function ScriptJobState({ operation, status, onRefresh }: { operation: string; status: string; onRefresh: () => Promise<void> }) {
  const validating = operation === "Validate"
  return <div className="analysis-state"><p>{validating ? "Checking current narration against ResearchClaims" : "Generating Script from the approved Outline"}... Status: {status}.</p>
    <ol className="script-progress"><li>Loading approved Outline and relevant Claims</li><li>{validating ? "Auditing edited narration" : "Writing structured narration"}</li><li>Checking factual grounding</li><li>Finalizing persisted Script state</li></ol>
    <button className="quiet-button" type="button" onClick={() => void onRefresh()}>Refresh Script status</button></div>
}

function ScriptView({ script, isLatest, canGenerate, busy, editing, drafts, onGenerate, onBeginEdit,
  onCancelEdit, onDraftChange, onSave, onValidate, onApprove }: {
  script: VideoScript; isLatest: boolean; canGenerate: boolean; busy: boolean; editing: boolean
  drafts: BlockDraft[]; onGenerate: () => Promise<void>; onBeginEdit: (script: VideoScript) => void
  onCancelEdit: () => void; onDraftChange: (blockId: string, text: string) => void
  onSave: (script: VideoScript) => Promise<void>; onValidate: (script: VideoScript) => Promise<void>
  onApprove: (script: VideoScript) => Promise<void>
}) {
  const draftsById = useMemo(() => new Map(drafts.map(block => [block.id, block.text])), [drafts])
  const ready = script.status === "Ready" && isLatest
  return <div className="analysis-report">
    <div className="section-heading"><div><p className="eyebrow">Script v{script.version} · {script.status}</p><p className="section-copy">Outline v{script.videoOutlineVersion} · Research v{script.researchReportVersion} · {script.isStale ? "Outdated" : "Current"}</p></div>{canGenerate && isLatest && !editing ? <button className="quiet-button" type="button" disabled={busy} onClick={() => void onGenerate()}>Generate new version</button> : null}</div>
    <div className="confidence-grid"><div><span>Content language</span><strong>{script.contentLanguage}</strong></div><div><span>Words</span><strong>{script.totalWordCount.toLocaleString()}</strong></div><div><span>Estimated runtime</span><strong>{formatDuration(script.estimatedDurationSeconds)}</strong></div><div><span>Grounding</span><strong>{script.groundingStatus}</strong></div></div>
    {script.isStale ? <p className="stale-note">This Script is tied to an older Outline or ResearchReport. It cannot be approved.</p> : null}
    {script.warnings.map((warning, index) => <p className="stale-note" key={`${index}-${warning}`}>{warning}</p>)}
    {script.groundingIssues.length > 0 ? <section className="script-grounding-issues"><h4>Grounding issues</h4>{script.groundingIssues.map((issue, index) => <article className="pilot-card" key={`${issue.sectionSequence}-${issue.blockSequence}-${issue.issueType}-${index}`}><strong>{issue.severity}: {issue.issueType}</strong><p>Section {issue.sectionSequence}, block {issue.blockSequence}: {issue.problematicText}</p><p>{issue.explanation}</p></article>)}</section> : null}
    <section className="script-reader"><h4>Narration</h4>{script.sections.toSorted((left, right) => left.sequence - right.sequence).map(section => <ScriptSectionView key={section.id} section={section} editing={editing} draftsById={draftsById} onDraftChange={onDraftChange} />)}</section>
    {ready && editing ? <div className="idea-actions"><button className="primary-button" type="button" disabled={busy} onClick={() => void onSave(script)}>{busy ? "Saving..." : "Save narration edits"}</button><button className="quiet-button" type="button" disabled={busy} onClick={onCancelEdit}>Cancel</button></div> : null}
    {ready && !editing ? <div className="idea-actions"><button className="quiet-button" type="button" disabled={busy || script.isStale} onClick={() => onBeginEdit(script)}>Edit narration</button>{script.groundingStatus !== "Passed" ? <button className="primary-button" type="button" disabled={busy || script.isStale} onClick={() => void onValidate(script)}>{busy ? "Queuing..." : "Validate Script"}</button> : <button className="primary-button" type="button" disabled={busy || script.isStale} onClick={() => void onApprove(script)}>Approve Script</button>}</div> : null}
    {script.status === "Approved" ? <p className="idea-approval-summary">Script Approved. This immutable structured narration is the explicit Phase 11 source.</p> : null}
  </div>
}

function ScriptSectionView({ section, editing, draftsById, onDraftChange }: {
  section: VideoScript["sections"][number]; editing: boolean; draftsById: Map<string, string>
  onDraftChange: (blockId: string, text: string) => void
}) {
  return <article className="script-section-card"><div className="section-heading"><div><span className="eyebrow">Section {section.sequence}</span><h4>{section.heading}</h4></div><span>{section.wordCount} words · {formatDuration(section.estimatedDurationSeconds)}</span></div>
    {section.blocks.toSorted((left, right) => left.sequence - right.sequence).map(block => <ScriptBlockView key={block.id} block={block} editing={editing} draft={draftsById.get(block.id)} onDraftChange={onDraftChange} />)}
  </article>
}

function ScriptBlockView({ block, editing, draft, onDraftChange }: {
  block: ScriptBlock; editing: boolean; draft: string | undefined
  onDraftChange: (blockId: string, text: string) => void
}) {
  return <div className="script-block"><span className="script-block-type">{block.type} · {block.wordCount} words</span>
    {editing ? <textarea aria-label={`Narration block ${block.sequence}`} maxLength={20000} value={draft ?? block.text} onChange={event => onDraftChange(block.id, event.target.value)} /> : <p>{block.text}</p>}
    {block.conflicts.map(conflict => <p className="stale-note" key={conflict.id}><strong>Conflicted evidence:</strong> {conflict.explanation}</p>)}
    {block.claims.length > 0 ? <details><summary>Supporting Claims ({block.claims.length})</summary>{block.claims.map(claim => <div className="outline-claim" key={claim.id}><p><strong>{claim.supportStatus}</strong> · {claim.statement}</p>{claim.evidence.map(item => <details key={item.id}><summary>{item.source.title ?? item.source.domain}</summary><p>{item.supportingExcerpt}</p><p className="section-copy">{item.type} · {item.sourceLocator}</p><a className="quiet-button" href={item.source.url} target="_blank" rel="noreferrer noopener">Open source</a></details>)}</div>)}</details> : null}
  </div>
}

function formatDuration(seconds: number) {
  const minutes = Math.floor(seconds / 60)
  const remainder = seconds % 60
  return `${minutes}:${remainder.toString().padStart(2, "0")}`
}

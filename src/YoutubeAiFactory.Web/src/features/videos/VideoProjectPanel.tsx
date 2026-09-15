import { useCallback, useEffect, useState } from "react"
import { api, type VideoProject, type VideoProjectListItem } from "../../services/api"
import { messageFrom } from "../../lib/errors"

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
    {loading ? <p className="empty-state">Loading video projects…</p> : error ? <p className="error-copy" role="alert">{error}</p> : items.length === 0 ? <p className="empty-state">No videos have entered production yet. Create a Video Project from an approved Pilot.</p> : <div className="analysis-report">{items.map((item) => <article className="pilot-card" key={item.id}><div className="section-heading"><div><strong>{item.workingTitle}</strong><p>Slot {item.pilotSequence} · {item.experimentType} experiment · {item.status}</p></div><button className="quiet-button" type="button" onClick={() => void open(item)}>Open Video Project</button></div></article>)}</div>}
  </section>
}

export function VideoProjectWorkspace({ project, onBack }: { project: VideoProject; onBack: () => void }) {
  const [workingTitle, setWorkingTitle] = useState(project.workingTitle)
  const [notes, setNotes] = useState(project.executionNotes ?? "")
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [current, setCurrent] = useState(project)
  async function save() {
    setBusy(true)
    try { setCurrent(await api.updateVideoProject(current.projectId, current.id, workingTitle, notes || null)); setError(null) }
    catch (requestError) { setError(messageFrom(requestError)) }
    finally { setBusy(false) }
  }
  return <section className="analysis-section panel">
    <div className="section-heading"><div><span className="eyebrow">Video Project</span><h2>{current.workingTitle}</h2><p className="section-copy">Status: {current.status}</p></div><button className="quiet-button" type="button" onClick={onBack}>Back to Videos</button></div>
    {error && <p className="error-copy" role="alert">{error}</p>}
    {current.sourceRequiresReview && <p className="stale-note">Source strategy has changed since this Video Project was created. Its execution history remains intact.</p>}
    {current.sourceWarnings.map((warning) => <p className="stale-note" key={warning}>{warning}</p>)}
    <div className="confidence-grid"><div><span>Planning</span><strong>Complete</strong></div><div><span>Research</span><strong>Not started</strong></div><div><span>Outline</span><strong>Locked</strong></div><div><span>Script</span><strong>Locked</strong></div><div><span>Production</span><strong>Locked</strong></div></div>
    <section className="analysis-block"><h3>Source context</h3><p><strong>Opportunity:</strong> {current.opportunityName}</p><p><strong>Idea score:</strong> {current.ideaScore}</p><p><strong>Pilot:</strong> Version {current.pilotVersion}, slot {current.pilotSequence} · {current.experimentType}</p><p><strong>Hypothesis:</strong> {current.pilotHypothesis}</p><p><strong>Variable:</strong> {current.variableBeingTested}</p><p><strong>Primary metric:</strong> {current.primaryMetric}</p><p><strong>Success signal:</strong> {current.successSignal}</p></section>
    <section className="analysis-block"><h3>Execution brief</h3><p><strong>Topic:</strong> {current.topic}</p><p><strong>Angle:</strong> {current.angle}</p><p><strong>Format:</strong> {current.contentFormat}</p><p><strong>Audience:</strong> {current.targetAudience}</p><p><strong>Viewer promise:</strong> {current.viewerPromise}</p><p><strong>Hook:</strong> {current.hookConcept}</p><p><strong>Thumbnail concept:</strong> {current.thumbnailConcept}</p></section>
    <section className="analysis-block"><h3>Working details</h3><label>Working title<input value={workingTitle} maxLength={300} onChange={(event) => setWorkingTitle(event.target.value)} /></label><label>Execution notes<textarea value={notes} maxLength={10000} onChange={(event) => setNotes(event.target.value)} /></label><button className="primary-button" type="button" disabled={busy} onClick={() => void save()}>{busy ? "Saving…" : "Save working details"}</button></section>
  </section>
}

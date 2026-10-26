import { useState } from "react"
import type { FormEvent } from "react"
import { CompetitorDetails } from "../../competitors/CompetitorDetails"
import type { CompetitorDetails as CompetitorDetailsModel, CompetitorSummary } from "../../competitors/types"
import { OpportunityPanel } from "../../opportunities/OpportunityPanel"
import { PilotPanel } from "../../pilots/PilotPanel"
import { VideoProjectPanel, VideoProjectWorkspace } from "../../videos/VideoProjectPanel"
import type { VideoProject } from "../../../services/api"
import type { Project } from "../../../services/api"
import { ObservabilityDashboard } from "../../observability/ObservabilityDashboard"

type ProjectWorkspaceProps = {
  project: Project
  competitors: CompetitorSummary[]
  competitor: CompetitorDetailsModel | null
  loading: boolean
  collecting: boolean
  onAddCompetitor: (event: FormEvent<HTMLFormElement>) => Promise<void>
  onSelectCompetitor: (competitor: CompetitorSummary) => Promise<void>
}

export function ProjectWorkspace({ project, competitors, competitor, loading, collecting, onAddCompetitor, onSelectCompetitor }: ProjectWorkspaceProps) {
  const [opportunitiesOpen, setOpportunitiesOpen] = useState(false)
  const [pilotOpen, setPilotOpen] = useState(false)
  const [videosOpen, setVideosOpen] = useState(false)
  const [observabilityOpen, setObservabilityOpen] = useState(false)
  const [videoProject, setVideoProject] = useState<VideoProject | null>(null)
  if (videoProject) return <VideoProjectWorkspace project={videoProject} onBack={() => setVideoProject(null)} />
  return <>
    <section className="project-context">
      <div><span>Market</span><strong>{project.marketName}</strong></div>
      <div><span>Audience</span><strong>{project.audienceDescription}</strong></div>
      <div><span>Target</span><strong>{project.targetLanguage} · {project.targetGeography}</strong></div>
    </section>
    <section className="panel competitor-panel">
      <div className="section-heading"><div><h2>Competitors</h2><p className="section-copy">Collect a channel and its configured recent upload window.</p></div></div>
      <form className="competitor-form" onSubmit={onAddCompetitor}>
        <label>YouTube channel URL<input name="youtubeUrl" type="url" required placeholder="https://www.youtube.com/@handle" /></label>
        <button className="primary-button" disabled={collecting || loading}>{collecting ? "Collecting channel…" : "Add competitor"}</button>
      </form>
      {competitors.length > 0 && <div className="competitor-tabs" aria-label="Collected competitors">
        {competitors.map((item) => <button type="button" className={competitor?.id === item.id ? "active" : ""} key={item.id} onClick={() => void onSelectCompetitor(item)}>{item.title}</button>)}
      </div>}
    </section>
    {loading ? <p className="empty-state">Loading competitors…</p> : competitor ? <CompetitorDetails competitor={competitor} /> : <p className="empty-state">Add a YouTube channel to begin competitor research.</p>}
    <section className="panel competitor-panel"><PanelToggle eyebrow="Research" title="Opportunities" toggleLabel="opportunities" copy="Synthesize completed competitor analyses into ranked, evidence-backed opportunities." open={opportunitiesOpen} onToggle={() => setOpportunitiesOpen((open) => !open)} /></section>
    {opportunitiesOpen && <OpportunityPanel projectId={project.id} />}
    <section className="panel competitor-panel"><PanelToggle eyebrow="Validation" title="12-Video Pilot" toggleLabel="pilot" copy="Turn approved ideas into a deliberate topic, packaging, and storytelling learning plan." open={pilotOpen} onToggle={() => setPilotOpen((open) => !open)} /></section>
    {pilotOpen && <PilotPanel projectId={project.id} onVideoProjectCreated={setVideoProject} />}
    <section className="panel competitor-panel"><PanelToggle eyebrow="Execution" title="Videos" toggleLabel="videos" copy="Move an approved pilot experiment into a durable production workflow." open={videosOpen} onToggle={() => setVideosOpen((open) => !open)} /></section>
    {videosOpen && <VideoProjectPanel projectId={project.id} onOpen={setVideoProject} />}
    <section className="panel competitor-panel"><PanelToggle eyebrow="Operations" title="Observability" toggleLabel="dashboard" copy="Inspect production progress, AI usage, cost coverage, and workflow health." open={observabilityOpen} onToggle={() => setObservabilityOpen((open) => !open)} /></section>
    {observabilityOpen && <ObservabilityDashboard projectId={project.id} />}
  </>
}

function PanelToggle({ eyebrow, title, toggleLabel, copy, open, onToggle }: { eyebrow: string; title: string; toggleLabel: string; copy: string; open: boolean; onToggle: () => void }) {
  return <div className="section-heading"><div><span className="eyebrow">{eyebrow}</span><h2>{title}</h2><p className="section-copy">{copy}</p></div><button className="primary-button" type="button" onClick={onToggle}>{open ? `Hide ${toggleLabel}` : `Open ${toggleLabel}`}</button></div>
}

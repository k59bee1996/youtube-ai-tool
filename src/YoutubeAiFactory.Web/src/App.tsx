import { useCallback, useEffect, useRef, useState } from 'react'
import type { FormEvent } from 'react'
import {
  api,
  type CompetitorDetails,
  type CompetitorAnalysis,
  type CompetitorAnalysisStatus,
  type CompetitorSummary,
  type CreateProjectRequest,
  type Project,
} from './api'
import './App.css'

const selectedProjectKey = 'youtube-ai-factory:selected-project'

function App() {
  const [projects, setProjects] = useState<Project[]>([])
  const [project, setProject] = useState<Project | null>(null)
  const [competitors, setCompetitors] = useState<CompetitorSummary[]>([])
  const [competitor, setCompetitor] = useState<CompetitorDetails | null>(null)
  const [loading, setLoading] = useState(true)
  const [workspaceLoading, setWorkspaceLoading] = useState(false)
  const [creatingProject, setCreatingProject] = useState(false)
  const [collecting, setCollecting] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const mounted = useRef(true)
  const selectedProjectId = useRef<string | null>(null)
  const workspaceRequest = useRef(0)
  const detailRequest = useRef(0)
  const collectionRequest = useRef(0)
  const workspaceAbort = useRef<AbortController | null>(null)
  const detailAbort = useRef<AbortController | null>(null)
  const collectionAbort = useRef<AbortController | null>(null)

  useEffect(() => {
    let active = true
    const controller = new AbortController()
    mounted.current = true

    api
      .listProjects(controller.signal)
      .then(async (loadedProjects) => {
        if (!active) return
        setProjects(loadedProjects)
        const storedId = readSelectedProjectId()
        const storedProject = loadedProjects.find((item) => item.id === storedId)
        if (storedProject) await openProject(storedProject)
      })
      .catch((requestError: unknown) => {
        if (active) setError(messageFrom(requestError))
      })
      .finally(() => {
        if (active) setLoading(false)
      })

    return () => {
      active = false
      mounted.current = false
      controller.abort()
      invalidateProjectRequests()
    }
  }, [])

  async function openProject(selected: Project) {
    invalidateProjectRequests()
    const request = workspaceRequest.current
    const controller = new AbortController()
    workspaceAbort.current = controller
    selectedProjectId.current = selected.id
    setProject(selected)
    setCompetitors([])
    setCompetitor(null)
    setWorkspaceLoading(true)
    setCollecting(false)
    setError(null)
    tryPersistSelectedProject(selected.id)

    let requestedDetail: number | null = null

    try {
      const loadedCompetitors = await api.listCompetitors(selected.id, controller.signal)
      if (!isCurrentWorkspace(request, selected.id)) return

      setCompetitors(loadedCompetitors)
      if (loadedCompetitors[0]) {
        requestedDetail = ++detailRequest.current
        const detailController = new AbortController()
        detailAbort.current = detailController
        const loadedCompetitor = await api.getCompetitor(
          selected.id,
          loadedCompetitors[0].id,
          detailController.signal,
        )
        if (!isCurrentDetail(request, selected.id, requestedDetail)) return

        setCompetitor(loadedCompetitor)
      }
    } catch (requestError) {
      if (isCurrentDetail(request, selected.id, requestedDetail)) {
        setError(messageFrom(requestError))
      }
    } finally {
      if (isCurrentDetail(request, selected.id, requestedDetail)) {
        setWorkspaceLoading(false)
      }
    }
  }

  async function createProject(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setCreatingProject(true)
    setError(null)
    const data = new FormData(event.currentTarget)
    const request: CreateProjectRequest = {
      name: String(data.get('name') ?? ''),
      marketName: String(data.get('marketName') ?? ''),
      targetLanguage: String(data.get('targetLanguage') ?? ''),
      targetGeography: String(data.get('targetGeography') ?? ''),
      audienceDescription: String(data.get('audienceDescription') ?? ''),
    }

    try {
      const created = await api.createProject(request)
      setProjects((current) => [created, ...current])
      await openProject(created)
    } catch (requestError) {
      setError(messageFrom(requestError))
    } finally {
      setCreatingProject(false)
    }
  }

  async function addCompetitor(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!project) return

    const projectId = project.id
    const workspace = workspaceRequest.current
    const request = ++collectionRequest.current
    const requestedDetail = ++detailRequest.current
    collectionAbort.current?.abort()
    detailAbort.current?.abort()
    const controller = new AbortController()
    collectionAbort.current = controller
    setCollecting(true)
    setError(null)
    const form = event.currentTarget
    const youtubeUrl = String(new FormData(form).get('youtubeUrl') ?? '')

    try {
      const collected = await api.addCompetitor(projectId, youtubeUrl, controller.signal)
      if (!isCurrentCollection(workspace, projectId, request)) return

      if (detailRequest.current === requestedDetail) {
        setCompetitor(collected)
      }
      form.reset()
      const loadedCompetitors = await api.listCompetitors(projectId, controller.signal)
      if (!isCurrentCollection(workspace, projectId, request)) return

      setCompetitors(loadedCompetitors)
    } catch (requestError) {
      if (isCurrentCollection(workspace, projectId, request)) {
        setError(messageFrom(requestError))
      }
    } finally {
      if (isCurrentCollection(workspace, projectId, request)) {
        setCollecting(false)
      }
    }
  }

  async function selectCompetitor(selected: CompetitorSummary) {
    if (!project) return

    const projectId = project.id
    const workspace = workspaceRequest.current
    const request = ++detailRequest.current
    detailAbort.current?.abort()
    const controller = new AbortController()
    detailAbort.current = controller
    setCompetitor(null)
    setWorkspaceLoading(true)
    setError(null)
    try {
      const loadedCompetitor = await api.getCompetitor(projectId, selected.id, controller.signal)
      if (!isCurrentDetail(workspace, projectId, request)) return

      setCompetitor(loadedCompetitor)
    } catch (requestError) {
      if (isCurrentDetail(workspace, projectId, request)) {
        setError(messageFrom(requestError))
      }
    } finally {
      if (isCurrentDetail(workspace, projectId, request)) {
        setWorkspaceLoading(false)
      }
    }
  }

  function closeProject() {
    invalidateProjectRequests()
    selectedProjectId.current = null
    tryPersistSelectedProject(null)
    setProject(null)
    setCompetitors([])
    setCompetitor(null)
    setWorkspaceLoading(false)
    setCollecting(false)
    setError(null)
  }

  function invalidateProjectRequests() {
    ++workspaceRequest.current
    ++detailRequest.current
    ++collectionRequest.current
    workspaceAbort.current?.abort()
    detailAbort.current?.abort()
    collectionAbort.current?.abort()
    workspaceAbort.current = null
    detailAbort.current = null
    collectionAbort.current = null
  }

  function isCurrentWorkspace(request: number, projectId: string) {
    return mounted.current &&
      workspaceRequest.current === request &&
      selectedProjectId.current === projectId
  }

  function isCurrentDetail(workspace: number, projectId: string, request: number | null) {
    return isCurrentWorkspace(workspace, projectId) &&
      (request === null || detailRequest.current === request)
  }

  function isCurrentCollection(workspace: number, projectId: string, request: number) {
    return isCurrentWorkspace(workspace, projectId) && collectionRequest.current === request
  }

  if (loading) {
    return <main className="shell loading">Loading workspace…</main>
  }

  return (
    <main className="shell">
      <header className="masthead">
        <div>
          <span className="eyebrow">YouTube AI Factory</span>
          <h1>{project ? project.name : 'Competitor research starts here.'}</h1>
        </div>
        {project && (
          <button className="quiet-button" type="button" onClick={closeProject}>
            All projects
          </button>
        )}
      </header>

      {error && <div className="error-banner" role="alert">{error}</div>}

      {project ? (
        <ProjectWorkspace
          project={project}
          competitors={competitors}
          competitor={competitor}
          loading={workspaceLoading}
          collecting={collecting}
          onAddCompetitor={addCompetitor}
          onSelectCompetitor={selectCompetitor}
        />
      ) : (
        <ProjectHome
          projects={projects}
          creating={creatingProject}
          onCreate={createProject}
          onOpen={openProject}
        />
      )}
    </main>
  )
}

type ProjectHomeProps = {
  projects: Project[]
  creating: boolean
  onCreate: (event: FormEvent<HTMLFormElement>) => Promise<void>
  onOpen: (project: Project) => Promise<void>
}

function ProjectHome({ projects, creating, onCreate, onOpen }: ProjectHomeProps) {
  return (
    <div className="home-grid">
      <section className="panel">
        <h2>Create project</h2>
        <p className="section-copy">Define the market and audience for this research workspace.</p>
        <form className="form-grid" onSubmit={onCreate}>
          <label>
            Project name
            <input name="name" required maxLength={200} placeholder="Creator education research" />
          </label>
          <label>
            Market
            <input name="marketName" required maxLength={200} placeholder="Creator education" />
          </label>
          <label>
            Target language
            <input name="targetLanguage" required maxLength={50} placeholder="English" />
          </label>
          <label>
            Target geography
            <input name="targetGeography" required maxLength={100} placeholder="Global" />
          </label>
          <label className="full-field">
            Target audience
            <textarea
              name="audienceDescription"
              required
              maxLength={2000}
              placeholder="Independent creators building educational channels"
            />
          </label>
          <button className="primary-button full-field" disabled={creating}>
            {creating ? 'Creating…' : 'Create project'}
          </button>
        </form>
      </section>

      <section className="panel">
        <h2>Projects</h2>
        {projects.length === 0 ? (
          <p className="empty-state">No projects yet. Create the first one to begin.</p>
        ) : (
          <div className="project-list">
            {projects.map((item) => (
              <button
                type="button"
                key={item.id}
                disabled={creating}
                onClick={() => void onOpen(item)}
              >
                <strong>{item.name}</strong>
                <span>{item.marketName} · {item.targetLanguage} · {item.targetGeography}</span>
              </button>
            ))}
          </div>
        )}
      </section>
    </div>
  )
}

type ProjectWorkspaceProps = {
  project: Project
  competitors: CompetitorSummary[]
  competitor: CompetitorDetails | null
  loading: boolean
  collecting: boolean
  onAddCompetitor: (event: FormEvent<HTMLFormElement>) => Promise<void>
  onSelectCompetitor: (competitor: CompetitorSummary) => Promise<void>
}

function ProjectWorkspace({
  project,
  competitors,
  competitor,
  loading,
  collecting,
  onAddCompetitor,
  onSelectCompetitor,
}: ProjectWorkspaceProps) {
  const [opportunitiesOpen, setOpportunitiesOpen] = useState(false)
  return (
    <>
      <section className="project-context">
        <div><span>Market</span><strong>{project.marketName}</strong></div>
        <div><span>Audience</span><strong>{project.audienceDescription}</strong></div>
        <div><span>Target</span><strong>{project.targetLanguage} · {project.targetGeography}</strong></div>
      </section>

      <section className="panel competitor-panel">
        <div className="section-heading">
          <div>
            <h2>Competitors</h2>
            <p className="section-copy">Collect a channel and its configured recent upload window.</p>
          </div>
        </div>
        <form className="competitor-form" onSubmit={onAddCompetitor}>
          <label>
            YouTube channel URL
            <input
              name="youtubeUrl"
              type="url"
              required
              placeholder="https://www.youtube.com/@handle"
            />
          </label>
          <button className="primary-button" disabled={collecting || loading}>
            {collecting ? 'Collecting channel…' : 'Add competitor'}
          </button>
        </form>

        {competitors.length > 0 && (
          <div className="competitor-tabs" aria-label="Collected competitors">
            {competitors.map((item) => (
              <button
                type="button"
                className={competitor?.id === item.id ? 'active' : ''}
                key={item.id}
                onClick={() => void onSelectCompetitor(item)}
              >
                {item.title}
              </button>
            ))}
          </div>
        )}
      </section>

      {loading ? (
        <p className="empty-state">Loading competitors…</p>
      ) : competitor ? (
        <CompetitorView competitor={competitor} />
      ) : (
        <p className="empty-state">Add a YouTube channel to begin competitor research.</p>
      )}
      <section className="panel competitor-panel">
        <div className="section-heading"><div><span className="eyebrow">Research</span><h2>Opportunities</h2><p className="section-copy">Synthesize completed competitor analyses into ranked, evidence-backed opportunities.</p></div>
          <button className="primary-button" type="button" onClick={() => setOpportunitiesOpen((open) => !open)}>{opportunitiesOpen ? 'Hide opportunities' : 'Open opportunities'}</button></div>
      </section>
      {opportunitiesOpen && <OpportunityPanel projectId={project.id} />}
    </>
  )
}

function OpportunityPanel({ projectId }: { projectId: string }) {
  const [status, setStatus] = useState<import('./api').OpportunityStatus | null>(null)
  const [loading, setLoading] = useState(true)
  const [running, setRunning] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const active = status?.activeJob?.status === 'Queued' || status?.activeJob?.status === 'Running' || status?.activeJob?.status === 'Retrying'
  const load = useCallback(async () => { try { setStatus(await api.getOpportunities(projectId)); setError(null) } catch (requestError) { setError(messageFrom(requestError)) } finally { setLoading(false) } }, [projectId])
  useEffect(() => { void Promise.resolve().then(load); return undefined }, [load])
  useEffect(() => { if (!active) return undefined; const timer = window.setInterval(() => void load(), 2000); return () => window.clearInterval(timer) }, [active, load])
  async function generate() { setRunning(true); setError(null); try { await api.generateOpportunities(projectId); await load() } catch (requestError) { setError(messageFrom(requestError)) } finally { setRunning(false) } }
  return <section className="analysis-section panel">
    <div className="section-heading"><div><span className="eyebrow">Research</span><h2>Opportunities</h2></div>{status?.latestReport && <span>Report v{status.latestReport.version}</span>}</div>
    {loading ? <p className="empty-state">Loading opportunities…</p> : error ? <p className="error-copy" role="alert">{error}</p> : status && status.analyzedCompetitorCount === 0 ? <div className="analysis-state"><p>No opportunity analysis exists. Analyze at least one competitor first.</p></div> : active ? <div className="analysis-state"><strong>Generating opportunities…</strong><span>Status: {status?.activeJob?.status}</span></div> : status?.latestJob?.status === 'Failed' ? <div className="analysis-state"><p role="alert">{status.latestJob.failureReason ?? 'Opportunity generation failed.'}</p><button className="primary-button" type="button" onClick={() => void generate()} disabled={running}>Generate opportunities</button></div> : status?.latestReport ? <OpportunityReportView projectId={projectId} report={status.latestReport} onChanged={load} /> : <div className="analysis-state"><p>{status && status.analyzedCompetitorCount < status.competitorCount ? `${status.competitorCount - status.analyzedCompetitorCount} of ${status.competitorCount} competitors have not been analyzed. Results use the completed analyses only.` : 'Generate an evidence-backed report from your completed competitor analyses.'}</p><button className="primary-button" type="button" onClick={() => void generate()} disabled={running}>{running ? 'Queuing opportunities…' : 'Generate opportunities'}</button></div>}
  </section>
}

function OpportunityReportView({ projectId, report, onChanged }: { projectId: string; report: import('./api').OpportunityReport; onChanged: () => Promise<void> }) {
  return <div className="analysis-report">
    {report.isStale && <p className="stale-note">This report may be outdated because a source competitor analysis has a newer version.</p>}
    {report.limitations.map((item) => <p className="stale-note" key={item}>{item}</p>)}
    {report.opportunities.map((item, index) => <article className="analysis-block" key={item.id}>
      <div className="section-heading"><h3>#{index + 1} {item.name}</h3><strong>{item.scores.overallScore}/100</strong></div><p>{item.description}</p>
      <p><strong>Audience:</strong> {item.audience} · <strong>Topic:</strong> {item.topic} · <strong>Format:</strong> {item.contentFormat}</p><p><strong>Angle:</strong> {item.angle}</p><p>{item.whyThisOpportunity}</p>
      <div className="confidence-grid"><div><span>Observed demand</span><strong>{item.scores.observedDemandSignal}</strong></div><div><span>Novelty</span><strong>{item.scores.noveltySignal}</strong></div><div><span>Audience fit</span><strong>{item.scores.audienceFitSignal}</strong></div><div><span>Competition risk</span><strong>{item.scores.competitionRiskSignal}</strong></div><div><span>Evidence strength</span><strong>{item.scores.evidenceStrength}</strong></div><div><span>Confidence</span><strong>{item.confidence}%</strong></div></div>
      <p><strong>Evidence:</strong> {item.evidence.map((evidence) => evidence.summary).join(' · ')}</p>{item.risks.length > 0 && <p><strong>Risks:</strong> {item.risks.join(' · ')}</p>}{item.limitations.length > 0 && <p><strong>Limitations:</strong> {item.limitations.join(' · ')}</p>}
      <OpportunityActions projectId={projectId} opportunity={item} onChanged={onChanged} />
      {item.decisionStatus === 'Approved' && <IdeaBank projectId={projectId} opportunityId={item.id} />}
    </article>)}
  </div>
}

function OpportunityActions({ projectId, opportunity, onChanged }: { projectId: string; opportunity: import('./api').OpportunityCandidate; onChanged: () => Promise<void> }) {
  const [busy, setBusy] = useState(false)
  async function setDecision(decision: 'Approved' | 'Rejected') { setBusy(true); try { if (decision === 'Approved') await api.approveOpportunity(projectId, opportunity.id); else await api.rejectOpportunity(projectId, opportunity.id); await onChanged() } finally { setBusy(false) } }
  return <div className="idea-actions"><strong>Status: {opportunity.decisionStatus}</strong><button className="quiet-button" type="button" onClick={() => void setDecision('Approved')} disabled={busy}>Approve opportunity</button><button className="quiet-button" type="button" onClick={() => void setDecision('Rejected')} disabled={busy}>Reject opportunity</button></div>
}

function IdeaBank({ projectId, opportunityId }: { projectId: string; opportunityId: string }) {
  const [bank, setBank] = useState<import('./api').IdeaBank | null>(null); const [loading, setLoading] = useState(true); const [running, setRunning] = useState(false); const [error, setError] = useState<string | null>(null); const [sort, setSort] = useState<'overallScore' | 'novelty' | 'thumbnailPotential' | 'storyPotential' | 'competitionRisk' | 'evidenceStrength'>('overallScore'); const [selected, setSelected] = useState<string | null>(null)
  const load = useCallback(async () => { try { setBank(await api.getIdeaBank(projectId, opportunityId)); setError(null) } catch (requestError) { setError(messageFrom(requestError)) } finally { setLoading(false) } }, [projectId, opportunityId])
  useEffect(() => { void Promise.resolve().then(load); return undefined }, [load]); useEffect(() => { if (!bank?.activeJobStatus) return undefined; const timer = window.setInterval(() => void load(), 2000); return () => window.clearInterval(timer) }, [bank?.activeJobStatus, load])
  async function generate() { setRunning(true); try { await api.generateIdeas(projectId, opportunityId); await load() } catch (requestError) { setError(messageFrom(requestError)) } finally { setRunning(false) } }
  async function decide(ideaId: string, approve: boolean) { try { if (approve) await api.approveIdea(projectId, ideaId); else await api.rejectIdea(projectId, ideaId); await load() } catch (requestError) { setError(messageFrom(requestError)) } }
  const ideas = [...(bank?.ideas ?? [])].sort((a, b) => sort === 'competitionRisk' ? a.scores[sort] - b.scores[sort] : b.scores[sort] - a.scores[sort]); const detail = ideas.find(item => item.id === selected)
  return <section className="idea-bank"><div className="section-heading"><div><span className="eyebrow">Idea Bank</span><h3>Ranked video concepts</h3></div><button className="primary-button" type="button" onClick={() => void generate()} disabled={running || Boolean(bank?.activeJobStatus)}>{running || bank?.activeJobStatus ? 'Generating ideas…' : 'Generate ideas'}</button></div>{loading ? <p className="empty-state">Loading Idea Bank…</p> : error ? <p className="error-copy" role="alert">{error}</p> : <><p className="section-copy">Ideas are persisted proposals; generation is never triggered by this page loading.</p>{bank?.latestGeneration?.isStale && <p className="stale-note">This generation is based on an older opportunity report.</p>}<label className="idea-sort">Sort ideas <select value={sort} onChange={(event) => setSort(event.target.value as typeof sort)}><option value="overallScore">Overall score</option><option value="novelty">Novelty</option><option value="thumbnailPotential">Thumbnail potential</option><option value="storyPotential">Story potential</option><option value="competitionRisk">Competition risk</option><option value="evidenceStrength">Evidence strength</option></select></label>{ideas.length === 0 ? <p className="empty-state">No ideas have been generated for this approved opportunity.</p> : <div className="idea-list">{ideas.map(item => <article className="idea-card" key={item.id}><button className="idea-title" type="button" onClick={() => setSelected(item.id)}>{item.workingTitle}</button><p>{item.topic} · {item.angle} · {item.contentFormat}</p><div className="confidence-grid"><div><span>Overall</span><strong>{item.scores.overallScore}</strong></div><div><span>Novelty</span><strong>{item.scores.novelty}</strong></div><div><span>Thumbnail</span><strong>{item.scores.thumbnailPotential}</strong></div><div><span>Story</span><strong>{item.scores.storyPotential}</strong></div><div><span>Evidence</span><strong>{item.scores.evidenceStrength}</strong></div><div><span>Competition risk</span><strong>{item.scores.competitionRisk}</strong></div></div><div className="idea-actions"><strong>{item.decisionStatus}</strong><button className="quiet-button" type="button" onClick={() => void decide(item.id, true)}>Approve</button><button className="quiet-button" type="button" onClick={() => void decide(item.id, false)}>Reject</button></div></article>)}</div>}{detail && <article className="idea-detail"><button className="quiet-button" type="button" onClick={() => setSelected(null)}>Close detail</button><h3>{detail.workingTitle}</h3><p><strong>Audience:</strong> {detail.targetAudience} · {detail.viewerIntent}</p><p><strong>Core question:</strong> {detail.coreQuestion}</p><p><strong>Viewer promise:</strong> {detail.viewerPromise}</p><p><strong>Hook:</strong> {detail.hookConcept}</p><p><strong>Thumbnail concept:</strong> {detail.thumbnailConcept}</p><p><strong>Hypothesis:</strong> {detail.hypothesis}</p><p><strong>Why it fits:</strong> {detail.whyViewerWouldCare}</p><p><strong>Evidence:</strong> {detail.evidence.map(e => e.summary).join(' · ')}</p><p><strong>Risks:</strong> {detail.risks.join(' · ') || 'None recorded'}</p></article>}</>}</section>
}

function CompetitorView({ competitor }: { competitor: CompetitorDetails }) {
  return (
    <>
      <section className="channel-card">
        {competitor.thumbnailUrl && (
          <img src={competitor.thumbnailUrl} alt="" width="88" height="88" />
        )}
        <div className="channel-identity">
          <span className="eyebrow">Collected channel</span>
          <h2>{competitor.title}</h2>
          {competitor.handle && <p>{competitor.handle}</p>}
        </div>
        <dl className="channel-stats">
          <Stat label="Subscribers" value={formatNumber(competitor.subscriberCount)} />
          <Stat label="Channel videos" value={formatNumber(competitor.videoCount)} />
          <Stat label="Total views" value={formatNumber(competitor.viewCount)} />
          <Stat label="Collected" value={formatDate(competitor.lastCollectedAt)} />
        </dl>
      </section>

      <section className="videos-section">
        <div className="section-heading">
          <h2>Recent videos</h2>
          <span>{competitor.videos.length} collected</span>
        </div>
        {competitor.videos.length === 0 ? (
          <p className="empty-state">No recent videos were returned for this channel.</p>
        ) : (
          <div className="video-list">
            {competitor.videos.map((video) => (
              <article className="video-card" key={video.id}>
                {video.thumbnailUrl ? (
                  <img src={video.thumbnailUrl} alt="" loading="lazy" />
                ) : (
                  <div className="thumbnail-placeholder" />
                )}
                <div className="video-details">
                  <a href={video.url} target="_blank" rel="noreferrer">{video.title}</a>
                  <span>{formatDate(video.publishedAt)}</span>
                  <div className="video-metrics">
                    <span>{formatNumber(video.viewCount)} views</span>
                    {video.likeCount !== null && <span>{formatNumber(video.likeCount)} likes</span>}
                    {video.commentCount !== null && <span>{formatNumber(video.commentCount)} comments</span>}
                  </div>
                </div>
              </article>
            ))}
          </div>
        )}
      </section>
      <AnalysisPanel projectId={competitor.projectId} competitor={competitor} />
    </>
  )
}

function AnalysisPanel({ projectId, competitor }: { projectId: string; competitor: CompetitorDetails }) {
  const [status, setStatus] = useState<CompetitorAnalysisStatus | null>(null)
  const [loading, setLoading] = useState(true)
  const [running, setRunning] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const active = status?.activeJob?.status === 'Queued' || status?.activeJob?.status === 'Running' || status?.activeJob?.status === 'Retrying'

  const load = useCallback(async () => {
    try { setStatus(await api.getCompetitorAnalysis(projectId, competitor.id)); setError(null) }
    catch (requestError) { setError(messageFrom(requestError)) }
    finally { setLoading(false) }
  }, [projectId, competitor.id])

  useEffect(() => {
    void Promise.resolve().then(load)
    return undefined
  }, [load])

  useEffect(() => {
    if (!active) return undefined
    const timer = window.setInterval(() => void load(), 2000)
    return () => window.clearInterval(timer)
  }, [active, load])

  async function run() {
    setRunning(true); setError(null)
    try { await api.runCompetitorAnalysis(projectId, competitor.id); await load() }
    catch (requestError) { setError(messageFrom(requestError)) }
    finally { setRunning(false) }
  }

  return <section className="analysis-section panel">
    <div className="section-heading"><div><span className="eyebrow">AI Analysis</span><h2>Competitor intelligence</h2></div>{status?.latestAnalysis && <span>Version {status.latestAnalysis.version}</span>}</div>
    {loading ? <p className="empty-state">Loading analysis…</p> : error ? <><p className="error-copy" role="alert">{error}</p><button className="primary-button" type="button" onClick={() => void load()}>Retry</button></> : active ? <div className="analysis-state"><strong>Analyzing competitor…</strong><span>Status: {status?.activeJob?.status}</span></div> : status?.latestJob?.status === 'Failed' ? <div className="analysis-state"><p role="alert">{status.latestJob.failureReason ?? 'Analysis failed. You can retry safely.'}</p><button className="primary-button" type="button" onClick={() => void run()} disabled={running}>Run AI Analysis</button></div> : status?.latestAnalysis ? <AnalysisReport analysis={status.latestAnalysis} videos={competitor.videos} /> : <div className="analysis-state"><p>No analysis has been generated yet.</p><button className="primary-button" type="button" onClick={() => void run()} disabled={running}>{running ? 'Queuing analysis…' : 'Run AI Analysis'}</button></div>}
  </section>
}

function AnalysisReport({ analysis, videos }: { analysis: CompetitorAnalysis; videos: CompetitorDetails['videos'] }) {
  const result = analysis.result
  const names = (ids: string[]) => ids.map((id) => videos.find((video) => video.id === id)?.title).filter((title): title is string => Boolean(title))
  return <div className="analysis-report">
    {analysis.isStale && <p className="stale-note">This analysis may be outdated because the competitor data was refreshed after it ran.</p>}
    <div className="confidence-grid"><div><span>Overall confidence</span><strong>{result.confidence.overallConfidence}%</strong></div><div><span>Data quality</span><strong>{result.confidence.dataQuality}</strong></div><div><span>Evidence window</span><strong>{analysis.analyzedVideoCount} videos</strong></div></div>
    <AnalysisBlock title="Target audience"><p>{result.audience.likelyAgeRange && `Likely age: ${result.audience.likelyAgeRange}`}</p><p>{result.audience.likelyInterests.join(' · ')}</p><p>{result.audience.likelyViewerIntent.join(' · ')}</p></AnalysisBlock>
    <AnalysisBlock title="Topic clusters">{result.topicClusters.map((item) => <article key={item.name}><strong>{item.name}</strong><p>{item.description}</p><small>{item.frequency} observed · {item.performanceSignal} · {item.confidence}% confidence</small><Evidence names={names(item.exampleVideoIds)} /></article>)}</AnalysisBlock>
    <AnalysisBlock title="Winning title patterns">{result.titlePatterns.map((item) => <article key={item.patternName}><strong>{item.patternName}</strong><p>{item.description}</p><code>{item.template}</code><p>{item.exampleTitles.join(' · ')}</p><small>{item.observedFrequency} observed · {item.performanceSignal}</small></article>)}</AnalysisBlock>
    <AnalysisBlock title="Thumbnail patterns">{result.thumbnailPatterns.map((item) => <article key={item.patternName}><strong>{item.patternName}</strong><p>{item.observation}</p><small>{item.confidence}% confidence · {item.limitations.join(' ')}</small><Evidence names={names(item.evidenceVideoIds)} /></article>)}</AnalysisBlock>
    <AnalysisBlock title="Hook patterns">{result.hookPatterns.map((item) => <article key={item.patternName}><strong>{item.patternName}</strong><p>{item.observation}</p><small>{item.confidence}% confidence · {item.limitations.join(' ')}</small><Evidence names={names(item.evidenceVideoIds)} /></article>)}</AnalysisBlock>
    <AnalysisBlock title="Content formats">{result.contentFormats.map((item) => <article key={item.format}><strong>{item.format}</strong><p>{item.performanceSignal} · {item.confidence}% confidence</p><Evidence names={names(item.evidenceVideoIds)} /></article>)}</AnalysisBlock>
    <AnalysisBlock title="Performance patterns">{result.performanceInsights.map((item, index) => <article key={index}><p>{item.insight}</p><Evidence names={names(item.supportingVideoIds)} /></article>)}</AnalysisBlock>
    <AnalysisBlock title="Potential weaknesses">{result.potentialWeaknesses.map((item, index) => <article key={index}><p>{item.observation}</p><Evidence names={names(item.supportingVideoIds)} /></article>)}</AnalysisBlock>
    <AnalysisBlock title="Transferable formats">{result.transferableFormats.map((item) => <article key={item.format}><strong>{item.format}</strong><p>{item.whyItMayWork}</p><p>{item.transferableMechanic}</p><small>Do not copy: {item.doNotCopy}</small><Evidence names={names(item.evidenceVideoIds)} /></article>)}</AnalysisBlock>
    <AnalysisBlock title="Evidence & limitations"><ul>{result.confidence.limitations.map((item) => <li key={item}>{item}</li>)}{result.evidenceNotes.map((item) => <li key={item.note}>{item.note}</li>)}</ul></AnalysisBlock>
  </div>
}

function AnalysisBlock({ title, children }: { title: string; children: React.ReactNode }) { return <section className="analysis-block"><h3>{title}</h3>{children}</section> }
function Evidence({ names }: { names: string[] }) { return names.length ? <small className="evidence">Observed in: {names.join(' · ')}</small> : null }

function Stat({ label, value }: { label: string; value: string }) {
  return <div><dt>{label}</dt><dd>{value}</dd></div>
}

function formatNumber(value: number | null) {
  return value === null ? 'Unavailable' : new Intl.NumberFormat(undefined, { notation: 'compact' }).format(value)
}

function formatDate(value: string | null) {
  return value ? new Intl.DateTimeFormat(undefined, { dateStyle: 'medium' }).format(new Date(value)) : 'Unavailable'
}

function messageFrom(error: unknown) {
  return error instanceof Error ? error.message : 'Something went wrong.'
}

function readSelectedProjectId() {
  try {
    return localStorage.getItem(selectedProjectKey)
  } catch {
    return null
  }
}

function tryPersistSelectedProject(projectId: string | null) {
  try {
    if (projectId === null) {
      localStorage.removeItem(selectedProjectKey)
    } else {
      localStorage.setItem(selectedProjectKey, projectId)
    }
    return true
  } catch {
    return false
  }
}

export default App

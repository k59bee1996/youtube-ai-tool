import { useEffect, useRef, useState } from 'react'
import type { FormEvent } from 'react'
import {
  api,
  type CompetitorDetails,
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
    </>
  )
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
    </>
  )
}

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

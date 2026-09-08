import { act, cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react'
import { afterEach, expect, test, vi } from 'vitest'
import App from './App'
import type { CompetitorDetails, CompetitorSummary, Project } from './api'

const project: Project = {
  id: 'project-1',
  name: 'Creator Research',
  marketName: 'Creator education',
  targetLanguage: 'English',
  targetGeography: 'Global',
  audienceDescription: 'Independent video creators',
  createdAt: '2026-09-06T00:00:00Z',
  updatedAt: null,
}

const summary: CompetitorSummary = {
  id: 'competitor-1',
  projectId: project.id,
  youtubeChannelId: 'UC1234567890abcdefghij12',
  title: 'Practical Creator',
  handle: '@practicalcreator',
  thumbnailUrl: 'https://example.test/channel.jpg',
  subscriberCount: 125000,
  videoCount: 240,
  viewCount: 14000000,
  lastCollectedAt: '2026-09-06T00:00:00Z',
}

const competitor: CompetitorDetails = {
  ...summary,
  sourceUrl: 'https://youtube.com/@practicalcreator',
  description: 'Evidence-based creator education.',
  publishedAt: '2020-04-03T00:00:00Z',
  videos: [
    {
      id: 'video-1',
      youtubeVideoId: 'a1b2c3d4e5F',
      title: 'How to Research a Video',
      description: 'A repeatable research process.',
      url: 'https://youtube.com/watch?v=a1b2c3d4e5F',
      thumbnailUrl: 'https://example.test/video.jpg',
      duration: '00:12:00',
      viewCount: 42000,
      likeCount: 1900,
      commentCount: 143,
      publishedAt: '2026-07-01T00:00:00Z',
      collectedAt: '2026-09-06T00:00:00Z',
    },
  ],
}

afterEach(() => {
  cleanup()
  localStorage.clear()
  vi.restoreAllMocks()
  vi.unstubAllGlobals()
})

test('restores a persisted project and displays its competitor videos', async () => {
  localStorage.setItem('youtube-ai-factory:selected-project', project.id)
  vi.stubGlobal('fetch', vi.fn((input: RequestInfo | URL) => {
    const url = String(input)
    if (url === '/api/projects') return json([project])
    if (url.endsWith('/competitors')) return json([summary])
    if (url.endsWith('/competitors/competitor-1')) return json(competitor)
    throw new Error(`Unexpected request: ${url}`)
  }))

  render(<App />)

  expect(await screen.findByRole('heading', { name: 'Creator Research' })).toBeVisible()
  expect((await screen.findAllByText('Practical Creator')).length).toBeGreaterThan(0)
  expect(await screen.findByText('How to Research a Video')).toBeVisible()
})

test('creates a project and collects a competitor from the UI', async () => {
  let competitorAdded = false
  vi.stubGlobal('fetch', vi.fn((input: RequestInfo | URL, init?: RequestInit) => {
    const url = String(input)
    const method = init?.method ?? 'GET'
    if (url === '/api/projects' && method === 'GET') return json([])
    if (url === '/api/projects' && method === 'POST') return json(project, 201)
    if (url.endsWith('/competitors') && method === 'POST') {
      competitorAdded = true
      return json(competitor, 201)
    }
    if (url.endsWith('/competitors') && method === 'GET') {
      return json(competitorAdded ? [summary] : [])
    }
    throw new Error(`Unexpected request: ${method} ${url}`)
  }))
  render(<App />)

  fireEvent.change(await screen.findByLabelText('Project name'), { target: { value: project.name } })
  fireEvent.change(screen.getByLabelText('Market'), { target: { value: project.marketName } })
  fireEvent.change(screen.getByLabelText('Target language'), { target: { value: project.targetLanguage } })
  fireEvent.change(screen.getByLabelText('Target geography'), { target: { value: project.targetGeography } })
  fireEvent.change(screen.getByLabelText('Target audience'), { target: { value: project.audienceDescription } })
  fireEvent.click(screen.getByRole('button', { name: 'Create project' }))

  expect(await screen.findByRole('heading', { name: project.name })).toBeVisible()
  expect(await screen.findByText('Add a YouTube channel to begin competitor research.')).toBeVisible()
  fireEvent.change(screen.getByLabelText('YouTube channel URL'), {
    target: { value: 'https://youtube.com/@practicalcreator' },
  })
  fireEvent.click(screen.getByRole('button', { name: 'Add competitor' }))

  expect(await screen.findByText('How to Research a Video')).toBeVisible()
  await waitFor(() => expect(screen.getAllByText('Practical Creator').length).toBeGreaterThan(0))
})

test('ignores an old project response after another project is opened', async () => {
  const otherProject: Project = {
    ...project,
    id: 'project-2',
    name: 'Focused Research',
  }
  const otherSummary: CompetitorSummary = {
    ...summary,
    id: 'competitor-2',
    projectId: otherProject.id,
    youtubeChannelId: 'UCabcdefghijklmnopqrstuv',
    title: 'Focused Creator',
  }
  const otherCompetitor: CompetitorDetails = {
    ...competitor,
    ...otherSummary,
    videos: [{ ...competitor.videos[0], id: 'video-2', title: 'Focused channel video' }],
  }
  const oldProjectResponse = deferred<Response>()
  const fetchMock = vi.fn((input: RequestInfo | URL) => {
    const url = String(input)
    if (url === '/api/projects') return json([project, otherProject])
    if (url === `/api/projects/${project.id}/competitors`) return oldProjectResponse.promise
    if (url === `/api/projects/${otherProject.id}/competitors`) return json([otherSummary])
    if (url === `/api/projects/${otherProject.id}/competitors/${otherSummary.id}`) {
      return json(otherCompetitor)
    }
    if (url === `/api/projects/${project.id}/competitors/${summary.id}`) return json(competitor)
    throw new Error(`Unexpected request: ${url}`)
  })
  vi.stubGlobal('fetch', fetchMock)

  render(<App />)

  fireEvent.click(await screen.findByRole('button', { name: /Creator Research/ }))
  expect(await screen.findByRole('heading', { name: project.name })).toBeVisible()
  fireEvent.click(screen.getByRole('button', { name: 'All projects' }))
  fireEvent.click(await screen.findByRole('button', { name: /Focused Research/ }))
  expect(await screen.findByText('Focused channel video')).toBeVisible()

  await act(async () => {
    oldProjectResponse.resolve(response([summary]))
    await oldProjectResponse.promise
    await Promise.resolve()
  })

  expect(screen.getByRole('heading', { name: otherProject.name })).toBeVisible()
  expect(screen.getByText('Focused channel video')).toBeVisible()
  expect(screen.queryByText('How to Research a Video')).not.toBeInTheDocument()
  expect(fetchMock).not.toHaveBeenCalledWith(
    `/api/projects/${project.id}/competitors/${summary.id}`,
    expect.anything(),
  )
})

test('ignores a collection response after leaving its project', async () => {
  const otherProject: Project = {
    ...project,
    id: 'project-2',
    name: 'Focused Research',
  }
  const pendingCollection = deferred<Response>()
  const fetchMock = vi.fn((input: RequestInfo | URL, init?: RequestInit) => {
    const url = String(input)
    const method = init?.method ?? 'GET'
    if (url === '/api/projects') return json([project, otherProject])
    if (url === `/api/projects/${project.id}/competitors` && method === 'GET') return json([])
    if (url === `/api/projects/${project.id}/competitors` && method === 'POST') {
      return pendingCollection.promise
    }
    if (url === `/api/projects/${otherProject.id}/competitors`) return json([])
    throw new Error(`Unexpected request: ${method} ${url}`)
  })
  vi.stubGlobal('fetch', fetchMock)

  render(<App />)

  fireEvent.click(await screen.findByRole('button', { name: /Creator Research/ }))
  expect(await screen.findByText('Add a YouTube channel to begin competitor research.')).toBeVisible()
  fireEvent.change(screen.getByLabelText('YouTube channel URL'), {
    target: { value: 'https://youtube.com/@practicalcreator' },
  })
  fireEvent.click(screen.getByRole('button', { name: 'Add competitor' }))
  expect(await screen.findByRole('button', { name: 'Collecting channel…' })).toBeDisabled()

  fireEvent.click(screen.getByRole('button', { name: 'All projects' }))
  fireEvent.click(await screen.findByRole('button', { name: /Focused Research/ }))
  expect(await screen.findByRole('heading', { name: otherProject.name })).toBeVisible()

  await act(async () => {
    pendingCollection.resolve(response(competitor, 201))
    await pendingCollection.promise
    await Promise.resolve()
  })

  expect(screen.getByRole('heading', { name: otherProject.name })).toBeVisible()
  expect(screen.queryByText('How to Research a Video')).not.toBeInTheDocument()
  expect(fetchMock).toHaveBeenCalledTimes(4)
})

test('keeps navigation working when local storage is unavailable', async () => {
  vi.spyOn(localStorage, 'setItem').mockImplementation(() => {
    throw new DOMException('Storage is unavailable.', 'SecurityError')
  })
  vi.spyOn(localStorage, 'removeItem').mockImplementation(() => {
    throw new DOMException('Storage is unavailable.', 'SecurityError')
  })
  vi.stubGlobal('fetch', vi.fn((input: RequestInfo | URL) => {
    const url = String(input)
    if (url === '/api/projects') return json([project])
    if (url === `/api/projects/${project.id}/competitors`) return json([])
    throw new Error(`Unexpected request: ${url}`)
  }))

  render(<App />)

  fireEvent.click(await screen.findByRole('button', { name: /Creator Research/ }))
  expect(await screen.findByText('Add a YouTube channel to begin competitor research.')).toBeVisible()
  expect(screen.queryByRole('alert')).not.toBeInTheDocument()
  fireEvent.click(screen.getByRole('button', { name: 'All projects' }))
  expect(await screen.findByRole('heading', { name: 'Create project' })).toBeVisible()
})

test('renders API problem details and allows the user to retry', async () => {
  vi.stubGlobal('fetch', vi.fn((input: RequestInfo | URL, init?: RequestInit) => {
    const url = String(input)
    const method = init?.method ?? 'GET'
    if (url === '/api/projects') return json([project])
    if (url === `/api/projects/${project.id}/competitors` && method === 'GET') return json([])
    if (url === `/api/projects/${project.id}/competitors` && method === 'POST') {
      return json({
        title: 'YouTube quota unavailable',
        detail: 'YouTube quota is temporarily unavailable.',
      }, 429)
    }
    throw new Error(`Unexpected request: ${method} ${url}`)
  }))

  render(<App />)

  fireEvent.click(await screen.findByRole('button', { name: /Creator Research/ }))
  expect(await screen.findByText('Add a YouTube channel to begin competitor research.')).toBeVisible()
  fireEvent.change(screen.getByLabelText('YouTube channel URL'), {
    target: { value: 'https://youtube.com/@practicalcreator' },
  })
  fireEvent.click(screen.getByRole('button', { name: 'Add competitor' }))

  expect(await screen.findByRole('alert')).toHaveTextContent('YouTube quota is temporarily unavailable.')
  expect(screen.getByRole('button', { name: 'Add competitor' })).toBeEnabled()
})

test('renders an explicit empty state and unavailable channel metrics', async () => {
  const emptySummary: CompetitorSummary = {
    ...summary,
    thumbnailUrl: null,
    subscriberCount: null,
    videoCount: null,
    viewCount: null,
  }
  const emptyCompetitor: CompetitorDetails = {
    ...competitor,
    ...emptySummary,
    description: null,
    publishedAt: null,
    videos: [],
  }
  localStorage.setItem('youtube-ai-factory:selected-project', project.id)
  vi.stubGlobal('fetch', vi.fn((input: RequestInfo | URL) => {
    const url = String(input)
    if (url === '/api/projects') return json([project])
    if (url === `/api/projects/${project.id}/competitors`) return json([emptySummary])
    if (url === `/api/projects/${project.id}/competitors/${emptySummary.id}`) {
      return json(emptyCompetitor)
    }
    throw new Error(`Unexpected request: ${url}`)
  }))

  render(<App />)

  expect(await screen.findByText('No recent videos were returned for this channel.')).toBeVisible()
  expect(screen.getByText('0 collected')).toBeVisible()
  expect(screen.getAllByText('Unavailable')).toHaveLength(3)
})

test('queues AI analysis from the competitor detail without auto-generating it', async () => {
  localStorage.setItem('youtube-ai-factory:selected-project', project.id)
  const fetchMock = vi.fn((input: RequestInfo | URL, init?: RequestInit) => {
    const url = String(input)
    const method = init?.method ?? 'GET'
    if (url === '/api/projects') return json([project])
    if (url.endsWith('/competitors')) return json([summary])
    if (url.endsWith('/competitors/competitor-1')) return json(competitor)
    if (url.endsWith('/analysis') && method === 'GET') return json({ latestAnalysis: null, activeJob: null, latestJob: null })
    if (url.endsWith('/analysis:run') && method === 'POST') return json({ jobId: 'job-1', status: 'Queued', existing: false }, 202)
    throw new Error(`Unexpected request: ${method} ${url}`)
  })
  vi.stubGlobal('fetch', fetchMock)

  render(<App />)

  expect(await screen.findByText('No analysis has been generated yet.')).toBeVisible()
  fireEvent.click(screen.getByRole('button', { name: 'Run AI Analysis' }))
  await waitFor(() => expect(fetchMock).toHaveBeenCalledWith(
    `/api/projects/${project.id}/competitors/${summary.id}/analysis:run`,
    expect.objectContaining({ method: 'POST' }),
  ))
})

function json(body: unknown, status = 200) {
  return Promise.resolve(response(body, status))
}

function response(body: unknown, status = 200) {
  return new Response(JSON.stringify(body), {
    status,
    headers: { 'Content-Type': 'application/json' },
  })
}

function deferred<T>() {
  let resolve: (value: T) => void = () => {
    throw new Error('Deferred promise was not initialized.')
  }
  const promise = new Promise<T>((promiseResolve) => {
    resolve = promiseResolve
  })
  return { promise, resolve }
}

export type Project = {
  id: string
  name: string
  marketName: string
  targetLanguage: string
  targetGeography: string
  audienceDescription: string
  createdAt: string
  updatedAt: string | null
}

export type CompetitorSummary = {
  id: string
  projectId: string
  youtubeChannelId: string
  title: string
  handle: string | null
  thumbnailUrl: string | null
  subscriberCount: number | null
  videoCount: number | null
  viewCount: number | null
  lastCollectedAt: string
}

export type CompetitorVideo = {
  id: string
  youtubeVideoId: string
  title: string
  description: string | null
  url: string
  thumbnailUrl: string | null
  duration: string | null
  viewCount: number | null
  likeCount: number | null
  commentCount: number | null
  publishedAt: string | null
  collectedAt: string
}

export type CompetitorDetails = CompetitorSummary & {
  sourceUrl: string
  description: string | null
  publishedAt: string | null
  videos: CompetitorVideo[]
}

export type CreateProjectRequest = {
  name: string
  marketName: string
  targetLanguage: string
  targetGeography: string
  audienceDescription: string
}

type ProblemDetails = {
  title?: string
  detail?: string
}

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(path, {
    ...init,
    headers: {
      'Content-Type': 'application/json',
      ...init?.headers,
    },
  })

  if (!response.ok) {
    const problem = (await response.json().catch(() => ({}))) as ProblemDetails
    throw new Error(problem.detail ?? problem.title ?? `Request failed (${response.status}).`)
  }

  return (await response.json()) as T
}

export const api = {
  listProjects: (signal?: AbortSignal) => request<Project[]>('/api/projects', { signal }),
  createProject: (project: CreateProjectRequest) =>
    request<Project>('/api/projects', {
      method: 'POST',
      body: JSON.stringify(project),
    }),
  listCompetitors: (projectId: string, signal?: AbortSignal) =>
    request<CompetitorSummary[]>(`/api/projects/${projectId}/competitors`, { signal }),
  getCompetitor: (projectId: string, competitorId: string, signal?: AbortSignal) =>
    request<CompetitorDetails>(
      `/api/projects/${projectId}/competitors/${competitorId}`,
      { signal },
    ),
  addCompetitor: (projectId: string, youtubeUrl: string, signal?: AbortSignal) =>
    request<CompetitorDetails>(`/api/projects/${projectId}/competitors`, {
      method: 'POST',
      body: JSON.stringify({ youtubeUrl }),
      signal,
    }),
}

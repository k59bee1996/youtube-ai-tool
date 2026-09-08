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

export type AnalysisJob = { id: string; status: string; failureReason: string | null }
export type AudienceAnalysis = { likelyAgeRange: string | null; likelyInterests: string[]; likelyViewerIntent: string[]; geographyHints: string[]; confidence: number; evidence: string[] }
export type TopicCluster = { name: string; description: string; exampleVideoIds: string[]; frequency: number; performanceSignal: string; confidence: number }
export type TitlePattern = { patternName: string; description: string; template: string; exampleTitles: string[]; observedFrequency: number; performanceSignal: string; confidence: number }
export type EvidencePattern = { patternName: string; observation: string; evidenceVideoIds: string[]; confidence: number; limitations: string[] }
export type ContentFormat = { format: string; evidenceVideoIds: string[]; performanceSignal: string; confidence: number }
export type VideoInsight = { insight?: string; observation?: string; supportingVideoIds: string[]; confidence: number }
export type TransferableFormat = { format: string; whyItMayWork: string; evidenceVideoIds: string[]; transferableMechanic: string; doNotCopy: string; confidence: number }
export type EvidenceNote = { note: string; videoIds: string[] }
export type CompetitorAnalysis = {
  id: string; version: number; promptKey: string; promptVersion: number; provider: string; model: string
  sourceDataAsOf: string; analyzedVideoCount: number; createdAt: string; isStale: boolean
  result: { audience: AudienceAnalysis; topicClusters: TopicCluster[]; titlePatterns: TitlePattern[]; thumbnailPatterns: EvidencePattern[]; hookPatterns: EvidencePattern[]; contentFormats: ContentFormat[]; performanceInsights: VideoInsight[]; potentialWeaknesses: VideoInsight[]; transferableFormats: TransferableFormat[]; evidenceNotes: EvidenceNote[]; confidence: { overallConfidence: number; dataQuality: string; limitations: string[] } }
}
export type CompetitorAnalysisStatus = { latestAnalysis: CompetitorAnalysis | null; activeJob: AnalysisJob | null; latestJob: AnalysisJob | null }
export type AnalysisRun = { jobId: string; status: string; existing: boolean }

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
  getCompetitorAnalysis: (projectId: string, competitorId: string, signal?: AbortSignal) =>
    request<CompetitorAnalysisStatus>(`/api/projects/${projectId}/competitors/${competitorId}/analysis`, { signal }),
  runCompetitorAnalysis: (projectId: string, competitorId: string) =>
    request<AnalysisRun>(`/api/projects/${projectId}/competitors/${competitorId}/analysis:run`, { method: 'POST' }),
}

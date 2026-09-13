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
export type OpportunityEvidence = { id: string; competitorChannelId: string; competitorAnalysisId: string; competitorVideoId: string | null; evidenceId: string; summary: string }
export type OpportunityScores = { observedDemandSignal: number; noveltySignal: number; competitionRiskSignal: number; audienceFitSignal: number; transferabilitySignal: number; evidenceStrength: number; storyPotential: number; productionComplexity: number; overallScore: number }
export type OpportunityCandidate = { id: string; name: string; description: string; audience: string; topic: string; contentFormat: string; angle: string; whyThisOpportunity: string; scores: OpportunityScores; confidence: number; risks: string[]; limitations: string[]; decisionStatus: string; evidence: OpportunityEvidence[] }
export type OpportunityReport = { id: string; version: number; promptKey: string; promptVersion: number; provider: string; model: string; scoringAlgorithmVersion: string; createdAt: string; isStale: boolean; sources: { competitorChannelId: string; competitorAnalysisId: string; competitorAnalysisVersion: number }[]; limitations: string[]; opportunities: OpportunityCandidate[] }
export type OpportunityStatus = { latestReport: OpportunityReport | null; activeJob: AnalysisJob | null; latestJob: AnalysisJob | null; competitorCount: number; analyzedCompetitorCount: number }
export type OpportunityRun = { jobId: string; status: string; existing: boolean }
export type IdeaScores = { opportunityFit: number; observedDemandAlignment: number; novelty: number; titlePotential: number; thumbnailPotential: number; storyPotential: number; audienceFit: number; evidenceStrength: number; productionEase: number; competitionRisk: number; researchRisk: number; duplicationPenalty: number; overallScore: number }
export type IdeaEvidence = { id: string; opportunityEvidenceId: string; summary: string }
export type VideoIdea = { id: string; opportunityId: string; generationId: string; workingTitle: string; topic: string; angle: string; contentFormat: string; targetAudience: string; viewerIntent: string; hookConcept: string; thumbnailConcept: string; viewerPromise: string; coreQuestion: string; whyViewerWouldCare: string; hypothesis: string; scores: IdeaScores; evidence: IdeaEvidence[]; risks: string[]; confidence: number; decisionStatus: string; createdAt: string }
export type IdeaBank = { ideas: VideoIdea[]; generations: { id: string; version: number; opportunityReportVersion: number; createdAt: string; candidateCount: number }[]; latestGeneration: { id: string; version: number; opportunityReportVersion: number; isStale: boolean } | null; activeJobStatus: string | null; latestJobFailureReason: string | null }
export type PilotVideo = { id: string; sequence: number; videoIdeaId: string; opportunityId: string; workingTitle: string; opportunityName: string; overallIdeaScore: number; experimentType: string; hypothesis: string; variableBeingTested: string; controlStrategy: string; primaryMetric: string; successSignal: string; rationale: string; secondaryMetrics: string[]; notes: string | null }
export type Pilot = { id: string; version: number; name: string; objective: string; status: string; createdAt: string; approvedAt: string | null; eligibleIdeaCount: number; promptKey: string; promptVersion: number; provider: string; model: string; planningAlgorithmVersion: string; assumptions: string[]; limitations: string[]; warnings: string[]; requiresReview: boolean; videos: PilotVideo[] }
export type PilotStatus = { latestPilot: Pilot | null; activeJobStatus: string | null; latestJobFailureReason: string | null; eligibleIdeaCount: number; requiredIdeaCount: number }
export type PilotCandidate = { videoIdeaId: string; opportunityId: string; opportunityName: string; workingTitle: string; topic: string; contentFormat: string; overallScore: number }

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
  getOpportunities: (projectId: string, signal?: AbortSignal) =>
    request<OpportunityStatus>(`/api/projects/${projectId}/opportunities/latest`, { signal }),
  generateOpportunities: (projectId: string) =>
    request<OpportunityRun>(`/api/projects/${projectId}/opportunities:generate`, { method: 'POST' }),
  approveOpportunity: (projectId: string, opportunityId: string) => request<OpportunityCandidate>(`/api/projects/${projectId}/opportunities/${opportunityId}:approve`, { method: 'POST' }),
  rejectOpportunity: (projectId: string, opportunityId: string) => request<OpportunityCandidate>(`/api/projects/${projectId}/opportunities/${opportunityId}:reject`, { method: 'POST' }),
  getIdeaBank: (projectId: string, opportunityId: string, signal?: AbortSignal) => request<IdeaBank>(`/api/projects/${projectId}/opportunities/${opportunityId}/ideas`, { signal }),
  generateIdeas: (projectId: string, opportunityId: string) => request<AnalysisRun>(`/api/projects/${projectId}/opportunities/${opportunityId}/ideas:generate`, { method: 'POST' }),
  approveIdea: (projectId: string, ideaId: string) => request<VideoIdea>(`/api/projects/${projectId}/ideas/${ideaId}:approve`, { method: 'POST' }),
  rejectIdea: (projectId: string, ideaId: string) => request<VideoIdea>(`/api/projects/${projectId}/ideas/${ideaId}:reject`, { method: 'POST' }),
  getPilot: (projectId: string, signal?: AbortSignal) => request<PilotStatus>(`/api/projects/${projectId}/pilots/latest`, { signal }),
  listPilots: (projectId: string, signal?: AbortSignal) => request<Pilot[]>(`/api/projects/${projectId}/pilots`, { signal }),
  generatePilot: (projectId: string) => request<AnalysisRun>(`/api/projects/${projectId}/pilots:generate`, { method: 'POST' }),
  approvePilot: (projectId: string, pilotId: string) => request<Pilot>(`/api/projects/${projectId}/pilots/${pilotId}:approve`, { method: 'POST' }),
  getPilotCandidates: (projectId: string) => request<PilotCandidate[]>(`/api/projects/${projectId}/pilots/eligible-ideas`),
  replacePilotSlot: (projectId: string, pilotId: string, sequence: number, videoIdeaId: string) => request<Pilot>(`/api/projects/${projectId}/pilots/${pilotId}/slots/${sequence}:replace`, { method: 'POST', body: JSON.stringify({ videoIdeaId }) }),
  movePilotSlot: (projectId: string, pilotId: string, sequence: number, direction: 'up' | 'down') => request<Pilot>(`/api/projects/${projectId}/pilots/${pilotId}/slots/${sequence}:move`, { method: 'POST', body: JSON.stringify({ direction }) }),
}

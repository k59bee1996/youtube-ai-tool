export type Project = { id: string; name: string; marketName: string; targetLanguage: string; targetGeography: string; audienceDescription: string; createdAt: string; updatedAt: string | null }
export type CreateProjectRequest = Omit<Project, "id" | "createdAt" | "updatedAt">

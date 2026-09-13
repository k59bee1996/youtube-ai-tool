import { useCallback, useEffect, useRef, useState } from "react"
import type { FormEvent } from "react"
import {
  api,
  type CompetitorDetails,
  type CompetitorSummary,
  type CreateProjectRequest,
  type Project,
} from "../../../services/api"
import { messageFrom } from "../../../lib/errors"
import {
  clearSelectedProjectId,
  persistSelectedProjectId,
  readSelectedProjectId,
} from "../utils/selectedProjectStorage"

export function useWorkspace() {
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
  const projectRef = useRef<Project | null>(null)

  const setCurrentProject = useCallback((nextProject: Project | null) => {
    projectRef.current = nextProject
    setProject(nextProject)
  }, [])

  const invalidateProjectRequests = useCallback(() => {
    ++workspaceRequest.current
    ++detailRequest.current
    ++collectionRequest.current
    workspaceAbort.current?.abort()
    detailAbort.current?.abort()
    collectionAbort.current?.abort()
    workspaceAbort.current = null
    detailAbort.current = null
    collectionAbort.current = null
  }, [])

  const isCurrentWorkspace = useCallback((request: number, projectId: string) => (
    mounted.current && workspaceRequest.current === request && selectedProjectId.current === projectId
  ), [])

  const isCurrentDetail = useCallback((workspace: number, projectId: string, request: number | null) => (
    isCurrentWorkspace(workspace, projectId) && (request === null || detailRequest.current === request)
  ), [isCurrentWorkspace])

  const isCurrentCollection = useCallback((workspace: number, projectId: string, request: number) => (
    isCurrentWorkspace(workspace, projectId) && collectionRequest.current === request
  ), [isCurrentWorkspace])

  const openProject = useCallback(async (selected: Project) => {
    invalidateProjectRequests()
    const request = workspaceRequest.current
    const controller = new AbortController()
    workspaceAbort.current = controller
    selectedProjectId.current = selected.id
    setCurrentProject(selected)
    setCompetitors([])
    setCompetitor(null)
    setWorkspaceLoading(true)
    setCollecting(false)
    setError(null)
    persistSelectedProjectId(selected.id)

    let requestedDetail: number | null = null
    try {
      const loadedCompetitors = await api.listCompetitors(selected.id, controller.signal)
      if (!isCurrentWorkspace(request, selected.id)) return

      setCompetitors(loadedCompetitors)
      if (loadedCompetitors[0]) {
        requestedDetail = ++detailRequest.current
        const detailController = new AbortController()
        detailAbort.current = detailController
        const loadedCompetitor = await api.getCompetitor(selected.id, loadedCompetitors[0].id, detailController.signal)
        if (!isCurrentDetail(request, selected.id, requestedDetail)) return
        setCompetitor(loadedCompetitor)
      }
    } catch (requestError) {
      if (isCurrentDetail(request, selected.id, requestedDetail)) setError(messageFrom(requestError))
    } finally {
      if (isCurrentDetail(request, selected.id, requestedDetail)) setWorkspaceLoading(false)
    }
  }, [invalidateProjectRequests, isCurrentDetail, isCurrentWorkspace, setCurrentProject])

  const closeProject = useCallback(() => {
    invalidateProjectRequests()
    selectedProjectId.current = null
    clearSelectedProjectId()
    setCurrentProject(null)
    setCompetitors([])
    setCompetitor(null)
    setWorkspaceLoading(false)
    setCollecting(false)
    setError(null)
  }, [invalidateProjectRequests, setCurrentProject])

  const createProject = useCallback(async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    setCreatingProject(true)
    setError(null)
    const data = new FormData(event.currentTarget)
    const request: CreateProjectRequest = {
      name: String(data.get("name") ?? ""),
      marketName: String(data.get("marketName") ?? ""),
      targetLanguage: String(data.get("targetLanguage") ?? ""),
      targetGeography: String(data.get("targetGeography") ?? ""),
      audienceDescription: String(data.get("audienceDescription") ?? ""),
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
  }, [openProject])

  const addCompetitor = useCallback(async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    const currentProject = projectRef.current
    if (!currentProject) return

    const projectId = currentProject.id
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
    const youtubeUrl = String(new FormData(form).get("youtubeUrl") ?? "")
    try {
      const collected = await api.addCompetitor(projectId, youtubeUrl, controller.signal)
      if (!isCurrentCollection(workspace, projectId, request)) return
      if (detailRequest.current === requestedDetail) setCompetitor(collected)
      form.reset()
      const loadedCompetitors = await api.listCompetitors(projectId, controller.signal)
      if (isCurrentCollection(workspace, projectId, request)) setCompetitors(loadedCompetitors)
    } catch (requestError) {
      if (isCurrentCollection(workspace, projectId, request)) setError(messageFrom(requestError))
    } finally {
      if (isCurrentCollection(workspace, projectId, request)) setCollecting(false)
    }
  }, [isCurrentCollection])

  const selectCompetitor = useCallback(async (selected: CompetitorSummary) => {
    const currentProject = projectRef.current
    if (!currentProject) return

    const projectId = currentProject.id
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
      if (isCurrentDetail(workspace, projectId, request)) setCompetitor(loadedCompetitor)
    } catch (requestError) {
      if (isCurrentDetail(workspace, projectId, request)) setError(messageFrom(requestError))
    } finally {
      if (isCurrentDetail(workspace, projectId, request)) setWorkspaceLoading(false)
    }
  }, [isCurrentDetail])

  useEffect(() => {
    let active = true
    const controller = new AbortController()
    mounted.current = true

    void api.listProjects(controller.signal)
      .then(async (loadedProjects) => {
        if (!active) return
        setProjects(loadedProjects)
        const storedProject = loadedProjects.find((item) => item.id === readSelectedProjectId())
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
  }, [invalidateProjectRequests, openProject])

  return {
    projects, project, competitors, competitor, loading, workspaceLoading,
    creatingProject, collecting, error, openProject, closeProject,
    createProject, addCompetitor, selectCompetitor,
  }
}

const selectedProjectKey = "youtube-ai-factory:selected-project"

export function readSelectedProjectId() {
  try {
    return localStorage.getItem(selectedProjectKey)
  } catch {
    return null
  }
}

export function persistSelectedProjectId(projectId: string) {
  try {
    localStorage.setItem(selectedProjectKey, projectId)
  } catch {
    // Persisted selection is optional; navigation remains available.
  }
}

export function clearSelectedProjectId() {
  try {
    localStorage.removeItem(selectedProjectKey)
  } catch {
    // Persisted selection is optional; navigation remains available.
  }
}

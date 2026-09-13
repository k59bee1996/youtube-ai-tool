import { ProjectHome } from "../../features/workspace/components/ProjectHome"
import { ProjectWorkspace } from "../../features/workspace/components/ProjectWorkspace"
import { useWorkspace } from "../../features/workspace/hooks/useWorkspace"

export function WorkspacePage() {
  const workspace = useWorkspace()

  if (workspace.loading) {
    return <main className="shell loading">Loading workspace…</main>
  }

  return (
    <main className="shell">
      <header className="masthead">
        <div>
          <span className="eyebrow">YouTube AI Factory</span>
          <h1>{workspace.project ? workspace.project.name : "Competitor research starts here."}</h1>
        </div>
        {workspace.project && <button className="quiet-button" type="button" onClick={workspace.closeProject}>All projects</button>}
      </header>
      {workspace.error && <div className="error-banner" role="alert">{workspace.error}</div>}
      {workspace.project ? (
        <ProjectWorkspace
          project={workspace.project}
          competitors={workspace.competitors}
          competitor={workspace.competitor}
          loading={workspace.workspaceLoading}
          collecting={workspace.collecting}
          onAddCompetitor={workspace.addCompetitor}
          onSelectCompetitor={workspace.selectCompetitor}
        />
      ) : (
        <ProjectHome projects={workspace.projects} creating={workspace.creatingProject} onCreate={workspace.createProject} onOpen={workspace.openProject} />
      )}
    </main>
  )
}

import type { FormEvent } from "react"
import type { Project } from "../../../services/api"

type ProjectHomeProps = {
  projects: Project[]
  creating: boolean
  onCreate: (event: FormEvent<HTMLFormElement>) => Promise<void>
  onOpen: (project: Project) => Promise<void>
}

export function ProjectHome({ projects, creating, onCreate, onOpen }: ProjectHomeProps) {
  return (
    <div className="home-grid">
      <section className="panel">
        <h2>Create project</h2>
        <p className="section-copy">Define the market and audience for this research workspace.</p>
        <form className="form-grid" onSubmit={onCreate}>
          <label>Project name<input name="name" required maxLength={200} placeholder="Creator education research" /></label>
          <label>Market<input name="marketName" required maxLength={200} placeholder="Creator education" /></label>
          <label>Target language<input name="targetLanguage" required maxLength={50} placeholder="English" /></label>
          <label>Target geography<input name="targetGeography" required maxLength={100} placeholder="Global" /></label>
          <label className="full-field">Target audience<textarea name="audienceDescription" required maxLength={2000} placeholder="Independent creators building educational channels" /></label>
          <button className="primary-button full-field" disabled={creating}>{creating ? "Creating…" : "Create project"}</button>
        </form>
      </section>
      <section className="panel">
        <h2>Projects</h2>
        {projects.length === 0 ? <p className="empty-state">No projects yet. Create the first one to begin.</p> : (
          <div className="project-list">
            {projects.map((item) => (
              <button type="button" key={item.id} disabled={creating} onClick={() => void onOpen(item)}>
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

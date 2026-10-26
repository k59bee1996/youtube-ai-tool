import { useCallback, useEffect, useState } from "react"
import { api, type AiCostBreakdown, type AiRunExecution, type JobExecution, type ProjectObservabilityOverview, type WorkflowHealth } from "../../services/api"
import { messageFrom } from "../../lib/errors"

function amountText(amounts: { currency: string; amount: number }[]) {
  if (amounts.length === 0) return "No cost recorded"
  return amounts.map((item) => `${new Intl.NumberFormat(undefined, { style: "currency", currency: item.currency }).format(item.amount)} ${item.currency}`).join(" · ")
}

export function ObservabilityDashboard({ projectId }: { projectId: string }) {
  const [overview, setOverview] = useState<ProjectObservabilityOverview | null>(null)
  const [costs, setCosts] = useState<AiCostBreakdown | null>(null)
  const [workflows, setWorkflows] = useState<WorkflowHealth[]>([])
  const [jobs, setJobs] = useState<JobExecution[]>([])
  const [runs, setRuns] = useState<AiRunExecution[]>([])
  const [range, setRange] = useState<"all" | "7" | "30">("30")
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const load = useCallback(async () => {
    setLoading(true)
    try {
      const from = range === "all" ? undefined : new Date(Date.now() - Number(range) * 24 * 60 * 60 * 1000).toISOString()
      const [nextOverview, nextCosts, nextWorkflows, nextJobs, nextRuns] = await Promise.all([
        api.getObservabilityOverview(projectId),
        api.getObservabilityCosts(projectId, { from }),
        api.getObservabilityWorkflows(projectId),
        api.getObservabilityJobs(projectId, undefined, 1, 10),
        api.getObservabilityAiRuns(projectId, undefined, 1, 10),
      ])
      setOverview(nextOverview)
      setCosts(nextCosts)
      setWorkflows(nextWorkflows)
      setJobs(nextJobs.items)
      setRuns(nextRuns.items)
      setError(null)
    } catch (requestError) {
      setError(messageFrom(requestError))
    } finally {
      setLoading(false)
    }
  }, [projectId, range])

  useEffect(() => { void Promise.resolve().then(load) }, [load])

  if (loading) return <section className="panel"><p className="empty-state">Loading observability...</p></section>
  if (error) return <section className="panel"><p className="error-copy" role="alert">{error}</p><button className="quiet-button" type="button" onClick={() => void load()}>Retry</button></section>
  if (!overview || !costs) return <section className="panel"><p className="empty-state">No observability data is available yet.</p></section>

  const hasTelemetry = overview.aiCost.aiRequestCount > 0 || overview.jobs.completedJobCount > 0 || overview.jobs.activeJobCount > 0 || overview.totalVideoProjects > 0
  return <section className="analysis-section panel observability-dashboard">
    <div className="section-heading"><div><span className="eyebrow">Operations</span><h2>Observability</h2><p className="section-copy">Production progress, recorded AI usage, cost coverage, and workflow health. Dashboard reads never start a workflow.</p></div><button className="quiet-button" type="button" onClick={() => void load()}>Refresh</button></div>
    {!hasTelemetry ? <p className="empty-state">No AI usage has been recorded yet. Run a supported workflow to see usage and cost information.</p> : null}
    <div className="section-heading"><div><h3>AI spending</h3><p className="section-copy">Known and estimated cost only; unknown-cost requests remain visible.</p></div><label>Range<select value={range} onChange={(event) => setRange(event.target.value as "all" | "7" | "30")}><option value="7">Last 7 days</option><option value="30">Last 30 days</option><option value="all">All available</option></select></label></div>
    <div className="confidence-grid">
      <div><span>Video Projects</span><strong>{overview.totalVideoProjects}</strong></div>
      <div><span>Active Jobs</span><strong>{overview.jobs.activeJobCount}</strong></div>
      <div><span>Failed Jobs</span><strong>{overview.jobs.failedJobCount}</strong></div>
      <div><span>Known / Estimated AI Cost</span><strong>{amountText(costs.total.knownOrEstimatedCost)}</strong></div>
      <div><span>Unknown-Cost Requests</span><strong>{costs.total.unknownCostRequestCount}</strong></div>
    </div>
    <div className="analysis-block"><h3>Production pipeline</h3><div className="confidence-grid">{overview.pipeline.map((item) => <div key={item.stage}><span>{item.stage}</span><strong>{item.count}</strong></div>)}</div></div>
    <div className="analysis-block"><h3>Cost attribution</h3><div className="analysis-report">
      <article className="pilot-card"><strong>Shared project strategy</strong><p>{amountText(costs.sharedProject.knownOrEstimatedCost)}</p><p className="section-copy">{costs.sharedProject.aiRequestCount} AI requests · {costs.sharedProject.unknownCostRequestCount} unknown cost</p></article>
      <article className="pilot-card"><strong>VideoProject direct</strong><p>{amountText(costs.videoProjectDirect.knownOrEstimatedCost)}</p><p className="section-copy">{costs.videoProjectDirect.aiRequestCount} AI requests · {costs.videoProjectDirect.unknownCostRequestCount} unknown cost</p></article>
    </div></div>
    <div className="analysis-block"><h3>Cost by workflow</h3>{costs.byWorkflow.length === 0 ? <p className="empty-state">No workflow cost data is available.</p> : <div className="analysis-report">{costs.byWorkflow.map((item) => <article className="pilot-card" key={item.key}><div className="section-heading"><strong>{item.key}</strong><span>{amountText(item.knownOrEstimatedCost)}</span></div><p className="section-copy">{item.aiRequestCount} requests · {item.unknownCostRequestCount} unknown cost</p></article>)}</div>}</div>
    <div className="analysis-block"><h3>AI cost trend</h3>{costs.dailyTrend.length === 0 ? <p className="empty-state">No cost data is available for this range.</p> : <div className="analysis-report">{costs.dailyTrend.map((item) => <article className="pilot-card" key={item.date}><div className="section-heading"><strong>{item.date}</strong><span>{amountText(item.knownOrEstimatedCost)}</span></div><p className="section-copy">{item.unknownCostRequestCount} unknown-cost requests; chart shows known/estimated amounts only.</p></article>)}</div>}</div>
    <div className="analysis-block"><h3>Workflow health</h3>{workflows.length === 0 ? <p className="empty-state">No jobs have been recorded yet.</p> : <div className="analysis-report">{workflows.map((item) => <article className="pilot-card" key={item.workflow}><div className="section-heading"><strong>{item.workflow}</strong><span>{item.failureRate === null ? "No terminal sample" : `${Math.round(item.failureRate * 100)}% failure rate`}</span></div><p className="section-copy">{item.completedJobs} completed · {item.failedJobs} failed · {item.activeJobs} active · {item.retriedJobCount} retried · {item.aiRequestCount} AI requests</p></article>)}</div>}</div>
    <div className="analysis-block"><h3>Recent activity</h3>{overview.recentActivity.length === 0 ? <p className="empty-state">No recent job or AI execution activity.</p> : <div className="analysis-report">{overview.recentActivity.map((item) => <article className="pilot-card" key={`${item.kind}-${item.id}`}><div className="section-heading"><strong>{item.workflow}</strong><span>{item.status}</span></div><p className="section-copy">{item.kind} · {new Date(item.occurredAt).toLocaleString()}{item.failureReason ? ` · ${item.failureReason}` : ""}</p></article>)}</div>}</div>
    <div className="analysis-block"><h3>Execution history</h3>{runs.length === 0 && jobs.length === 0 ? <p className="empty-state">No paginated execution history is available.</p> : <div className="analysis-report">{runs.slice(0, 10).map((item) => <article className="pilot-card" key={item.id}><div className="section-heading"><strong>{item.workflowStage ? `${item.workflow} · ${item.workflowStage}` : item.workflow}</strong><span>{item.status}</span></div><p className="section-copy">{item.provider} / {item.model} · {item.modelProfile} · {item.cost === null ? "Cost unavailable" : `${item.currency} ${item.cost.toFixed(6)} (${item.costSource})`} · {item.inputTokens ?? "?"} input / {item.outputTokens ?? "?"} output tokens</p></article>)}</div>}</div>
  </section>
}

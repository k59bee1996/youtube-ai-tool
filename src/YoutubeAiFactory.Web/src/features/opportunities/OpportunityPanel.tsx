import { useCallback, useEffect, useState } from "react";
import { api } from "../../services/api";
import { messageFrom } from "../../lib/errors";
import type { OpportunityCandidate, OpportunityReport, OpportunityStatus, IdeaBank } from "./types";

export function OpportunityPanel({ projectId }: { projectId: string }) {
  const [status, setStatus] = useState<
    OpportunityStatus | null
  >(null);
  const [loading, setLoading] = useState(true);
  const [running, setRunning] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const active =
    status?.activeJob?.status === "Queued" ||
    status?.activeJob?.status === "Running" ||
    status?.activeJob?.status === "Retrying";
  const load = useCallback(async () => {
    try {
      setStatus(await api.getOpportunities(projectId));
      setError(null);
    } catch (requestError) {
      setError(messageFrom(requestError));
    } finally {
      setLoading(false);
    }
  }, [projectId]);
  useEffect(() => {
    void Promise.resolve().then(load);
    return undefined;
  }, [load]);
  useEffect(() => {
    if (!active) return undefined;
    const timer = window.setInterval(() => void load(), 2000);
    return () => window.clearInterval(timer);
  }, [active, load]);
  async function generate() {
    setRunning(true);
    setError(null);
    try {
      await api.generateOpportunities(projectId);
      await load();
    } catch (requestError) {
      setError(messageFrom(requestError));
    } finally {
      setRunning(false);
    }
  }
  return (
    <section className="analysis-section panel">
      <div className="section-heading">
        <div>
          <span className="eyebrow">Research</span>
          <h2>Opportunities</h2>
        </div>
        {status?.latestReport && (
          <span>Report v{status.latestReport.version}</span>
        )}
      </div>
      {loading ? (
        <p className="empty-state">Loading opportunities…</p>
      ) : error ? (
        <p className="error-copy" role="alert">
          {error}
        </p>
      ) : status && status.analyzedCompetitorCount === 0 ? (
        <div className="analysis-state">
          <p>
            No opportunity analysis exists. Analyze at least one competitor
            first.
          </p>
        </div>
      ) : active ? (
        <div className="analysis-state">
          <strong>Generating opportunities…</strong>
          <span>Status: {status?.activeJob?.status}</span>
        </div>
      ) : status?.latestJob?.status === "Failed" ? (
        <div className="analysis-state">
          <p role="alert">
            {status.latestJob.failureReason ?? "Opportunity generation failed."}
          </p>
          <button
            className="primary-button"
            type="button"
            onClick={() => void generate()}
            disabled={running}
          >
            Generate opportunities
          </button>
        </div>
      ) : status?.latestReport ? (
        <OpportunityReportView
          projectId={projectId}
          report={status.latestReport}
          onChanged={load}
        />
      ) : (
        <div className="analysis-state">
          <p>
            {status && status.analyzedCompetitorCount < status.competitorCount
              ? `${status.competitorCount - status.analyzedCompetitorCount} of ${status.competitorCount} competitors have not been analyzed. Results use the completed analyses only.`
              : "Generate an evidence-backed report from your completed competitor analyses."}
          </p>
          <button
            className="primary-button"
            type="button"
            onClick={() => void generate()}
            disabled={running}
          >
            {running ? "Queuing opportunities…" : "Generate opportunities"}
          </button>
        </div>
      )}
    </section>
  );
}

function OpportunityReportView({
  projectId,
  report,
  onChanged,
}: {
  projectId: string;
  report: OpportunityReport;
  onChanged: () => Promise<void>;
}) {
  return (
    <div className="analysis-report">
      {report.isStale && (
        <p className="stale-note">
          This report may be outdated because a source competitor analysis has a
          newer version.
        </p>
      )}
      {report.limitations.map((item) => (
        <p className="stale-note" key={item}>
          {item}
        </p>
      ))}
      {report.opportunities.map((item, index) => (
        <article className="analysis-block" key={item.id}>
          <div className="section-heading">
            <h3>
              #{index + 1} {item.name}
            </h3>
            <strong>{item.scores.overallScore}/100</strong>
          </div>
          <p>{item.description}</p>
          <p>
            <strong>Audience:</strong> {item.audience} · <strong>Topic:</strong>{" "}
            {item.topic} · <strong>Format:</strong> {item.contentFormat}
          </p>
          <p>
            <strong>Angle:</strong> {item.angle}
          </p>
          <p>{item.whyThisOpportunity}</p>
          <div className="confidence-grid">
            <div>
              <span>Observed demand</span>
              <strong>{item.scores.observedDemandSignal}</strong>
            </div>
            <div>
              <span>Novelty</span>
              <strong>{item.scores.noveltySignal}</strong>
            </div>
            <div>
              <span>Audience fit</span>
              <strong>{item.scores.audienceFitSignal}</strong>
            </div>
            <div>
              <span>Competition risk</span>
              <strong>{item.scores.competitionRiskSignal}</strong>
            </div>
            <div>
              <span>Evidence strength</span>
              <strong>{item.scores.evidenceStrength}</strong>
            </div>
            <div>
              <span>Confidence</span>
              <strong>{item.confidence}%</strong>
            </div>
          </div>
          <p>
            <strong>Evidence:</strong>{" "}
            {item.evidence.map((evidence) => evidence.summary).join(" · ")}
          </p>
          {item.risks.length > 0 && (
            <p>
              <strong>Risks:</strong> {item.risks.join(" · ")}
            </p>
          )}
          {item.limitations.length > 0 && (
            <p>
              <strong>Limitations:</strong> {item.limitations.join(" · ")}
            </p>
          )}
          <OpportunityActions
            projectId={projectId}
            opportunity={item}
            onChanged={onChanged}
          />
          {item.decisionStatus === "Approved" && (
            <IdeaBank projectId={projectId} opportunityId={item.id} />
          )}
        </article>
      ))}
    </div>
  );
}

function OpportunityActions({
  projectId,
  opportunity,
  onChanged,
}: {
  projectId: string;
  opportunity: OpportunityCandidate;
  onChanged: () => Promise<void>;
}) {
  const [busy, setBusy] = useState(false);
  async function setDecision(decision: "Approved" | "Rejected") {
    setBusy(true);
    try {
      if (decision === "Approved")
        await api.approveOpportunity(projectId, opportunity.id);
      else await api.rejectOpportunity(projectId, opportunity.id);
      await onChanged();
    } finally {
      setBusy(false);
    }
  }
  return (
    <div className="idea-actions">
      <strong>Status: {opportunity.decisionStatus}</strong>
      <button
        className="quiet-button"
        type="button"
        onClick={() => void setDecision("Approved")}
        disabled={busy}
      >
        Approve opportunity
      </button>
      <button
        className="quiet-button"
        type="button"
        onClick={() => void setDecision("Rejected")}
        disabled={busy}
      >
        Reject opportunity
      </button>
    </div>
  );
}

function IdeaBank({
  projectId,
  opportunityId,
}: {
  projectId: string;
  opportunityId: string;
}) {
  const [bank, setBank] = useState<IdeaBank | null>(null);
  const [loading, setLoading] = useState(true);
  const [running, setRunning] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [sort, setSort] = useState<
    | "overallScore"
    | "novelty"
    | "thumbnailPotential"
    | "storyPotential"
    | "competitionRisk"
    | "evidenceStrength"
  >("overallScore");
  const [statusFilter, setStatusFilter] = useState<
    "All" | "Candidate" | "Approved" | "Rejected"
  >("All");
  const [selected, setSelected] = useState<string | null>(null);
  const load = useCallback(async () => {
    try {
      setBank(await api.getIdeaBank(projectId, opportunityId));
      setError(null);
    } catch (requestError) {
      setError(messageFrom(requestError));
    } finally {
      setLoading(false);
    }
  }, [projectId, opportunityId]);
  useEffect(() => {
    void Promise.resolve().then(load);
    return undefined;
  }, [load]);
  useEffect(() => {
    if (!bank?.activeJobStatus) return undefined;
    const timer = window.setInterval(() => void load(), 2000);
    return () => window.clearInterval(timer);
  }, [bank?.activeJobStatus, load]);
  async function generate() {
    setRunning(true);
    try {
      await api.generateIdeas(projectId, opportunityId);
      await load();
    } catch (requestError) {
      setError(messageFrom(requestError));
    } finally {
      setRunning(false);
    }
  }
  async function decide(ideaId: string, approve: boolean) {
    try {
      if (approve) await api.approveIdea(projectId, ideaId);
      else await api.rejectIdea(projectId, ideaId);
      await load();
    } catch (requestError) {
      setError(messageFrom(requestError));
    }
  }
  const ideas = [...(bank?.ideas ?? [])]
    .filter(
      (item) => statusFilter === "All" || item.decisionStatus === statusFilter,
    )
    .sort((a, b) =>
      sort === "competitionRisk"
        ? a.scores[sort] - b.scores[sort]
        : b.scores[sort] - a.scores[sort],
    );
  const detail = ideas.find((item) => item.id === selected);
  return (
    <section className="idea-bank">
      <div className="section-heading">
        <div>
          <span className="eyebrow">Idea Bank</span>
          <h3>Ranked video concepts</h3>
        </div>
        <button
          className="primary-button"
          type="button"
          onClick={() => void generate()}
          disabled={running || Boolean(bank?.activeJobStatus)}
        >
          {running || bank?.activeJobStatus
            ? "Generating ideas…"
            : "Generate ideas"}
        </button>
      </div>
      {loading ? (
        <p className="empty-state">Loading Idea Bank…</p>
      ) : error ? (
        <p className="error-copy" role="alert">
          {error}
        </p>
      ) : (
        <>
          <p className="section-copy">
            Ideas are persisted proposals; generation is never triggered by this
            page loading.
          </p>
          {bank?.latestGeneration?.isStale && (
            <p className="stale-note">
              This generation is based on an older opportunity report.
            </p>
          )}
          <div className="idea-filters">
            <label className="idea-sort">
              Sort ideas{" "}
              <select
                value={sort}
                onChange={(event) => setSort(event.target.value as typeof sort)}
              >
                <option value="overallScore">Overall score</option>
                <option value="novelty">Novelty</option>
                <option value="thumbnailPotential">Thumbnail potential</option>
                <option value="storyPotential">Story potential</option>
                <option value="competitionRisk">Competition risk</option>
                <option value="evidenceStrength">Evidence strength</option>
              </select>
            </label>
            <label className="idea-sort">
              Status{" "}
              <select
                value={statusFilter}
                onChange={(event) =>
                  setStatusFilter(event.target.value as typeof statusFilter)
                }
              >
                <option value="All">All</option>
                <option value="Candidate">Candidate</option>
                <option value="Approved">Approved</option>
                <option value="Rejected">Rejected</option>
              </select>
            </label>
          </div>
          {ideas.length === 0 ? (
            <p className="empty-state">No ideas match this filter.</p>
          ) : (
            <div className="idea-list">
              {ideas.map((item) => (
                <article className="idea-card" key={item.id}>
                  <button
                    className="idea-title"
                    type="button"
                    onClick={() => setSelected(item.id)}
                  >
                    {item.workingTitle}
                  </button>
                  <p>
                    {item.topic} · {item.angle} · {item.contentFormat}
                  </p>
                  <div className="confidence-grid">
                    <div>
                      <span>Overall</span>
                      <strong>{item.scores.overallScore}</strong>
                    </div>
                    <div>
                      <span>Novelty</span>
                      <strong>{item.scores.novelty}</strong>
                    </div>
                    <div>
                      <span>Thumbnail</span>
                      <strong>{item.scores.thumbnailPotential}</strong>
                    </div>
                    <div>
                      <span>Story</span>
                      <strong>{item.scores.storyPotential}</strong>
                    </div>
                    <div>
                      <span>Evidence</span>
                      <strong>{item.scores.evidenceStrength}</strong>
                    </div>
                    <div>
                      <span>Competition risk</span>
                      <strong>{item.scores.competitionRisk}</strong>
                    </div>
                  </div>
                  <div className="idea-actions">
                    <strong>{item.decisionStatus}</strong>
                    <button
                      className="quiet-button"
                      type="button"
                      onClick={() => void decide(item.id, true)}
                    >
                      Approve
                    </button>
                    <button
                      className="quiet-button"
                      type="button"
                      onClick={() => void decide(item.id, false)}
                    >
                      Reject
                    </button>
                  </div>
                </article>
              ))}
            </div>
          )}
          {detail && (
            <article className="idea-detail">
              <button
                className="quiet-button"
                type="button"
                onClick={() => setSelected(null)}
              >
                Close detail
              </button>
              <h3>{detail.workingTitle}</h3>
              <p>
                <strong>Audience:</strong> {detail.targetAudience} ·{" "}
                {detail.viewerIntent}
              </p>
              <p>
                <strong>Core question:</strong> {detail.coreQuestion}
              </p>
              <p>
                <strong>Viewer promise:</strong> {detail.viewerPromise}
              </p>
              <p>
                <strong>Hook:</strong> {detail.hookConcept}
              </p>
              <p>
                <strong>Thumbnail concept:</strong> {detail.thumbnailConcept}
              </p>
              <p>
                <strong>Hypothesis:</strong> {detail.hypothesis}
              </p>
              <p>
                <strong>Why it fits:</strong> {detail.whyViewerWouldCare}
              </p>
              <p>
                <strong>Evidence:</strong>{" "}
                {detail.evidence.map((e) => e.summary).join(" · ")}
              </p>
              <p>
                <strong>Risks:</strong>{" "}
                {detail.risks.join(" · ") || "None recorded"}
              </p>
            </article>
          )}
        </>
      )}
    </section>
  );
}



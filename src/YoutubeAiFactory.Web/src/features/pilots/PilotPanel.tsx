import { useCallback, useEffect, useState } from "react";
import { api } from "../../services/api";
import { messageFrom } from "../../lib/errors";
import type { Pilot, PilotCandidate, PilotStatus, PilotVideo } from "./types";

export function PilotPanel({ projectId }: { projectId: string }) {
  const [status, setStatus] = useState<PilotStatus | null>(
    null,
  );
  const [candidates, setCandidates] = useState<
    PilotCandidate[]
  >([]);
  const [pilots, setPilots] = useState<Pilot[]>([]);
  const [selectedPilotId, setSelectedPilotId] = useState<string | null>(null);
  const [followLatest, setFollowLatest] = useState(true);
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const active = Boolean(status?.activeJobStatus);
  const load = useCallback(async () => {
    try {
      const [loadedStatus, loadedCandidates, loadedPilots] = await Promise.all([
        api.getPilot(projectId),
        api.getPilotCandidates(projectId),
        api.listPilots(projectId),
      ]);
      setStatus(loadedStatus);
      setCandidates(loadedCandidates);
      setPilots(loadedPilots);
      setSelectedPilotId((current) =>
        followLatest ||
        !current ||
        !loadedPilots.some((pilot) => pilot.id === current)
          ? (loadedStatus.latestPilot?.id ?? null)
          : current,
      );
      setError(null);
    } catch (requestError) {
      setError(messageFrom(requestError));
    } finally {
      setLoading(false);
    }
  }, [followLatest, projectId]);
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
    setBusy(true);
    setFollowLatest(true);
    try {
      await api.generatePilot(projectId);
      await load();
    } catch (requestError) {
      setError(messageFrom(requestError));
    } finally {
      setBusy(false);
    }
  }
  async function approve() {
    if (!selectedPilotId) return;
    setBusy(true);
    try {
      await api.approvePilot(projectId, selectedPilotId);
      await load();
    } catch (requestError) {
      setError(messageFrom(requestError));
    } finally {
      setBusy(false);
    }
  }
  async function move(sequence: number, direction: "up" | "down") {
    if (!selectedPilotId) return;
    setBusy(true);
    try {
      await api.movePilotSlot(projectId, selectedPilotId, sequence, direction);
      await load();
    } catch (requestError) {
      setError(messageFrom(requestError));
    } finally {
      setBusy(false);
    }
  }
  async function replace(sequence: number, videoIdeaId: string) {
    if (!selectedPilotId || !videoIdeaId) return;
    setBusy(true);
    try {
      await api.replacePilotSlot(
        projectId,
        selectedPilotId,
        sequence,
        videoIdeaId,
      );
      await load();
    } catch (requestError) {
      setError(messageFrom(requestError));
    } finally {
      setBusy(false);
    }
  }
  const pilot =
    pilots.find((item) => item.id === selectedPilotId) ?? status?.latestPilot;
  const failure = status?.latestJobFailureReason;
  return (
    <section className="analysis-section panel pilot-panel">
      <div className="section-heading">
        <div>
          <span className="eyebrow">Validation</span>
          <h2>12-Video Pilot</h2>
        </div>
        {pilot && (
          <span>
            Version {pilot.version} · {pilot.status}
          </span>
        )}
      </div>
      {loading ? (
        <p className="empty-state">Loading pilot…</p>
      ) : error ? (
        <p className="error-copy" role="alert">
          {error}
        </p>
      ) : active ? (
        <div className="analysis-state">
          <strong>Generating 12-video pilot…</strong>
          <span>Status: {status?.activeJobStatus}</span>
        </div>
      ) : !pilot ? (
        <div className="analysis-state">
          {failure && (
            <p className="error-copy" role="alert">
              {failure}
            </p>
          )}
          <p>
            {status?.eligibleIdeaCount ?? 0} approved ideas available. At least
            12 are required to create a complete pilot.
          </p>
          <button
            className="primary-button"
            type="button"
            onClick={() => void generate()}
            disabled={busy || (status?.eligibleIdeaCount ?? 0) < 12}
          >
            {busy
              ? "Queuing pilot…"
              : failure
                ? "Retry Pilot Generation"
                : "Generate 12-Video Pilot"}
          </button>
        </div>
      ) : (
        <>
          {failure && (
            <div className="analysis-state">
              <p className="error-copy" role="alert">
                {failure}
              </p>
              <button
                className="primary-button"
                type="button"
                onClick={() => void generate()}
                disabled={busy}
              >
                {busy ? "Queuing pilot…" : "Retry Pilot Generation"}
              </button>
            </div>
          )}
          <PilotView
            pilot={pilot}
            candidates={candidates}
            onApprove={approve}
            onMove={move}
            onReplace={replace}
            busy={busy}
          />
        </>
      )}
      {pilot && <PilotMetadata pilot={pilot} />}
      {pilot && (
        <div className="idea-actions">
          {pilots.length > 1 && (
            <label className="idea-sort">
              View version{" "}
              <select
                value={pilot.id}
                onChange={(event) => {
                  setFollowLatest(
                    event.target.value === status?.latestPilot?.id,
                  );
                  setSelectedPilotId(event.target.value);
                }}
              >
                {pilots.map((item) => (
                  <option value={item.id} key={item.id}>
                    Version {item.version} - {item.status}
                  </option>
                ))}
              </select>
            </label>
          )}
          <button
            className="primary-button"
            type="button"
            onClick={() => void generate()}
            disabled={busy || active || (status?.eligibleIdeaCount ?? 0) < 12}
          >
            {busy ? "Queuing pilot..." : "Generate new pilot version"}
          </button>
        </div>
      )}
    </section>
  );
}

function PilotMetadata({ pilot }: { pilot: Pilot }) {
  if (pilot.assumptions.length === 0 && pilot.limitations.length === 0)
    return null;
  return (
    <section className="analysis-block">
      <h3>Planning assumptions and limitations</h3>
      {pilot.assumptions.length > 0 && (
        <>
          <strong>Assumptions</strong>
          <ul>
            {pilot.assumptions.map((item) => (
              <li key={item}>{item}</li>
            ))}
          </ul>
        </>
      )}
      {pilot.limitations.length > 0 && (
        <>
          <strong>Limitations</strong>
          <ul>
            {pilot.limitations.map((item) => (
              <li key={item}>{item}</li>
            ))}
          </ul>
        </>
      )}
    </section>
  );
}

function PilotView({
  pilot,
  candidates,
  onApprove,
  onMove,
  onReplace,
  busy,
}: {
  pilot: Pilot;
  candidates: PilotCandidate[];
  onApprove: () => Promise<void>;
  onMove: (sequence: number, direction: "up" | "down") => Promise<void>;
  onReplace: (sequence: number, videoIdeaId: string) => Promise<void>;
  busy: boolean;
}) {
  const groups: Array<[string, PilotVideo[]]> = [
    [
      "1–4 Topic Tests",
      pilot.videos.filter((x) => x.experimentType === "Topic"),
    ],
    [
      "5–8 Packaging Tests",
      pilot.videos.filter((x) => x.experimentType === "Packaging"),
    ],
    [
      "9–12 Storytelling Tests",
      pilot.videos.filter((x) => x.experimentType === "Storytelling"),
    ],
  ];
  const used = new Set(pilot.videos.map((video) => video.videoIdeaId));
  return (
    <div className="analysis-report">
      <p>{pilot.objective}</p>
      <div className="confidence-grid">
        <div>
          <span>Videos</span>
          <strong>12</strong>
        </div>
        <div>
          <span>Eligible ideas</span>
          <strong>{pilot.eligibleIdeaCount}</strong>
        </div>
        <div>
          <span>Source opportunities</span>
          <strong>
            {new Set(pilot.videos.map((x) => x.opportunityId)).size}
          </strong>
        </div>
      </div>
      {pilot.requiresReview && (
        <p className="stale-note">
          This pilot requires review because an included idea is no longer
          approved.
        </p>
      )}
      {pilot.warnings.map((warning) => (
        <p className="stale-note" key={warning}>
          {warning}
        </p>
      ))}
      {groups.map(([heading, videos]) => (
        <section className="pilot-block" key={heading}>
          <h3>{heading}</h3>
          {videos.map((video) => (
            <article className="pilot-card" key={video.id}>
              <div className="section-heading">
                <strong>
                  {String(video.sequence).padStart(2, "0")} {video.workingTitle}
                </strong>
                <span>{video.experimentType}</span>
              </div>
              <p>
                {video.opportunityName} · Idea score {video.overallIdeaScore}
              </p>
              <p>
                <strong>Hypothesis:</strong> {video.hypothesis}
              </p>
              <p>
                <strong>Variable:</strong> {video.variableBeingTested}
              </p>
              <p>
                <strong>Primary metric:</strong> {video.primaryMetric}
              </p>
              <p>
                <strong>Success signal:</strong> {video.successSignal}
              </p>
              <details>
                <summary>Experiment detail</summary>
                <p>
                  <strong>Control:</strong> {video.controlStrategy}
                </p>
                <p>
                  <strong>Rationale:</strong> {video.rationale}
                </p>
              </details>
              {pilot.status === "Draft" && (
                <div className="idea-actions">
                  <button
                    className="quiet-button"
                    type="button"
                    onClick={() => void onMove(video.sequence, "up")}
                    disabled={
                      busy ||
                      video.sequence === 1 ||
                      video.sequence === 5 ||
                      video.sequence === 9
                    }
                  >
                    Move up
                  </button>
                  <button
                    className="quiet-button"
                    type="button"
                    onClick={() => void onMove(video.sequence, "down")}
                    disabled={
                      busy ||
                      video.sequence === 4 ||
                      video.sequence === 8 ||
                      video.sequence === 12
                    }
                  >
                    Move down
                  </button>
                  <label className="idea-sort">
                    Replace{" "}
                    <select
                      defaultValue=""
                      disabled={busy}
                      onChange={(event) => {
                        void onReplace(video.sequence, event.target.value);
                        event.currentTarget.value = "";
                      }}
                    >
                      <option value="">Choose approved idea</option>
                      {candidates
                        .filter((candidate) => !used.has(candidate.videoIdeaId))
                        .map((candidate) => (
                          <option
                            value={candidate.videoIdeaId}
                            key={candidate.videoIdeaId}
                          >
                            {candidate.workingTitle} · {candidate.overallScore}
                          </option>
                        ))}
                    </select>
                  </label>
                </div>
              )}
            </article>
          ))}
        </section>
      ))}
      {pilot.status === "Draft" && (
        <button
          className="primary-button"
          type="button"
          onClick={() => void onApprove()}
          disabled={busy || pilot.requiresReview}
        >
          {busy ? "Approving…" : "Approve Pilot"}
        </button>
      )}
    </div>
  );
}


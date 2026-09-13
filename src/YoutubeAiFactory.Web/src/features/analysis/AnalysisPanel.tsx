import { useCallback, useEffect, useState } from "react";
import { api } from "../../services/api";
import { messageFrom } from "../../lib/errors";
import type { CompetitorAnalysis, CompetitorAnalysisStatus, CompetitorVideo } from "../competitors/types";

export function AnalysisPanel({
  projectId,
  competitor,
}: {
  projectId: string;
  competitor: { id: string; videos: CompetitorVideo[] };
}) {
  const [status, setStatus] = useState<CompetitorAnalysisStatus | null>(null);
  const [loading, setLoading] = useState(true);
  const [running, setRunning] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const active =
    status?.activeJob?.status === "Queued" ||
    status?.activeJob?.status === "Running" ||
    status?.activeJob?.status === "Retrying";

  const load = useCallback(async () => {
    try {
      setStatus(await api.getCompetitorAnalysis(projectId, competitor.id));
      setError(null);
    } catch (requestError) {
      setError(messageFrom(requestError));
    } finally {
      setLoading(false);
    }
  }, [projectId, competitor.id]);

  useEffect(() => {
    void Promise.resolve().then(load);
    return undefined;
  }, [load]);

  useEffect(() => {
    if (!active) return undefined;
    const timer = window.setInterval(() => void load(), 2000);
    return () => window.clearInterval(timer);
  }, [active, load]);

  async function run() {
    setRunning(true);
    setError(null);
    try {
      await api.runCompetitorAnalysis(projectId, competitor.id);
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
          <span className="eyebrow">AI Analysis</span>
          <h2>Competitor intelligence</h2>
        </div>
        {status?.latestAnalysis && (
          <span>Version {status.latestAnalysis.version}</span>
        )}
      </div>
      {loading ? (
        <p className="empty-state">Loading analysis…</p>
      ) : error ? (
        <>
          <p className="error-copy" role="alert">
            {error}
          </p>
          <button
            className="primary-button"
            type="button"
            onClick={() => void load()}
          >
            Retry
          </button>
        </>
      ) : active ? (
        <div className="analysis-state">
          <strong>Analyzing competitor…</strong>
          <span>Status: {status?.activeJob?.status}</span>
        </div>
      ) : status?.latestJob?.status === "Failed" ? (
        <div className="analysis-state">
          <p role="alert">
            {status.latestJob.failureReason ??
              "Analysis failed. You can retry safely."}
          </p>
          <button
            className="primary-button"
            type="button"
            onClick={() => void run()}
            disabled={running}
          >
            Run AI Analysis
          </button>
        </div>
      ) : status?.latestAnalysis ? (
        <AnalysisReport
          analysis={status.latestAnalysis}
          videos={competitor.videos}
        />
      ) : (
        <div className="analysis-state">
          <p>No analysis has been generated yet.</p>
          <button
            className="primary-button"
            type="button"
            onClick={() => void run()}
            disabled={running}
          >
            {running ? "Queuing analysis…" : "Run AI Analysis"}
          </button>
        </div>
      )}
    </section>
  );
}

function AnalysisBlock({ title, children }: { title: string; children: React.ReactNode }) {
  return <section className="analysis-block"><h3>{title}</h3>{children}</section>
}

function Evidence({ names }: { names: string[] }) {
  return names.length ? <small className="evidence">Observed in: {names.join(" · ")}</small> : null
}

function AnalysisReport({
  analysis,
  videos,
}: {
  analysis: CompetitorAnalysis;
  videos: CompetitorVideo[];
}) {
  const result = analysis.result;
  const names = (ids: string[]) =>
    ids
      .map((id) => videos.find((video) => video.id === id)?.title)
      .filter((title): title is string => Boolean(title));
  return (
    <div className="analysis-report">
      {analysis.isStale && (
        <p className="stale-note">
          This analysis may be outdated because the competitor data was
          refreshed after it ran.
        </p>
      )}
      <div className="confidence-grid">
        <div>
          <span>Overall confidence</span>
          <strong>{result.confidence.overallConfidence}%</strong>
        </div>
        <div>
          <span>Data quality</span>
          <strong>{result.confidence.dataQuality}</strong>
        </div>
        <div>
          <span>Evidence window</span>
          <strong>{analysis.analyzedVideoCount} videos</strong>
        </div>
      </div>
      <AnalysisBlock title="Target audience">
        <p>
          {result.audience.likelyAgeRange &&
            `Likely age: ${result.audience.likelyAgeRange}`}
        </p>
        <p>{result.audience.likelyInterests.join(" · ")}</p>
        <p>{result.audience.likelyViewerIntent.join(" · ")}</p>
      </AnalysisBlock>
      <AnalysisBlock title="Topic clusters">
        {result.topicClusters.map((item) => (
          <article key={item.name}>
            <strong>{item.name}</strong>
            <p>{item.description}</p>
            <small>
              {item.frequency} observed · {item.performanceSignal} ·{" "}
              {item.confidence}% confidence
            </small>
            <Evidence names={names(item.exampleVideoIds)} />
          </article>
        ))}
      </AnalysisBlock>
      <AnalysisBlock title="Winning title patterns">
        {result.titlePatterns.map((item) => (
          <article key={item.patternName}>
            <strong>{item.patternName}</strong>
            <p>{item.description}</p>
            <code>{item.template}</code>
            <p>{item.exampleTitles.join(" · ")}</p>
            <small>
              {item.observedFrequency} observed · {item.performanceSignal}
            </small>
          </article>
        ))}
      </AnalysisBlock>
      <AnalysisBlock title="Thumbnail patterns">
        {result.thumbnailPatterns.map((item) => (
          <article key={item.patternName}>
            <strong>{item.patternName}</strong>
            <p>{item.observation}</p>
            <small>
              {item.confidence}% confidence · {item.limitations.join(" ")}
            </small>
            <Evidence names={names(item.evidenceVideoIds)} />
          </article>
        ))}
      </AnalysisBlock>
      <AnalysisBlock title="Hook patterns">
        {result.hookPatterns.map((item) => (
          <article key={item.patternName}>
            <strong>{item.patternName}</strong>
            <p>{item.observation}</p>
            <small>
              {item.confidence}% confidence · {item.limitations.join(" ")}
            </small>
            <Evidence names={names(item.evidenceVideoIds)} />
          </article>
        ))}
      </AnalysisBlock>
      <AnalysisBlock title="Content formats">
        {result.contentFormats.map((item) => (
          <article key={item.format}>
            <strong>{item.format}</strong>
            <p>
              {item.performanceSignal} · {item.confidence}% confidence
            </p>
            <Evidence names={names(item.evidenceVideoIds)} />
          </article>
        ))}
      </AnalysisBlock>
      <AnalysisBlock title="Performance patterns">
        {result.performanceInsights.map((item, index) => (
          <article key={index}>
            <p>{item.insight}</p>
            <Evidence names={names(item.supportingVideoIds)} />
          </article>
        ))}
      </AnalysisBlock>
      <AnalysisBlock title="Potential weaknesses">
        {result.potentialWeaknesses.map((item, index) => (
          <article key={index}>
            <p>{item.observation}</p>
            <Evidence names={names(item.supportingVideoIds)} />
          </article>
        ))}
      </AnalysisBlock>
      <AnalysisBlock title="Transferable formats">
        {result.transferableFormats.map((item) => (
          <article key={item.format}>
            <strong>{item.format}</strong>
            <p>{item.whyItMayWork}</p>
            <p>{item.transferableMechanic}</p>
            <small>Do not copy: {item.doNotCopy}</small>
            <Evidence names={names(item.evidenceVideoIds)} />
          </article>
        ))}
      </AnalysisBlock>
      <AnalysisBlock title="Evidence & limitations">
        <ul>
          {result.confidence.limitations.map((item) => (
            <li key={item}>{item}</li>
          ))}
          {result.evidenceNotes.map((item) => (
            <li key={item.note}>{item.note}</li>
          ))}
        </ul>
      </AnalysisBlock>
    </div>
  );
}



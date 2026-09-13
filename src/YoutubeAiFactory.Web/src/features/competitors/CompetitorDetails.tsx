import { formatDate, formatNumber } from "../../lib/formatters";
import type { CompetitorDetails as CompetitorDetailsType } from "./types";
import { AnalysisPanel } from "../analysis/AnalysisPanel";

export function CompetitorDetails({ competitor }: { competitor: CompetitorDetailsType }) {
  return (
    <>
      <section className="channel-card">
        {competitor.thumbnailUrl && (
          <img src={competitor.thumbnailUrl} alt="" width="88" height="88" />
        )}
        <div className="channel-identity">
          <span className="eyebrow">Collected channel</span>
          <h2>{competitor.title}</h2>
          {competitor.handle && <p>{competitor.handle}</p>}
        </div>
        <dl className="channel-stats">
          <Stat
            label="Subscribers"
            value={formatNumber(competitor.subscriberCount)}
          />
          <Stat
            label="Channel videos"
            value={formatNumber(competitor.videoCount)}
          />
          <Stat
            label="Total views"
            value={formatNumber(competitor.viewCount)}
          />
          <Stat
            label="Collected"
            value={formatDate(competitor.lastCollectedAt)}
          />
        </dl>
      </section>

      <section className="videos-section">
        <div className="section-heading">
          <h2>Recent videos</h2>
          <span>{competitor.videos.length} collected</span>
        </div>
        {competitor.videos.length === 0 ? (
          <p className="empty-state">
            No recent videos were returned for this channel.
          </p>
        ) : (
          <div className="video-list">
            {competitor.videos.map((video) => (
              <article className="video-card" key={video.id}>
                {video.thumbnailUrl ? (
                  <img src={video.thumbnailUrl} alt="" loading="lazy" />
                ) : (
                  <div className="thumbnail-placeholder" />
                )}
                <div className="video-details">
                  <a href={video.url} target="_blank" rel="noreferrer">
                    {video.title}
                  </a>
                  <span>{formatDate(video.publishedAt)}</span>
                  <div className="video-metrics">
                    <span>{formatNumber(video.viewCount)} views</span>
                    {video.likeCount !== null && (
                      <span>{formatNumber(video.likeCount)} likes</span>
                    )}
                    {video.commentCount !== null && (
                      <span>{formatNumber(video.commentCount)} comments</span>
                    )}
                  </div>
                </div>
              </article>
            ))}
          </div>
        )}
      </section>
      <AnalysisPanel projectId={competitor.projectId} competitor={competitor} />
    </>
  );
}

function Stat({ label, value }: { label: string; value: string }) {
  return <div><dt>{label}</dt><dd>{value}</dd></div>
}





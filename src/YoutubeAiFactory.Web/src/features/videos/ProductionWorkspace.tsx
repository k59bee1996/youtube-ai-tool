import { useCallback, useEffect, useMemo, useState } from "react"
import { api, type LocalizedProductionPackageContent, type ProductionAsset, type ProductionHistoryItem, type ProductionLocalizationStatus, type ProductionPackage, type ProductionScene, type ProductionStatus, type ProductionUpdate } from "../../services/api"
import { messageFrom } from "../../lib/errors"
import { AnalysisLanguageToggle, type AnalysisLocale } from "../localization/AnalysisLanguageToggle"

export function ProductionWorkspace({ projectId, videoProjectId, revisionKey, onWorkflowStatusChange }: {
  projectId: string
  videoProjectId: string
  revisionKey: string
  onWorkflowStatusChange: (status: string) => void
}) {
  const [status, setStatus] = useState<ProductionStatus | null>(null)
  const [viewed, setViewed] = useState<ProductionPackage | null>(null)
  const [history, setHistory] = useState<ProductionHistoryItem[]>([])
  const [draft, setDraft] = useState<ProductionPackage | null>(null)
  const [editing, setEditing] = useState(false)
  const [loading, setLoading] = useState(true)
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const load = useCallback(async () => {
    if (!revisionKey) return
    try {
      const [nextStatus, nextHistory] = await Promise.all([
        api.getProductionStatus(projectId, videoProjectId),
        api.listProductionPackages(projectId, videoProjectId),
      ])
      setStatus(nextStatus); setHistory(nextHistory); setViewed(nextStatus.latestPackage); setError(null)
      if (nextStatus.latestPackage?.status === "Approved") onWorkflowStatusChange("ProductionReady")
      else if (nextStatus.activeJob || nextStatus.latestPackage) onWorkflowStatusChange("Packaging")
    } catch (requestError) { setError(messageFrom(requestError)) }
    finally { setLoading(false) }
  }, [onWorkflowStatusChange, projectId, revisionKey, videoProjectId])

  useEffect(() => { void Promise.resolve().then(load) }, [load])
  useEffect(() => {
    if (!status?.activeJob) return undefined
    const timer = window.setTimeout(() => void load(), 2000)
    return () => window.clearTimeout(timer)
  }, [load, status?.activeJob])

  const run = useCallback(async (action: () => Promise<unknown>) => {
    setBusy(true)
    try { await action(); await load(); setError(null) }
    catch (requestError) { setError(messageFrom(requestError)) }
    finally { setBusy(false) }
  }, [load])

  async function selectVersion(id: string) {
    if (id === status?.latestPackage?.id) { setViewed(status.latestPackage); return }
    setBusy(true); try { setViewed(await api.getProductionPackage(projectId, videoProjectId, id)) } catch (requestError) { setError(messageFrom(requestError)) } finally { setBusy(false) }
  }

  function beginEdit(value: ProductionPackage) { setDraft(structuredClone(value)); setEditing(true) }
  async function save() {
    if (!draft) return
    await run(async () => { const updated = await api.updateProductionPackage(projectId, videoProjectId, draft.id, toUpdate(draft)); setViewed(updated); setEditing(false); setDraft(null) })
  }
  async function exportJson() {
    setBusy(true)
    try {
      const value = await api.exportProductionPackage(projectId, videoProjectId)
      const url = URL.createObjectURL(new Blob([JSON.stringify(value, null, 2)], { type: "application/json" }))
      const anchor = document.createElement("a"); anchor.href = url; anchor.download = `production-package-v${viewed?.version ?? 1}.json`; anchor.click(); URL.revokeObjectURL(url)
    } catch (requestError) { setError(messageFrom(requestError)) }
    finally { setBusy(false) }
  }

  if (loading) return <section className="analysis-block"><h3>Production</h3><p>Loading production workspace...</p></section>
  if (error && !status) return <section className="analysis-block"><h3>Production</h3><p className="error-copy" role="alert">{error}</p></section>
  const current = editing ? draft : viewed
  return <section className="analysis-block production-workspace">
    <div className="section-heading"><div><h3>Production package</h3><p className="section-copy">Structured scene, shot, asset, text, motion, transition and audio instructions grounded in the approved Script.</p></div>
      {history.length > 0 ? <label>View version<select value={viewed?.id ?? ""} disabled={busy || editing} onChange={event => void selectVersion(event.target.value)}>{history.map(item => <option key={item.id} value={item.id}>v{item.version} · {item.status} · {item.groundingStatus}</option>)}</select></label> : null}
    </div>
    {error ? <p className="error-copy" role="alert">{error}</p> : null}
    {status?.activeJob ? <div className="analysis-state"><p>{status.activeJob.operation === "Validate" ? "Auditing edited production instructions" : "Building and grounding the production plan"}... Status: {status.activeJob.status}.</p><button className="quiet-button" onClick={() => void load()}>Refresh status</button></div> : null}
    {!status?.activeJob && !current ? <div className="analysis-state"><p>{status?.blockReason ?? "The approved Script is ready."}</p>{status?.canGenerate ? <button className="primary-button" disabled={busy} onClick={() => void run(() => api.generateProductionPackage(projectId, videoProjectId))}>Generate production package</button> : null}</div> : null}
    {!status?.activeJob && status?.latestJob?.status === "Failed" ? <p className="error-copy" role="alert">Production workflow failed: {status.latestJob.failureReason ?? "No failure reason was provided."} {viewed ? "The previous version remains available." : ""}</p> : null}
    {current ? <PackageView key={`${current.id}-${current.version}`} value={current} editing={editing} isLatest={viewed?.id === status?.latestPackage?.id} busy={busy} canGenerate={Boolean(status?.canGenerate)}
      onChange={setDraft} onRegenerate={() => run(() => api.generateProductionPackage(projectId, videoProjectId))}
      onEdit={() => beginEdit(current)} onCancel={() => { setEditing(false); setDraft(null) }} onSave={save}
      onValidate={() => run(() => api.validateProductionPackage(projectId, videoProjectId, current.id))}
      onApprove={() => run(() => api.approveProductionPackage(projectId, videoProjectId, current.id))}
      onExport={exportJson} projectId={projectId} videoProjectId={videoProjectId} /> : null}
  </section>
}

function PackageView({ value, editing, isLatest, busy, canGenerate, onChange, onRegenerate, onEdit, onCancel, onSave, onValidate, onApprove, onExport, projectId, videoProjectId }: {
  value: ProductionPackage; editing: boolean; isLatest: boolean; busy: boolean; canGenerate: boolean
  onChange: (value: ProductionPackage) => void; onRegenerate: () => Promise<void>; onEdit: () => void; onCancel: () => void
  onSave: () => Promise<void>; onValidate: () => Promise<void>; onApprove: () => Promise<void>; onExport: () => Promise<void>
  projectId: string; videoProjectId: string
}) {
  const assets = useMemo(() => new Map(value.assetRequirements.map(asset => [asset.id, asset])), [value.assetRequirements])
  const [locale, setLocale] = useState<AnalysisLocale>("en")
  const [localized, setLocalized] = useState<ProductionLocalizationStatus | null>(null)
  const [localizationBusy, setLocalizationBusy] = useState(false)
  const [localizationError, setLocalizationError] = useState<string | null>(null)
  const presentation = locale === "vi" ? localized?.content : null
  const scenesById = useMemo(() => new Map(presentation?.scenes.map(scene => [scene.sceneId, scene])), [presentation])
  const assetsById = useMemo(() => new Map(presentation?.assets.map(asset => [asset.assetId, asset])), [presentation])
  const warnings = presentation?.warnings.map(item => item.text) ?? value.warnings

  const loadLocalization = useCallback(async () => {
    const next = await api.getProductionPackageLocalization(projectId, videoProjectId, value.id, "vi")
    setLocalized(next)
    if (next.content) { setLocalizationBusy(false); setLocalizationError(null); return }
    setLocalizationBusy(Boolean(next.activeJob))
    if (next.latestJob?.status === "Failed") setLocalizationError(next.latestJob.failureReason ?? "Vietnamese production translation failed. English remains available.")
  }, [projectId, value.id, videoProjectId])

  useEffect(() => {
    if (locale !== "vi" || !localizationBusy) return undefined
    const timer = window.setInterval(() => void loadLocalization(), 2000)
    return () => window.clearInterval(timer)
  }, [loadLocalization, locale, localizationBusy])

  async function selectLocale(nextLocale: AnalysisLocale) {
    setLocale(nextLocale)
    if (nextLocale === "en") return
    setLocalizationBusy(true); setLocalizationError(null)
    try {
      const existing = await api.getProductionPackageLocalization(projectId, videoProjectId, value.id, "vi")
      if (existing.content) { setLocalized(existing); setLocalizationBusy(false); return }
      await api.requestProductionPackageLocalization(projectId, videoProjectId, value.id, "vi")
      await loadLocalization()
    } catch (requestError) { setLocalized(null); setLocalizationBusy(false); setLocalizationError(messageFrom(requestError)) }
  }

  const ready = value.status === "Ready" && isLatest
  const update = (fields: Partial<ProductionPackage>) => onChange({ ...value, ...fields })
  const updateScene = (scene: ProductionScene) => update({ scenes: value.scenes.map(item => item.id === scene.id ? scene : item) })
  const updateAsset = (asset: ProductionAsset) => update({ assetRequirements: value.assetRequirements.map(item => item.id === asset.id ? asset : item) })
  return <div className="analysis-report">
    <div className="section-heading"><div><span className="eyebrow">Package v{value.version} · {value.status}</span><p className="section-copy">Script v{value.videoScriptVersion} · Outline v{value.videoOutlineVersion} · Research v{value.researchReportVersion}</p></div><div className="section-actions"><AnalysisLanguageToggle locale={locale} onChange={next => void selectLocale(next)} disabled={localizationBusy || editing} />{canGenerate && isLatest && !editing ? <button className="quiet-button" disabled={busy} onClick={() => void onRegenerate()}>Generate new version</button> : null}</div></div>
    {locale === "vi" && !presentation && localizationBusy ? <p className="section-copy">Vietnamese reading aid is being prepared. Canonical English remains available.</p> : null}
    {localizationError ? <p className="error-copy" role="alert">{localizationError}</p> : null}
    <div className="confidence-grid"><div><span>Grounding</span><strong>{value.groundingStatus}</strong></div><div><span>Runtime</span><strong>{formatDuration(value.estimatedDurationSeconds)}</strong></div><div><span>Scenes</span><strong>{value.scenes.length}</strong></div><div><span>Assets</span><strong>{value.assetRequirements.length}</strong></div><div><span>Language</span><strong>{value.contentLanguage}</strong></div></div>
    {value.isStale ? <p className="stale-note">This package no longer matches the current approved Script.</p> : null}
    {warnings.map((warning, index) => <p className="stale-note" key={`${index}-${warning}`}>{warning}</p>)}
    {value.groundingIssues.map((issue, index) => <p className={issue.severity === "Error" ? "error-copy" : "stale-note"} key={`${issue.sceneSequence}-${index}`}><strong>{issue.severity}: {issue.issueType}</strong> · Scene {issue.sceneSequence}: {presentation?.groundingIssues[index]?.explanation ?? issue.explanation}</p>)}
    <DirectionFields value={value} editing={editing} onChange={update} />
    <section className="analysis-block"><h4>Scenes</h4>{value.scenes.map(scene => <SceneCard key={scene.id} scene={scene} localized={scenesById.get(scene.id)} editing={editing} assets={assets} onChange={updateScene} />)}</section>
    <section className="analysis-block"><h4>Reusable assets</h4>{value.assetRequirements.map(asset => <AssetCard key={asset.id} asset={asset} localized={assetsById.get(asset.id)} editing={editing} onChange={updateAsset} />)}</section>
    <div className="idea-actions">{ready && !editing ? <><button className="quiet-button" disabled={busy || value.isStale} onClick={onEdit}>Edit instructions</button>{value.groundingStatus === "Passed" ? <button className="primary-button" disabled={busy || value.isStale} onClick={() => void onApprove()}>Approve package</button> : <button className="primary-button" disabled={busy || value.isStale} onClick={() => void onValidate()}>Validate package</button>}</> : null}{ready && editing ? <><button className="primary-button" disabled={busy} onClick={() => void onSave()}>Save edits</button><button className="quiet-button" disabled={busy} onClick={onCancel}>Cancel</button></> : null}{value.status === "Approved" ? <button className="primary-button" disabled={busy || value.isStale} onClick={() => void onExport()}>Export JSON</button> : null}</div>
    {value.status === "Approved" ? <p className="idea-approval-summary">ProductionReady. Narration in the export is derived from the immutable approved Script.</p> : null}
  </div>
}

function DirectionFields({ value, editing, onChange }: { value: ProductionPackage; editing: boolean; onChange: (fields: Partial<ProductionPackage>) => void }) {
  const fields: { key: keyof ProductionPackage; label: string }[] = [{key:"visualDirection",label:"Visual direction"},{key:"pacingDirection",label:"Pacing"},{key:"colorDirection",label:"Color"},{key:"typographyDirection",label:"Typography"},{key:"audioDirection",label:"Audio"},{key:"experimentProductionNotes",label:"Pilot experiment notes"}]
  return <section className="analysis-block"><h4>Global direction</h4>{fields.map(field => <label key={field.key}>{field.label}{editing ? <textarea value={String(value[field.key])} onChange={event => onChange({ [field.key]: event.target.value })} /> : <p>{String(value[field.key])}</p>}</label>)}</section>
}

function SceneCard({ scene, localized, editing, assets, onChange }: { scene: ProductionScene; localized: LocalizedProductionPackageContent["scenes"][number] | undefined; editing: boolean; assets: Map<string, ProductionAsset>; onChange: (scene: ProductionScene) => void }) {
  const set = (fields: Partial<ProductionScene>) => onChange({ ...scene, ...fields })
  const shotsById = useMemo(() => new Map(localized?.shots.map(shot => [shot.shotId, shot])), [localized])
  return <article className="script-section-card"><div className="section-heading"><div><span className="eyebrow">Scene {scene.sequence} · {scene.purpose}</span><strong>{formatDuration(scene.estimatedDurationSeconds)} · {scene.complexity}</strong></div></div>
    <p><strong>Narration (read-only canonical Script)</strong></p><p>{scene.narration}</p>
    <Editable label="Narration summary" value={editing ? scene.narrationSummary : localized?.narrationSummary ?? scene.narrationSummary} editing={editing} onChange={narrationSummary => set({ narrationSummary })} />
    <Editable label="Visual strategy" value={editing ? scene.visualStrategy : localized?.visualStrategy ?? scene.visualStrategy} editing={editing} onChange={visualStrategy => set({ visualStrategy })} />
    <Editable label="Transition" value={scene.transitionIntent} editing={editing} onChange={transitionIntent => set({ transitionIntent })} />
    <Editable label="Music" value={scene.musicBrief} editing={editing} onChange={musicBrief => set({ musicBrief })} />
    <Editable label="SFX" value={scene.soundEffectCue} editing={editing} onChange={soundEffectCue => set({ soundEffectCue })} />
    <Editable label="Voice" value={scene.voiceDirection} editing={editing} onChange={voiceDirection => set({ voiceDirection })} />
    <details open><summary>Shots ({scene.shots.length})</summary>{scene.shots.map(shot => <div className="script-block" key={shot.id}><strong>Shot {shot.sequence} · {shot.shotType} · {formatDuration(shot.estimatedDurationSeconds)}</strong><Editable label="Visual" value={editing ? shot.visualDescription : shotsById.get(shot.id)?.visualDescription ?? shot.visualDescription} editing={editing} onChange={visualDescription => set({ shots: scene.shots.map(item => item.id === shot.id ? { ...item, visualDescription } : item) })} /><Editable label="Composition" value={shot.composition} editing={editing} onChange={composition => set({ shots: scene.shots.map(item => item.id === shot.id ? { ...item, composition } : item) })} /><Editable label="Motion" value={shot.motionSuggestion} editing={editing} onChange={motionSuggestion => set({ shots: scene.shots.map(item => item.id === shot.id ? { ...item, motionSuggestion } : item) })} /><p>{shot.factualityMode}{shot.assetRequirementId ? ` · Asset ${assets.get(shot.assetRequirementId)?.assetKey ?? shot.assetRequirementId}` : ""}</p><ClaimList claims={shot.claims} /></div>)}</details>
    {scene.onScreenText.length > 0 ? <details><summary>On-screen text ({scene.onScreenText.length})</summary>{scene.onScreenText.map(text => <div key={text.id}><Editable label={text.type} value={text.text} editing={editing} onChange={next => set({ onScreenText: scene.onScreenText.map(item => item.id === text.id ? { ...item, text: next } : item) })} /><Editable label="Timing" value={text.timingIntent} editing={editing} onChange={timingIntent => set({ onScreenText: scene.onScreenText.map(item => item.id === text.id ? { ...item, timingIntent } : item) })} /><ClaimList claims={text.claims} /></div>)}</details> : null}
  </article>
}

function AssetCard({ asset, localized, editing, onChange }: { asset: ProductionAsset; localized: LocalizedProductionPackageContent["assets"][number] | undefined; editing: boolean; onChange: (asset: ProductionAsset) => void }) {
  const set = (fields: Partial<ProductionAsset>) => onChange({ ...asset, ...fields })
  return <article className="pilot-card"><strong>{asset.assetKey} · {asset.assetType}</strong><p>{asset.acquisitionMode} · {asset.factualityMode} · {asset.complexity}{asset.rightsVerificationRequired ? " · rights check required" : ""}</p><Editable label="Creative brief" value={editing ? asset.creativeBrief : localized?.creativeBrief ?? asset.creativeBrief} editing={editing} onChange={creativeBrief => set({ creativeBrief })} /><Editable label="Generation prompt" value={asset.generationPrompt} editing={editing} onChange={generationPrompt => set({ generationPrompt })} /><Editable label="Source search brief" value={editing ? asset.sourceSearchBrief : localized?.sourceSearchBrief ?? asset.sourceSearchBrief} editing={editing} onChange={sourceSearchBrief => set({ sourceSearchBrief })} /><ClaimList claims={asset.claims} /></article>
}

function Editable({ label, value, editing, onChange }: { label: string; value: string; editing: boolean; onChange: (value: string) => void }) { return <label><strong>{label}</strong>{editing ? <textarea value={value} onChange={event => onChange(event.target.value)} /> : <p>{value || "—"}</p>}</label> }
function ClaimList({ claims }: { claims: ProductionAsset["claims"] }) { return claims.length ? <details><summary>Grounding claims ({claims.length})</summary>{claims.map(claim => <div className="outline-claim" key={claim.id}><p><strong>{claim.supportStatus}</strong> · {claim.statement}</p>{claim.sourceLocators.map(source => <a key={source} href={source} target="_blank" rel="noreferrer noopener">Source</a>)}</div>)}</details> : null }
function toUpdate(value: ProductionPackage): ProductionUpdate { return { visualDirection:value.visualDirection,pacingDirection:value.pacingDirection,colorDirection:value.colorDirection,typographyDirection:value.typographyDirection,audioDirection:value.audioDirection,experimentProductionNotes:value.experimentProductionNotes,scenes:value.scenes.map(x=>({sceneId:x.id,narrationSummary:x.narrationSummary,visualStrategy:x.visualStrategy,transitionIntent:x.transitionIntent,musicBrief:x.musicBrief,soundEffectCue:x.soundEffectCue,voiceDirection:x.voiceDirection})),shots:value.scenes.flatMap(x=>x.shots.map(y=>({shotId:y.id,visualDescription:y.visualDescription,composition:y.composition,motionSuggestion:y.motionSuggestion,notes:y.notes}))),assets:value.assetRequirements.map(x=>({assetId:x.id,creativeBrief:x.creativeBrief,generationPrompt:x.generationPrompt,sourceSearchBrief:x.sourceSearchBrief})),onScreenText:value.scenes.flatMap(x=>x.onScreenText.map(y=>({onScreenTextId:y.id,text:y.text,timingIntent:y.timingIntent}))) } }
function formatDuration(seconds: number) { return `${Math.floor(seconds / 60)}:${(seconds % 60).toString().padStart(2,"0")}` }

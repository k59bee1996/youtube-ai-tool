import { cleanup, fireEvent, render, screen } from "@testing-library/react"
import { afterEach, expect, test, vi } from "vitest"
import type { ProductionPackage, ProductionStatus } from "../../services/api"
import { ProductionWorkspace } from "./ProductionWorkspace"

afterEach(() => { cleanup(); vi.restoreAllMocks(); vi.unstubAllGlobals() })

test("queues production generation from an approved Script", async () => {
  let queued=false
  vi.stubGlobal("fetch",vi.fn((input:RequestInfo|URL,init?:RequestInit)=>{const url=String(input);const method=init?.method??"GET";if(url.endsWith("/production-packages"))return json([]);if(url.endsWith("/production-package:generate")&&method==="POST"){queued=true;return json({jobId:"job-1",status:"Queued",existing:false},202)}if(url.endsWith("/production-package/latest"))return json(queued?status({activeJob:{id:"job-1",status:"Queued",operation:"Generate",failureReason:null}}):status({canGenerate:true,blockReason:null}));throw new Error(`Unexpected ${method} ${url}`)}))
  render(<ProductionWorkspace projectId="project-1" videoProjectId="video-1" revisionKey="r1" onWorkflowStatusChange={vi.fn()} />)
  fireEvent.click(await screen.findByRole("button",{name:"Generate production package"}))
  expect(await screen.findByText(/Building and grounding.*Queued/)).toBeVisible()
})

test("renders read-only canonical narration, visual plan, claims, and approval action", async () => {
  const value=readyPackage();vi.stubGlobal("fetch",vi.fn((input:RequestInfo|URL,init?:RequestInit)=>{const url=String(input);const method=init?.method??"GET";if(url.endsWith("/production-package/latest"))return json(status({latestPackage:value,canGenerate:true,blockReason:null}));if(url.endsWith("/production-packages")&&method==="GET")return json([history(value)]);throw new Error(`Unexpected ${method} ${url}`)}))
  const { container } = render(<ProductionWorkspace projectId="project-1" videoProjectId="video-1" revisionKey="r1" onWorkflowStatusChange={vi.fn()} />)
  expect(await screen.findByText("Castle ownership required recurring staffing obligations.")).toBeVisible()
  expect(screen.getAllByText(/EvidenceBasedDepiction/)).toHaveLength(2)
  expect(screen.getByRole("button",{name:"Approve package"})).toBeVisible()
  expect(container.textContent).not.toMatch(/Â|â€”/)
})

test("saving structured instruction edits produces Pending state and validate action", async () => {
  const value=readyPackage();const pending={...value,groundingStatus:"Pending",visualDirection:"Updated documentary direction"};
  vi.stubGlobal("fetch",vi.fn((input:RequestInfo|URL,init?:RequestInit)=>{const url=String(input);const method=init?.method??"GET";if(url.endsWith("/production-package/latest"))return json(status({latestPackage:pending,canGenerate:true,blockReason:null}));if(url.endsWith("/production-packages")&&method==="GET")return json([history(pending)]);if(url.endsWith(`/production-packages/${value.id}`)&&method==="PATCH")return json(pending);throw new Error(`Unexpected ${method} ${url}`)}))
  render(<ProductionWorkspace projectId="project-1" videoProjectId="video-1" revisionKey="r1" onWorkflowStatusChange={vi.fn()} />)
  fireEvent.click(await screen.findByRole("button",{name:"Edit instructions"}))
  const direction=screen.getAllByRole("textbox")[0];fireEvent.change(direction,{target:{value:"Updated documentary direction"}})
  fireEvent.click(screen.getByRole("button",{name:"Save edits"}))
  expect(await screen.findByRole("button",{name:"Validate package"})).toBeVisible()
})

function status(overrides:Partial<ProductionStatus>):ProductionStatus{return{latestPackage:null,activeJob:null,latestJob:null,canGenerate:false,blockReason:"Approve a Script first.",...overrides}}
function history(value:ProductionPackage){return{id:value.id,version:value.version,status:value.status,groundingStatus:value.groundingStatus,videoScriptVersion:value.videoScriptVersion,isStale:value.isStale,createdAt:value.createdAt,approvedAt:value.approvedAt}}
function readyPackage():ProductionPackage{return{id:"package-1",projectId:"project-1",videoProjectId:"video-1",videoScriptId:"script-1",videoScriptVersion:1,videoOutlineId:"outline-1",videoOutlineVersion:1,researchReportId:"report-1",researchReportVersion:1,version:1,status:"Ready",groundingStatus:"Passed",engineVersion:"production-package-engine:v1",promptKey:"production-package",promptVersion:1,provider:"fake",model:"reasoning",contentLanguage:"English",estimatedDurationSeconds:30,visualDirection:"Documentary illustration",pacingDirection:"Measured",colorDirection:"Muted",typographyDirection:"Readable",audioDirection:"Sparse",experimentProductionNotes:"Keep packaging variable stable",isStale:false,warnings:[],groundingIssues:[],scenes:[{id:"scene-1",sequence:1,purpose:"Explain",narration:"Castle ownership required recurring staffing obligations.",narrationSummary:"Explain staffing",visualStrategy:"Diagram",estimatedDurationSeconds:30,complexity:"Medium",transitionIntent:"Cut",musicBrief:"Sparse",soundEffectCue:"None",voiceDirection:"Measured",scriptBlockIds:["block-1"],shots:[{id:"shot-1",sequence:1,shotType:"Illustration",visualDescription:"Labelled castle staffing diagram",composition:"Wide",motionSuggestion:"Slow push",estimatedDurationSeconds:30,factualityMode:"EvidenceBasedDepiction",assetRequirementId:"asset-1",claims:[{id:"claim-1",statement:"Staffing was recurring",supportStatus:"Supported",sourceLocators:["https://example.com/source"]}],notes:""}],onScreenText:[]}],assetRequirements:[{id:"asset-1",assetKey:"castle",assetType:"Illustration",acquisitionMode:"CreateGraphic",creativeBrief:"Labelled diagram",generationPrompt:"",sourceSearchBrief:"",rightsVerificationRequired:false,factualityMode:"EvidenceBasedDepiction",reuseKey:"castle-base",complexity:"Medium",claims:[{id:"claim-1",statement:"Staffing was recurring",supportStatus:"Supported",sourceLocators:["https://example.com/source"]}]}],createdAt:"2026-09-19T00:00:00Z",updatedAt:"2026-09-19T00:00:00Z",approvedAt:null}}
function json(value:unknown,statusCode=200):Promise<Response>{return Promise.resolve(new Response(JSON.stringify(value),{status:statusCode,headers:{"Content-Type":"application/json"}}))}

# YouTube AI Factory Web

React 19 and TypeScript client for the Phase 3 project, competitor collection, and persisted AI intelligence workflow.

```powershell
npm install
npm run dev
```

The Vite development server runs at `http://localhost:5173` and proxies `/api` and `/health` to the API at `http://localhost:5050`. The competitor detail screen displays a persisted structured analysis and polls only while an analysis job is active; it never starts work automatically. Run `npm run lint`, `npm test`, and `npm run build` before submitting changes. API errors are shown from RFC 7807 problem responses, and the selected project ID is retained in browser local storage.

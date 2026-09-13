import { AppProviders } from "./providers"
import { AppRouter } from "./router"
import "../App.css"

export function App() {
  return <AppProviders><AppRouter /></AppProviders>
}

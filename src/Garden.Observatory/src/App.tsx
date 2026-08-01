import { Routes, Route, Navigate, useSearchParams } from 'react-router-dom'
import AtlasShell from './components/shell/AtlasShell.tsx'
import type { PanelId } from './lib/usePanelState'

/**
 * The old app was ten routed pages. The new one is a single persistent
 * world with panels layered over it (see AtlasShell + usePanelState). These
 * routes exist only so old bookmarks/links keep working - they redirect
 * into the panel query-param scheme instead of 404ing.
 */
function LegacyRedirect({ panel }: { panel: PanelId }) {
  const [params] = useSearchParams()
  const selected = params.get('selected')
  const target = `/?panel=${panel}${selected ? `&selected=${encodeURIComponent(selected)}` : ''}`
  return <Navigate to={target} replace />
}

function App() {
  return (
    <Routes>
      <Route path="/" element={<AtlasShell />} />
      <Route path="/environment" element={<LegacyRedirect panel="almanac" />} />
      <Route path="/economy" element={<LegacyRedirect panel="almanac" />} />
      <Route path="/map" element={<Navigate to="/" replace />} />
      <Route path="/citizens" element={<LegacyRedirect panel="citizens" />} />
      <Route path="/settlements" element={<LegacyRedirect panel="settlements" />} />
      <Route path="/history" element={<LegacyRedirect panel="chronicle" />} />
      <Route path="/civilization" element={<LegacyRedirect panel="civilization" />} />
      <Route path="/production" element={<LegacyRedirect panel="steward" />} />
      <Route path="/diagnostics" element={<LegacyRedirect panel="steward" />} />
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  )
}

export default App

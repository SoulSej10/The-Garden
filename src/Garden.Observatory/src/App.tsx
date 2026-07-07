import { Routes, Route, Navigate } from 'react-router-dom'
import DashboardPage from './pages/DashboardPage.tsx'
import EnvironmentPage from './pages/EnvironmentPage.tsx'
import WorldMapPage from './pages/WorldMapPage.tsx'
import Layout from './components/Layout.tsx'

function App() {
  return (
    <Routes>
      <Route element={<Layout />}>
        <Route path="/" element={<DashboardPage />} />
        <Route path="/environment" element={<EnvironmentPage />} />
        <Route path="/map" element={<WorldMapPage />} />
        <Route path="*" element={<Navigate to="/" replace />} />
      </Route>
    </Routes>
  )
}

export default App

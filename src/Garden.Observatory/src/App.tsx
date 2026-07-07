import { Routes, Route, Navigate } from 'react-router-dom'
import DashboardPage from './pages/DashboardPage.tsx'
import Layout from './components/Layout.tsx'

function App() {
  return (
    <Routes>
      <Route element={<Layout />}>
        <Route path="/" element={<DashboardPage />} />
        <Route path="*" element={<Navigate to="/" replace />} />
      </Route>
    </Routes>
  )
}

export default App

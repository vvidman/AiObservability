import { BrowserRouter, Routes, Route } from 'react-router-dom'
import TraceListPage from './pages/TraceListPage'
import TraceDetailPage from './pages/TraceDetailPage'

export default function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/" element={<TraceListPage />} />
        <Route path="/traces/:id" element={<TraceDetailPage />} />
      </Routes>
    </BrowserRouter>
  )
}

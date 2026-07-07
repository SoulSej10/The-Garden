import { useState, useRef, useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import { globalSearch } from '@/lib/api'

export default function GlobalSearch() {
  const [open, setOpen] = useState(false)
  const [query, setQuery] = useState('')
  const [results, setResults] = useState<Array<{ type: string; id: string; label: string; subLabel: string }>>([])
  const [selectedIndex, setSelectedIndex] = useState(0)
  const inputRef = useRef<HTMLInputElement>(null)
  const navigate = useNavigate()

  useEffect(() => {
    const handler = (e: KeyboardEvent) => {
      if ((e.metaKey || e.ctrlKey) && e.key === 'k') {
        e.preventDefault()
        setOpen((v) => !v)
      }
      if (e.key === 'Escape') setOpen(false)
    }
    window.addEventListener('keydown', handler)
    return () => window.removeEventListener('keydown', handler)
  }, [])

  useEffect(() => {
    if (open) {
      inputRef.current?.focus()
    }
  }, [open])

  useEffect(() => {
    if (query.length < 2) {
      setResults([])
      return
    }
    const timer = setTimeout(async () => {
      try {
        const res = await globalSearch(query, 10)
        setResults(res.results)
        setSelectedIndex(0)
      } catch { /* ignore */ }
    }, 200)
    return () => clearTimeout(timer)
  }, [query])

  const handleSelect = (result: typeof results[0]) => {
    setOpen(false)
    setQuery('')
    if (result.type === 'citizen') navigate(`/citizens?selected=${result.id}`)
    if (result.type === 'settlement') navigate(`/settlements?selected=${result.id}`)
  }

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'ArrowDown') {
      e.preventDefault()
      setSelectedIndex((i) => Math.min(i + 1, results.length - 1))
    }
    if (e.key === 'ArrowUp') {
      e.preventDefault()
      setSelectedIndex((i) => Math.max(i - 1, 0))
    }
    if (e.key === 'Enter' && results[selectedIndex]) {
      handleSelect(results[selectedIndex])
    }
  }

  if (!open) return null

  return (
    <div className="fixed inset-0 z-50 flex items-start justify-center pt-[15vh]">
      <div className="fixed inset-0 bg-black/50" onClick={() => setOpen(false)} />
      <div className="relative z-10 w-full max-w-lg rounded-lg border bg-background shadow-2xl">
        <div className="flex items-center border-b px-3">
          <span className="text-muted-foreground mr-2">🔍</span>
          <input
            ref={inputRef}
            type="text"
            placeholder="Search citizens, settlements..."
            value={query}
            onChange={(e) => setQuery(e.target.value)}
            onKeyDown={handleKeyDown}
            className="flex-1 bg-transparent py-3 text-sm outline-none"
          />
          <kbd className="text-[10px] text-muted-foreground/60 border rounded px-1.5 py-0.5">ESC</kbd>
        </div>
        {results.length > 0 && (
          <div className="max-h-72 overflow-y-auto p-2">
            {results.map((r, i) => (
              <button
                key={`${r.type}-${r.id}`}
                onClick={() => handleSelect(r)}
                className={`w-full text-left flex items-center gap-3 rounded-md px-3 py-2 text-sm transition-colors ${
                  i === selectedIndex ? 'bg-accent' : 'hover:bg-accent/50'
                }`}
              >
                <span className="text-muted-foreground w-5 shrink-0">
                  {r.type === 'citizen' ? '👤' : '🏘'}
                </span>
                <div className="min-w-0 flex-1">
                  <p className="font-medium truncate">{r.label}</p>
                  <p className="text-xs text-muted-foreground truncate">{r.subLabel}</p>
                </div>
                <span className="text-[10px] text-muted-foreground/60 capitalize">{r.type}</span>
              </button>
            ))}
          </div>
        )}
        {query.length >= 2 && results.length === 0 && (
          <div className="p-4 text-center text-sm text-muted-foreground">
            No results found
          </div>
        )}
      </div>
    </div>
  )
}

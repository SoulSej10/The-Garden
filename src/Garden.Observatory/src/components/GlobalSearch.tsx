import { useState, useRef, useEffect } from 'react'
import { MagnifyingGlass, UsersThree, HouseLine } from '@phosphor-icons/react'
import { globalSearch } from '@/lib/api'
import { usePanelState } from '@/lib/usePanelState'

export default function GlobalSearch({
  open: openProp,
  onOpenChange,
}: { open?: boolean; onOpenChange?: (open: boolean) => void } = {}) {
  const [internalOpen, setInternalOpen] = useState(false)
  const open = openProp ?? internalOpen
  const setOpen = (updater: boolean | ((prev: boolean) => boolean)) => {
    const next = typeof updater === 'function' ? updater(open) : updater
    setInternalOpen(next)
    onOpenChange?.(next)
  }
  const [query, setQuery] = useState('')
  const [results, setResults] = useState<Array<{ type: string; id: string; label: string; subLabel: string }>>([])
  const [selectedIndex, setSelectedIndex] = useState(0)
  const inputRef = useRef<HTMLInputElement>(null)
  const { openPanel } = usePanelState()

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
    if (open) inputRef.current?.focus()
    else {
      setQuery('')
      setResults([])
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
      } catch {
        /* ignore */
      }
    }, 200)
    return () => clearTimeout(timer)
  }, [query])

  const handleSelect = (result: (typeof results)[0]) => {
    setOpen(false)
    if (result.type === 'citizen') openPanel('citizens', { selected: result.id })
    if (result.type === 'settlement') openPanel('settlements', { selected: result.id })
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
        <div className="fixed inset-0 z-[60] flex items-start justify-center pt-[15vh]">
          <div
            className="animate-backdrop fixed inset-0 bg-background-deep/50 backdrop-blur-sm"
            onClick={() => setOpen(false)}
          />
          <div
            className="animate-dialog-pop panel-carved relative z-10 w-full max-w-lg overflow-hidden border border-border bg-panel shadow-atlas-lg"
          >
            <div className="flex items-center gap-2 border-b border-border/70 px-4">
              <MagnifyingGlass size={15} className="text-muted-foreground" />
              <input
                ref={inputRef}
                type="text"
                placeholder="Search citizens, settlements…"
                value={query}
                onChange={(e) => setQuery(e.target.value)}
                onKeyDown={handleKeyDown}
                className="flex-1 bg-transparent py-3.5 text-sm outline-none"
              />
              <kbd className="rounded border border-border px-1.5 py-0.5 text-[10px] text-muted-foreground/70">ESC</kbd>
            </div>
            {results.length > 0 && (
              <div className="scroll-atlas max-h-72 overflow-y-auto p-2">
                {results.map((r, i) => (
                  <button
                    key={`${r.type}-${r.id}`}
                    onClick={() => handleSelect(r)}
                    className={`flex w-full items-center gap-3 rounded-2xl px-3 py-2 text-left text-sm transition-colors ${
                      i === selectedIndex ? 'bg-accent' : 'hover:bg-accent/50'
                    }`}
                  >
                    <span className="flex h-7 w-7 shrink-0 items-center justify-center rounded-full bg-primary/15 text-primary">
                      {r.type === 'citizen' ? <UsersThree size={14} weight="bold" /> : <HouseLine size={14} weight="bold" />}
                    </span>
                    <div className="min-w-0 flex-1">
                      <p className="truncate font-medium">{r.label}</p>
                      <p className="truncate text-xs text-muted-foreground">{r.subLabel}</p>
                    </div>
                    <span className="shrink-0 text-[10px] capitalize text-muted-foreground/60">{r.type}</span>
                  </button>
                ))}
              </div>
            )}
            {query.length >= 2 && results.length === 0 && (
              <div className="p-5 text-center text-sm text-muted-foreground">No results found.</div>
            )}
          </div>
        </div>
  )
}

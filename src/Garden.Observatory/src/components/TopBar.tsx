import { useState, useEffect } from 'react'

export default function TopBar({ onToggleSearch }: { onToggleSearch: () => void }) {
  const [dark, setDark] = useState(false)

  useEffect(() => {
    setDark(document.documentElement.classList.contains('dark'))
  }, [])

  useEffect(() => {
    document.documentElement.classList.toggle('dark', dark)
  }, [dark])

  return (
    <header className="flex h-14 items-center border-b px-4 md:px-6">
      <div className="flex items-center gap-2">
        <span className="text-lg font-semibold">The Garden</span>
      </div>
      <div className="ml-auto flex items-center gap-2">
        <button
          onClick={onToggleSearch}
          className="flex items-center gap-1.5 rounded-md border px-2.5 py-1.5 text-xs text-muted-foreground hover:bg-accent"
          title="Search (Ctrl+K)"
        >
          <span>🔍</span>
          <span className="hidden sm:inline">Search</span>
          <kbd className="ml-1 text-[10px] text-muted-foreground/60 border rounded px-1">Ctrl+K</kbd>
        </button>
        <button
          onClick={() => setDark((d) => !d)}
          className="rounded-md border px-2.5 py-1.5 text-xs text-muted-foreground hover:bg-accent"
          title="Toggle theme"
        >
          {dark ? '☀️' : '🌙'}
        </button>
      </div>
    </header>
  )
}

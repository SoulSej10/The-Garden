import { useEffect, useState } from 'react'
import { MagnifyingGlass, PersonSimpleRun, Moon, Sun, Scroll } from '@phosphor-icons/react'
import { usePanelState } from '@/lib/usePanelState'
import { useCitizenHub, useSettlementHub, useHistoryHub } from '@/lib/useSimulationHub'
import { pushNotification } from '@/components/NotificationToast'
import { cn } from '@/lib/utils'

/**
 * Top-right HUD corner: utility toggles (search, motion, theme) plus the
 * Chronicle bell. The bell listens on all three "moment" hubs (citizen /
 * settlement / history) and republishes into the existing notification bus -
 * these hubs were fully built and broadcasting since the backend's
 * SignalRBroadcastService shipped, but no component ever subscribed, so
 * every birth/death/founding/harvest event was silently dropped. This is the
 * first thing in the app that actually listens.
 */
export function TopRightCluster({ onOpenSearch }: { onOpenSearch: () => void }) {
  const { openPanel } = usePanelState()
  const [dark, setDark] = useState(false)
  const [reduceMotion, setReduceMotion] = useState(false)
  const [pulse, setPulse] = useState(false)

  useEffect(() => {
    setDark(document.documentElement.classList.contains('dark'))
    setReduceMotion(localStorage.getItem('reduce-motion') === 'true')
  }, [])
  useEffect(() => {
    document.documentElement.classList.toggle('dark', dark)
  }, [dark])
  useEffect(() => {
    document.documentElement.classList.toggle('reduce-motion', reduceMotion)
    localStorage.setItem('reduce-motion', String(reduceMotion))
  }, [reduceMotion])

  const citizenHub = useCitizenHub()
  const settlementHub = useSettlementHub()
  const historyHub = useHistoryHub()

  function ring() {
    setPulse(true)
    setTimeout(() => setPulse(false), 1400)
  }

  useEffect(() => {
    if (!citizenHub.notification) return
    pushNotification(
      citizenHub.notification.title,
      citizenHub.notification.description,
      citizenHub.notification.category,
      citizenHub.notification.severity
    )
    citizenHub.clearNotification()
    ring()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [citizenHub.notification])

  useEffect(() => {
    if (!settlementHub.notification) return
    pushNotification(
      settlementHub.notification.title,
      settlementHub.notification.description,
      settlementHub.notification.category,
      settlementHub.notification.severity
    )
    settlementHub.clearNotification()
    ring()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [settlementHub.notification])

  useEffect(() => {
    if (!historyHub.notification) return
    pushNotification(
      historyHub.notification.title,
      historyHub.notification.description,
      historyHub.notification.category,
      historyHub.notification.severity
    )
    historyHub.clearNotification()
    ring()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [historyHub.notification])

  return (
    <div className="flex h-14 shrink-0 items-center gap-2 border-b border-border/70 bg-panel/60 px-3 backdrop-blur-md">
      <button
        onClick={onOpenSearch}
        className="flex h-9 flex-1 items-center gap-2 rounded-full border border-border/70 bg-card px-3 text-left text-xs text-muted-foreground transition-colors hover:bg-accent/60"
      >
        <MagnifyingGlass size={14} />
        <span className="flex-1">Search the world…</span>
        <kbd className="hidden rounded border border-border/70 px-1 text-[10px] sm:inline">Ctrl K</kbd>
      </button>
      <button
        onClick={() => setReduceMotion((r) => !r)}
        className="flex h-9 w-9 shrink-0 items-center justify-center rounded-full text-muted-foreground transition-colors hover:bg-accent hover:text-accent-foreground"
        title={reduceMotion ? 'Motion reduced' : 'Reduce motion'}
        aria-pressed={reduceMotion}
      >
        <PersonSimpleRun size={15} weight={reduceMotion ? 'fill' : 'regular'} />
      </button>
      <button
        onClick={() => setDark((d) => !d)}
        className="flex h-9 w-9 shrink-0 items-center justify-center rounded-full text-muted-foreground transition-colors hover:bg-accent hover:text-accent-foreground"
        title="Toggle day / night"
      >
        {dark ? <Sun size={15} /> : <Moon size={15} />}
      </button>
      <button
        onClick={() => openPanel('chronicle')}
        className={cn(
          'relative flex h-9 w-9 shrink-0 items-center justify-center rounded-full text-muted-foreground transition-colors hover:bg-accent hover:text-foreground',
          pulse && 'text-primary'
        )}
        title="Chronicle"
      >
        <Scroll size={16} weight={pulse ? 'fill' : 'regular'} />
        {pulse && <span className="absolute inset-0 animate-ping rounded-full border-2 border-primary" />}
      </button>
    </div>
  )
}

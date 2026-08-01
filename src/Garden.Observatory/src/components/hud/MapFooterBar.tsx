import { useQuery } from '@tanstack/react-query'
import { fetchHistoryTimeline } from '@/lib/api'

/**
 * Thin status strip along the bottom of the map column - version identity
 * plus a single cycling line of "what just happened," so even a player who
 * never opens the sidebar's Overview still catches the world doing
 * something. Deliberately quiet: one line, no icons competing with the map.
 */
export function MapFooterBar() {
  const { data: timeline } = useQuery({
    queryKey: ['live-event-ticker'],
    queryFn: () => fetchHistoryTimeline(1, 1),
    refetchInterval: 4000,
  })

  const latest = timeline?.entries?.[0]

  return (
    <div className="flex h-7 shrink-0 items-center justify-between border-t border-border/70 bg-panel/60 px-3 text-[10px] text-muted-foreground backdrop-blur-md">
      <span>The Garden · v0.4</span>
      {latest && (
        <span key={latest.id} className="animate-view-swap truncate">
          {latest.title}
        </span>
      )}
    </div>
  )
}

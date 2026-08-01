import { useQuery } from '@tanstack/react-query'
import {
  Baby,
  Skull,
  HouseLine,
  Heart,
  CloudLightning,
  Drop,
  Fire,
  Mountains,
  Scroll,
} from '@phosphor-icons/react'
import { fetchHistoryTimeline, type HistoryRecord } from '@/lib/api'
import { cn } from '@/lib/utils'

function eventIcon(eventType: string) {
  const t = eventType.toLowerCase()
  if (t.includes('birth') || t.includes('born')) return Baby
  if (t.includes('death') || t.includes('died')) return Skull
  if (t.includes('settlement') || t.includes('founded')) return HouseLine
  if (t.includes('marriage') || t.includes('married')) return Heart
  if (t.includes('storm') || t.includes('lightning')) return CloudLightning
  if (t.includes('flood') || t.includes('drought') || t.includes('rain')) return Drop
  if (t.includes('fire') || t.includes('wildfire') || t.includes('volcan')) return Fire
  if (t.includes('mountain') || t.includes('erupt')) return Mountains
  return Scroll
}

function toneFor(category: string) {
  const c = category.toLowerCase()
  if (c === 'disaster') return 'text-status-danger'
  if (c === 'birth' || c === 'growth') return 'text-status-thriving'
  return 'text-muted-foreground'
}

/**
 * Right-side world activity feed, below TopRightCluster - the Chronicle
 * (full searchable history) still exists as a panel, but the player
 * shouldn't need to open it just to notice the world is doing something.
 * Polls the same timeline endpoint the Chronicle panel's Timeline tab uses;
 * newest record renders first and replays its entrance animation via a
 * fresh `key`, so the feed visibly "arrives" rather than silently updating.
 */
export function LiveEventTicker() {
  const { data: timeline } = useQuery({
    queryKey: ['live-event-ticker'],
    queryFn: () => fetchHistoryTimeline(1, 7),
    refetchInterval: 4000,
  })

  const entries = timeline?.entries ?? []
  if (entries.length === 0) return null

  return (
    <div className="pointer-events-none absolute right-4 top-20 z-20 hidden w-64 lg:block md:right-6 md:top-[4.75rem]">
      <div className="pointer-events-auto panel-carved-r space-y-1 border border-border/70 bg-panel/85 p-2.5 shadow-atlas backdrop-blur-md">
        <p className="px-1 pb-0.5 font-display text-[10px] font-semibold uppercase tracking-wide text-muted-foreground">
          World Activity
        </p>
        <div className="space-y-0.5">
          {entries.map((entry: HistoryRecord) => {
            const Icon = eventIcon(entry.eventType)
            return (
              <div
                key={entry.id}
                className="animate-view-swap flex items-start gap-2 rounded-lg px-1 py-1 text-xs hover:bg-accent/40"
              >
                <span className={cn('mt-0.5 shrink-0', toneFor(entry.category))}>
                  <Icon size={13} weight="bold" />
                </span>
                <div className="min-w-0 flex-1">
                  <p className="truncate font-medium leading-tight">{entry.title}</p>
                  <p className="text-[10px] text-muted-foreground">
                    Y{entry.year} D{entry.day}
                    {entry.locationName && ` · ${entry.locationName}`}
                  </p>
                </div>
              </div>
            )
          })}
        </div>
      </div>
    </div>
  )
}

import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { MagnifyingGlass } from '@phosphor-icons/react'
import { Badge } from '@/components/ui/badge'
import { Tabs, TabsList, TabsTrigger, TabsContent } from '@/components/ui/tabs'
import {
  fetchHistoryTimeline,
  searchHistory,
  fetchHistoryStats,
  fetchHistoryFacets,
  fetchStories,
  type HistoryRecord,
  type StorySummary,
} from '@/lib/api'

/**
 * The backend's StoryEngine already writes prose - this is the one panel
 * that leans into the serif "font-chronicle" (Lora) reserved for exactly
 * this kind of in-world narrative text, styled like a kept record rather
 * than an activity log table.
 */
export default function ChroniclePanel() {
  const [page, setPage] = useState(1)
  const [searchQuery, setSearchQuery] = useState('')
  const [searchCategory, setSearchCategory] = useState('')
  const [searchSettlementId, setSearchSettlementId] = useState('')
  const [searchFromTick, setSearchFromTick] = useState('')
  const [searchToTick, setSearchToTick] = useState('')
  const [tab, setTab] = useState('timeline')

  const { data: stats } = useQuery({ queryKey: ['history-stats'], queryFn: fetchHistoryStats, refetchInterval: 5000 })
  const { data: timeline } = useQuery({ queryKey: ['history-timeline', page], queryFn: () => fetchHistoryTimeline(page, 30), refetchInterval: 5000 })
  const { data: stories } = useQuery({ queryKey: ['stories'], queryFn: () => fetchStories(1, 20), refetchInterval: 10000 })
  const { data: facets } = useQuery({ queryKey: ['history-facets'], queryFn: fetchHistoryFacets, staleTime: 60000 })
  const { data: disasterEvents } = useQuery({
    queryKey: ['history-disasters'],
    queryFn: () => searchHistory({ category: 'Disaster' }, 1, 50),
    refetchInterval: 8000,
  })

  const hasActiveFilter =
    searchQuery.length > 0 || searchCategory.length > 0 || searchSettlementId.length > 0 || searchFromTick.length > 0 || searchToTick.length > 0

  const { data: searchResults } = useQuery({
    queryKey: ['history-search', searchQuery, searchCategory, searchSettlementId, searchFromTick, searchToTick],
    queryFn: () =>
      searchHistory(
        {
          q: searchQuery || undefined,
          category: searchCategory || undefined,
          settlementId: searchSettlementId || undefined,
          fromTick: searchFromTick ? Number(searchFromTick) : undefined,
          toTick: searchToTick ? Number(searchToTick) : undefined,
        },
        1,
        50
      ),
    enabled: hasActiveFilter,
  })

  return (
    <div className="space-y-5">
      <div className="grid grid-cols-4 gap-2">
        <MiniStat label="Records" value={stats?.totalRecords} />
        <MiniStat label="Births" value={stats?.births} />
        <MiniStat label="Deaths" value={stats?.deaths} />
        <MiniStat label="Stories" value={stats?.storyCount} />
      </div>

      <Tabs value={tab} onValueChange={setTab}>
        <TabsList>
          <TabsTrigger value="timeline">Timeline</TabsTrigger>
          <TabsTrigger value="stories">Stories</TabsTrigger>
          <TabsTrigger value="events">Events</TabsTrigger>
          <TabsTrigger value="search">Search</TabsTrigger>
        </TabsList>

        <TabsContent value="timeline" className="space-y-4">
          {!timeline || timeline.entries.length === 0 ? (
            <EmptyNote>No historical records yet. Events will appear as the simulation progresses.</EmptyNote>
          ) : (
            <div className="relative space-y-0 border-l border-border/70 pl-4">
              {timeline.entries.map((entry: HistoryRecord) => (
                <TimelineEntry key={entry.id} entry={entry} />
              ))}
            </div>
          )}

          {timeline && timeline.totalPages > 1 && (
            <div className="flex items-center justify-center gap-3 pt-1">
              <button onClick={() => setPage(Math.max(1, page - 1))} disabled={page <= 1} className="rounded-full border border-border px-3 py-1 text-xs disabled:opacity-40">
                Previous
              </button>
              <span className="text-xs text-muted-foreground">Page {page} of {timeline.totalPages}</span>
              <button onClick={() => setPage(Math.min(timeline.totalPages, page + 1))} disabled={page >= timeline.totalPages} className="rounded-full border border-border px-3 py-1 text-xs disabled:opacity-40">
                Next
              </button>
            </div>
          )}
        </TabsContent>

        <TabsContent value="stories" className="space-y-3">
          {!stories || stories.stories.length === 0 ? (
            <EmptyNote>No stories generated yet. Stories are woven from historical records as the simulation runs.</EmptyNote>
          ) : (
            stories.stories.map((story: StorySummary) => (
              <div key={story.id} className="panel-carved border border-border/70 bg-card p-4">
                <div className="flex items-start justify-between gap-2">
                  <h4 className="font-chronicle text-base font-semibold leading-snug">{story.title}</h4>
                  <Badge variant="secondary" className="shrink-0">{story.category}</Badge>
                </div>
                <p className="mt-2 font-chronicle text-sm italic leading-relaxed text-muted-foreground">&ldquo;{story.summary}&rdquo;</p>
                {story.participantNames.length > 0 && (
                  <div className="mt-2 flex flex-wrap gap-1">
                    {story.participantNames.map((name: string) => (
                      <Badge key={name} variant="outline" className="text-[10px]">{name}</Badge>
                    ))}
                  </div>
                )}
              </div>
            ))
          )}
        </TabsContent>

        <TabsContent value="events" className="space-y-3">
          <p className="text-xs text-muted-foreground">
            Droughts, wildfires, floods, and volcanic eruptions - triggered by the spatial weather grid and the map's own geology, not scripted.
          </p>
          {!disasterEvents || disasterEvents.records.length === 0 ? (
            <EmptyNote>No natural events recorded yet. Sustained dry or stormy weather somewhere on the map will eventually trigger one.</EmptyNote>
          ) : (
            <div className="space-y-2">
              {disasterEvents.records.map((record: HistoryRecord) => (
                <div key={record.id} className="panel-carved border border-status-danger/30 bg-status-danger/5 p-3">
                  <div className="flex flex-wrap items-center gap-2">
                    <span className="text-sm font-medium">{record.title}</span>
                    <Badge variant="status-danger" className="text-[10px]">{record.eventType}</Badge>
                  </div>
                  <p className="mt-0.5 text-xs text-muted-foreground">{record.description}</p>
                  <p className="mt-0.5 text-[10px] text-muted-foreground/60">
                    Y{record.year} D{record.day} · {record.season}{record.locationName && ` · ${record.locationName}`}
                  </p>
                </div>
              ))}
            </div>
          )}
        </TabsContent>

        <TabsContent value="search" className="space-y-4">
          <div className="space-y-2">
            <div className="relative">
              <MagnifyingGlass size={14} className="pointer-events-none absolute left-3 top-1/2 -translate-y-1/2 text-muted-foreground" />
              <input
                type="text"
                placeholder="A citizen's name, &ldquo;founding&rdquo;, &ldquo;drought&rdquo;…"
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                className="h-9 w-full rounded-full border border-border bg-card pl-8 pr-3 text-sm outline-none focus-visible:ring-2 focus-visible:ring-ring"
              />
            </div>
            <p className="text-[11px] text-muted-foreground">
              Search prioritizes relationships over isolated records. Family search isn't available yet — families aren't modeled in the simulation.
            </p>
            <div className="grid grid-cols-2 gap-2">
              <select value={searchCategory} onChange={(e) => setSearchCategory(e.target.value)} className="rounded-full border border-border bg-card px-2.5 py-1.5 text-xs">
                <option value="">Any theme</option>
                {facets?.categories.map((c) => <option key={c} value={c}>{c}</option>)}
              </select>
              <select value={searchSettlementId} onChange={(e) => setSearchSettlementId(e.target.value)} className="rounded-full border border-border bg-card px-2.5 py-1.5 text-xs">
                <option value="">Any settlement</option>
                {facets?.settlements.map((s) => <option key={s.id} value={s.id}>{s.name}</option>)}
              </select>
              <input type="number" placeholder="From tick" value={searchFromTick} onChange={(e) => setSearchFromTick(e.target.value)} className="rounded-full border border-border bg-card px-2.5 py-1.5 text-xs" />
              <input type="number" placeholder="To tick" value={searchToTick} onChange={(e) => setSearchToTick(e.target.value)} className="rounded-full border border-border bg-card px-2.5 py-1.5 text-xs" />
            </div>
          </div>

          {hasActiveFilter && searchResults && (
            <div>
              <p className="mb-2 text-xs font-medium text-muted-foreground">{searchResults.totalRecords} found</p>
              {searchResults.records.length === 0 ? (
                <p className="text-sm text-muted-foreground">No matching records.</p>
              ) : (
                <div className="space-y-2">
                  {searchResults.records.map((record: HistoryRecord) => (
                    <div key={record.id} className="panel-carved border border-border/70 bg-card p-3">
                      <div className="flex flex-wrap items-center gap-2">
                        <span className="text-sm font-medium">{record.title}</span>
                        <Badge variant="outline" className="text-[10px]">{record.importance}</Badge>
                      </div>
                      <p className="mt-0.5 text-xs text-muted-foreground">{record.description}</p>
                      <p className="mt-0.5 text-[10px] text-muted-foreground/60">
                        Y{record.year} D{record.day} · {record.season}{record.locationName && ` · ${record.locationName}`}
                      </p>
                    </div>
                  ))}
                </div>
              )}
            </div>
          )}
        </TabsContent>
      </Tabs>
    </div>
  )
}

function TimelineEntry({ entry }: { entry: HistoryRecord }) {
  return (
    <div className="relative pb-4 last:pb-0">
      <span className="absolute -left-[1.15rem] top-1 h-2 w-2 rounded-full bg-primary ring-4 ring-background" />
      <p className="text-[10px] uppercase tracking-wide text-muted-foreground">
        Year {entry.year} · Day {entry.day} · {entry.season}
      </p>
      <div className="mt-0.5 flex flex-wrap items-center gap-1.5">
        <h4 className="font-chronicle text-sm font-semibold">{entry.title}</h4>
        <Badge variant="outline" className="px-1.5 py-0 text-[10px]">{entry.importance}</Badge>
      </div>
      <p className="mt-0.5 font-chronicle text-xs italic leading-relaxed text-muted-foreground">{entry.description}</p>
      {entry.locationName && (
        <p className="mt-0.5 text-[10px] text-muted-foreground/60">
          {entry.locationName}{entry.locationX !== 0 && ` (${entry.locationX}, ${entry.locationY})`}
        </p>
      )}
      {entry.participantNames.length > 0 && (
        <div className="mt-1 flex flex-wrap gap-1">
          {entry.participantNames.map((name: string) => (
            <Badge key={name} variant="outline" className="px-1.5 py-0 text-[10px]">{name}</Badge>
          ))}
        </div>
      )}
    </div>
  )
}

function MiniStat({ label, value }: { label: string; value?: number }) {
  return (
    <div className="rounded-2xl border border-border/70 bg-card px-2 py-2 text-center">
      <p className="font-display text-lg font-semibold">{value ?? '–'}</p>
      <p className="text-[10px] uppercase tracking-wide text-muted-foreground">{label}</p>
    </div>
  )
}

function EmptyNote({ children }: { children: React.ReactNode }) {
  return <p className="py-6 text-center font-chronicle text-sm italic text-muted-foreground">{children}</p>
}

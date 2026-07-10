import { useQuery } from '@tanstack/react-query'
import { useState } from 'react'
import {
  Card,
  CardContent,
  CardHeader,
  CardTitle,
} from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import {
  fetchHistoryTimeline,
  searchHistory,
  fetchHistoryStats,
  fetchHistoryFacets,
  fetchStories,
  type HistoryRecord,
  type StorySummary,
} from '@/lib/api'

export default function HistoryPage() {
  const [page, setPage] = useState(1)
  const [searchQuery, setSearchQuery] = useState('')
  const [searchCategory, setSearchCategory] = useState('')
  const [searchSettlementId, setSearchSettlementId] = useState('')
  const [searchFromTick, setSearchFromTick] = useState('')
  const [searchToTick, setSearchToTick] = useState('')
  const [activeTab, setActiveTab] = useState('timeline')

  const { data: stats } = useQuery({
    queryKey: ['history-stats'],
    queryFn: fetchHistoryStats,
    refetchInterval: 5000,
  })

  const { data: timeline } = useQuery({
    queryKey: ['history-timeline', page],
    queryFn: () => fetchHistoryTimeline(page, 30),
    refetchInterval: 5000,
  })

  const { data: stories } = useQuery({
    queryKey: ['stories'],
    queryFn: () => fetchStories(1, 20),
    refetchInterval: 10000,
  })

  const { data: facets } = useQuery({
    queryKey: ['history-facets'],
    queryFn: fetchHistoryFacets,
    staleTime: 60000,
  })

  const hasActiveFilter = searchQuery.length > 0 || searchCategory.length > 0
    || searchSettlementId.length > 0 || searchFromTick.length > 0 || searchToTick.length > 0

  const { data: searchResults } = useQuery({
    queryKey: ['history-search', searchQuery, searchCategory, searchSettlementId, searchFromTick, searchToTick],
    queryFn: () => searchHistory({
      q: searchQuery || undefined,
      category: searchCategory || undefined,
      settlementId: searchSettlementId || undefined,
      fromTick: searchFromTick ? Number(searchFromTick) : undefined,
      toTick: searchToTick ? Number(searchToTick) : undefined,
    }, 1, 50),
    enabled: hasActiveFilter,
  })

  return (
    <div className="mx-auto max-w-[1600px] space-y-6">
      <div>
        <h1 className="text-2xl font-semibold">History</h1>
        <p className="text-sm text-muted-foreground">
          {stats?.totalRecords ?? 0} records &middot; {stats?.births ?? 0} births &middot; {stats?.deaths ?? 0} deaths &middot; {stats?.storyCount ?? 0} stories
        </p>
      </div>

      <div className="grid grid-cols-1 gap-4 md:grid-cols-4">
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium text-muted-foreground">Total Records</CardTitle>
          </CardHeader>
          <CardContent>
            <p className="text-2xl font-semibold">{stats?.totalRecords ?? '—'}</p>
          </CardContent>
        </Card>
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium text-muted-foreground">Births Recorded</CardTitle>
          </CardHeader>
          <CardContent>
            <p className="text-2xl font-semibold">{stats?.births ?? '—'}</p>
          </CardContent>
        </Card>
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium text-muted-foreground">Deaths Recorded</CardTitle>
          </CardHeader>
          <CardContent>
            <p className="text-2xl font-semibold">{stats?.deaths ?? '—'}</p>
          </CardContent>
        </Card>
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium text-muted-foreground">Stories</CardTitle>
          </CardHeader>
          <CardContent>
            <p className="text-2xl font-semibold">{stats?.storyCount ?? '—'}</p>
          </CardContent>
        </Card>
      </div>

      <div data-slot="tabs" className="flex flex-col gap-4">
        <div data-slot="tabs-list" className="inline-flex h-9 items-center justify-center rounded-lg bg-muted p-1 text-muted-foreground w-fit">
          {['timeline', 'stories', 'search'].map((tab) => (
            <button
              key={tab}
              data-slot="tabs-trigger"
              data-state={activeTab === tab ? 'active' : 'inactive'}
              onClick={() => setActiveTab(tab)}
              className="inline-flex items-center justify-center whitespace-nowrap rounded-md px-3 py-1 text-sm font-medium ring-offset-background transition-all focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:pointer-events-none disabled:opacity-50 data-[state=active]:bg-background data-[state=active]:text-foreground data-[state=active]:shadow capitalize"
            >
              {tab}
            </button>
          ))}
        </div>

        {activeTab === 'timeline' && (
          <div data-slot="tabs-content" className="space-y-4">
            <Card>
              <CardHeader>
                <CardTitle>World Timeline</CardTitle>
              </CardHeader>
              <CardContent>
                {!timeline || timeline.entries.length === 0 ? (
                  <p className="text-sm text-muted-foreground">
                    No historical records yet. Events will appear as the simulation progresses.
                  </p>
                ) : (
                  <div className="space-y-0">
                    {timeline.entries.map((entry: HistoryRecord) => (
                      <div
                        key={entry.id}
                        className="flex gap-4 border-b py-3 last:border-0"
                      >
                        <div className="w-28 shrink-0 text-right text-xs text-muted-foreground">
                          <div>Y{entry.year} D{entry.day}</div>
                          <div>{entry.season}</div>
                        </div>
                        <div className="flex-1 min-w-0">
                          <div className="flex items-center gap-2 flex-wrap">
                            <span className="text-sm font-medium">{entry.title}</span>
                            <Badge variant="outline" className="text-[10px] px-1.5 py-0">
                              {entry.importance}
                            </Badge>
                            <Badge variant="secondary" className="text-[10px] px-1.5 py-0">
                              {entry.eventType}
                            </Badge>
                          </div>
                          <p className="text-xs text-muted-foreground mt-0.5">
                            {entry.description}
                          </p>
                          {entry.locationName && (
                            <p className="text-[10px] text-muted-foreground/60 mt-0.5">
                              {entry.locationName}
                              {entry.locationX !== 0 && ` (${entry.locationX}, ${entry.locationY})`}
                            </p>
                          )}
                          {entry.participantNames.length > 0 && (
                            <div className="flex flex-wrap gap-1 mt-1">
                              {entry.participantNames.map((name: string) => (
                                <Badge key={name} variant="outline" className="text-[10px] px-1.5 py-0">
                                  {name}
                                </Badge>
                              ))}
                            </div>
                          )}
                        </div>
                      </div>
                    ))}
                  </div>
                )}
              </CardContent>
            </Card>

            {timeline && timeline.totalPages > 1 && (
              <div className="flex items-center justify-center gap-2">
                <button
                  onClick={() => setPage(Math.max(1, page - 1))}
                  disabled={page <= 1}
                  className="rounded-md border px-3 py-1 text-sm disabled:opacity-50"
                >
                  Previous
                </button>
                <span className="text-xs text-muted-foreground">
                  Page {page} of {timeline.totalPages}
                </span>
                <button
                  onClick={() => setPage(Math.min(timeline.totalPages, page + 1))}
                  disabled={page >= timeline.totalPages}
                  className="rounded-md border px-3 py-1 text-sm disabled:opacity-50"
                >
                  Next
                </button>
              </div>
            )}
          </div>
        )}

        {activeTab === 'stories' && (
          <div data-slot="tabs-content" className="space-y-4">
            {!stories || stories.stories.length === 0 ? (
              <Card>
                <CardHeader>
                  <CardTitle>Story Feed</CardTitle>
                </CardHeader>
                <CardContent>
                  <p className="text-sm text-muted-foreground">
                    No stories generated yet. Stories are created from historical records as the simulation runs.
                  </p>
                </CardContent>
              </Card>
            ) : (
              stories.stories.map((story: StorySummary) => (
                <Card key={story.id}>
                  <CardHeader>
                    <div className="flex items-center justify-between">
                      <CardTitle className="text-base">{story.title}</CardTitle>
                      <Badge variant="secondary">{story.category}</Badge>
                    </div>
                  </CardHeader>
                  <CardContent>
                    <p className="text-sm text-muted-foreground italic">
                      &ldquo;{story.summary}&rdquo;
                    </p>
                    {story.participantNames.length > 0 && (
                      <div className="flex flex-wrap gap-1 mt-2">
                        {story.participantNames.map((name: string) => (
                          <Badge key={name} variant="outline" className="text-xs">
                            {name}
                          </Badge>
                        ))}
                      </div>
                    )}
                  </CardContent>
                </Card>
              ))
            )}
          </div>
        )}

        {activeTab === 'search' && (
          <div data-slot="tabs-content" className="space-y-4">
            <Card>
              <CardHeader>
                <CardTitle>Historical Search</CardTitle>
                <p className="text-xs text-muted-foreground">
                  Search by person, settlement, event theme, or date - per TG-OBS-005, search
                  prioritizes relationships rather than isolated records. (Family search isn't
                  available yet - families aren't modeled in the simulation.)
                </p>
              </CardHeader>
              <CardContent className="space-y-3">
                <input
                  type="text"
                  placeholder="Search by person or theme (e.g. a citizen's name, &quot;founding&quot;, &quot;drought&quot;)..."
                  value={searchQuery}
                  onChange={(e) => setSearchQuery(e.target.value)}
                  className="w-full rounded-md border px-3 py-2 text-sm"
                />
                <div className="grid grid-cols-2 gap-2 sm:grid-cols-4">
                  <select
                    value={searchCategory}
                    onChange={(e) => setSearchCategory(e.target.value)}
                    className="rounded-md border px-2 py-1.5 text-sm"
                    aria-label="Filter by event theme"
                  >
                    <option value="">Any theme</option>
                    {facets?.categories.map((c) => (
                      <option key={c} value={c}>{c}</option>
                    ))}
                  </select>
                  <select
                    value={searchSettlementId}
                    onChange={(e) => setSearchSettlementId(e.target.value)}
                    className="rounded-md border px-2 py-1.5 text-sm"
                    aria-label="Filter by settlement"
                  >
                    <option value="">Any settlement</option>
                    {facets?.settlements.map((s) => (
                      <option key={s.id} value={s.id}>{s.name}</option>
                    ))}
                  </select>
                  <input
                    type="number"
                    placeholder="From tick"
                    value={searchFromTick}
                    onChange={(e) => setSearchFromTick(e.target.value)}
                    className="rounded-md border px-2 py-1.5 text-sm"
                    aria-label="From tick"
                  />
                  <input
                    type="number"
                    placeholder="To tick"
                    value={searchToTick}
                    onChange={(e) => setSearchToTick(e.target.value)}
                    className="rounded-md border px-2 py-1.5 text-sm"
                    aria-label="To tick"
                  />
                </div>
              </CardContent>
            </Card>

            {hasActiveFilter && searchResults && (
              <Card>
                <CardHeader>
                  <CardTitle>
                    Results ({searchResults.totalRecords} found)
                  </CardTitle>
                </CardHeader>
                <CardContent>
                  {searchResults.records.length === 0 ? (
                    <p className="text-sm text-muted-foreground">No matching records.</p>
                  ) : (
                    <div className="space-y-3">
                      {searchResults.records.map((record: HistoryRecord) => (
                        <div key={record.id} className="rounded-lg border p-3">
                          <div className="flex items-center gap-2 flex-wrap">
                            <span className="text-sm font-medium">{record.title}</span>
                            <Badge variant="outline" className="text-[10px]">
                              {record.importance}
                            </Badge>
                          </div>
                          <p className="text-xs text-muted-foreground mt-0.5">
                            {record.description}
                          </p>
                          <p className="text-[10px] text-muted-foreground/60 mt-0.5">
                            Y{record.year} D{record.day} &middot; {record.season}
                            {record.locationName && ` &middot; ${record.locationName}`}
                          </p>
                        </div>
                      ))}
                    </div>
                  )}
                </CardContent>
              </Card>
            )}
          </div>
        )}
      </div>
    </div>
  )
}

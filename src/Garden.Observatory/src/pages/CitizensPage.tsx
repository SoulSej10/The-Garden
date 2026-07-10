import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Skeleton } from '@/components/ui/skeleton'
import { Progress } from '@/components/ui/progress'
import { Button } from '@/components/ui/button'
import { Avatar, AvatarFallback } from '@/components/ui/avatar'
import { Sheet } from '@/components/ui/sheet'
import { Table, TableHeader, TableBody, TableRow, TableHead, TableCell } from '@/components/ui/table'
import {
  fetchCitizens,
  fetchPopulation,
  fetchCitizenDetail,
  fetchCitizenRelationships,
  type CitizenSummary,
  type CitizenDetail,
  type CitizenRelationship,
} from '@/lib/api'

function getInitials(name: string) {
  return name.split(' ').map(n => n[0]).join('').toUpperCase().slice(0, 2)
}

function needColor(value: number, warning: number, critical: number): string {
  if (value >= critical) return 'bg-red-500'
  if (value >= warning) return 'bg-yellow-500'
  return 'bg-green-500'
}

function healthColor(value: number): string {
  if (value <= 20) return 'bg-red-500'
  if (value <= 40) return 'bg-yellow-500'
  return 'bg-green-500'
}

const PAGE_SIZES = [20, 50, 100]

export default function CitizensPage() {
  const [page, setPage] = useState(1)
  const [pageSize, setPageSize] = useState(50)
  const [sortBy, setSortBy] = useState('name')
  const [search, setSearch] = useState('')
  const [selectedId, setSelectedId] = useState<string | null>(null)

  const { data: citizensData, isLoading: citizensLoading } = useQuery({
    queryKey: ['citizens', page, pageSize, sortBy, search],
    queryFn: () => fetchCitizens(page, pageSize, sortBy, search),
    refetchInterval: 5000,
  })

  const { data: populationData, isLoading: popLoading } = useQuery({
    queryKey: ['population'],
    queryFn: fetchPopulation,
    refetchInterval: 5000,
  })

  const { data: citizenDetail, isLoading: detailLoading } = useQuery({
    queryKey: ['citizen', selectedId],
    queryFn: () => fetchCitizenDetail(selectedId!),
    enabled: selectedId !== null,
  })

  const { data: relationships } = useQuery({
    queryKey: ['citizen-relationships', selectedId],
    queryFn: () => fetchCitizenRelationships(selectedId!),
    enabled: selectedId !== null,
  })

  const handleSort = (column: string) => {
    setSortBy((prev) => (prev === column ? column : column))
    setPage(1)
  }

  return (
    <div className="mx-auto max-w-[1600px] space-y-6">
      <div>
        <h1 className="text-2xl font-semibold">Citizens</h1>
        <p className="text-sm text-muted-foreground">
          Population monitoring &mdash; {populationData?.alive ?? '—'} alive
        </p>
      </div>

      <div className="grid grid-cols-1 gap-4 md:grid-cols-2 lg:grid-cols-4">
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium text-muted-foreground">Total Population</CardTitle>
          </CardHeader>
          <CardContent>
            {popLoading ? <Skeleton className="h-7 w-12" /> : (
              <p className="text-2xl font-semibold">{populationData?.alive ?? '—'}</p>
            )}
          </CardContent>
        </Card>
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium text-muted-foreground">Deceased</CardTitle>
          </CardHeader>
          <CardContent>
            {popLoading ? <Skeleton className="h-7 w-12" /> : (
              <p className="text-2xl font-semibold text-muted-foreground">{populationData?.dead ?? '—'}</p>
            )}
          </CardContent>
        </Card>
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium text-muted-foreground">Average Age</CardTitle>
          </CardHeader>
          <CardContent>
            {popLoading ? <Skeleton className="h-7 w-16" /> : (
              <p className="text-2xl font-semibold">{populationData?.averageAge ?? '—'}</p>
            )}
          </CardContent>
        </Card>
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium text-muted-foreground">Total Deaths</CardTitle>
          </CardHeader>
          <CardContent>
            {popLoading ? <Skeleton className="h-7 w-12" /> : (
              <p className="text-2xl font-semibold text-muted-foreground">{populationData?.totalDeaths ?? '—'}</p>
            )}
          </CardContent>
        </Card>
      </div>

      <div className="flex items-center gap-4">
        <div className="relative flex-1">
          <input
            type="text"
            placeholder="Search citizens..."
            value={search}
            onChange={(e) => { setSearch(e.target.value); setPage(1) }}
            className="flex h-9 w-full rounded-md border bg-background px-3 py-1 text-sm shadow-xs transition-colors placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring"
          />
        </div>
        <div className="flex items-center gap-1 rounded-lg border p-1">
          {PAGE_SIZES.map((size) => (
            <Button
              key={size}
              variant={pageSize === size ? 'default' : 'ghost'}
              size="xs"
              onClick={() => { setPageSize(size); setPage(1) }}
            >
              {size}
            </Button>
          ))}
        </div>
      </div>

      <Card>
        <CardContent className="p-0">
          {citizensLoading ? (
            <div className="p-6 space-y-3">
              {Array.from({ length: 8 }).map((_, i) => (
                <Skeleton key={i} className="h-8 w-full" />
              ))}
            </div>
          ) : citizensData?.citizens?.length ? (
            <div className="max-h-[65vh] overflow-y-auto rounded-md">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead className="w-10" />
                  <SortableTableHead label="Name" onSort={() => handleSort('name')} />
                  <SortableTableHead label="Age" onSort={() => handleSort('age')} />
                  <SortableTableHead label="Activity" onSort={() => handleSort('activity')} />
                  <TableHead>Health</TableHead>
                  <TableHead>Hunger</TableHead>
                  <TableHead>Energy</TableHead>
                  <TableHead className="text-right">Location</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {citizensData.citizens.map((c: CitizenSummary) => (
                  <TableRow
                    key={c.id}
                    className="cursor-pointer focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-inset"
                    data-state={selectedId === c.id ? 'selected' : undefined}
                    onClick={() => setSelectedId(c.id)}
                    tabIndex={0}
                    role="button"
                    aria-label={`View details for ${c.name}`}
                    onKeyDown={(e) => {
                      if (e.key === 'Enter' || e.key === ' ') {
                        e.preventDefault()
                        setSelectedId(c.id)
                      }
                    }}
                  >
                    <TableCell>
                      <Avatar className="h-8 w-8">
                        <AvatarFallback>{getInitials(c.name)}</AvatarFallback>
                      </Avatar>
                    </TableCell>
                    <TableCell>
                      <p className="font-medium">{c.name}</p>
                      <p className="text-xs text-muted-foreground">{c.stage}</p>
                    </TableCell>
                    <TableCell>{c.age}</TableCell>
                    <TableCell>
                      <Badge variant="outline" className="text-xs">{c.currentActivity}</Badge>
                    </TableCell>
                    <TableCell className="w-24">
                      <div className="flex items-center gap-2">
                        <Progress value={c.health} indicatorClassName={healthColor(c.health)} aria-label={`Health: ${c.health.toFixed(0)}`} />
                        <span className="text-xs w-6">{c.health.toFixed(0)}</span>
                      </div>
                    </TableCell>
                    <TableCell className="w-24">
                      <div className="flex items-center gap-2">
                        <Progress
                          value={c.hunger}
                          indicatorClassName={needColor(c.hunger, 60, 80)}
                          aria-label={`Hunger: ${c.hunger.toFixed(0)}`}
                        />
                        <span className="text-xs w-6">{c.hunger.toFixed(0)}</span>
                      </div>
                    </TableCell>
                    <TableCell className="w-24">
                      <div className="flex items-center gap-2">
                        <Progress
                          value={c.energy}
                          indicatorClassName={needColor(100 - c.energy, 70, 85)}
                          aria-label={`Energy: ${c.energy.toFixed(0)}`}
                        />
                        <span className="text-xs w-6">{c.energy.toFixed(0)}</span>
                      </div>
                    </TableCell>
                    <TableCell className="text-right font-mono text-xs text-muted-foreground">
                      ({c.tileX},{c.tileY})
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
            </div>
          ) : (
            <p className="p-6 text-sm text-muted-foreground">No citizens found.</p>
          )}

          {citizensData && (
            <div className="flex items-center justify-between border-t px-4 py-3">
              <p className="text-xs text-muted-foreground">
                Page {citizensData.page} of {citizensData.totalPages} ({citizensData.total} total)
              </p>
              <div className="flex gap-1">
                <Button
                  variant="outline"
                  size="xs"
                  disabled={page <= 1}
                  onClick={() => setPage((p) => Math.max(1, p - 1))}
                >
                  Previous
                </Button>
                <Button
                  variant="outline"
                  size="xs"
                  disabled={page >= (citizensData.totalPages ?? 1)}
                  onClick={() => setPage((p) => p + 1)}
                >
                  Next
                </Button>
              </div>
            </div>
          )}
        </CardContent>
      </Card>

      <Sheet open={selectedId !== null} onClose={() => setSelectedId(null)}>
        {detailLoading ? (
          <div className="space-y-3">
            <Skeleton className="h-8 w-32" />
            <Skeleton className="h-48 w-full" />
          </div>
        ) : citizenDetail?.citizen ? (
          <CitizenDetailPanel data={citizenDetail.citizen} relationships={relationships ?? []} />
        ) : (
          <p className="text-sm text-muted-foreground">Citizen not found.</p>
        )}
      </Sheet>
    </div>
  )
}

function CitizenDetailPanel({ data, relationships }: { data: CitizenDetail; relationships: CitizenRelationship[] }) {
  return (
    <div className="space-y-6">
      <div className="flex items-center gap-3">
        <Avatar className="h-12 w-12">
          <AvatarFallback className="text-base">
            {getInitials(`${data.firstName} ${data.lastName}`)}
          </AvatarFallback>
        </Avatar>
        <div>
          <h3 className="text-lg font-semibold">{data.firstName} {data.lastName}</h3>
          <p className="text-sm text-muted-foreground">
            {data.stage} &middot; {data.age} years &middot; {data.biologicalSex}
          </p>
        </div>
      </div>

      <div>
        <p className="text-xs font-medium text-muted-foreground mb-2">Activity</p>
        <div className="rounded-lg border p-3">
          <Badge>{data.currentActivity}</Badge>
          <p className="mt-1 text-xs text-muted-foreground">
            Goal: {data.currentGoal} &middot; Tile ({data.tileX},{data.tileY})
          </p>
        </div>
      </div>

      <div>
        <p className="text-xs font-medium text-muted-foreground mb-2">Needs</p>
        <div className="space-y-2">
          {[
            { label: 'Health', value: data.needs.health, max: 100, color: healthColor(data.needs.health) },
            { label: 'Hunger', value: data.needs.hunger, max: 100, color: needColor(data.needs.hunger, 60, 80) },
            { label: 'Thirst', value: data.needs.thirst, max: 100, color: needColor(data.needs.thirst, 60, 80) },
            { label: 'Energy', value: data.needs.energy, max: 100, color: needColor(100 - data.needs.energy, 70, 85) },
            { label: 'Warmth', value: data.needs.warmth, max: 100, color: needColor(100 - data.needs.warmth, 70, 85) },
          ].map((n) => (
            <div key={n.label} className="flex items-center gap-3">
              <span className="w-14 text-xs text-muted-foreground">{n.label}</span>
              <Progress value={n.value} indicatorClassName={n.color} />
              <span className="w-8 text-xs text-right">{n.value.toFixed(0)}</span>
            </div>
          ))}
        </div>
      </div>

      <div>
        <p className="text-xs font-medium text-muted-foreground mb-2">Attributes</p>
        <div className="grid grid-cols-2 gap-2 text-sm">
          {[
            { label: 'Strength', value: data.attributes.strength },
            { label: 'Endurance', value: data.attributes.endurance },
            { label: 'Intelligence', value: data.attributes.intelligence },
            { label: 'Dexterity', value: data.attributes.dexterity },
            { label: 'Perception', value: data.attributes.perception },
          ].map((a) => (
            <div key={a.label} className="rounded border px-2 py-1">
              <p className="text-xs text-muted-foreground">{a.label}</p>
              <p className="font-medium">{a.value.toFixed(1)}</p>
            </div>
          ))}
        </div>
      </div>

      <div>
        <p className="text-xs font-medium text-muted-foreground mb-2">Personality</p>
        <div className="grid grid-cols-2 gap-2 text-sm">
          {[
            { label: 'Curiosity', value: data.personality.curiosity },
            { label: 'Patience', value: data.personality.patience },
            { label: 'Aggression', value: data.personality.aggression },
            { label: 'Compassion', value: data.personality.compassion },
            { label: 'Diligence', value: data.personality.diligence },
            { label: 'Introversion', value: data.personality.introversion },
          ].map((p) => (
            <div key={p.label} className="rounded border px-2 py-1">
              <p className="text-xs text-muted-foreground">{p.label}</p>
              <p className="font-medium">{p.value.toFixed(1)}</p>
            </div>
          ))}
        </div>
      </div>

      <div>
        <p className="text-xs font-medium text-muted-foreground mb-2">Emotions</p>
        <div className="grid grid-cols-2 gap-2 text-sm">
          {[
            { label: 'Fear', value: data.emotions.fear },
            { label: 'Joy', value: data.emotions.joy },
            { label: 'Sadness', value: data.emotions.sadness },
            { label: 'Trust', value: data.emotions.trust },
            { label: 'Curiosity', value: data.emotions.curiosity },
            { label: 'Loneliness', value: data.emotions.loneliness },
          ].map((e) => (
            <div key={e.label} className="rounded border px-2 py-1">
              <p className="text-xs text-muted-foreground">{e.label}</p>
              <p className="font-medium">{e.value.toFixed(1)}</p>
            </div>
          ))}
        </div>
      </div>

      {relationships.length > 0 && (
        <div>
          <p className="text-xs font-medium text-muted-foreground mb-2">Relationships</p>
          <div className="space-y-1">
            {relationships.map((r) => (
              <div key={r.otherCitizenId} className="rounded border px-2 py-1.5 text-sm">
                <div className="flex items-center justify-between">
                  <span className="font-medium">{r.otherCitizenName}</span>
                  <span className="text-xs text-muted-foreground">{r.interactionCount} interactions</span>
                </div>
                <div className="mt-1 flex gap-3 text-xs text-muted-foreground">
                  <span>Trust {r.trust.toFixed(0)}</span>
                  <span>Affection {r.affection.toFixed(0)}</span>
                  <span>Distance {r.socialDistance.toFixed(0)}</span>
                </div>
              </div>
            ))}
          </div>
        </div>
      )}

      <div>
        <p className="text-xs font-medium text-muted-foreground mb-2">
          What This Citizen Knows
        </p>
        {data.knownEvents.length > 0 ? (
          <ul className="space-y-1">
            {data.knownEvents.map((e) => (
              <li key={e.key} className="rounded border px-2 py-1.5 text-sm">
                {e.title}
              </li>
            ))}
          </ul>
        ) : (
          <p className="text-sm text-muted-foreground">
            Hasn't heard about any civilization events yet.
          </p>
        )}
      </div>
    </div>
  )
}

// TG-OBS-008 (Performance & Accessibility): a sortable column header needs
// to be a real focusable, keyboard-activatable control, not a <th onClick>
// that only mouse users can reach.
function SortableTableHead({ label, onSort }: { label: string; onSort: () => void }) {
  return (
    <TableHead className="p-0">
      <button
        onClick={onSort}
        className="flex w-full items-center px-2 py-2.5 text-left font-medium hover:text-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring rounded-sm"
      >
        {label}
      </button>
    </TableHead>
  )
}

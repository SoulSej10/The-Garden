import { useQuery } from '@tanstack/react-query'
import { useState } from 'react'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table'
import {
  Sheet,
  SheetContent,
  SheetHeader,
  SheetTitle,
  SheetDescription,
} from '@/components/ui/sheet'
import { Badge } from '@/components/ui/badge'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Progress } from '@/components/ui/progress'
import { fetchSettlements, fetchSettlementDetail, type SettlementSummary, type SettlementDetail } from '@/lib/api'

function formatTier(tier: string): string {
  // API sends PascalCase enum names (e.g. "RegionalCapital") - split on
  // capital letters for a readable label.
  return tier.replace(/([a-z])([A-Z])/g, '$1 $2')
}

export default function SettlementsPage() {
  const [selectedId, setSelectedId] = useState<string | null>(null)

  const { data: settlements, isLoading } = useQuery({
    queryKey: ['settlements'],
    queryFn: fetchSettlements,
    refetchInterval: 3000,
  })

  const { data: detail } = useQuery({
    queryKey: ['settlement', selectedId],
    queryFn: () => fetchSettlementDetail(selectedId!),
    enabled: !!selectedId,
    refetchInterval: 3000,
  })

  return (
    <div className="mx-auto max-w-[1600px] space-y-6">
      <div>
        <h1 className="text-2xl font-semibold">Settlements</h1>
        <p className="text-sm text-muted-foreground">
          {settlements?.length ?? 0} settlement{settlements?.length !== 1 ? 's' : ''} founded
        </p>
      </div>

      <div className="grid grid-cols-1 gap-4 md:grid-cols-3">
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium text-muted-foreground">Total Settlements</CardTitle>
          </CardHeader>
          <CardContent>
            <p className="text-2xl font-semibold">{settlements?.length ?? '—'}</p>
          </CardContent>
        </Card>
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium text-muted-foreground">Total Population</CardTitle>
          </CardHeader>
          <CardContent>
            <p className="text-2xl font-semibold">
              {settlements?.reduce((a, s) => a + s.population, 0) ?? '—'}
            </p>
          </CardContent>
        </Card>
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium text-muted-foreground">Total Buildings</CardTitle>
          </CardHeader>
          <CardContent>
            <p className="text-2xl font-semibold">
              {settlements?.reduce((a, s) => a + s.completedBuildings, 0) ?? '—'}
            </p>
          </CardContent>
        </Card>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>All Settlements</CardTitle>
        </CardHeader>
        <CardContent className="p-0">
          <div className="max-h-[65vh] overflow-y-auto rounded-md">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Name</TableHead>
                <TableHead>Tier</TableHead>
                <TableHead>Population</TableHead>
                <TableHead>Location</TableHead>
                <TableHead>Territory</TableHead>
                <TableHead>Buildings</TableHead>
                <TableHead>Food</TableHead>
                <TableHead>Wood</TableHead>
                <TableHead>Stone</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {isLoading ? (
                <TableRow>
                  <TableCell colSpan={9} className="text-center text-muted-foreground">
                    Loading...
                  </TableCell>
                </TableRow>
              ) : settlements?.length === 0 ? (
                <TableRow>
                  <TableCell colSpan={9} className="text-center text-muted-foreground">
                    No settlements yet. Citizens will found settlements as they explore.
                  </TableCell>
                </TableRow>
              ) : (
                settlements?.map((s: SettlementSummary) => (
                  <TableRow
                    key={s.id}
                    className="cursor-pointer"
                    onClick={() => setSelectedId(s.id)}
                  >
                    <TableCell className="font-medium">{s.name}</TableCell>
                    <TableCell>
                      <Badge variant="outline">{formatTier(s.tier)}</Badge>
                    </TableCell>
                    <TableCell>{s.population}</TableCell>
                    <TableCell>({s.tileX}, {s.tileY})</TableCell>
                    <TableCell>{s.territoryRadius} tiles</TableCell>
                    <TableCell>
                      {s.completedBuildings} / {s.totalBuildings}
                    </TableCell>
                    <TableCell>{Math.round(s.foodReserves)}</TableCell>
                    <TableCell>{Math.round(s.woodReserves)}</TableCell>
                    <TableCell>{Math.round(s.stoneReserves)}</TableCell>
                  </TableRow>
                ))
              )}
            </TableBody>
          </Table>
          </div>
        </CardContent>
      </Card>

      <Sheet open={!!selectedId} onClose={() => setSelectedId(null)}>
        <SheetContent className="w-[500px] overflow-y-auto sm:max-w-[500px]">
          {detail && <SettlementDetailPanel detail={detail} />}
        </SheetContent>
      </Sheet>
    </div>
  )
}

function SettlementDetailPanel({ detail }: { detail: SettlementDetail }) {
  return (
    <div className="space-y-6">
      <SheetHeader>
        <SheetTitle>{detail.name}</SheetTitle>
        <SheetDescription>
          Founded at tick {detail.foundedTick} &middot; ({detail.tileX}, {detail.tileY})
        </SheetDescription>
      </SheetHeader>

      <div>
        <h3 className="mb-2 text-sm font-medium">Overview</h3>
        <div className="grid grid-cols-2 gap-2 text-sm">
          <div className="flex items-center justify-between rounded border px-3 py-1.5">
            <span className="text-muted-foreground">Tier</span>
            <span className="font-medium">{formatTier(detail.tier)}</span>
          </div>
          <div className="flex items-center justify-between rounded border px-3 py-1.5">
            <span className="text-muted-foreground">Leader</span>
            <span className="font-medium">{detail.leaderName || 'None'}</span>
          </div>
          <div className="flex items-center justify-between rounded border px-3 py-1.5">
            <span className="text-muted-foreground">Government</span>
            <span className="font-medium">{detail.governmentType}</span>
          </div>
          <div className="flex items-center justify-between rounded border px-3 py-1.5">
            <span className="text-muted-foreground">Authority</span>
            <span className="font-medium">{detail.authoritySource}</span>
          </div>
          <div className="flex items-center justify-between rounded border px-3 py-1.5">
            <span className="text-muted-foreground">Religion</span>
            <span className="font-medium">{detail.religionName || 'None'}</span>
          </div>
          <div className="flex items-center justify-between rounded border px-3 py-1.5">
            <span className="text-muted-foreground">Technology</span>
            <span className="font-medium">{detail.technologyProgress.toFixed(0)}%</span>
          </div>
        </div>
        <div className="mt-2">
          <WellbeingBar label="Legitimacy" value={detail.legitimacy} goodHigh />
        </div>
      </div>

      <div className="grid grid-cols-2 gap-3">
        <div className="rounded-lg border p-3">
          <p className="text-xs text-muted-foreground">Population</p>
          <p className="text-xl font-semibold">{detail.population}</p>
        </div>
        <div className="rounded-lg border p-3">
          <p className="text-xs text-muted-foreground">Territory</p>
          <p className="text-xl font-semibold">{detail.territoryRadius} tiles</p>
        </div>
        <div className="rounded-lg border p-3">
          <p className="text-xs text-muted-foreground">Buildings</p>
          <p className="text-xl font-semibold">{detail.buildings.length}</p>
        </div>
        <div className="rounded-lg border p-3">
          <p className="text-xs text-muted-foreground">Members</p>
          <p className="text-xl font-semibold">{detail.members.length}</p>
        </div>
      </div>

      {detail.currentProblems.length > 0 && (
        <div>
          <h3 className="mb-2 text-sm font-medium">Current Problems</h3>
          <div className="space-y-1">
            {detail.currentProblems.map((p) => (
              <div key={p} className="rounded border border-amber-500/40 bg-amber-500/10 px-3 py-1.5 text-sm text-amber-700 dark:text-amber-400">
                {p}
              </div>
            ))}
          </div>
        </div>
      )}

      {detail.wellbeing && (
        <div>
          <h3 className="mb-2 text-sm font-medium">Wellbeing</h3>
          <p className="mb-2 text-xs text-muted-foreground">
            Averaged across living members - no dedicated morale system yet, so this reflects real citizen needs.
          </p>
          <div className="space-y-2">
            <WellbeingBar label="Health" value={detail.wellbeing.averageHealth} goodHigh />
            <WellbeingBar label="Hunger" value={detail.wellbeing.averageHunger} goodHigh={false} />
            <WellbeingBar label="Thirst" value={detail.wellbeing.averageThirst} goodHigh={false} />
            <WellbeingBar label="Energy" value={detail.wellbeing.averageEnergy} goodHigh />
          </div>
        </div>
      )}

      {detail.ongoingProjects.length > 0 && (
        <div>
          <h3 className="mb-2 text-sm font-medium">Ongoing Projects</h3>
          <div className="space-y-2">
            {detail.ongoingProjects.map((p, i) => (
              <div key={i} className="rounded-lg border p-3">
                <div className="flex items-center justify-between">
                  <span className="text-sm font-medium">{p.buildingType}</span>
                  <Badge variant="secondary">{p.status}</Badge>
                </div>
                <Progress value={p.buildTimeRequired > 0 ? (p.buildProgress / p.buildTimeRequired) * 100 : 0} className="mt-2 h-1.5" />
              </div>
            ))}
          </div>
        </div>
      )}

      {detail.nearbyResources.length > 0 && (
        <div>
          <h3 className="mb-2 text-sm font-medium">Nearby Resources</h3>
          <div className="grid grid-cols-2 gap-1">
            {detail.nearbyResources.map((r) => (
              <div key={r.type} className="flex items-center justify-between rounded border px-3 py-1.5 text-sm">
                <span>{r.type}</span>
                <span className="font-medium">{r.total}</span>
              </div>
            ))}
          </div>
        </div>
      )}

      <div>
        <h3 className="mb-2 text-sm font-medium">Not Yet Tracked</h3>
        <p className="text-xs text-muted-foreground">
          Families, security, and trade relationships aren't modeled in the simulation yet - this panel will show them here once those systems exist.
        </p>
      </div>

      <div>
        <h3 className="mb-2 text-sm font-medium">Storage</h3>
        <div className="space-y-1">
          {detail.storage.length === 0 ? (
            <p className="text-xs text-muted-foreground">Empty</p>
          ) : (
            detail.storage.map((item) => (
              <div key={item.itemType} className="flex items-center justify-between rounded border px-3 py-1.5 text-sm">
                <span>{item.itemType}</span>
                <span className="font-medium">{Math.round(item.quantity)}</span>
              </div>
            ))
          )}
        </div>
      </div>

      <div>
        <h3 className="mb-2 text-sm font-medium">Buildings</h3>
        <div className="space-y-2">
          {detail.buildings.map((b) => (
            <div key={b.id} className="rounded-lg border p-3">
              <div className="flex items-center justify-between">
                <div className="flex items-center gap-2">
                  <span className="text-sm font-medium">{b.buildingType}</span>
                  <Badge variant={b.status === 'Completed' ? 'default' : b.status === 'UnderConstruction' ? 'secondary' : 'outline'}>
                    {b.status}
                  </Badge>
                </div>
                <span className="text-xs text-muted-foreground">({b.tileX}, {b.tileY})</span>
              </div>
              {b.status !== 'Completed' && (
                <Progress value={b.buildTimeRequired > 0 ? (b.buildProgress / b.buildTimeRequired) * 100 : 0} className="mt-2 h-1.5" />
              )}
            </div>
          ))}
        </div>
      </div>

      <div>
        <h3 className="mb-2 text-sm font-medium">Members</h3>
        <div className="space-y-1">
          {detail.members.map((m) => (
            <div key={m.id} className="flex items-center justify-between rounded border px-3 py-1.5 text-sm">
              <span>{m.firstName} {m.lastName}</span>
              <span className="text-xs text-muted-foreground">
                {m.currentActivity} &middot; Age {m.age}
              </span>
            </div>
          ))}
        </div>
      </div>
    </div>
  )
}

function WellbeingBar({ label, value, goodHigh }: { label: string; value: number; goodHigh: boolean }) {
  const effective = goodHigh ? value : 100 - value
  const color = effective >= 60 ? 'bg-green-500' : effective >= 35 ? 'bg-yellow-500' : 'bg-red-500'

  return (
    <div className="flex items-center gap-3">
      <span className="w-14 text-xs text-muted-foreground">{label}</span>
      <Progress value={value} indicatorClassName={color} />
      <span className="w-8 text-right text-xs">{value.toFixed(0)}</span>
    </div>
  )
}

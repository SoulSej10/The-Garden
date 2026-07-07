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
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Name</TableHead>
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
                  <TableCell colSpan={8} className="text-center text-muted-foreground">
                    Loading...
                  </TableCell>
                </TableRow>
              ) : settlements?.length === 0 ? (
                <TableRow>
                  <TableCell colSpan={8} className="text-center text-muted-foreground">
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

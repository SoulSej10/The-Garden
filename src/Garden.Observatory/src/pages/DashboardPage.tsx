import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { fetchDashboardSummary, fetchDashboardActivity, fetchSimulationStatus, startSimulation, pauseSimulation, setSimulationSpeed, simulationStep } from '@/lib/api'

export default function DashboardPage() {
  const queryClient = useQueryClient()

  const { data: summary } = useQuery({
    queryKey: ['dashboard-summary'],
    queryFn: fetchDashboardSummary,
    refetchInterval: 2000,
  })

  const { data: activity } = useQuery({
    queryKey: ['dashboard-activity'],
    queryFn: () => fetchDashboardActivity(15),
    refetchInterval: 3000,
  })

  const { data: simStatus } = useQuery({
    queryKey: ['simulation-status'],
    queryFn: fetchSimulationStatus,
    refetchInterval: 2000,
  })

  const startMut = useMutation({ mutationFn: startSimulation, onSuccess: () => { queryClient.invalidateQueries({ queryKey: ['simulation-status'] }); queryClient.invalidateQueries({ queryKey: ['dashboard-summary'] }) } })
  const pauseMut = useMutation({ mutationFn: pauseSimulation, onSuccess: () => { queryClient.invalidateQueries({ queryKey: ['simulation-status'] }); queryClient.invalidateQueries({ queryKey: ['dashboard-summary'] }) } })
  const speedMut = useMutation({ mutationFn: setSimulationSpeed })
  const stepMut = useMutation({ mutationFn: simulationStep, onSuccess: () => { queryClient.invalidateQueries({ queryKey: ['simulation-status'] }); queryClient.invalidateQueries({ queryKey: ['dashboard-summary'] }) } })

  const speeds = [1, 2, 5, 10, 25, 50, 100, 250, 500, 1000]

  return (
    <div className="mx-auto max-w-[1600px] space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-semibold">Dashboard</h1>
          <p className="text-sm text-muted-foreground">
            {summary?.simulation.worldAge ?? '—'} &middot; Tick {summary?.simulation.tick ?? 0}
          </p>
        </div>
        <div className="flex items-center gap-2">
          <Badge variant={simStatus?.isRunning ? 'default' : 'secondary'}>
            {simStatus?.isRunning ? 'Running' : 'Paused'}
          </Badge>
        </div>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>Simulation Controls</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="flex flex-wrap items-center gap-2">
            {simStatus?.isRunning ? (
              <button onClick={() => pauseMut.mutate()} className="rounded-md bg-amber-600 px-3 py-1.5 text-xs text-white hover:bg-amber-700">Pause</button>
            ) : (
              <button onClick={() => startMut.mutate()} className="rounded-md bg-green-600 px-3 py-1.5 text-xs text-white hover:bg-green-700">Resume</button>
            )}
            <button onClick={() => stepMut.mutate()} className="rounded-md border px-3 py-1.5 text-xs hover:bg-accent">Step</button>
            <span className="text-xs text-muted-foreground mx-1">Speed:</span>
            <select
              value={simStatus?.speed ?? 1}
              onChange={(e) => speedMut.mutate(Number(e.target.value))}
              className="rounded-md border px-2 py-1.5 text-xs"
            >
              {speeds.map((s) => (
                <option key={s} value={s}>{s}x</option>
              ))}
            </select>
          </div>
        </CardContent>
      </Card>

      <div className="grid grid-cols-1 gap-4 md:grid-cols-2 lg:grid-cols-4">
        <SummaryCard title="Population" value={summary?.population.alive ?? '—'} />
        <SummaryCard title="Settlements" value={summary?.settlements.total ?? '—'} />
        <SummaryCard title="Season" value={summary?.environment.season ?? '—'} />
        <SummaryCard title="Buildings" value={summary?.settlements.totalBuildings ?? '—'} />
      </div>

      <div className="grid grid-cols-1 gap-4 lg:grid-cols-2">
        <Card>
          <CardHeader>
            <CardTitle>Population</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="space-y-2">
              <div className="flex justify-between text-sm">
                <span className="text-muted-foreground">Alive</span>
                <span className="font-medium">{summary?.population.alive ?? 0}</span>
              </div>
              <div className="flex justify-between text-sm">
                <span className="text-muted-foreground">Dead</span>
                <span className="font-medium">{summary?.population.dead ?? 0}</span>
              </div>
              <div className="flex justify-between text-sm">
                <span className="text-muted-foreground">Births</span>
                <span className="font-medium">{summary?.population.births ?? 0}</span>
              </div>
              <div className="flex justify-between text-sm">
                <span className="text-muted-foreground">Deaths</span>
                <span className="font-medium">{summary?.population.deaths ?? 0}</span>
              </div>
              <div className="flex justify-between text-sm">
                <span className="text-muted-foreground">Average Age</span>
                <span className="font-medium">{summary?.population.averageAge ?? 0}</span>
              </div>
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>Settlements</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="space-y-2">
              <div className="flex justify-between text-sm">
                <span className="text-muted-foreground">Total</span>
                <span className="font-medium">{summary?.settlements.total ?? 0}</span>
              </div>
              <div className="flex justify-between text-sm">
                <span className="text-muted-foreground">Buildings</span>
                <span className="font-medium">{summary?.settlements.totalBuildings ?? 0}</span>
              </div>
              <div className="flex justify-between text-sm">
                <span className="text-muted-foreground">Total Population</span>
                <span className="font-medium">{summary?.settlements.totalPopulation ?? 0}</span>
              </div>
              <div className="flex justify-between text-sm">
                <span className="text-muted-foreground">Food Reserves</span>
                <span className="font-medium">{summary?.settlements.totalFood ?? 0}</span>
              </div>
            </div>
          </CardContent>
        </Card>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>Recent Activity</CardTitle>
        </CardHeader>
        <CardContent>
          {!activity || activity.activities?.length === 0 ? (
            <p className="text-sm text-muted-foreground">No events recorded.</p>
          ) : (
            <div className="space-y-1 max-h-64 overflow-y-auto">
              {activity.activities.map((a: any, i: number) => (
                <div key={i} className="flex gap-3 text-sm border-b py-1.5 last:border-0">
                  <span className="shrink-0 text-[10px] text-muted-foreground w-16">
                    Y{a.year} D{a.day}
                  </span>
                  <span className="font-medium truncate">{a.title}</span>
                  <span className="shrink-0 text-[10px] text-muted-foreground/60">{a.importance}</span>
                </div>
              ))}
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  )
}

function SummaryCard({ title, value }: { title: string; value: string | number }) {
  return (
    <Card>
      <CardHeader className="pb-2">
        <CardTitle className="text-sm font-medium text-muted-foreground">{title}</CardTitle>
      </CardHeader>
      <CardContent>
        <p className="text-2xl font-semibold">{value}</p>
      </CardContent>
    </Card>
  )
}

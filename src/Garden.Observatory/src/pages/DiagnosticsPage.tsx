import { useQuery } from '@tanstack/react-query'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { fetchDiagnostics } from '@/lib/api'

export default function DiagnosticsPage() {
  const { data: diag, isLoading } = useQuery({
    queryKey: ['diagnostics'],
    queryFn: fetchDiagnostics,
    refetchInterval: 3000,
  })

  if (isLoading || !diag) {
    return (
      <div className="mx-auto max-w-[1600px] space-y-6">
        <h1 className="text-2xl font-semibold">Diagnostics</h1>
        <p className="text-sm text-muted-foreground">Loading...</p>
      </div>
    )
  }

  return (
    <div className="mx-auto max-w-[1600px] space-y-6">
      <div>
        <h1 className="text-2xl font-semibold">Diagnostics</h1>
        <p className="text-sm text-muted-foreground">Developer diagnostics &amp; performance monitoring</p>
      </div>

      <div className="grid grid-cols-1 gap-4 lg:grid-cols-2">
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center justify-between">
              <span>Simulation</span>
              <Badge variant={diag.simulation.isRunning ? 'default' : 'secondary'}>
                {diag.simulation.isRunning ? 'Running' : 'Paused'}
              </Badge>
            </CardTitle>
          </CardHeader>
          <CardContent>
            <div className="space-y-2">
              <Row label="Tick" value={diag.simulation.currentTick.toLocaleString()} />
              <Row label="Tick Duration" value={`${diag.simulation.tickDurationMs.toFixed(2)} ms`} />
              <Row label="Tick Rate" value={`${diag.simulation.tickRate.toFixed(1)}x`} />
              <Row label="Target Speed" value={`${diag.simulation.targetSpeed}x`} />
              <Row label="Uptime" value={formatUptime(diag.simulation.uptimeMs)} />
              <Row label="Registered Systems" value={diag.simulation.registeredSystems} />
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>Memory</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="space-y-2">
              <Row label="Working Set" value={`${diag.memory.workingSetMB.toFixed(1)} MB`} />
              <Row label="Private Memory" value={`${diag.memory.privateMemoryMB.toFixed(1)} MB`} />
              <Row label="Paged Memory" value={`${diag.memory.pagedMemoryMB.toFixed(1)} MB`} />
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>World</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="space-y-2">
              <Row label="Total Citizens" value={diag.world.totalCitizens} />
              <Row label="Alive" value={diag.world.aliveCitizens} />
              <Row label="Dead" value={diag.world.deadCitizens} />
              <Row label="Settlements" value={diag.world.totalSettlements} />
              <Row label="Map Size" value={`${diag.world.mapWidth} × ${diag.world.mapHeight}`} />
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>Process</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="space-y-2">
              <Row label="Process Name" value={diag.process.processName} />
              <Row label="Threads" value={diag.process.threads} />
              <Row label="Handles" value={diag.process.handleCount} />
              <Row label="Started" value={new Date(diag.process.startTime).toLocaleString()} />
            </div>
          </CardContent>
        </Card>
      </div>
    </div>
  )
}

function Row({ label, value }: { label: string; value: string | number }) {
  return (
    <div className="flex justify-between text-sm">
      <span className="text-muted-foreground">{label}</span>
      <span className="font-mono text-xs">{value}</span>
    </div>
  )
}

function formatUptime(ms: number): string {
  const s = Math.floor(ms / 1000)
  const m = Math.floor(s / 60)
  const h = Math.floor(m / 60)
  return `${h}h ${m % 60}m ${s % 60}s`
}

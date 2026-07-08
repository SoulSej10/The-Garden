import { useState } from 'react'
import { useQuery, useQueryClient } from '@tanstack/react-query'
import axios from 'axios'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { fetchDiagnostics, resetWorld } from '@/lib/api'

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

      <ResetWorldCard />
    </div>
  )
}

function ResetWorldCard() {
  const queryClient = useQueryClient()
  const [expanded, setExpanded] = useState(false)
  const [password, setPassword] = useState('')
  const [status, setStatus] = useState<
    | { kind: 'idle' }
    | { kind: 'pending' }
    | { kind: 'error'; message: string }
    | { kind: 'success'; message: string }
  >({ kind: 'idle' })

  const handleReset = async () => {
    if (!password) {
      setStatus({ kind: 'error', message: 'Enter the reset password.' })
      return
    }

    setStatus({ kind: 'pending' })
    try {
      const result = await resetWorld(password)
      setStatus({
        kind: 'success',
        message: `${result.message} (seed ${result.seed}, ${result.citizenCount} citizens)`,
      })
      setPassword('')
      queryClient.invalidateQueries()
    } catch (err) {
      if (axios.isAxiosError(err)) {
        if (err.response?.status === 401) {
          setStatus({ kind: 'error', message: 'Incorrect password.' })
        } else if (err.response?.status === 500 && err.response.data?.Error) {
          setStatus({ kind: 'error', message: err.response.data.Error })
        } else if (err.request && !err.response) {
          setStatus({ kind: 'error', message: 'Could not reach the server. Check your connection and try again.' })
        } else {
          setStatus({ kind: 'error', message: err.response?.data?.Error ?? 'Reset failed for an unknown reason.' })
        }
      } else {
        setStatus({ kind: 'error', message: 'Unexpected error. Please try again.' })
      }
    }
  }

  return (
    <Card className="border-destructive/30">
      <CardHeader>
        <CardTitle className="flex items-center justify-between text-destructive">
          <span>Danger Zone</span>
          <Badge variant="outline" className="border-destructive/40 text-destructive">
            Dev Only
          </Badge>
        </CardTitle>
      </CardHeader>
      <CardContent>
        {!expanded ? (
          <div className="flex items-center justify-between gap-4">
            <p className="text-sm text-muted-foreground">
              Permanently wipes all citizens, settlements, and history, then generates a fresh world
              and spawns a new population of 50. This cannot be undone.
            </p>
            <Button
              variant="outline"
              className="shrink-0 border-destructive/40 text-destructive hover:bg-destructive/10"
              onClick={() => {
                setExpanded(true)
                setStatus({ kind: 'idle' })
              }}
            >
              Reset World
            </Button>
          </div>
        ) : (
          <div className="space-y-3">
            <p className="text-sm text-muted-foreground">
              This will delete all citizens, settlements, and historical records, then start over with
              a fresh map and 50 new citizens. Enter the reset password to confirm.
            </p>
            <div className="flex flex-wrap items-center gap-2">
              <input
                type="password"
                autoFocus
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                onKeyDown={(e) => {
                  if (e.key === 'Enter') handleReset()
                }}
                placeholder="Reset password"
                disabled={status.kind === 'pending'}
                className="w-56 rounded-md border bg-background px-3 py-1.5 text-sm outline-none ring-offset-background focus-visible:ring-2 focus-visible:ring-destructive disabled:opacity-50"
              />
              <Button
                variant="destructive"
                disabled={status.kind === 'pending'}
                onClick={handleReset}
              >
                {status.kind === 'pending' ? 'Resetting...' : 'Confirm Reset'}
              </Button>
              <Button
                variant="ghost"
                disabled={status.kind === 'pending'}
                onClick={() => {
                  setExpanded(false)
                  setPassword('')
                  setStatus({ kind: 'idle' })
                }}
              >
                Cancel
              </Button>
            </div>
            {status.kind === 'error' && (
              <p className="text-sm font-medium text-destructive">{status.message}</p>
            )}
            {status.kind === 'success' && (
              <p className="text-sm font-medium text-emerald-600 dark:text-emerald-400">{status.message}</p>
            )}
          </div>
        )}
      </CardContent>
    </Card>
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

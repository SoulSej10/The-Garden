import { useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Play, Pause, SkipForward, Gauge } from '@phosphor-icons/react'
import { fetchSimulationStatus, startSimulation, pauseSimulation, setSimulationSpeed, simulationStep } from '@/lib/api'
import { useSimulationHub } from '@/lib/useSimulationHub'
import { cn } from '@/lib/utils'

const SPEEDS = [1, 2, 5, 10, 25, 50, 100, 250, 500, 1000]

/**
 * Bottom-center HUD capsule: the world's pulse. Prefers the SignalR tick
 * push (SimulationHostedService broadcasts every ~1s) over the 5s poll so
 * the counter visibly moves in real time instead of stalling on a
 * backgrounded tab - the exact failure mode found during the earlier
 * bug-hunt session, now actually fixed by consuming the hub that already
 * existed but nothing rendered.
 */
export function TimeHud() {
  const queryClient = useQueryClient()
  const [speedMenuOpen, setSpeedMenuOpen] = useState(false)
  const { data: status } = useQuery({
    queryKey: ['simulation-status'],
    queryFn: fetchSimulationStatus,
    refetchInterval: 5000,
  })
  const hub = useSimulationHub()

  const isRunning = hub.status !== 'Unknown' ? hub.status === 'Running' : Boolean(status?.isRunning)
  const tick = hub.tick || status?.totalTicks || 0
  const speed = hub.speed || status?.speed || 1

  const invalidate = () => {
    queryClient.invalidateQueries({ queryKey: ['simulation-status'] })
    queryClient.invalidateQueries({ queryKey: ['dashboard-summary'] })
  }
  const startMut = useMutation({ mutationFn: startSimulation, onSuccess: invalidate })
  const pauseMut = useMutation({ mutationFn: pauseSimulation, onSuccess: invalidate })
  const stepMut = useMutation({ mutationFn: simulationStep, onSuccess: invalidate })
  const speedMut = useMutation({ mutationFn: setSimulationSpeed, onSuccess: invalidate })

  return (
    <div className="pointer-events-none absolute inset-x-0 bottom-4 z-30 flex justify-center md:bottom-6">
      <div className="pointer-events-auto flex items-center gap-1.5 rounded-full border border-border/70 bg-panel/90 p-1.5 shadow-atlas-lg backdrop-blur-md">
        <button
          onClick={() => (isRunning ? pauseMut.mutate() : startMut.mutate())}
          className="flex h-10 w-10 items-center justify-center rounded-full bg-primary text-primary-foreground shadow-atlas transition-transform active:scale-95"
          title={isRunning ? 'Pause' : 'Resume'}
        >
          {isRunning ? <Pause size={17} weight="fill" /> : <Play size={17} weight="fill" />}
        </button>
        <button
          onClick={() => stepMut.mutate()}
          className="flex h-8 w-8 items-center justify-center rounded-full text-muted-foreground transition-colors hover:bg-accent hover:text-accent-foreground"
          title="Step one tick"
        >
          <SkipForward size={14} weight="bold" />
        </button>

        <div className="mx-1 h-6 w-px bg-border" />

        <div className="flex min-w-[3.5rem] flex-col items-center px-1">
          <span className="font-display text-sm font-semibold tabular-nums leading-tight">{tick.toLocaleString()}</span>
          <span className="text-[9px] font-medium uppercase tracking-wider text-muted-foreground">tick</span>
        </div>

        <div className="mx-1 h-6 w-px bg-border" />

        <div className="relative">
          <button
            onClick={() => setSpeedMenuOpen((v) => !v)}
            className="flex h-8 items-center gap-1 rounded-full bg-accent px-3 font-display text-xs font-semibold text-accent-foreground"
          >
            <Gauge size={13} weight="bold" />
            {speed}×
          </button>
          {speedMenuOpen && (
            <div className="absolute bottom-full left-1/2 mb-2 flex -translate-x-1/2 flex-wrap justify-center gap-1 rounded-2xl border border-border/70 bg-panel/95 p-1.5 shadow-atlas-lg backdrop-blur-md" style={{ width: '11rem' }}>
              {SPEEDS.map((s) => (
                <button
                  key={s}
                  onClick={() => {
                    speedMut.mutate(s)
                    setSpeedMenuOpen(false)
                  }}
                  className={cn(
                    'h-7 rounded-full px-2 font-display text-[11px] font-medium transition-colors',
                    speed === s ? 'bg-primary text-primary-foreground' : 'text-muted-foreground hover:bg-accent'
                  )}
                >
                  {s}×
                </button>
              ))}
            </div>
          )}
        </div>
      </div>
    </div>
  )
}

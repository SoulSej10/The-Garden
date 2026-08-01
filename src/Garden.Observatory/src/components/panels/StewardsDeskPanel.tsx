import { useState } from 'react'
import { useQuery, useQueryClient } from '@tanstack/react-query'
import axios from 'axios'
import { Warning } from '@phosphor-icons/react'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Tabs, TabsList, TabsTrigger, TabsContent } from '@/components/ui/tabs'
import {
  fetchDiagnostics, fetchSystemHealth, fetchSystemStatistics, fetchSystemSaves, fetchSystemBackups,
  fetchAssistantSummary, fetchTimeline, saveWorld, loadWorld, deleteSave, askQuestion, resetWorld,
  type TimelineSaveEntry,
} from '@/lib/api'

/**
 * Production Dashboard + Diagnostics merged, and deliberately kept the most
 * utilitarian panel in the app - dense functional tables re-skinned to the
 * material language, not reshaped into game metaphors where that would just
 * get in the way of an operator doing a save/load/reset.
 */
export default function StewardsDeskPanel() {
  const [tab, setTab] = useState('status')

  return (
    <div className="space-y-5">
      <Tabs value={tab} onValueChange={setTab}>
        <TabsList>
          <TabsTrigger value="status">Status</TabsTrigger>
          <TabsTrigger value="saves">Saves</TabsTrigger>
          <TabsTrigger value="backups">Backups</TabsTrigger>
          <TabsTrigger value="assistant">Assistant</TabsTrigger>
        </TabsList>
        <TabsContent value="status"><StatusTab /></TabsContent>
        <TabsContent value="saves"><SavesTab /></TabsContent>
        <TabsContent value="backups"><BackupsTab /></TabsContent>
        <TabsContent value="assistant"><AssistantTab /></TabsContent>
      </Tabs>

      <DangerZone />
    </div>
  )
}

function StatusTab() {
  const { data: diag } = useQuery({ queryKey: ['diagnostics'], queryFn: fetchDiagnostics, refetchInterval: 3000 })
  const { data: health } = useQuery({ queryKey: ['system-health'], queryFn: fetchSystemHealth, refetchInterval: 30000 })
  const { data: stats } = useQuery({ queryKey: ['system-statistics'], queryFn: fetchSystemStatistics, refetchInterval: 5000 })

  if (!diag || !health || !stats) return <p className="py-6 text-center text-sm text-muted-foreground">Reading the instruments…</p>

  return (
    <div className="space-y-4">
      <Group title="Simulation" badge={<Badge variant={diag.simulation.isRunning ? 'default' : 'secondary'}>{diag.simulation.isRunning ? 'Running' : 'Paused'}</Badge>}>
        <Row label="Tick" value={diag.simulation.currentTick.toLocaleString()} />
        <Row label="Tick duration" value={`${diag.simulation.tickDurationMs.toFixed(2)} ms`} />
        <Row label="Target speed" value={`${diag.simulation.targetSpeed}×`} />
        <Row label="Uptime" value={formatUptime(diag.simulation.uptimeMs)} />
        <Row label="Systems" value={diag.simulation.registeredSystems} />
        <Row label="API status" value={health.status} />
        <Row label="Version" value={health.version} />
      </Group>

      <Group title="Performance">
        <Row label="Working set" value={`${stats.performance.workingSetMB.toFixed(0)} MB`} />
        <Row label="Private memory" value={`${stats.performance.privateMemoryMB.toFixed(0)} MB`} />
        <Row label="Threads" value={stats.performance.threadCount} />
        <Row label="Handles" value={stats.performance.handleCount} />
        <Row label="CPU time" value={`${stats.performance.cpuTime.toFixed(0)}s`} />
      </Group>

      <Group title="World">
        <Row label="Citizens" value={`${diag.world.aliveCitizens} / ${diag.world.totalCitizens}`} />
        <Row label="Settlements" value={diag.world.totalSettlements} />
        <Row label="Map size" value={`${diag.world.mapWidth} × ${diag.world.mapHeight}`} />
      </Group>

      <Group title="Process">
        <Row label="Name" value={diag.process.processName} />
        <Row label="Started" value={new Date(diag.process.startTime).toLocaleString()} />
      </Group>
    </div>
  )
}

function SavesTab() {
  const [saveName, setSaveName] = useState('')
  const [loadName, setLoadName] = useState('')
  const { data: saves, refetch: refetchSaves } = useQuery({ queryKey: ['system-saves'], queryFn: fetchSystemSaves, refetchInterval: 30000 })
  const { data: timeline, refetch: refetchTimeline } = useQuery({ queryKey: ['timeline'], queryFn: fetchTimeline, refetchInterval: 30000 })

  const handleSave = async () => {
    try {
      await saveWorld(saveName || undefined)
      setSaveName('')
      refetchSaves()
      refetchTimeline()
    } catch (e) {
      console.error('Save failed', e)
    }
  }
  const handleLoad = async () => {
    try {
      await loadWorld(loadName)
      setLoadName('')
      refetchTimeline()
    } catch (e) {
      console.error('Load failed', e)
    }
  }
  const handleDelete = async (name: string) => {
    try {
      await deleteSave(name)
      refetchSaves()
    } catch (e) {
      console.error('Delete failed', e)
    }
  }

  return (
    <div className="space-y-4">
      <div className="space-y-2">
        <div className="flex gap-1.5">
          <input type="text" placeholder="Save name (optional)" value={saveName} onChange={(e) => setSaveName(e.target.value)} className="h-9 flex-1 rounded-full border border-border bg-card px-3 text-sm outline-none focus-visible:ring-2 focus-visible:ring-ring" />
          <Button size="sm" onClick={handleSave}>Save</Button>
        </div>
        <div className="flex gap-1.5">
          <input type="text" placeholder="Load name" value={loadName} onChange={(e) => setLoadName(e.target.value)} className="h-9 flex-1 rounded-full border border-border bg-card px-3 text-sm outline-none focus-visible:ring-2 focus-visible:ring-ring" />
          <Button size="sm" variant="secondary" disabled={!loadName.trim()} onClick={handleLoad}>Load</Button>
        </div>
        {saves && saves.saves.length > 0 && (
          <div className="space-y-1 pt-1">
            {saves.saves.map((s) => (
              <div key={s.name} className="flex items-center justify-between rounded-xl border border-border/70 px-3 py-1.5 text-xs">
                <span className="font-medium">{s.name}</span>
                <span className="text-muted-foreground">{(s.sizeBytes / 1024).toFixed(0)} KB</span>
                <button onClick={() => handleDelete(s.name)} className="text-status-danger hover:underline">Delete</button>
              </div>
            ))}
          </div>
        )}
      </div>

      {timeline && (
        <div>
          <p className="mb-1 font-display text-xs font-semibold uppercase tracking-wide text-muted-foreground">Timeline ({timeline.length})</p>
          <p className="mb-2 text-[11px] text-muted-foreground">Loading an earlier save and continuing creates a new branch — no branch is the "correct" one.</p>
          {timeline.length === 0 ? <p className="text-sm text-muted-foreground">No saves yet.</p> : <TimelineTree entries={timeline} />}
        </div>
      )}
    </div>
  )
}

function TimelineTree({ entries }: { entries: TimelineSaveEntry[] }) {
  const roots = entries.filter((e) => e.parentSaveId === null || !entries.some((p) => p.id === e.parentSaveId))
  const childrenOf = (id: string) => entries.filter((e) => e.parentSaveId === id)

  const renderNode = (entry: TimelineSaveEntry, depth: number): React.ReactNode => (
    <div key={entry.id}>
      <div className="flex items-center gap-2 py-0.5 text-[11px]" style={{ paddingLeft: `${depth * 14}px` }}>
        <span className="font-medium">{entry.name}</span>
        <span className="text-muted-foreground">tick {entry.tick}</span>
      </div>
      {childrenOf(entry.id).map((child) => renderNode(child, depth + 1))}
    </div>
  )

  return <div className="scroll-atlas max-h-40 space-y-0.5 overflow-y-auto rounded-xl border border-border/70 p-2">{roots.map((r) => renderNode(r, 0))}</div>
}

function BackupsTab() {
  const { data: backups } = useQuery({ queryKey: ['system-backups'], queryFn: fetchSystemBackups, refetchInterval: 60000 })
  if (!backups) return null
  if (backups.count === 0) return <p className="py-6 text-center text-sm text-muted-foreground">No backups yet. Backups run hourly.</p>

  return (
    <div className="space-y-1.5">
      {backups.backups.map((b) => (
        <div key={b.name} className="rounded-xl border border-border/70 px-3 py-2 text-sm">
          <div className="flex items-center justify-between">
            <span className="font-medium text-xs">{b.name}</span>
            <Badge variant="outline" className="text-[10px] capitalize">{b.type}</Badge>
          </div>
          <div className="mt-1 flex items-center gap-3 text-[11px] text-muted-foreground">
            <span>{b.citizenCount} citizens</span>
            <span>{b.settlementCount} settlements</span>
            <span>{new Date(b.createdAt).toLocaleDateString()}</span>
          </div>
        </div>
      ))}
    </div>
  )
}

function AssistantTab() {
  const [question, setQuestion] = useState('')
  const [answer, setAnswer] = useState<string | null>(null)
  const { data: summary } = useQuery({ queryKey: ['assistant-summary'], queryFn: fetchAssistantSummary, refetchInterval: 15000 })

  const handleAsk = async () => {
    if (!question.trim()) return
    try {
      const result = await askQuestion(question)
      setAnswer(result.answer)
    } catch (e) {
      console.error('Question failed', e)
    }
  }

  return (
    <div className="space-y-4">
      <div className="flex gap-1.5">
        <input
          type="text"
          placeholder="Ask about the world…"
          value={question}
          onChange={(e) => setQuestion(e.target.value)}
          onKeyDown={(e) => e.key === 'Enter' && handleAsk()}
          className="h-9 flex-1 rounded-full border border-border bg-card px-3 text-sm outline-none focus-visible:ring-2 focus-visible:ring-ring"
        />
        <Button size="sm" disabled={!question.trim()} onClick={handleAsk}>Ask</Button>
      </div>
      {answer && (
        <div className="panel-carved border border-border/70 bg-card p-3 text-sm">
          <p className="mb-1 text-xs text-muted-foreground">Answer</p>
          <p className="font-chronicle italic">{answer}</p>
        </div>
      )}
      {summary && (
        <div>
          <p className="mb-1.5 font-display text-xs font-semibold uppercase tracking-wide text-muted-foreground">World Summary</p>
          <div className="panel-carved border border-border/70 bg-card p-3">
            <p className="font-chronicle text-sm italic leading-relaxed">{summary.narrative}</p>
            <div className="mt-2 grid grid-cols-2 gap-1 text-[11px] text-muted-foreground">
              <span>{summary.statistics.aliveCitizens} alive</span>
              <span>{summary.statistics.totalSettlements} settlements</span>
              <span>{summary.statistics.totalKingdoms} kingdoms</span>
              <span>{summary.statistics.technologiesDiscovered} techs</span>
            </div>
          </div>
          {summary.insights?.length > 0 && (
            <div className="mt-2 space-y-1.5">
              {summary.insights.map((insight) => (
                <div key={insight.topic} className="rounded-xl border border-border/70 px-3 py-1.5 text-sm">
                  <p className="mb-0.5 text-[10px] font-medium uppercase tracking-wide text-muted-foreground">{insight.topic}</p>
                  <p>{insight.summary}</p>
                </div>
              ))}
            </div>
          )}
        </div>
      )}
    </div>
  )
}

function DangerZone() {
  const queryClient = useQueryClient()
  const [step, setStep] = useState<'idle' | 'confirming' | 'password'>('idle')
  const [password, setPassword] = useState('')
  const [status, setStatus] = useState<
    { kind: 'idle' } | { kind: 'pending' } | { kind: 'error'; message: string } | { kind: 'success'; message: string }
  >({ kind: 'idle' })

  const close = () => {
    setStep('idle')
    setPassword('')
    setStatus({ kind: 'idle' })
  }

  const handleReset = async () => {
    if (!password) {
      setStatus({ kind: 'error', message: 'Enter the reset password.' })
      return
    }
    setStatus({ kind: 'pending' })
    try {
      const result = await resetWorld(password)
      setStatus({ kind: 'success', message: `${result.message} (seed ${result.seed}, ${result.citizenCount} citizens)` })
      setPassword('')
      queryClient.invalidateQueries()
    } catch (err) {
      if (axios.isAxiosError(err)) {
        if (err.response?.status === 401) setStatus({ kind: 'error', message: 'Incorrect password.' })
        else if (err.response?.status === 500 && err.response.data?.Error) setStatus({ kind: 'error', message: err.response.data.Error })
        else if (err.request && !err.response) setStatus({ kind: 'error', message: 'Could not reach the server. Check your connection and try again.' })
        else setStatus({ kind: 'error', message: err.response?.data?.Error ?? 'Reset failed for an unknown reason.' })
      } else {
        setStatus({ kind: 'error', message: 'Unexpected error. Please try again.' })
      }
    }
  }

  return (
    <>
      <div className="panel-carved border border-status-danger/40 bg-status-danger/5 p-4">
        <div className="mb-2 flex items-center justify-between">
          <span className="flex items-center gap-1.5 font-display text-sm font-semibold text-status-danger">
            <Warning size={15} weight="fill" /> Danger Zone
          </span>
          <Badge variant="outline" className="border-status-danger/40 text-status-danger">Dev only</Badge>
        </div>
        <p className="mb-3 text-xs text-muted-foreground">
          Permanently wipes all citizens, settlements, and history, then generates a fresh world and spawns 50 new citizens. This cannot be undone.
        </p>
        <Button variant="outline" size="sm" className="border-status-danger/40 text-status-danger hover:bg-status-danger/10" onClick={() => setStep('confirming')}>
          Reset World
        </Button>
      </div>

      {step !== 'idle' && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-background-deep/60 p-4 backdrop-blur-sm" onClick={(e) => { if (e.target === e.currentTarget && status.kind !== 'pending') close() }}>
          <div className="panel-carved w-full max-w-sm border border-border bg-panel p-5 shadow-atlas-lg">
            {step === 'confirming' ? (
              <div className="space-y-4">
                <h3 className="font-display text-lg font-semibold text-status-danger">Reset the entire world?</h3>
                <p className="text-sm text-muted-foreground">This permanently deletes every citizen, settlement, and historical record, then generates a brand new world. There is no undo.</p>
                <div className="flex justify-end gap-2">
                  <Button variant="ghost" onClick={close}>Cancel</Button>
                  <Button variant="destructive" onClick={() => setStep('password')}>Continue</Button>
                </div>
              </div>
            ) : (
              <div className="space-y-4">
                <h3 className="font-display text-lg font-semibold text-status-danger">Confirm with password</h3>
                <input
                  type="password"
                  autoFocus
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  onKeyDown={(e) => e.key === 'Enter' && handleReset()}
                  placeholder="Reset password"
                  disabled={status.kind === 'pending'}
                  className="h-9 w-full rounded-full border border-border bg-card px-3 text-sm outline-none focus-visible:ring-2 focus-visible:ring-destructive disabled:opacity-50"
                />
                {status.kind === 'error' && <p className="text-sm font-medium text-status-danger">{status.message}</p>}
                {status.kind === 'success' && <p className="text-sm font-medium text-status-health">{status.message}</p>}
                <div className="flex justify-end gap-2">
                  <Button variant="ghost" disabled={status.kind === 'pending'} onClick={close}>{status.kind === 'success' ? 'Close' : 'Cancel'}</Button>
                  {status.kind !== 'success' && (
                    <Button variant="destructive" disabled={status.kind === 'pending'} onClick={handleReset}>
                      {status.kind === 'pending' ? 'Resetting…' : 'Confirm Reset'}
                    </Button>
                  )}
                </div>
              </div>
            )}
          </div>
        </div>
      )}
    </>
  )
}

function Group({ title, badge, children }: { title: string; badge?: React.ReactNode; children: React.ReactNode }) {
  return (
    <div>
      <div className="mb-1.5 flex items-center justify-between">
        <p className="font-display text-xs font-semibold uppercase tracking-wide text-muted-foreground">{title}</p>
        {badge}
      </div>
      <div className="space-y-1 rounded-xl border border-border/70 p-2.5">{children}</div>
    </div>
  )
}

function Row({ label, value }: { label: string; value: string | number }) {
  return (
    <div className="flex justify-between text-xs">
      <span className="text-muted-foreground">{label}</span>
      <span className="font-mono">{value}</span>
    </div>
  )
}

/** Clamped to zero: negative/near-zero uptime (possible right at process
 * start due to clock-resolution noise) used to render "-1s" because
 * Math.floor rounds small negative fractions down, not toward zero. */
function formatUptime(ms: number): string {
  const s = Math.max(0, Math.floor(ms / 1000))
  const m = Math.floor(s / 60)
  const h = Math.floor(m / 60)
  return `${h}h ${m % 60}m ${s % 60}s`
}

import { useQuery } from '@tanstack/react-query'
import { fetchSystemHealth, fetchSystemStatistics, fetchSystemSaves, fetchSystemBackups,
  fetchAssistantSummary, fetchTimeline, saveWorld, loadWorld, deleteSave, askQuestion,
  type TimelineSaveEntry } from '../lib/api'
import { useState } from 'react'

export default function ProductionDashboardPage() {
  const [saveName, setSaveName] = useState('')
  const [loadName, setLoadName] = useState('')
  const [question, setQuestion] = useState('')
  const [answer, setAnswer] = useState<string | null>(null)

  const { data: health } = useQuery({
    queryKey: ['system-health'],
    queryFn: fetchSystemHealth,
    refetchInterval: 30000
  })

  const { data: stats } = useQuery({
    queryKey: ['system-statistics'],
    queryFn: fetchSystemStatistics,
    refetchInterval: 5000
  })

  const { data: saves, refetch: refetchSaves } = useQuery({
    queryKey: ['system-saves'],
    queryFn: fetchSystemSaves,
    refetchInterval: 30000
  })

  const { data: backups } = useQuery({
    queryKey: ['system-backups'],
    queryFn: fetchSystemBackups,
    refetchInterval: 60000
  })

  const { data: timeline, refetch: refetchTimeline } = useQuery({
    queryKey: ['timeline'],
    queryFn: fetchTimeline,
    refetchInterval: 30000
  })

  const { data: summary } = useQuery({
    queryKey: ['assistant-summary'],
    queryFn: fetchAssistantSummary,
    refetchInterval: 15000
  })

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
    try { await deleteSave(name); refetchSaves() } catch (e) { console.error('Delete failed', e) }
  }

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
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold tracking-tight">Production Dashboard</h1>
        <p className="text-sm text-muted-foreground">System status, performance, and administrative controls</p>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
        <section className="rounded-lg border p-4 space-y-3">
          <h2 className="font-semibold">System Status</h2>
          {health ? (
            <div className="grid grid-cols-2 gap-3 text-sm">
              <div className="rounded bg-muted/30 p-2">
                <div className="text-xs text-muted-foreground">Status</div>
                <div className="font-medium text-green-500">{health.status}</div>
              </div>
              <div className="rounded bg-muted/30 p-2">
                <div className="text-xs text-muted-foreground">Version</div>
                <div className="font-medium">{health.version}</div>
              </div>
              <div className="rounded bg-muted/30 p-2">
                <div className="text-xs text-muted-foreground">Uptime</div>
                <div className="font-medium">{formatDuration(health.uptime)}</div>
              </div>
              <div className="rounded bg-muted/30 p-2">
                <div className="text-xs text-muted-foreground">Simulation</div>
                <div className={`font-medium ${health.simulationRunning ? 'text-green-500' : 'text-yellow-500'}`}>
                  {health.simulationRunning ? 'Running' : 'Paused'}
                </div>
              </div>
            </div>
          ) : (
            <p className="text-sm text-muted-foreground">Loading...</p>
          )}
        </section>

        <section className="rounded-lg border p-4 space-y-3">
          <h2 className="font-semibold">Performance</h2>
          {stats ? (
            <div className="grid grid-cols-2 gap-3 text-sm">
              <div className="rounded bg-muted/30 p-2">
                <div className="text-xs text-muted-foreground">Working Set</div>
                <div className="font-medium">{stats.performance.workingSetMB.toFixed(0)} MB</div>
              </div>
              <div className="rounded bg-muted/30 p-2">
                <div className="text-xs text-muted-foreground">Private Memory</div>
                <div className="font-medium">{stats.performance.privateMemoryMB.toFixed(0)} MB</div>
              </div>
              <div className="rounded bg-muted/30 p-2">
                <div className="text-xs text-muted-foreground">Threads</div>
                <div className="font-medium">{stats.performance.threadCount}</div>
              </div>
              <div className="rounded bg-muted/30 p-2">
                <div className="text-xs text-muted-foreground">Handles</div>
                <div className="font-medium">{stats.performance.handleCount}</div>
              </div>
              <div className="rounded bg-muted/30 p-2">
                <div className="text-xs text-muted-foreground">CPU Time</div>
                <div className="font-medium">{(stats.performance.cpuTime).toFixed(0)}s</div>
              </div>
              <div className="rounded bg-muted/30 p-2">
                <div className="text-xs text-muted-foreground">Tick Duration</div>
                <div className="font-medium">{stats.simulation.tickDurationMs.toFixed(1)}ms</div>
              </div>
            </div>
          ) : (
            <p className="text-sm text-muted-foreground">Loading...</p>
          )}
        </section>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
        <section className="rounded-lg border p-4 space-y-3">
          <h2 className="font-semibold">Save / Load</h2>
          <div className="flex gap-2">
            <input
              type="text"
              placeholder="Save name (optional)"
              value={saveName}
              onChange={(e) => setSaveName(e.target.value)}
              className="flex-1 rounded border px-3 py-1.5 text-sm bg-background"
            />
            <button
              onClick={handleSave}
              className="rounded bg-primary px-3 py-1.5 text-sm text-primary-foreground hover:bg-primary/90"
            >
              Save
            </button>
          </div>
          <div className="flex gap-2">
            <input
              type="text"
              placeholder="Load name"
              value={loadName}
              onChange={(e) => setLoadName(e.target.value)}
              className="flex-1 rounded border px-3 py-1.5 text-sm bg-background"
            />
            <button
              onClick={handleLoad}
              className="rounded bg-accent px-3 py-1.5 text-sm hover:bg-accent/90"
              disabled={!loadName.trim()}
            >
              Load
            </button>
          </div>
          {saves && saves.saves.length > 0 && (
            <div className="text-sm">
              <div className="text-xs text-muted-foreground mb-1">Existing saves:</div>
              <div className="space-y-1 max-h-32 overflow-y-auto">
                {saves.saves.map((s) => (
                  <div key={s.name} className="flex items-center justify-between text-xs">
                    <span className="font-medium">{s.name}</span>
                    <span className="text-muted-foreground">{(s.sizeBytes / 1024).toFixed(0)} KB</span>
                    <button
                      onClick={() => handleDelete(s.name)}
                      className="text-red-500 hover:text-red-700 text-xs"
                    >
                      Delete
                    </button>
                  </div>
                ))}
              </div>
            </div>
          )}
        </section>

        <section className="rounded-lg border p-4 space-y-4">
          <h2 className="font-semibold">AI Assistant</h2>
          <div className="flex gap-2">
            <input
              type="text"
              placeholder="Ask about the world..."
              value={question}
              onChange={(e) => setQuestion(e.target.value)}
              className="flex-1 rounded border px-3 py-1.5 text-sm bg-background"
              onKeyDown={(e) => e.key === 'Enter' && handleAsk()}
            />
            <button
              onClick={handleAsk}
              className="rounded bg-primary px-3 py-1.5 text-sm text-primary-foreground hover:bg-primary/90"
              disabled={!question.trim()}
            >
              Ask
            </button>
          </div>
          {answer && (
            <div className="rounded bg-muted/30 p-3 text-sm">
              <div className="text-xs text-muted-foreground mb-1">Answer:</div>
              <p>{answer}</p>
            </div>
          )}
          {summary && (
            <div>
              <div className="text-xs text-muted-foreground mb-1">World Summary:</div>
              <div className="rounded bg-muted/30 p-3 text-sm">
                <p>{summary.narrative}</p>
                <div className="mt-2 grid grid-cols-4 gap-1 text-xs text-muted-foreground">
                  <span>{summary.statistics.aliveCitizens} alive</span>
                  <span>{summary.statistics.totalSettlements} settlements</span>
                  <span>{summary.statistics.totalKingdoms} kingdoms</span>
                  <span>{summary.statistics.technologiesDiscovered} techs</span>
                </div>
              </div>
              {summary.insights?.length > 0 && (
                <div className="mt-3 grid grid-cols-1 gap-2 sm:grid-cols-2">
                  {summary.insights.map((insight) => (
                    <div key={insight.topic} className="rounded border p-3 text-sm">
                      <div className="mb-1 text-xs font-medium text-muted-foreground">{insight.topic}</div>
                      <p className="text-sm">{insight.summary}</p>
                    </div>
                  ))}
                </div>
              )}
            </div>
          )}
        </section>
      </div>

      {timeline && (
        <section className="rounded-lg border p-4 space-y-3">
          <h2 className="font-semibold">Timeline ({timeline.length})</h2>
          <p className="text-xs text-muted-foreground">
            Loading an earlier save and continuing creates a new branch - no branch is the "correct" one, each is its own valid history.
          </p>
          {timeline.length === 0 ? (
            <p className="text-sm text-muted-foreground">No saves yet.</p>
          ) : (
            <TimelineTree entries={timeline} />
          )}
        </section>
      )}

      {backups && (
        <section className="rounded-lg border p-4 space-y-3">
          <h2 className="font-semibold">Backups ({backups.count})</h2>
          {backups.count === 0 ? (
            <p className="text-sm text-muted-foreground">No backups yet. Backups run hourly.</p>
          ) : (
            <div className="overflow-x-auto text-sm">
              <table className="w-full">
                <thead className="bg-muted/50">
                  <tr>
                    <th className="text-left p-2 font-medium">Type</th>
                    <th className="text-left p-2 font-medium">Name</th>
                    <th className="text-right p-2 font-medium">Citizens</th>
                    <th className="text-right p-2 font-medium">Settlements</th>
                    <th className="text-left p-2 font-medium">Created</th>
                  </tr>
                </thead>
                <tbody>
                  {backups.backups.map((b) => (
                    <tr key={b.name} className="border-t hover:bg-muted/20">
                      <td className="p-2">
                        <span className={`rounded px-1.5 py-0.5 text-xs ${b.type === 'weekly' ? 'bg-blue-100 text-blue-700 dark:bg-blue-900/30 dark:text-blue-400' : b.type === 'daily' ? 'bg-green-100 text-green-700 dark:bg-green-900/30 dark:text-green-400' : 'bg-muted/30'}`}>
                          {b.type}
                        </span>
                      </td>
                      <td className="p-2 font-medium text-xs">{b.name}</td>
                      <td className="p-2 text-right">{b.citizenCount}</td>
                      <td className="p-2 text-right">{b.settlementCount}</td>
                      <td className="p-2 text-xs text-muted-foreground">{new Date(b.createdAt).toLocaleString()}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </section>
      )}
    </div>
  )
}

// RFC-017: renders the flat save list as a branch tree, client-side, from
// each entry's parentSaveId - the same "server returns flat data, client
// renders structure" pattern already used elsewhere in the Observatory.
function TimelineTree({ entries }: { entries: TimelineSaveEntry[] }) {
  const roots = entries.filter((e) => e.parentSaveId === null || !entries.some((p) => p.id === e.parentSaveId))
  const childrenOf = (id: string) => entries.filter((e) => e.parentSaveId === id)

  const renderNode = (entry: TimelineSaveEntry, depth: number) => (
    <div key={entry.id}>
      <div className="flex items-center gap-2 text-xs py-0.5" style={{ paddingLeft: `${depth * 16}px` }}>
        <span className="font-medium">{entry.name}</span>
        <span className="text-muted-foreground">tick {entry.tick}</span>
        <span className="text-muted-foreground">{new Date(entry.savedAt).toLocaleString()}</span>
      </div>
      {childrenOf(entry.id).map((child) => renderNode(child, depth + 1))}
    </div>
  )

  return <div className="space-y-0.5 max-h-48 overflow-y-auto">{roots.map((r) => renderNode(r, 0))}</div>
}

function formatDuration(seconds: number): string {
  const h = Math.floor(seconds / 3600)
  const m = Math.floor((seconds % 3600) / 60)
  const s = Math.floor(seconds % 60)
  if (h > 0) return `${h}h ${m}m`
  if (m > 0) return `${m}m ${s}s`
  return `${s}s`
}
import { useEffect, useRef, useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { TrendUp, TrendDown, Baby, Skull, HouseLine, Hammer, Basket, GaugeIcon } from '@phosphor-icons/react'
import { fetchDashboardSummary, fetchHistoryStats, fetchEconomy, fetchEconomyResources } from '@/lib/api'
import { LineChart, MultiLineChart } from '@/components/ui/charts'
import { cn } from '@/lib/utils'

const HISTORY_LENGTH = 40
const RESOURCE_COLORS = ['var(--color-primary)', 'var(--color-status-water)', 'var(--color-status-hunger)', 'var(--color-status-danger)']

/**
 * Default content of the sidebar's display window (panel = null / "Overview"
 * tab). VitalsCluster answers "is the world alive right now" in one glance
 * (population/settlements/weather); this answers "which direction is it
 * moving" - line charts built from samples taken on the client (no backend
 * endpoint returns a timeseries), not just numbers.
 */
export function CivilizationOverview() {
  const { data: summary } = useQuery({
    queryKey: ['dashboard-summary'],
    queryFn: fetchDashboardSummary,
    refetchInterval: 5000,
  })
  const { data: historyStats } = useQuery({
    queryKey: ['history-stats'],
    queryFn: fetchHistoryStats,
    refetchInterval: 8000,
  })
  const { data: economy } = useQuery({
    queryKey: ['economy'],
    queryFn: fetchEconomy,
    refetchInterval: 8000,
  })
  const { data: resources } = useQuery({
    queryKey: ['economy-resources'],
    queryFn: fetchEconomyResources,
    refetchInterval: 10000,
  })

  const population = summary?.population.alive
  const populationHistory = useTrend(population)
  const birthsHistory = useTrend(historyStats?.births)
  const deathsHistory = useTrend(historyStats?.deaths)

  const topResourceKeys = resources
    ? Object.entries(resources)
        .sort((a, b) => b[1] - a[1])
        .slice(0, 4)
        .map(([label]) => label)
    : []
  const resourceHistories = useResourceTrends(resources, topResourceKeys)

  if (!summary) return null

  return (
    <div className="space-y-4 p-3">
      <p className="font-display text-[10px] font-semibold uppercase tracking-wide text-muted-foreground">
        Civilization
      </p>

      <div>
        <div className="flex items-center justify-between">
          <span className="text-[11px] text-muted-foreground">Population</span>
          <span className="flex items-center gap-1 font-display text-lg font-semibold tabular-nums">
            {population ?? '–'}
            {trendDirection(populationHistory) === 'up' && <TrendUp size={13} weight="bold" className="text-status-thriving" />}
            {trendDirection(populationHistory) === 'down' && <TrendDown size={13} weight="bold" className="text-status-danger" />}
          </span>
        </div>
        <LineChart data={populationHistory} height={80} />
      </div>

      <div>
        <p className="mb-1.5 text-[10px] uppercase tracking-wide text-muted-foreground">Births vs deaths</p>
        <MultiLineChart
          height={88}
          series={[
            { label: 'Births', data: birthsHistory, color: 'var(--color-status-thriving)' },
            { label: 'Deaths', data: deathsHistory, color: 'var(--color-status-danger)' },
          ]}
        />
      </div>

      {topResourceKeys.length > 0 && (
        <div>
          <p className="mb-1.5 text-[10px] uppercase tracking-wide text-muted-foreground">Resource stockpiles</p>
          <MultiLineChart
            height={88}
            formatValue={(v) => (v >= 1000 ? `${Math.round(v / 1000)}k` : Math.round(v).toString())}
            series={topResourceKeys.map((key, i) => ({
              label: key,
              data: resourceHistories[key] ?? [],
              color: RESOURCE_COLORS[i % RESOURCE_COLORS.length],
            }))}
          />
        </div>
      )}

      <div className="grid grid-cols-2 gap-1.5 border-t border-border/60 pt-2.5">
        <MiniStat icon={<HouseLine size={12} weight="bold" />} label="Buildings" value={summary.settlements.totalBuildings} />
        <MiniStat icon={<Hammer size={12} weight="bold" />} label="Goods" value={economy?.globalGoodsCrafted != null ? Math.round(economy.globalGoodsCrafted) : undefined} />
        <MiniStat icon={<Baby size={12} weight="bold" />} label="Births" value={historyStats?.births} tone="thriving" />
        <MiniStat icon={<Skull size={12} weight="bold" />} label="Deaths" value={historyStats?.deaths} tone="danger" />
      </div>

      <div className="flex items-center justify-between border-t border-border/60 pt-2.5 text-[10px] text-muted-foreground">
        <span className="flex items-center gap-1">
          <Basket size={11} weight="bold" /> Trades {economy?.globalTradeCount ?? '–'}
        </span>
        <span className="flex items-center gap-1">
          <GaugeIcon size={11} weight="bold" /> Avg age {summary.population.averageAge != null ? Math.round(summary.population.averageAge) : '–'}
        </span>
      </div>
    </div>
  )
}

function useTrend(value: number | undefined) {
  const [history, setHistory] = useState<number[]>([])
  const lastValueRef = useRef<number | undefined>(undefined)

  useEffect(() => {
    if (value == null || value === lastValueRef.current) return
    lastValueRef.current = value
    setHistory((prev) => [...prev, value].slice(-HISTORY_LENGTH))
  }, [value])

  return history
}

/** Same idea as useTrend, but tracking a dynamic set of resource-type keys at once. */
function useResourceTrends(resources: Record<string, number> | undefined, keys: string[]) {
  const [histories, setHistories] = useState<Record<string, number[]>>({})
  const lastRef = useRef<Record<string, number>>({})

  useEffect(() => {
    if (!resources) return
    let changed = false
    const next = { ...histories }
    for (const key of keys) {
      const value = resources[key]
      if (value == null || value === lastRef.current[key]) continue
      lastRef.current[key] = value
      next[key] = [...(next[key] ?? []), value].slice(-HISTORY_LENGTH)
      changed = true
    }
    if (changed) setHistories(next)
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [resources, keys.join(',')])

  return histories
}

function trendDirection(history: number[]): 'up' | 'down' | 'flat' {
  if (history.length < 2) return 'flat'
  const first = history[0]
  const last = history[history.length - 1]
  if (last > first * 1.002) return 'up'
  if (last < first * 0.998) return 'down'
  return 'flat'
}

function MiniStat({
  icon,
  label,
  value,
  tone,
}: {
  icon: React.ReactNode
  label: string
  value?: number
  tone?: 'thriving' | 'danger'
}) {
  return (
    <div className="flex items-center gap-1.5 rounded-lg bg-muted/50 px-1.5 py-1">
      <span
        className={cn(
          'text-muted-foreground',
          tone === 'thriving' && 'text-status-thriving',
          tone === 'danger' && 'text-status-danger'
        )}
      >
        {icon}
      </span>
      <div className="min-w-0 leading-tight">
        <p className="font-display text-xs font-semibold tabular-nums">{value ?? '–'}</p>
        <p className="truncate text-[9px] text-muted-foreground">{label}</p>
      </div>
    </div>
  )
}

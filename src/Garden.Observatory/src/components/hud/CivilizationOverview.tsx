import { useEffect, useRef, useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { TrendUp, TrendDown, Baby, Skull, HouseLine, Hammer, Basket, GaugeIcon } from '@phosphor-icons/react'
import { fetchDashboardSummary, fetchHistoryStats, fetchEconomy } from '@/lib/api'
import { cn } from '@/lib/utils'

const HISTORY_LENGTH = 40

/**
 * Left-side persistent readout, below VitalsCluster. VitalsCluster answers
 * "is the world alive right now" in one glance (population/settlements/
 * weather); this answers "which direction is it moving" - population and
 * food trend as sparklines built from samples taken on the client (no
 * history endpoint returns a timeseries), births/deaths/food/labor as
 * always-visible numbers instead of requiring a trip into the Almanac or
 * Chronicle panels. Secondary-priority content per the "glanceable in three
 * seconds" brief - sits quietly under the vitals, never competes with them.
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

  const population = summary?.population.alive
  const populationHistory = useTrend(population)
  const food = summary?.settlements.totalFood
  const foodHistory = useTrend(food)

  if (!summary) return null

  const populationTrend = trendDirection(populationHistory)
  const foodTrend = trendDirection(foodHistory)

  return (
    <div className="pointer-events-none absolute left-4 top-16 z-20 hidden w-56 sm:block md:left-6 md:top-[4.25rem]">
      <div className="pointer-events-auto panel-carved space-y-2.5 border border-border/70 bg-panel/85 p-3 shadow-atlas backdrop-blur-md">
        <p className="font-display text-[10px] font-semibold uppercase tracking-wide text-muted-foreground">
          Civilization
        </p>

        <TrendRow
          label="Population"
          value={population}
          history={populationHistory}
          trend={populationTrend}
        />
        <TrendRow label="Settlement food" value={food != null ? Math.round(food) : undefined} history={foodHistory} trend={foodTrend} />

        <div className="grid grid-cols-2 gap-1.5 border-t border-border/60 pt-2">
          <MiniStat icon={<Baby size={12} weight="bold" />} label="Births" value={historyStats?.births} tone="thriving" />
          <MiniStat icon={<Skull size={12} weight="bold" />} label="Deaths" value={historyStats?.deaths} tone="danger" />
          <MiniStat icon={<HouseLine size={12} weight="bold" />} label="Buildings" value={summary.settlements.totalBuildings} />
          <MiniStat icon={<Hammer size={12} weight="bold" />} label="Goods" value={economy?.globalGoodsCrafted != null ? Math.round(economy.globalGoodsCrafted) : undefined} />
        </div>

        <div className="flex items-center justify-between border-t border-border/60 pt-2 text-[10px] text-muted-foreground">
          <span className="flex items-center gap-1">
            <Basket size={11} weight="bold" /> Trades {economy?.globalTradeCount ?? '–'}
          </span>
          <span className="flex items-center gap-1">
            <GaugeIcon size={11} weight="bold" /> Avg age {summary.population.averageAge != null ? Math.round(summary.population.averageAge) : '–'}
          </span>
        </div>
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

function trendDirection(history: number[]): 'up' | 'down' | 'flat' {
  if (history.length < 2) return 'flat'
  const first = history[0]
  const last = history[history.length - 1]
  if (last > first * 1.002) return 'up'
  if (last < first * 0.998) return 'down'
  return 'flat'
}

function Sparkline({ history }: { history: number[] }) {
  if (history.length < 2) {
    return <div className="h-6 w-full" />
  }
  const min = Math.min(...history)
  const max = Math.max(...history)
  const range = max - min || 1
  const points = history
    .map((v, i) => {
      const x = (i / (history.length - 1)) * 100
      const y = 24 - ((v - min) / range) * 22 - 1
      return `${x},${y}`
    })
    .join(' ')

  return (
    <svg viewBox="0 0 100 24" preserveAspectRatio="none" className="h-6 w-full overflow-visible">
      <polyline
        points={points}
        fill="none"
        stroke="currentColor"
        strokeWidth="1.75"
        strokeLinecap="round"
        strokeLinejoin="round"
        className="text-primary transition-all duration-500"
      />
    </svg>
  )
}

function TrendRow({
  label,
  value,
  history,
  trend,
}: {
  label: string
  value?: number
  history: number[]
  trend: 'up' | 'down' | 'flat'
}) {
  return (
    <div>
      <div className="flex items-center justify-between">
        <span className="text-[11px] text-muted-foreground">{label}</span>
        <span className="flex items-center gap-1 font-display text-sm font-semibold tabular-nums">
          {value ?? '–'}
          {trend === 'up' && <TrendUp size={12} weight="bold" className="text-status-thriving" />}
          {trend === 'down' && <TrendDown size={12} weight="bold" className="text-status-danger" />}
        </span>
      </div>
      <Sparkline history={history} />
    </div>
  )
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

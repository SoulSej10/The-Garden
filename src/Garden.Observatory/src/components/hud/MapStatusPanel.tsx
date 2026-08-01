import { useQuery } from '@tanstack/react-query'
import { UsersThree, HouseLine, Baby, Skull, Basket, Sun, CloudSun, Cloud, CloudRain, CloudSnow, CloudLightning } from '@phosphor-icons/react'
import { fetchDashboardSummary, fetchHistoryStats } from '@/lib/api'

function weatherIcon(condition?: string) {
  const c = (condition ?? '').toLowerCase()
  if (c.includes('storm') || c.includes('thunder')) return CloudLightning
  if (c.includes('snow')) return CloudSnow
  if (c.includes('rain')) return CloudRain
  if (c.includes('cloud')) return Cloud
  if (c.includes('clear') || c.includes('sun')) return Sun
  return CloudSun
}

/**
 * World/civilization status strip under the map - ambient, always-visible
 * numbers (not a deep-dive) distinct from the sidebar's tabbed display
 * window: this answers "how is the world doing" without switching tabs
 * away from whatever's open on the right.
 */
export function MapStatusPanel() {
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

  const WeatherIcon = weatherIcon(summary?.environment.weather)

  return (
    <div className="grid shrink-0 grid-cols-3 gap-px border-t border-border/70 bg-border/60 sm:grid-cols-6">
      <StatCell icon={<UsersThree size={14} weight="bold" />} label="Population" value={summary?.population.alive} />
      <StatCell icon={<HouseLine size={14} weight="bold" />} label="Settlements" value={summary?.settlements.total} />
      <StatCell icon={<Baby size={14} weight="bold" />} label="Births" value={historyStats?.births} />
      <StatCell icon={<Skull size={14} weight="bold" />} label="Deaths" value={historyStats?.deaths} />
      <StatCell icon={<Basket size={14} weight="bold" />} label="Food stores" value={summary?.settlements.totalFood != null ? Math.round(summary.settlements.totalFood) : undefined} />
      <StatCell
        icon={<WeatherIcon size={14} weight="bold" />}
        label={summary?.environment.season ?? 'Weather'}
        value={summary?.environment.temperature != null ? `${Math.round(summary.environment.temperature)}°` : undefined}
      />
    </div>
  )
}

function StatCell({ icon, label, value }: { icon: React.ReactNode; label: string; value?: number | string }) {
  return (
    <div className="flex items-center gap-2 bg-panel/80 px-3 py-2 backdrop-blur-md">
      <span className="text-primary">{icon}</span>
      <div className="min-w-0 leading-tight">
        <p className="font-display text-sm font-semibold tabular-nums">{value ?? '–'}</p>
        <p className="truncate text-[9px] uppercase tracking-wide text-muted-foreground">{label}</p>
      </div>
    </div>
  )
}

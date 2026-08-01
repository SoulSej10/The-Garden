import { useQuery } from '@tanstack/react-query'
import { Sun, CloudSun, Cloud, CloudRain, CloudSnow, CloudLightning, UsersThree, HouseLine, Leaf } from '@phosphor-icons/react'
import { fetchDashboardSummary } from '@/lib/api'
import { useCitizenHub } from '@/lib/useSimulationHub'

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
 * Top-left HUD corner: the world's vital signs, always visible, never a
 * grid of equal-sized dashboard cards. Population prefers the SignalR
 * citizenHub push (see useSimulationHub.ts, previously built but never
 * consumed anywhere) and falls back to the polled dashboard summary -
 * so the count keeps moving even if the push channel is still connecting.
 */
export function VitalsCluster() {
  const { data: summary } = useQuery({
    queryKey: ['dashboard-summary'],
    queryFn: fetchDashboardSummary,
    refetchInterval: 5000,
  })
  const { population } = useCitizenHub()

  const alive = population?.alive ?? summary?.population.alive
  const settlements = summary?.settlements.total
  const season = summary?.environment.season
  const weather = summary?.environment.weather
  const temperature = summary?.environment.temperature
  const WeatherIcon = weatherIcon(weather)

  return (
    <div className="pointer-events-none absolute left-4 top-4 z-30 md:left-6 md:top-6">
      <div className="pointer-events-auto flex items-center gap-0.5 rounded-full border border-border/70 bg-panel/90 py-1.5 pl-1.5 pr-2 shadow-atlas-lg backdrop-blur-md">
        <div className="flex h-8 w-8 items-center justify-center rounded-full bg-primary/15 text-primary">
          <Leaf size={16} weight="fill" />
        </div>
        <span className="hidden pl-1.5 pr-2.5 font-display text-sm font-semibold sm:inline">The Garden</span>
        <div className="hidden h-6 w-px bg-border sm:block" />
        <VitalChip icon={<UsersThree size={14} weight="bold" />} value={alive} title="Living citizens" />
        <VitalChip icon={<HouseLine size={14} weight="bold" />} value={settlements} title="Settlements" />
        <VitalChip
          icon={<WeatherIcon size={14} weight="bold" />}
          value={temperature != null ? `${Math.round(temperature)}°` : undefined}
          title={season ? `${season} · ${weather}` : weather}
        />
      </div>
    </div>
  )
}

function VitalChip({ icon, value, title }: { icon: React.ReactNode; value?: number | string; title?: string }) {
  return (
    <div
      className="flex items-center gap-1.5 rounded-full px-2 py-1 text-xs font-medium text-muted-foreground"
      title={title}
    >
      <span className="text-primary">{icon}</span>
      <span className="tabular-nums text-foreground">{value ?? '–'}</span>
    </div>
  )
}

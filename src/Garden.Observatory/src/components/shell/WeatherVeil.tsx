import { useQuery } from '@tanstack/react-query'
import { fetchWeather } from '@/lib/api'

/**
 * Ambient environmental framing (research principle 6): weather should be
 * felt on the world itself, not led with a stat-card wall. This is a thin,
 * non-interactive veil over the map - a color wash plus a drifting texture
 * for precipitation - with the numbers still available on demand in the
 * Almanac panel for anyone who wants them.
 */
export function WeatherVeil() {
  const { data: weather } = useQuery({
    queryKey: ['weather'],
    queryFn: fetchWeather,
    refetchInterval: 15000,
  })

  const condition = (weather?.condition ?? 'Clear').toLowerCase()
  const intensity = Math.min(1, Math.max(0.15, weather?.intensity ?? 0.3))

  const wash = condition.includes('storm')
    ? `radial-gradient(circle at 30% 20%, oklch(0.3 0.03 260 / ${0.32 * intensity}), transparent 65%)`
    : condition.includes('rain')
      ? `linear-gradient(180deg, oklch(0.55 0.04 230 / ${0.16 * intensity}), transparent 70%)`
      : condition.includes('snow')
        ? `linear-gradient(180deg, oklch(0.95 0.01 240 / ${0.22 * intensity}), transparent 70%)`
        : condition.includes('cloud')
          ? `linear-gradient(180deg, oklch(0.6 0.01 240 / ${0.12 * intensity}), transparent 75%)`
          : `radial-gradient(circle at 50% -10%, oklch(0.85 0.1 80 / ${0.14 * intensity}), transparent 60%)`

  const showStreaks = condition.includes('rain') || condition.includes('storm')
  const showFlakes = condition.includes('snow')

  return (
    <div className="pointer-events-none absolute inset-0 z-20 overflow-hidden mix-blend-multiply dark:mix-blend-screen">
      <div className="absolute inset-0 transition-[background] duration-[3000ms]" style={{ background: wash }} />
      {showStreaks && (
        <div
          className="animate-drift absolute -inset-x-10 -inset-y-10 opacity-40"
          style={{
            backgroundImage:
              'repeating-linear-gradient(115deg, transparent 0 6px, oklch(0.7 0.03 230 / 0.35) 6px 7px, transparent 7px 34px)',
          }}
        />
      )}
      {showFlakes && (
        <div
          className="animate-drift absolute -inset-x-10 -inset-y-10 opacity-50"
          style={{
            backgroundImage:
              'radial-gradient(oklch(1 0 0 / 0.9) 1.4px, transparent 1.6px)',
            backgroundSize: '28px 28px',
          }}
        />
      )}
    </div>
  )
}

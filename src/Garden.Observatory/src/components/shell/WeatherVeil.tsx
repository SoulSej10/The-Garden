import { useQuery } from '@tanstack/react-query'
import { fetchWeatherGrid, type WeatherGridCell } from '@/lib/api'

interface WeatherVeilProps {
  offsetX: number
  offsetY: number
  gridWidth: number
  gridHeight: number
}

/**
 * Ambient environmental framing (research principle 6): weather should be
 * felt on the world itself, not led with a stat-card wall. Now genuinely
 * spatial - each weather cell (see WeatherSystem/WorldState.WeatherCells on
 * the backend) is mapped to its actual screen rectangle within the current
 * camera viewport, so panning/zooming across the map reveals different
 * regions mid-storm, mid-drought, or clear, instead of one uniform tint.
 */
export function WeatherVeil({ offsetX, offsetY, gridWidth, gridHeight }: WeatherVeilProps) {
  const { data: grid } = useQuery({
    queryKey: ['weather-grid'],
    queryFn: fetchWeatherGrid,
    refetchInterval: 4000,
  })

  if (!grid || grid.cellsX === 0) return null

  const visible = grid.cells.filter((cell) => {
    const cellStartX = cell.cellX * grid.tileSize
    const cellStartY = cell.cellY * grid.tileSize
    return (
      cellStartX < offsetX + gridWidth &&
      cellStartX + grid.tileSize > offsetX &&
      cellStartY < offsetY + gridHeight &&
      cellStartY + grid.tileSize > offsetY
    )
  })

  return (
    <div className="pointer-events-none absolute inset-0 z-20 overflow-hidden">
      {visible.map((cell) => (
        <WeatherCellVeil key={`${cell.cellX}-${cell.cellY}`} cell={cell} grid={grid} offsetX={offsetX} offsetY={offsetY} gridWidth={gridWidth} gridHeight={gridHeight} />
      ))}
    </div>
  )
}

function WeatherCellVeil({
  cell,
  grid,
  offsetX,
  offsetY,
  gridWidth,
  gridHeight,
}: {
  cell: WeatherGridCell
  grid: { tileSize: number }
  offsetX: number
  offsetY: number
  gridWidth: number
  gridHeight: number
}) {
  const cellStartX = cell.cellX * grid.tileSize
  const cellStartY = cell.cellY * grid.tileSize

  const leftPct = ((cellStartX - offsetX) / gridWidth) * 100
  const topPct = ((cellStartY - offsetY) / gridHeight) * 100
  const widthPct = (grid.tileSize / gridWidth) * 100
  const heightPct = (grid.tileSize / gridHeight) * 100

  const condition = cell.condition.toLowerCase()
  // "Clear" cells (the common case) should be effectively invisible - this
  // was previously flooring at 0.12 even for clear skies, which meant every
  // one of the ~64 cells tinted the map at once and the whole thing read as
  // a uniform haze instead of weather standing out only where it matters.
  const isClear = condition === 'clear'
  const intensity = isClear ? 0 : Math.min(1, Math.max(0.25, cell.intensity))

  const wash = condition.includes('storm')
    ? `radial-gradient(circle at 40% 30%, oklch(0.3 0.03 260 / ${0.22 * intensity}), transparent 70%)`
    : condition.includes('heavyrain') || condition.includes('rain')
      ? `linear-gradient(180deg, oklch(0.55 0.04 230 / ${0.1 * intensity}), transparent 75%)`
      : condition.includes('snow')
        ? `linear-gradient(180deg, oklch(0.95 0.01 240 / ${0.16 * intensity}), transparent 75%)`
        : condition.includes('fog')
          ? `linear-gradient(180deg, oklch(0.75 0.005 240 / ${0.18 * intensity}), transparent 80%)`
          : condition.includes('cloud')
            ? `linear-gradient(180deg, oklch(0.6 0.01 240 / ${0.07 * intensity}), transparent 80%)`
            : 'none'

  const streaky = condition.includes('rain') || condition.includes('storm')
  const flaky = condition.includes('snow')
  const driftAngle = Math.atan2(cell.windDirY, cell.windDirX) * (180 / Math.PI)
  const driftDuration = 40 / Math.max(0.15, Math.abs(cell.windStrength) + 0.15)

  return (
    <div
      className="absolute overflow-hidden transition-[background] duration-[3000ms]"
      style={{
        left: `${leftPct}%`,
        top: `${topPct}%`,
        width: `${widthPct}%`,
        height: `${heightPct}%`,
        background: wash,
      }}
    >
      {streaky && (
        <div
          className="animate-drift absolute -inset-y-10"
          style={{
            left: '-40%',
            right: '-40%',
            opacity: 0.18,
            animationDuration: `${driftDuration}s`,
            backgroundImage: `repeating-linear-gradient(${115 + driftAngle}deg, transparent 0 6px, oklch(0.7 0.03 230 / 0.3) 6px 7px, transparent 7px 34px)`,
          }}
        />
      )}
      {flaky && (
        <div
          className="animate-drift absolute -inset-y-10"
          style={{
            left: '-40%',
            right: '-40%',
            opacity: 0.45,
            animationDuration: `${driftDuration}s`,
            backgroundImage: 'radial-gradient(oklch(1 0 0 / 0.9) 1.4px, transparent 1.6px)',
            backgroundSize: '28px 28px',
          }}
        />
      )}
    </div>
  )
}

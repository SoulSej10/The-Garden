/**
 * Small hand-rolled SVG chart primitives - no charting library in this
 * project (recharts was pulled during the redesign), and these are simple
 * enough not to need one. Sized and labeled to actually read as a chart,
 * not a decorative squiggle next to a number.
 */

interface LineChartProps {
  data: number[]
  height?: number
  color?: string
  label?: string
  formatValue?: (v: number) => string
}

export function LineChart({ data, height = 72, color = 'var(--color-primary)', label, formatValue }: LineChartProps) {
  const fmt = formatValue ?? ((v: number) => Math.round(v).toString())
  const width = 300

  if (data.length < 2) {
    return (
      <div className="flex items-center justify-center text-xs text-muted-foreground" style={{ height }}>
        Gathering data…
      </div>
    )
  }

  const min = Math.min(...data)
  const max = Math.max(...data)
  const range = max - min || 1
  const padTop = 8
  const padBottom = 16
  const plotHeight = height - padTop - padBottom

  const points = data.map((v, i) => {
    const x = (i / (data.length - 1)) * width
    const y = padTop + plotHeight - ((v - min) / range) * plotHeight
    return { x, y }
  })
  const linePath = points.map((p, i) => `${i === 0 ? 'M' : 'L'} ${p.x.toFixed(1)} ${p.y.toFixed(1)}`).join(' ')
  const areaPath = `${linePath} L ${width} ${height - padBottom} L 0 ${height - padBottom} Z`

  const gridLines = [0, 0.5, 1].map((f) => padTop + plotHeight * f)

  return (
    <div>
      {label && <p className="mb-1 text-[10px] uppercase tracking-wide text-muted-foreground">{label}</p>}
      <svg viewBox={`0 0 ${width} ${height}`} preserveAspectRatio="none" className="h-auto w-full" style={{ height }}>
        {gridLines.map((y) => (
          <line key={y} x1={0} x2={width} y1={y} y2={y} stroke="currentColor" strokeOpacity={0.08} strokeWidth={1} />
        ))}
        <path d={areaPath} fill={color} fillOpacity={0.14} stroke="none" />
        <path d={linePath} fill="none" stroke={color} strokeWidth={2} strokeLinecap="round" strokeLinejoin="round" className="transition-all duration-500" />
        <circle cx={points[points.length - 1].x} cy={points[points.length - 1].y} r={3} fill={color} />
        <text x={2} y={height - 4} fontSize={9} fill="currentColor" opacity={0.5}>
          {fmt(min)}
        </text>
        <text x={width - 2} y={height - 4} fontSize={9} fill="currentColor" opacity={0.5} textAnchor="end">
          {fmt(max)}
        </text>
      </svg>
    </div>
  )
}

interface BarChartDatum {
  label: string
  value: number
  color?: string
}

interface BarChartProps {
  data: BarChartDatum[]
  height?: number
  formatValue?: (v: number) => string
}

export function BarChart({ data, height = 96, formatValue }: BarChartProps) {
  const fmt = formatValue ?? ((v: number) => Math.round(v).toString())
  const max = Math.max(...data.map((d) => d.value), 1)

  return (
    <div className="flex items-end gap-2" style={{ height }}>
      {data.map((d) => {
        const barHeight = Math.max(2, (d.value / max) * (height - 28))
        return (
          <div key={d.label} className="flex flex-1 flex-col items-center justify-end gap-1">
            <span className="text-[10px] font-semibold tabular-nums">{fmt(d.value)}</span>
            <div
              className="w-full rounded-t-md transition-all duration-500"
              style={{ height: barHeight, background: d.color ?? 'var(--color-primary)' }}
            />
            <span className="truncate text-[9px] text-muted-foreground">{d.label}</span>
          </div>
        )
      })}
    </div>
  )
}

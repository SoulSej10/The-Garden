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

interface LineSeries {
  label: string
  data: number[]
  color: string
}

interface MultiLineChartProps {
  series: LineSeries[]
  height?: number
  formatValue?: (v: number) => string
}

/** Same visual language as LineChart, multiple named/colored lines sharing one scale - for direct comparisons like births vs deaths over time. */
export function MultiLineChart({ series, height = 88, formatValue }: MultiLineChartProps) {
  const fmt = formatValue ?? ((v: number) => Math.round(v).toString())
  const width = 300
  const longestLen = Math.max(...series.map((s) => s.data.length), 0)

  if (longestLen < 2) {
    return (
      <div className="flex items-center justify-center text-xs text-muted-foreground" style={{ height }}>
        Gathering data…
      </div>
    )
  }

  const allValues = series.flatMap((s) => s.data)
  const min = Math.min(...allValues, 0)
  const max = Math.max(...allValues, 1)
  const range = max - min || 1
  const padTop = 8
  const padBottom = 18
  const plotHeight = height - padTop - padBottom
  const gridLines = [0, 0.5, 1].map((f) => padTop + plotHeight * f)

  return (
    <div>
      <svg viewBox={`0 0 ${width} ${height}`} preserveAspectRatio="none" className="h-auto w-full" style={{ height }}>
        {gridLines.map((y) => (
          <line key={y} x1={0} x2={width} y1={y} y2={y} stroke="currentColor" strokeOpacity={0.08} strokeWidth={1} />
        ))}
        {series.map((s) => {
          if (s.data.length < 2) return null
          const points = s.data.map((v, i) => {
            const x = (i / (s.data.length - 1)) * width
            const y = padTop + plotHeight - ((v - min) / range) * plotHeight
            return { x, y }
          })
          const linePath = points.map((p, i) => `${i === 0 ? 'M' : 'L'} ${p.x.toFixed(1)} ${p.y.toFixed(1)}`).join(' ')
          const last = points[points.length - 1]
          return (
            <g key={s.label}>
              <path d={linePath} fill="none" stroke={s.color} strokeWidth={2} strokeLinecap="round" strokeLinejoin="round" className="transition-all duration-500" />
              <circle cx={last.x} cy={last.y} r={3} fill={s.color} />
            </g>
          )
        })}
      </svg>
      <div className="mt-1 flex items-center justify-center gap-4">
        {series.map((s) => (
          <span key={s.label} className="flex items-center gap-1.5 text-[10px] text-muted-foreground">
            <span className="h-2 w-2 rounded-full" style={{ background: s.color }} />
            {s.label} <span className="font-semibold tabular-nums text-foreground">{fmt(s.data[s.data.length - 1] ?? 0)}</span>
          </span>
        ))}
      </div>
    </div>
  )
}


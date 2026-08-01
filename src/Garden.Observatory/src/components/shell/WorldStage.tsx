import { useCallback, useEffect, useMemo, useState } from 'react'
import { useQuery, keepPreviousData } from '@tanstack/react-query'
import { Rows, Sparkle, UsersThree, HouseLine, X } from '@phosphor-icons/react'
import { fetchMap, fetchTile, fetchWorldStatus, fetchCitizens, fetchSettlements } from '@/lib/api'
import type { TileData } from '@/lib/api'
import { WorldMapCanvas, type MapOverlay } from '@/components/WorldMapCanvas'
import { useLocalStorageState } from '@/lib/useLocalStorageState'
import { cn } from '@/lib/utils'
import { WeatherVeil } from './WeatherVeil'

// Reference ROW count per zoom tier - the actual fetched/rendered grid is
// gridHeight=tier, gridWidth=round(tier*aspectRatio), so every tier below
// "Max" fills the actual screen rectangle edge-to-edge (tiles stay square,
// wider screens just show proportionally more columns) instead of forcing
// a square crop that letterboxes on any non-square viewport. "Max" is the
// one deliberate exception - the whole (square) world genuinely can't fill
// a wide rectangle without distortion or cropping, so it letterboxes on
// purpose as the "see the whole planet" view.
const BASE_VIEW_SIZES = [24, 40, 64, 100, 160] as const
const VIEW_SIZE_LABELS: Record<number, string> = { 24: 'Local', 40: 'Near', 64: 'Regional', 100: 'Wide', 160: 'Continental' }

function clamp(value: number, min: number, max: number) {
  return Math.max(min, Math.min(max, value))
}

/**
 * The world map, permanently mounted as the shell's backdrop instead of a
 * routed page - every panel floats on top of this and can be dismissed to
 * reveal it again. Interaction logic (pan/zoom/select, adaptive polling,
 * settlement/citizen overlay dots) is carried over unchanged from the old
 * WorldMapPage; only the chrome around it changes shape and position to sit
 * as floating HUD pieces instead of a page header + sidebar cards.
 */
export function WorldStage() {
  const [viewSize, setViewSize] = useLocalStorageState<number>('garden.map.viewSize', 64)
  const [offsetX, setOffsetX] = useLocalStorageState<number>('garden.map.offsetX', 0)
  const [offsetY, setOffsetY] = useLocalStorageState<number>('garden.map.offsetY', 0)
  const [selectedTile, setSelectedTile] = useState<{ x: number; y: number } | null>(null)
  const [showLabels, setShowLabels] = useLocalStorageState<boolean>('garden.map.showLabels', false)
  const [showCitizens, setShowCitizens] = useLocalStorageState<boolean>('garden.map.showCitizens', true)
  const [showSettlements, setShowSettlements] = useLocalStorageState<boolean>('garden.map.showSettlements', true)
  const [toolbarOpen, setToolbarOpen] = useState(false)
  const [viewport, setViewport] = useState({ width: window.innerWidth, height: window.innerHeight })

  useEffect(() => {
    const onResize = () => setViewport({ width: window.innerWidth, height: window.innerHeight })
    window.addEventListener('resize', onResize)
    return () => window.removeEventListener('resize', onResize)
  }, [])

  const { data: worldStatus } = useQuery({
    queryKey: ['world-status-bounds'],
    queryFn: fetchWorldStatus,
    staleTime: Infinity,
  })
  const worldWidth = worldStatus?.width ?? 256
  const worldHeight = worldStatus?.height ?? 256
  const maxViewSize = Math.max(worldWidth, worldHeight)

  const viewSizeOptions = useMemo(() => {
    const sizes = BASE_VIEW_SIZES.filter((s) => s < maxViewSize)
    return [...sizes, maxViewSize] as number[]
  }, [maxViewSize])

  const aspect = viewport.width / Math.max(1, viewport.height)
  const isMaxZoom = viewSize >= maxViewSize
  const gridHeight = isMaxZoom ? worldHeight : clamp(Math.round(viewSize), 1, worldHeight)
  const gridWidth = isMaxZoom ? worldWidth : clamp(Math.round(viewSize * aspect), 1, worldWidth)

  const { data: citizensData } = useQuery({
    queryKey: ['world-map-citizens'],
    queryFn: () => fetchCitizens(1, 500),
    enabled: showCitizens,
    refetchInterval: 5000,
  })

  const { data: settlementsData } = useQuery({
    queryKey: ['world-map-settlements'],
    queryFn: () => fetchSettlements(),
    enabled: showSettlements,
    refetchInterval: 5000,
  })

  const { data: mapData } = useQuery({
    queryKey: ['world-map', gridWidth, gridHeight, offsetX, offsetY],
    queryFn: () => fetchMap(offsetX, offsetY, gridWidth, gridHeight),
    refetchInterval: gridWidth * gridHeight > 4000 ? 20000 : 5000,
    placeholderData: keepPreviousData,
  })

  const { data: tileData } = useQuery({
    queryKey: ['tile', selectedTile?.x, selectedTile?.y],
    queryFn: () => fetchTile(selectedTile!.x, selectedTile!.y),
    enabled: selectedTile !== null,
    refetchInterval: 5000,
  })

  const handleSelectTile = useCallback((x: number, y: number) => {
    setSelectedTile((prev) => (prev?.x === x && prev?.y === y ? null : { x, y }))
  }, [])

  const handlePan = useCallback(
    (deltaTilesX: number, deltaTilesY: number) => {
      setOffsetX((prev) => clamp(prev + deltaTilesX, 0, Math.max(0, worldWidth - gridWidth)))
      setOffsetY((prev) => clamp(prev + deltaTilesY, 0, Math.max(0, worldHeight - gridHeight)))
    },
    [gridWidth, gridHeight, worldWidth, worldHeight, setOffsetX, setOffsetY]
  )

  const gridForTier = useCallback(
    (size: number) => {
      const max = size >= maxViewSize
      return {
        h: max ? worldHeight : clamp(Math.round(size), 1, worldHeight),
        w: max ? worldWidth : clamp(Math.round(size * aspect), 1, worldWidth),
      }
    },
    [maxViewSize, worldWidth, worldHeight, aspect]
  )

  const handleZoom = useCallback(
    (direction: 1 | -1, centerTile: { x: number; y: number }) => {
      const currentIndex = viewSizeOptions.indexOf(viewSize)
      const nextIndex =
        direction === 1 ? Math.max(0, currentIndex - 1) : Math.min(viewSizeOptions.length - 1, currentIndex + 1)
      const newSize = viewSizeOptions[nextIndex]
      if (newSize === viewSize) return

      const { w: newGridWidth, h: newGridHeight } = gridForTier(newSize)
      const maxOffsetX = Math.max(0, worldWidth - newGridWidth)
      const maxOffsetY = Math.max(0, worldHeight - newGridHeight)
      setViewSize(newSize)
      setOffsetX(clamp(centerTile.x - Math.floor(newGridWidth / 2), 0, maxOffsetX))
      setOffsetY(clamp(centerTile.y - Math.floor(newGridHeight / 2), 0, maxOffsetY))
    },
    [viewSize, worldWidth, worldHeight, viewSizeOptions, gridForTier, setViewSize, setOffsetX, setOffsetY]
  )

  const handleViewSizeChange = useCallback(
    (size: number) => {
      const centerX = offsetX + Math.floor(gridWidth / 2)
      const centerY = offsetY + Math.floor(gridHeight / 2)
      const { w: newGridWidth, h: newGridHeight } = gridForTier(size)
      const maxOffsetX = Math.max(0, worldWidth - newGridWidth)
      const maxOffsetY = Math.max(0, worldHeight - newGridHeight)
      setViewSize(size)
      setOffsetX(clamp(centerX - Math.floor(newGridWidth / 2), 0, maxOffsetX))
      setOffsetY(clamp(centerY - Math.floor(newGridHeight / 2), 0, maxOffsetY))
    },
    [offsetX, offsetY, gridWidth, gridHeight, worldWidth, worldHeight, gridForTier, setViewSize, setOffsetX, setOffsetY]
  )

  const overlays = useMemo(() => {
    const list: MapOverlay[] = []

    if (showSettlements && settlementsData) {
      list.push((ctx, viewport) => {
        for (const s of settlementsData) {
          const gx = s.tileX - viewport.offsetX
          const gy = s.tileY - viewport.offsetY
          if (gx < 0 || gy < 0 || gx >= viewport.gridWidth || gy >= viewport.gridHeight) continue

          const cx = viewport.originX + gx * viewport.tileSize + viewport.tileSize / 2
          const cy = viewport.originY + gy * viewport.tileSize + viewport.tileSize / 2
          const r = Math.max(3, Math.min(8, viewport.tileSize * 0.4))

          ctx.beginPath()
          ctx.arc(cx, cy, r, 0, Math.PI * 2)
          ctx.fillStyle = '#e0a730'
          ctx.fill()
          ctx.lineWidth = Math.max(1, r * 0.35)
          ctx.strokeStyle = 'rgba(255,255,255,0.85)'
          ctx.stroke()
        }
      })
    }

    if (showCitizens && citizensData) {
      list.push((ctx, viewport) => {
        for (const c of citizensData.citizens) {
          if (c.tileX == null || c.tileY == null) continue
          const gx = c.tileX - viewport.offsetX
          const gy = c.tileY - viewport.offsetY
          if (gx < 0 || gy < 0 || gx >= viewport.gridWidth || gy >= viewport.gridHeight) continue

          const cx = viewport.originX + gx * viewport.tileSize + viewport.tileSize / 2
          const cy = viewport.originY + gy * viewport.tileSize + viewport.tileSize / 2
          const r = Math.max(1.2, Math.min(3, viewport.tileSize * 0.15))

          ctx.beginPath()
          ctx.arc(cx, cy, r, 0, Math.PI * 2)
          ctx.fillStyle = 'rgba(94, 168, 199, 0.9)'
          ctx.fill()
        }
      })
    }

    return list
  }, [showSettlements, settlementsData, showCitizens, citizensData])

  return (
    <div className="absolute inset-0">
      {mapData?.tiles ? (
        <WorldMapCanvas
          tiles={mapData.tiles}
          gridWidth={gridWidth}
          gridHeight={gridHeight}
          offsetX={offsetX}
          offsetY={offsetY}
          selectedTile={selectedTile}
          showLabels={showLabels}
          onSelectTile={handleSelectTile}
          onPan={handlePan}
          onZoom={handleZoom}
          overlays={overlays}
          className="h-full w-full rounded-none border-none"
        />
      ) : (
        <div className="flex h-full w-full items-center justify-center bg-background-deep text-sm text-muted-foreground">
          Charting the world&hellip;
        </div>
      )}

      {/* Weather is only legible as a spatial texture once tiles are big
          enough to actually see - at zoomed-out tiers dozens of overlapping
          per-cell gradients just compound into an indistinct haze over the
          whole map, which is exactly what was making the map unreadable. */}
      {viewSize <= 100 && (
        <WeatherVeil offsetX={offsetX} offsetY={offsetY} gridWidth={gridWidth} gridHeight={gridHeight} />
      )}

      {/* Map toolbar — top-center, collapses to a single icon so it never
          competes with the vitals cluster / chronicle bell in the corners. */}
      <div className="pointer-events-none absolute inset-x-0 top-4 z-30 flex justify-center">
        <div className="pointer-events-auto flex items-center gap-1 rounded-full border border-border/70 bg-panel/90 p-1 shadow-atlas-lg backdrop-blur-md">
          <button
            onClick={() => setToolbarOpen((v) => !v)}
            className="flex h-8 w-8 items-center justify-center rounded-full text-muted-foreground transition-colors hover:bg-accent hover:text-accent-foreground"
            title="Map layers"
            aria-expanded={toolbarOpen}
          >
            <Rows size={16} weight="bold" />
          </button>
          {toolbarOpen && (
            <>
              <div className="mx-0.5 h-5 w-px bg-border" />
              {viewSizeOptions.map((size) => (
                <button
                  key={size}
                  onClick={() => handleViewSizeChange(size)}
                  className={cn(
                    'h-8 rounded-full px-3 font-display text-xs font-medium transition-colors',
                    viewSize === size
                      ? 'bg-primary text-primary-foreground'
                      : 'text-muted-foreground hover:bg-accent hover:text-accent-foreground'
                  )}
                >
                  {size >= maxViewSize ? 'Planet' : (VIEW_SIZE_LABELS[size] ?? `${size}`)}
                </button>
              ))}
              <div className="mx-0.5 h-5 w-px bg-border" />
              <button
                onClick={() => setShowLabels((v) => !v)}
                className={cn(
                  'flex h-8 w-8 items-center justify-center rounded-full transition-colors',
                  showLabels ? 'bg-primary text-primary-foreground' : 'text-muted-foreground hover:bg-accent'
                )}
                title="Terrain labels"
              >
                <Sparkle size={16} weight={showLabels ? 'fill' : 'regular'} />
              </button>
              <button
                onClick={() => setShowSettlements((v) => !v)}
                className={cn(
                  'flex h-8 w-8 items-center justify-center rounded-full transition-colors',
                  showSettlements ? 'bg-status-thriving text-status-thriving-foreground' : 'text-muted-foreground hover:bg-accent'
                )}
                title="Settlements layer"
              >
                <HouseLine size={16} weight={showSettlements ? 'fill' : 'regular'} />
              </button>
              <button
                onClick={() => setShowCitizens((v) => !v)}
                className={cn(
                  'flex h-8 w-8 items-center justify-center rounded-full transition-colors',
                  showCitizens ? 'bg-status-water text-status-water-foreground' : 'text-muted-foreground hover:bg-accent'
                )}
                title="Citizens layer"
              >
                <UsersThree size={16} weight={showCitizens ? 'fill' : 'regular'} />
              </button>
            </>
          )}
        </div>
      </div>

      {/* Tile inspector — bottom-left, only present once something is
          actually selected, so the world stays clear otherwise. */}
      {selectedTile && (
        <div className="pointer-events-none absolute bottom-24 left-4 z-30 w-72 md:bottom-6 md:left-6">
          <div className="pointer-events-auto panel-carved border border-border/70 bg-panel/95 p-4 shadow-atlas-lg backdrop-blur-md">
            <div className="mb-2 flex items-center justify-between">
              <span className="font-display text-xs font-semibold uppercase tracking-wide text-muted-foreground">
                Tile ({selectedTile.x}, {selectedTile.y})
              </span>
              <button
                onClick={() => setSelectedTile(null)}
                className="flex h-6 w-6 items-center justify-center rounded-full text-muted-foreground hover:bg-accent hover:text-accent-foreground"
              >
                <X size={13} />
              </button>
            </div>
            {tileData ? <TileDetails tile={tileData} /> : <p className="text-sm text-muted-foreground">Reading the ground&hellip;</p>}
          </div>
        </div>
      )}
    </div>
  )
}

function TileDetails({ tile }: { tile: TileData }) {
  return (
    <div className="space-y-3 text-sm">
      <div className="flex items-center justify-between">
        <span className="font-display font-semibold">{tile.terrain}</span>
        <span className="text-xs text-muted-foreground">{tile.biome}</span>
      </div>
      <div className="grid grid-cols-2 gap-2 text-xs">
        <Stat label="Climate" value={tile.climate} />
        <Stat label="Elevation" value={tile.elevation.toFixed(2)} />
        <Stat label="Moisture" value={`${(tile.moisture * 100).toFixed(0)}%`} />
        <Stat label="Temp" value={`${tile.temperature.toFixed(1)}°C`} />
        <Stat label="Water" value={tile.isRiver ? 'River' : tile.isLake ? 'Lake' : 'None'} />
        {tile.occupancy && <Stat label="Occupied" value={tile.occupancy.structureType} />}
      </div>
      {tile.resources.length > 0 && (
        <div className="space-y-1 border-t border-border/60 pt-2">
          {tile.resources.map((r) => (
            <div key={r.type} className="flex items-center justify-between text-xs">
              <span className="text-muted-foreground">{r.type}</span>
              <span className="font-medium tabular-nums">
                {r.quantity.toFixed(0)}/{r.maxCapacity.toFixed(0)}
              </span>
            </div>
          ))}
        </div>
      )}
    </div>
  )
}

function Stat({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <p className="text-muted-foreground">{label}</p>
      <p className="font-medium text-foreground">{value}</p>
    </div>
  )
}

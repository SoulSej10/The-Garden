import { useCallback, useMemo, useState } from 'react'
import { useQuery, keepPreviousData } from '@tanstack/react-query'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Skeleton } from '@/components/ui/skeleton'
import { fetchMap, fetchTile, fetchWorldStatus, fetchCitizens, fetchSettlements } from '@/lib/api'
import type { TileData } from '@/lib/api'
import { WorldMapCanvas, type MapOverlay } from '@/components/WorldMapCanvas'
import { TERRAIN_ORDER, TERRAIN_PALETTE } from '@/lib/terrainColors'

const BASE_VIEW_SIZES = [10, 20, 30, 50] as const

function clamp(value: number, min: number, max: number) {
  return Math.max(min, Math.min(max, value))
}

export default function WorldMapPage() {
  const [viewSize, setViewSize] = useState<number>(20)
  const [offsetX, setOffsetX] = useState(0)
  const [offsetY, setOffsetY] = useState(0)
  const [selectedTile, setSelectedTile] = useState<{ x: number; y: number } | null>(null)
  const [showLabels, setShowLabels] = useState(false)
  const [showCitizens, setShowCitizens] = useState(false)
  const [showSettlements, setShowSettlements] = useState(true)

  const { data: worldStatus } = useQuery({
    queryKey: ['world-status-bounds'],
    queryFn: fetchWorldStatus,
    staleTime: Infinity,
  })
  const worldWidth = worldStatus?.width ?? 100
  const worldHeight = worldStatus?.height ?? 100
  const maxViewSize = Math.max(worldWidth, worldHeight)

  // "Max" shows the entire generated world in one view, not just a fixed crop.
  const viewSizeOptions = useMemo(() => {
    const sizes = BASE_VIEW_SIZES.filter((s) => s < maxViewSize)
    return [...sizes, maxViewSize] as number[]
  }, [maxViewSize])

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

  const { data: mapData, isLoading } = useQuery({
    queryKey: ['world-map', viewSize, offsetX, offsetY],
    queryFn: () => fetchMap(offsetX, offsetY, viewSize, viewSize),
    // Terrain itself is static; only resource quantities/occupancy drift
    // over time. A close-up view benefits from a quick refresh, but a
    // full-world view is thousands of tiles - polling that every 5s wastes
    // bandwidth for data that's barely changed. Scale the interval with the
    // number of tiles being fetched instead of using one fixed interval.
    refetchInterval: viewSize * viewSize > 900 ? 20000 : 5000,
    placeholderData: keepPreviousData,
  })

  const { data: tileData } = useQuery({
    queryKey: ['tile', selectedTile?.x, selectedTile?.y],
    queryFn: () => fetchTile(selectedTile!.x, selectedTile!.y),
    enabled: selectedTile !== null,
  })

  const handleSelectTile = useCallback((x: number, y: number) => {
    setSelectedTile((prev) => (prev?.x === x && prev?.y === y ? null : { x, y }))
  }, [])

  const handlePan = useCallback(
    (deltaTilesX: number, deltaTilesY: number) => {
      setOffsetX((prev) => clamp(prev + deltaTilesX, 0, Math.max(0, worldWidth - viewSize)))
      setOffsetY((prev) => clamp(prev + deltaTilesY, 0, Math.max(0, worldHeight - viewSize)))
    },
    [viewSize, worldWidth, worldHeight]
  )

  const handleZoom = useCallback(
    (direction: 1 | -1, centerTile: { x: number; y: number }) => {
      const currentIndex = viewSizeOptions.indexOf(viewSize)
      const nextIndex =
        direction === 1 ? Math.max(0, currentIndex - 1) : Math.min(viewSizeOptions.length - 1, currentIndex + 1)
      const newSize = viewSizeOptions[nextIndex]
      if (newSize === viewSize) return

      const maxOffsetX = Math.max(0, worldWidth - newSize)
      const maxOffsetY = Math.max(0, worldHeight - newSize)
      setViewSize(newSize)
      setOffsetX(clamp(centerTile.x - Math.floor(newSize / 2), 0, maxOffsetX))
      setOffsetY(clamp(centerTile.y - Math.floor(newSize / 2), 0, maxOffsetY))
    },
    [viewSize, worldWidth, worldHeight, viewSizeOptions]
  )

  const handleViewSizeChange = useCallback(
    (size: number) => {
      const centerX = offsetX + Math.floor(viewSize / 2)
      const centerY = offsetY + Math.floor(viewSize / 2)
      const maxOffsetX = Math.max(0, worldWidth - size)
      const maxOffsetY = Math.max(0, worldHeight - size)
      setViewSize(size)
      setOffsetX(clamp(centerX - Math.floor(size / 2), 0, maxOffsetX))
      setOffsetY(clamp(centerY - Math.floor(size / 2), 0, maxOffsetY))
    },
    [offsetX, offsetY, viewSize, worldWidth, worldHeight]
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
          const r = Math.max(3, Math.min(7, viewport.tileSize * 0.35))

          ctx.beginPath()
          ctx.arc(cx, cy, r, 0, Math.PI * 2)
          ctx.fillStyle = '#f59e0b'
          ctx.fill()
          ctx.lineWidth = Math.max(1, r * 0.3)
          ctx.strokeStyle = '#ffffff'
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
          ctx.fillStyle = 'rgba(14, 165, 233, 0.85)'
          ctx.fill()
        }
      })
    }

    return list
  }, [showSettlements, settlementsData, showCitizens, citizensData])

  return (
    <div className="mx-auto max-w-[1800px] space-y-6">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h1 className="text-2xl font-semibold">World Map</h1>
          <p className="text-sm text-muted-foreground">
            Tile ({offsetX},{offsetY}) &ndash; ({offsetX + viewSize - 1},{offsetY + viewSize - 1})
          </p>
        </div>
        <div className="flex flex-wrap items-center gap-2">
          <div className="flex items-center gap-1 rounded-lg border p-1">
            {viewSizeOptions.map((size) => (
              <Button
                key={size}
                variant={viewSize === size ? 'default' : 'ghost'}
                size="xs"
                onClick={() => handleViewSizeChange(size)}
              >
                {size === maxViewSize ? 'Max' : `${size}x${size}`}
              </Button>
            ))}
          </div>
          <Button variant="outline" size="xs" onClick={() => setShowLabels((v) => !v)}>
            {showLabels ? 'Hide Labels' : 'Show Labels'}
          </Button>
          <Button
            variant={showSettlements ? 'default' : 'outline'}
            size="xs"
            onClick={() => setShowSettlements((v) => !v)}
          >
            Settlements
          </Button>
          <Button
            variant={showCitizens ? 'default' : 'outline'}
            size="xs"
            onClick={() => setShowCitizens((v) => !v)}
          >
            Citizens
          </Button>
        </div>
      </div>

      <div className="grid grid-cols-1 gap-6 xl:grid-cols-4">
        <div className="xl:col-span-3">
          <Card className="h-full">
            <CardHeader className="pb-3">
              <CardTitle className="text-sm">Terrain View</CardTitle>
            </CardHeader>
            <CardContent>
              {isLoading && !mapData ? (
                <Skeleton className="mx-auto aspect-square w-full max-w-[1100px]" />
              ) : mapData?.tiles ? (
                <div className="mx-auto aspect-square w-full max-w-[1100px]">
                  <WorldMapCanvas
                    tiles={mapData.tiles}
                    gridWidth={viewSize}
                    gridHeight={viewSize}
                    offsetX={offsetX}
                    offsetY={offsetY}
                    selectedTile={selectedTile}
                    showLabels={showLabels}
                    onSelectTile={handleSelectTile}
                    onPan={handlePan}
                    onZoom={handleZoom}
                    overlays={overlays}
                  />
                </div>
              ) : (
                <p className="text-sm text-muted-foreground">Not initialized.</p>
              )}
              <p className="mt-2 text-center text-xs text-muted-foreground">
                Drag to pan &middot; Scroll to zoom &middot; Click a tile to inspect
              </p>
            </CardContent>
          </Card>
        </div>

        <div className="space-y-4">
          <Card>
            <CardHeader className="pb-3">
              <CardTitle className="text-sm">Tile Inspector</CardTitle>
            </CardHeader>
            <CardContent>
              {selectedTile ? (
                tileData ? (
                  <TileDetails tile={tileData} />
                ) : (
                  <Skeleton className="h-48 w-full" />
                )
              ) : (
                <p className="text-sm text-muted-foreground">Click a tile to inspect.</p>
              )}
            </CardContent>
          </Card>

          <Card>
            <CardHeader className="pb-3">
              <CardTitle className="text-sm">Legend</CardTitle>
            </CardHeader>
            <CardContent>
              <div className="grid grid-cols-2 gap-x-3 gap-y-2 text-xs">
                {TERRAIN_ORDER.map((name) => (
                  <div key={name} className="flex items-center gap-2">
                    <span
                      className="inline-block h-3 w-3 shrink-0 rounded-sm border border-black/10"
                      style={{ backgroundColor: TERRAIN_PALETTE[name].color }}
                    />
                    <span className="text-muted-foreground">{TERRAIN_PALETTE[name].label}</span>
                  </div>
                ))}
                <div className="col-span-2 my-1 border-t" />
                <div className="flex items-center gap-2">
                  <span className="inline-block h-3 w-3 shrink-0 rounded-full border border-white bg-amber-500" />
                  <span className="text-muted-foreground">Settlement</span>
                </div>
                <div className="flex items-center gap-2">
                  <span className="inline-block h-2 w-2 shrink-0 rounded-full bg-sky-500" />
                  <span className="text-muted-foreground">Citizen</span>
                </div>
              </div>
            </CardContent>
          </Card>
        </div>
      </div>
    </div>
  )
}

function TileDetails({ tile }: { tile: TileData }) {
  return (
    <div className="space-y-3">
      <div className="flex items-center justify-between">
        <Badge variant="outline">
          ({tile.x}, {tile.y})
        </Badge>
        <Badge>{tile.terrain}</Badge>
      </div>

      <div className="grid grid-cols-2 gap-2 text-sm">
        <div>
          <p className="text-xs text-muted-foreground">Biome</p>
          <p className="font-medium">{tile.biome}</p>
        </div>
        <div>
          <p className="text-xs text-muted-foreground">Climate</p>
          <p className="font-medium">{tile.climate}</p>
        </div>
        <div>
          <p className="text-xs text-muted-foreground">Elevation</p>
          <p className="font-medium">{tile.elevation.toFixed(3)}</p>
        </div>
        <div>
          <p className="text-xs text-muted-foreground">Moisture</p>
          <p className="font-medium">{(tile.moisture * 100).toFixed(1)}%</p>
        </div>
        <div>
          <p className="text-xs text-muted-foreground">Temperature</p>
          <p className="font-medium">{tile.temperature.toFixed(1)}&deg;C</p>
        </div>
        <div>
          <p className="text-xs text-muted-foreground">River/Lake</p>
          <p className="font-medium">{tile.isRiver ? 'River' : tile.isLake ? 'Lake' : 'No'}</p>
        </div>
      </div>

      {tile.resources.length > 0 && (
        <>
          <p className="text-xs font-medium text-muted-foreground">Resources</p>
          <div className="space-y-1">
            {tile.resources.map((r) => (
              <div key={r.type} className="flex items-center justify-between rounded border px-2 py-1 text-xs">
                <span>{r.type}</span>
                <span>
                  {r.quantity.toFixed(0)}/{r.maxCapacity.toFixed(0)}
                </span>
              </div>
            ))}
          </div>
        </>
      )}

      {tile.occupancy && (
        <div className="rounded bg-accent/30 px-2 py-1 text-xs">
          Occupied by {tile.occupancy.occupiedBy} ({tile.occupancy.structureType})
        </div>
      )}
    </div>
  )
}

import { useCallback, useState } from 'react'
import { useQuery, keepPreviousData } from '@tanstack/react-query'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Skeleton } from '@/components/ui/skeleton'
import { fetchMap, fetchTile, fetchWorldStatus } from '@/lib/api'
import type { TileData } from '@/lib/api'
import { WorldMapCanvas } from '@/components/WorldMapCanvas'
import { TERRAIN_ORDER, TERRAIN_PALETTE } from '@/lib/terrainColors'

const VIEW_SIZES = [10, 20, 30, 50] as const

function clamp(value: number, min: number, max: number) {
  return Math.max(min, Math.min(max, value))
}

export default function WorldMapPage() {
  const [viewSize, setViewSize] = useState<number>(20)
  const [offsetX, setOffsetX] = useState(0)
  const [offsetY, setOffsetY] = useState(0)
  const [selectedTile, setSelectedTile] = useState<{ x: number; y: number } | null>(null)
  const [showLabels, setShowLabels] = useState(false)

  const { data: worldStatus } = useQuery({
    queryKey: ['world-status-bounds'],
    queryFn: fetchWorldStatus,
    staleTime: Infinity,
  })
  const worldWidth = worldStatus?.width ?? 100
  const worldHeight = worldStatus?.height ?? 100

  const { data: mapData, isLoading } = useQuery({
    queryKey: ['world-map', viewSize, offsetX, offsetY],
    queryFn: () => fetchMap(offsetX, offsetY, viewSize, viewSize),
    refetchInterval: 5000,
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
      const currentIndex = VIEW_SIZES.indexOf(viewSize as (typeof VIEW_SIZES)[number])
      const nextIndex =
        direction === 1 ? Math.max(0, currentIndex - 1) : Math.min(VIEW_SIZES.length - 1, currentIndex + 1)
      const newSize = VIEW_SIZES[nextIndex]
      if (newSize === viewSize) return

      const maxOffsetX = Math.max(0, worldWidth - newSize)
      const maxOffsetY = Math.max(0, worldHeight - newSize)
      setViewSize(newSize)
      setOffsetX(clamp(centerTile.x - Math.floor(newSize / 2), 0, maxOffsetX))
      setOffsetY(clamp(centerTile.y - Math.floor(newSize / 2), 0, maxOffsetY))
    },
    [viewSize, worldWidth, worldHeight]
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

  return (
    <div className="mx-auto max-w-[1600px] space-y-6">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h1 className="text-2xl font-semibold">World Map</h1>
          <p className="text-sm text-muted-foreground">
            Tile ({offsetX},{offsetY}) &ndash; ({offsetX + viewSize - 1},{offsetY + viewSize - 1})
          </p>
        </div>
        <div className="flex items-center gap-2">
          <div className="flex items-center gap-1 rounded-lg border p-1">
            {VIEW_SIZES.map((size) => (
              <Button
                key={size}
                variant={viewSize === size ? 'default' : 'ghost'}
                size="xs"
                onClick={() => handleViewSizeChange(size)}
              >
                {size}x{size}
              </Button>
            ))}
          </div>
          <Button variant="outline" size="xs" onClick={() => setShowLabels((v) => !v)}>
            {showLabels ? 'Hide Labels' : 'Show Labels'}
          </Button>
        </div>
      </div>

      <div className="grid grid-cols-1 gap-6 lg:grid-cols-3">
        <div className="lg:col-span-2">
          <Card className="h-full">
            <CardHeader className="pb-3">
              <CardTitle className="text-sm">Terrain View</CardTitle>
            </CardHeader>
            <CardContent>
              {isLoading && !mapData ? (
                <Skeleton className="mx-auto aspect-square w-full max-w-[640px]" />
              ) : mapData?.tiles ? (
                <div className="mx-auto aspect-square w-full max-w-[640px]">
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

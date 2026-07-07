import { useState, useCallback, useRef } from 'react'
import { useQuery } from '@tanstack/react-query'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Skeleton } from '@/components/ui/skeleton'
import { fetchMap, fetchTile } from '@/lib/api'
import type { TileData } from '@/lib/api'

const TERRAIN_COLORS: Record<string, string> = {
  Ocean: '#3b82f6',
  Coast: '#93c5fd',
  Plains: '#fef9c3',
  Grassland: '#86efac',
  Hills: '#d97706',
  Mountains: '#a8a29e',
  Forest: '#15803d',
  Swamp: '#064e3b',
  River: '#22d3ee',
  Lake: '#60a5fa',
}

const TERRAIN_LABELS: Record<string, string> = {
  Ocean: '~',
  Coast: '~',
  Plains: '.',
  Grassland: '"',
  Hills: '^',
  Mountains: '^',
  Forest: '#',
  Swamp: '~',
  River: '~',
  Lake: '~',
}

const VIEW_SIZES = [10, 20, 30, 50] as const

export default function WorldMapPage() {
  const [viewSize, setViewSize] = useState<number>(20)
  const [offsetX, setOffsetX] = useState(0)
  const [offsetY, setOffsetY] = useState(0)
  const [selectedTile, setSelectedTile] = useState<{ x: number; y: number } | null>(null)
  const [showLabels, setShowLabels] = useState(false)
  const dragRef = useRef({ dragging: false, startX: 0, startY: 0, offX: 0, offY: 0 })

  const { data: mapData, isLoading } = useQuery({
    queryKey: ['world-map', viewSize, offsetX, offsetY],
    queryFn: () => fetchMap(offsetX, offsetY, viewSize, viewSize),
    refetchInterval: 5000,
  })

  const { data: tileData } = useQuery({
    queryKey: ['tile', selectedTile?.x, selectedTile?.y],
    queryFn: () => fetchTile(selectedTile!.x, selectedTile!.y),
    enabled: selectedTile !== null,
  })

  const handleTileClick = useCallback((x: number, y: number) => {
    setSelectedTile((prev) => (prev?.x === x && prev?.y === y ? null : { x, y }))
  }, [])

  const handleMouseDown = useCallback((e: React.MouseEvent) => {
    dragRef.current.dragging = true
    dragRef.current.startX = e.clientX
    dragRef.current.startY = e.clientY
    dragRef.current.offX = offsetX
    dragRef.current.offY = offsetY
  }, [offsetX, offsetY])

  const handleMouseMove = useCallback((e: React.MouseEvent) => {
    if (!dragRef.current.dragging) return
    const dx = dragRef.current.startX - e.clientX
    const dy = dragRef.current.startY - e.clientY
    const tileSize = 16
    const newOffX = Math.max(0, Math.min(100 - viewSize, dragRef.current.offX + Math.round(dx / tileSize)))
    const newOffY = Math.max(0, Math.min(100 - viewSize, dragRef.current.offY + Math.round(dy / tileSize)))
    setOffsetX(newOffX)
    setOffsetY(newOffY)
    dragRef.current.startX = e.clientX
    dragRef.current.startY = e.clientY
    dragRef.current.offX = newOffX
    dragRef.current.offY = newOffY
  }, [viewSize])

  const handleMouseUp = useCallback(() => {
    dragRef.current.dragging = false
  }, [])

  return (
    <div className="mx-auto max-w-[1600px] space-y-6">
      <div className="flex items-center justify-between">
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
                onClick={() => {
                  setViewSize(size)
                  setSelectedTile(null)
                }}
              >
                {size}x{size}
              </Button>
            ))}
          </div>
          <Button
            variant="outline"
            size="xs"
            onClick={() => setShowLabels(!showLabels)}
          >
            {showLabels ? 'Hide Labels' : 'Show Labels'}
          </Button>
        </div>
      </div>

      <div className="grid grid-cols-1 gap-6 lg:grid-cols-3">
        <div className="lg:col-span-2">
          <Card>
            <CardHeader className="pb-3">
              <CardTitle className="text-sm">Terrain View</CardTitle>
            </CardHeader>
            <CardContent>
              {isLoading ? (
                <Skeleton className="h-80 w-full" />
              ) : mapData?.tiles ? (
                <div
                  className="select-none overflow-hidden rounded-md border"
                  onMouseDown={handleMouseDown}
                  onMouseMove={handleMouseMove}
                  onMouseUp={handleMouseUp}
                  onMouseLeave={handleMouseUp}
                >
                  <div className="flex flex-col" style={{ cursor: 'grab' }}>
                    {Array.from({ length: mapData.height }, (_, row) => (
                      <div key={row} className="flex">
                        {mapData.tiles
                          .filter((t) => t.y === mapData.offsetY + row)
                          .sort((a, b) => a.x - b.x)
                          .map((tile) => {
                            const isSelected =
                              selectedTile?.x === tile.x && selectedTile?.y === tile.y
                            const isRiver = tile.isRiver
                            const isLake = tile.isLake || tile.terrain === 'Lake'
                            const color = isRiver
                              ? '#22d3ee'
                              : isLake
                                ? '#60a5fa'
                                : TERRAIN_COLORS[tile.terrain] ?? '#ccc'
                            return (
                              <div
                                key={`${tile.x}-${tile.y}`}
                                className="flex items-center justify-center text-[9px] font-mono text-black/40 transition-colors hover:ring-1 hover:ring-ring"
                                style={{
                                  width: 16,
                                  height: 16,
                                  backgroundColor: color,
                                  outline: isSelected ? '2px solid #000' : '1px solid rgba(0,0,0,0.08)',
                                  outlineOffset: isSelected ? -2 : 0,
                                }}
                                onClick={() => handleTileClick(tile.x, tile.y)}
                                title={`(${tile.x},${tile.y}) ${tile.terrain}`}
                              >
                                {showLabels ? TERRAIN_LABELS[tile.terrain] ?? '?' : ''}
                              </div>
                            )
                          })}
                      </div>
                    ))}
                  </div>
                </div>
              ) : (
                <p className="text-sm text-muted-foreground">Not initialized.</p>
              )}
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
                <p className="text-sm text-muted-foreground">
                  Click a tile to inspect.
                </p>
              )}
            </CardContent>
          </Card>

          <Card>
            <CardHeader className="pb-3">
              <CardTitle className="text-sm">Legend</CardTitle>
            </CardHeader>
            <CardContent>
              <div className="grid grid-cols-2 gap-1 text-xs">
                {Object.entries(TERRAIN_COLORS).map(([name, color]) => (
                  <div key={name} className="flex items-center gap-2">
                    <span
                      className="inline-block h-3 w-3 rounded-sm"
                      style={{ backgroundColor: color }}
                    />
                    {name}
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
          <p className="font-medium">
            {tile.isRiver ? 'River' : tile.isLake ? 'Lake' : 'No'}
          </p>
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

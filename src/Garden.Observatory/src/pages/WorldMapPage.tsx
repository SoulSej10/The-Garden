import { useState, useCallback, useRef } from 'react'
import { useQuery } from '@tanstack/react-query'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Skeleton } from '@/components/ui/skeleton'
import { fetchMap, fetchTile, fetchCitizens, fetchSettlements } from '@/lib/api'
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

const VIEW_SIZES = [20, 30, 50, 75, 100] as const
const MIN_TILE_SIZE = 6
const MAX_TILE_SIZE = 32
const WORLD_SIZE = 100

export default function WorldMapPage() {
  const [viewSize, setViewSize] = useState<number>(50)
  const [tileSize, setTileSize] = useState<number>(14)
  const [offsetX, setOffsetX] = useState(0)
  const [offsetY, setOffsetY] = useState(0)
  const [selectedTile, setSelectedTile] = useState<{ x: number; y: number } | null>(null)
  const [showLabels, setShowLabels] = useState(false)
  const [showCitizens, setShowCitizens] = useState(true)
  const [showSettlements, setShowSettlements] = useState(true)
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
    const newOffX = Math.max(0, Math.min(WORLD_SIZE - viewSize, dragRef.current.offX + Math.round(dx / tileSize)))
    const newOffY = Math.max(0, Math.min(WORLD_SIZE - viewSize, dragRef.current.offY + Math.round(dy / tileSize)))
    setOffsetX(newOffX)
    setOffsetY(newOffY)
    dragRef.current.startX = e.clientX
    dragRef.current.startY = e.clientY
    dragRef.current.offX = newOffX
    dragRef.current.offY = newOffY
  }, [viewSize, tileSize])

  const handleMouseUp = useCallback(() => {
    dragRef.current.dragging = false
  }, [])

  const handleWheel = useCallback((e: React.WheelEvent) => {
    e.preventDefault()
    setTileSize((prev) => Math.max(MIN_TILE_SIZE, Math.min(MAX_TILE_SIZE, prev - Math.sign(e.deltaY) * 2)))
  }, [])

  const inView = (x: number, y: number) =>
    x >= offsetX && x < offsetX + viewSize && y >= offsetY && y < offsetY + viewSize

  return (
    <div className="mx-auto max-w-[1800px] space-y-6">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h1 className="text-2xl font-semibold">World Map</h1>
          <p className="text-sm text-muted-foreground">
            Tile ({offsetX},{offsetY}) &ndash; ({offsetX + viewSize - 1},{offsetY + viewSize - 1}) &middot; zoom {tileSize}px
          </p>
        </div>
        <div className="flex flex-wrap items-center gap-2">
          <div className="flex items-center gap-1 rounded-lg border p-1">
            {VIEW_SIZES.map((size) => (
              <Button
                key={size}
                variant={viewSize === size ? 'default' : 'ghost'}
                size="xs"
                onClick={() => {
                  setViewSize(size)
                  setOffsetX((o) => Math.min(o, WORLD_SIZE - size))
                  setOffsetY((o) => Math.min(o, WORLD_SIZE - size))
                  setSelectedTile(null)
                }}
              >
                {size}x{size}
              </Button>
            ))}
          </div>
          <Button variant="outline" size="xs" onClick={() => setShowLabels(!showLabels)}>
            {showLabels ? 'Hide Labels' : 'Show Labels'}
          </Button>
          <Button
            variant={showCitizens ? 'default' : 'outline'}
            size="xs"
            onClick={() => setShowCitizens((v) => !v)}
          >
            Citizens
          </Button>
          <Button
            variant={showSettlements ? 'default' : 'outline'}
            size="xs"
            onClick={() => setShowSettlements((v) => !v)}
          >
            Settlements
          </Button>
        </div>
      </div>

      <div className="grid grid-cols-1 gap-6 xl:grid-cols-4">
        <div className="xl:col-span-3">
          <Card>
            <CardHeader className="pb-3">
              <CardTitle className="text-sm">Terrain View</CardTitle>
            </CardHeader>
            <CardContent>
              {isLoading ? (
                <Skeleton className="h-80 w-full" />
              ) : mapData?.tiles ? (
                <div
                  className="relative select-none overflow-hidden rounded-md border"
                  onMouseDown={handleMouseDown}
                  onMouseMove={handleMouseMove}
                  onMouseUp={handleMouseUp}
                  onMouseLeave={handleMouseUp}
                  onWheel={handleWheel}
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
                                className="flex items-center justify-center font-mono text-black/40 transition-colors hover:ring-1 hover:ring-ring"
                                style={{
                                  width: tileSize,
                                  height: tileSize,
                                  fontSize: Math.max(7, tileSize * 0.55),
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

                  {/* Overlay layer: settlements */}
                  {showSettlements && settlementsData?.map((s) => (
                    inView(s.tileX, s.tileY) && (
                      <div
                        key={s.id}
                        className="pointer-events-none absolute flex items-center justify-center rounded-full border-2 border-white bg-amber-500 shadow"
                        style={{
                          left: (s.tileX - offsetX) * tileSize + tileSize / 2 - 6,
                          top: (s.tileY - offsetY) * tileSize + tileSize / 2 - 6,
                          width: 12,
                          height: 12,
                        }}
                        title={`${s.name} (pop ${s.population})`}
                      />
                    )
                  ))}

                  {/* Overlay layer: citizens */}
                  {showCitizens && citizensData?.citizens
                    .filter((c) => c.tileX != null && c.tileY != null)
                    .map((c) => (
                      inView(c.tileX, c.tileY) && (
                        <div
                          key={c.id}
                          className="pointer-events-none absolute rounded-full bg-sky-500/80"
                          style={{
                            left: (c.tileX - offsetX) * tileSize + tileSize / 2 - 2,
                            top: (c.tileY - offsetY) * tileSize + tileSize / 2 - 2,
                            width: 4,
                            height: 4,
                          }}
                          title={c.name}
                        />
                      )
                    ))}
                </div>
              ) : (
                <p className="text-sm text-muted-foreground">Not initialized.</p>
              )}
              <p className="mt-2 text-xs text-muted-foreground">
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
                <div className="col-span-2 my-1 border-t" />
                <div className="flex items-center gap-2">
                  <span className="inline-block h-3 w-3 rounded-full border-2 border-white bg-amber-500" />
                  Settlement
                </div>
                <div className="flex items-center gap-2">
                  <span className="inline-block h-2 w-2 rounded-full bg-sky-500/80" />
                  Citizen
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

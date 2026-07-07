import { useEffect, useRef } from 'react'
import { getTerrainColor, TERRAIN_LABELS } from '@/lib/terrainColors'

export interface MapTileLite {
  x: number
  y: number
  terrain: string
  biome: string
  elevation: number
  isRiver: boolean
  isLake: boolean
}

export interface MapViewport {
  tileSize: number
  originX: number
  originY: number
  gridWidth: number
  gridHeight: number
  offsetX: number
  offsetY: number
}

export type MapOverlay = (ctx: CanvasRenderingContext2D, viewport: MapViewport) => void

interface WorldMapCanvasProps {
  tiles: MapTileLite[]
  gridWidth: number
  gridHeight: number
  offsetX: number
  offsetY: number
  selectedTile: { x: number; y: number } | null
  showLabels: boolean
  onSelectTile: (x: number, y: number) => void
  onPan: (deltaTilesX: number, deltaTilesY: number) => void
  onZoom?: (direction: 1 | -1, centerTile: { x: number; y: number }) => void
  overlays?: MapOverlay[]
}

const DRAG_THRESHOLD_PX = 4
const MIN_TILE_SIZE = 3
const MAX_TILE_SIZE = 56

interface PointerState {
  active: boolean
  pointerId: number
  startClientX: number
  startClientY: number
  isDragging: boolean
}

interface HoverState {
  x: number
  y: number
  key: string
  terrain: string
}

export function WorldMapCanvas({
  tiles,
  gridWidth,
  gridHeight,
  offsetX,
  offsetY,
  selectedTile,
  showLabels,
  onSelectTile,
  onPan,
  onZoom,
  overlays,
}: WorldMapCanvasProps) {
  const containerRef = useRef<HTMLDivElement | null>(null)
  const canvasRef = useRef<HTMLCanvasElement | null>(null)
  const tooltipRef = useRef<HTMLDivElement | null>(null)

  const tileMapRef = useRef<Map<string, MapTileLite>>(new Map())
  const gridDimsRef = useRef({ gridWidth, gridHeight, offsetX, offsetY })
  const selectedRef = useRef(selectedTile)
  const showLabelsRef = useRef(showLabels)
  const overlaysRef = useRef(overlays)
  const viewportRef = useRef<MapViewport>({
    tileSize: 16,
    originX: 0,
    originY: 0,
    gridWidth,
    gridHeight,
    offsetX,
    offsetY,
  })
  const hoverRef = useRef<HoverState | null>(null)
  const panPreviewRef = useRef({ dx: 0, dy: 0 })
  const pointerStateRef = useRef<PointerState | null>(null)
  const rafRef = useRef<number | null>(null)
  const onSelectTileRef = useRef(onSelectTile)
  const onPanRef = useRef(onPan)
  const onZoomRef = useRef(onZoom)

  useEffect(() => {
    const map = new Map<string, MapTileLite>()
    for (const tile of tiles) map.set(`${tile.x},${tile.y}`, tile)
    tileMapRef.current = map
    scheduleDraw()
  }, [tiles])

  useEffect(() => {
    gridDimsRef.current = { gridWidth, gridHeight, offsetX, offsetY }
    viewportRef.current = { ...viewportRef.current, gridWidth, gridHeight, offsetX, offsetY }
    scheduleDraw()
  }, [gridWidth, gridHeight, offsetX, offsetY])

  useEffect(() => {
    selectedRef.current = selectedTile
    scheduleDraw()
  }, [selectedTile])

  useEffect(() => {
    showLabelsRef.current = showLabels
    scheduleDraw()
  }, [showLabels])

  useEffect(() => {
    overlaysRef.current = overlays
    scheduleDraw()
  }, [overlays])

  useEffect(() => {
    onSelectTileRef.current = onSelectTile
    onPanRef.current = onPan
    onZoomRef.current = onZoom
  }, [onSelectTile, onPan, onZoom])

  function scheduleDraw() {
    if (rafRef.current != null) return
    rafRef.current = requestAnimationFrame(() => {
      rafRef.current = null
      draw()
    })
  }

  function draw() {
    const canvas = canvasRef.current
    const ctx = canvas?.getContext('2d')
    if (!canvas || !ctx) return

    const { gridWidth: gw, gridHeight: gh, offsetX: ox, offsetY: oy } = gridDimsRef.current
    const viewport = viewportRef.current
    const dpr = window.devicePixelRatio || 1
    ctx.setTransform(dpr, 0, 0, dpr, 0, 0)

    const cssWidth = canvas.width / dpr
    const cssHeight = canvas.height / dpr

    ctx.fillStyle = 'rgba(148, 163, 184, 0.12)'
    ctx.fillRect(0, 0, cssWidth, cssHeight)

    const { tileSize, originX, originY } = viewport
    const previewDx = panPreviewRef.current.dx
    const previewDy = panPreviewRef.current.dy
    const tileMap = tileMapRef.current
    const selected = selectedRef.current
    const hovered = hoverRef.current
    const labels = showLabelsRef.current

    for (let row = 0; row < gh; row++) {
      const worldY = oy + row
      const screenY = originY + previewDy + row * tileSize
      if (screenY + tileSize < 0 || screenY > cssHeight) continue

      for (let col = 0; col < gw; col++) {
        const worldX = ox + col
        const screenX = originX + previewDx + col * tileSize
        if (screenX + tileSize < 0 || screenX > cssWidth) continue

        const tile = tileMap.get(`${worldX},${worldY}`)
        if (!tile) continue

        const entry = getTerrainColor(tile.terrain, tile.isRiver, tile.isLake)
        ctx.fillStyle = entry.color
        ctx.fillRect(screenX, screenY, tileSize, tileSize)

        if (entry.highlight && tileSize >= 6) {
          ctx.fillStyle = entry.highlight
          const hs = Math.max(2, tileSize * 0.4)
          ctx.fillRect(screenX + tileSize * 0.12, screenY + tileSize * 0.12, hs, hs)
        }

        if (tileSize >= 10) {
          ctx.strokeStyle = 'rgba(0, 0, 0, 0.08)'
          ctx.lineWidth = 1
          ctx.strokeRect(screenX + 0.5, screenY + 0.5, tileSize - 1, tileSize - 1)
        }

        if (labels && tileSize >= 14) {
          ctx.fillStyle = 'rgba(0, 0, 0, 0.45)'
          ctx.font = `${Math.floor(tileSize * 0.55)}px monospace`
          ctx.textAlign = 'center'
          ctx.textBaseline = 'middle'
          ctx.fillText(
            TERRAIN_LABELS[tile.terrain] ?? '?',
            screenX + tileSize / 2,
            screenY + tileSize / 2 + 1
          )
        }

        const isSelected = selected?.x === worldX && selected?.y === worldY
        if (isSelected) {
          ctx.strokeStyle = '#0f172a'
          ctx.lineWidth = 2
          ctx.strokeRect(screenX + 1, screenY + 1, tileSize - 2, tileSize - 2)
        }

        const isHovered = hovered?.x === worldX && hovered?.y === worldY
        if (isHovered && !isSelected) {
          ctx.fillStyle = 'rgba(255, 255, 255, 0.25)'
          ctx.fillRect(screenX, screenY, tileSize, tileSize)
          ctx.strokeStyle = 'rgba(255, 255, 255, 0.9)'
          ctx.lineWidth = 1.5
          ctx.strokeRect(screenX + 0.75, screenY + 0.75, tileSize - 1.5, tileSize - 1.5)
        }
      }
    }

    for (const overlay of overlaysRef.current ?? []) {
      overlay(ctx, viewport)
    }
  }

  function recomputeViewport() {
    const canvas = canvasRef.current
    const container = containerRef.current
    if (!canvas || !container) return

    const { gridWidth: gw, gridHeight: gh } = gridDimsRef.current
    const rect = container.getBoundingClientRect()
    const dpr = window.devicePixelRatio || 1

    canvas.width = Math.max(1, Math.round(rect.width * dpr))
    canvas.height = Math.max(1, Math.round(rect.height * dpr))
    canvas.style.width = `${rect.width}px`
    canvas.style.height = `${rect.height}px`

    const rawTileSize = Math.min(rect.width / gw, rect.height / gh)
    const tileSize = Math.max(MIN_TILE_SIZE, Math.min(MAX_TILE_SIZE, Math.floor(rawTileSize)))
    const drawnWidth = tileSize * gw
    const drawnHeight = tileSize * gh

    viewportRef.current = {
      ...viewportRef.current,
      tileSize,
      originX: Math.max(0, (rect.width - drawnWidth) / 2),
      originY: Math.max(0, (rect.height - drawnHeight) / 2),
    }

    scheduleDraw()
  }

  useEffect(() => {
    recomputeViewport()
    const container = containerRef.current
    if (!container) return

    const observer = new ResizeObserver(() => recomputeViewport())
    observer.observe(container)
    return () => observer.disconnect()
  }, [gridWidth, gridHeight])

  function screenToTile(px: number, py: number) {
    const viewport = viewportRef.current
    const localX = px - viewport.originX
    const localY = py - viewport.originY
    if (localX < 0 || localY < 0) return null
    const col = Math.floor(localX / viewport.tileSize)
    const row = Math.floor(localY / viewport.tileSize)
    if (col < 0 || col >= viewport.gridWidth || row < 0 || row >= viewport.gridHeight) return null
    return { x: viewport.offsetX + col, y: viewport.offsetY + row }
  }

  function updateTooltip(text: string | null, clientX?: number, clientY?: number) {
    const tooltip = tooltipRef.current
    const container = containerRef.current
    if (!tooltip || !container) return
    if (!text || clientX === undefined || clientY === undefined) {
      tooltip.style.opacity = '0'
      return
    }
    const rect = container.getBoundingClientRect()
    tooltip.textContent = text
    tooltip.style.opacity = '1'
    tooltip.style.transform = `translate(${clientX - rect.left + 14}px, ${clientY - rect.top + 14}px)`
  }

  useEffect(() => {
    const canvas = canvasRef.current
    if (!canvas) return

    function handlePointerDown(e: PointerEvent) {
      pointerStateRef.current = {
        active: true,
        pointerId: e.pointerId,
        startClientX: e.clientX,
        startClientY: e.clientY,
        isDragging: false,
      }
      canvas!.setPointerCapture(e.pointerId)
    }

    function handlePointerMove(e: PointerEvent) {
      const rect = canvas!.getBoundingClientRect()
      const px = e.clientX - rect.left
      const py = e.clientY - rect.top
      const ps = pointerStateRef.current

      if (ps?.active) {
        const dx = e.clientX - ps.startClientX
        const dy = e.clientY - ps.startClientY
        if (!ps.isDragging && Math.hypot(dx, dy) > DRAG_THRESHOLD_PX) {
          ps.isDragging = true
          if (hoverRef.current) {
            hoverRef.current = null
            updateTooltip(null)
          }
        }
        if (ps.isDragging) {
          panPreviewRef.current = { dx, dy }
          scheduleDraw()
          return
        }
      }

      const tile = screenToTile(px, py)
      if (tile) {
        const key = `${tile.x},${tile.y}`
        if (hoverRef.current?.key !== key) {
          const data = tileMapRef.current.get(key)
          hoverRef.current = data ? { ...tile, key, terrain: data.terrain } : null
          scheduleDraw()
        }
        if (hoverRef.current) {
          updateTooltip(`(${tile.x}, ${tile.y}) ${hoverRef.current.terrain}`, e.clientX, e.clientY)
        } else {
          updateTooltip(null)
        }
      } else if (hoverRef.current) {
        hoverRef.current = null
        scheduleDraw()
        updateTooltip(null)
      }
    }

    function handlePointerUp(e: PointerEvent) {
      const ps = pointerStateRef.current
      if (!ps?.active) return
      canvas!.releasePointerCapture(e.pointerId)

      if (ps.isDragging) {
        const { dx, dy } = panPreviewRef.current
        const tileSize = viewportRef.current.tileSize
        const deltaTilesX = -Math.round(dx / tileSize)
        const deltaTilesY = -Math.round(dy / tileSize)
        panPreviewRef.current = { dx: 0, dy: 0 }
        pointerStateRef.current = null
        scheduleDraw()
        if (deltaTilesX !== 0 || deltaTilesY !== 0) onPanRef.current(deltaTilesX, deltaTilesY)
      } else {
        pointerStateRef.current = null
        const rect = canvas!.getBoundingClientRect()
        const tile = screenToTile(e.clientX - rect.left, e.clientY - rect.top)
        if (tile) onSelectTileRef.current(tile.x, tile.y)
      }
    }

    function handlePointerCancel() {
      panPreviewRef.current = { dx: 0, dy: 0 }
      pointerStateRef.current = null
      scheduleDraw()
    }

    function handlePointerLeave() {
      if (pointerStateRef.current?.active) return
      if (hoverRef.current) {
        hoverRef.current = null
        scheduleDraw()
      }
      updateTooltip(null)
    }

    function handleWheel(e: WheelEvent) {
      if (!onZoomRef.current) return
      e.preventDefault()
      const rect = canvas!.getBoundingClientRect()
      const tile = screenToTile(e.clientX - rect.left, e.clientY - rect.top)
      const center = tile ?? {
        x: viewportRef.current.offsetX + Math.floor(viewportRef.current.gridWidth / 2),
        y: viewportRef.current.offsetY + Math.floor(viewportRef.current.gridHeight / 2),
      }
      onZoomRef.current(e.deltaY < 0 ? 1 : -1, center)
    }

    canvas.style.touchAction = 'none'
    canvas.addEventListener('pointerdown', handlePointerDown)
    canvas.addEventListener('pointermove', handlePointerMove)
    canvas.addEventListener('pointerup', handlePointerUp)
    canvas.addEventListener('pointercancel', handlePointerCancel)
    canvas.addEventListener('pointerleave', handlePointerLeave)
    canvas.addEventListener('wheel', handleWheel, { passive: false })

    return () => {
      canvas.removeEventListener('pointerdown', handlePointerDown)
      canvas.removeEventListener('pointermove', handlePointerMove)
      canvas.removeEventListener('pointerup', handlePointerUp)
      canvas.removeEventListener('pointercancel', handlePointerCancel)
      canvas.removeEventListener('pointerleave', handlePointerLeave)
      canvas.removeEventListener('wheel', handleWheel)
      if (rafRef.current != null) cancelAnimationFrame(rafRef.current)
    }
  }, [])

  return (
    <div ref={containerRef} className="relative h-full w-full overflow-hidden rounded-md border bg-muted/20">
      <canvas ref={canvasRef} className="block cursor-grab active:cursor-grabbing" />
      <div
        ref={tooltipRef}
        className="pointer-events-none absolute left-0 top-0 z-10 rounded-md border bg-card px-2 py-1 text-xs font-medium text-card-foreground shadow-md opacity-0 transition-opacity duration-75"
      />
    </div>
  )
}

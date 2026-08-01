import { createContext, useContext, useMemo, useRef, type MutableRefObject, type ReactNode } from 'react'
import { useQuery } from '@tanstack/react-query'
import { fetchWorldStatus } from '@/lib/api'
import { useLocalStorageState } from '@/lib/useLocalStorageState'

const BASE_VIEW_SIZES = [24, 40, 64, 100, 160] as const
const VIEW_SIZE_LABELS: Record<number, string> = { 24: 'Local', 40: 'Near', 64: 'Regional', 100: 'Wide', 160: 'Continental' }

interface MapControlsValue {
  viewSize: number
  setViewSize: (size: number) => void
  /**
   * WorldStage owns pan offset locally (it isn't shared state), so
   * re-centering when the tier changes has to happen there. WorldStage
   * registers its own recentering handler into this ref on mount;
   * MapNavRail calls through it instead of calling setViewSize directly,
   * which would leave the pan offset stale/out of bounds for the new tier.
   */
  changeViewSizeRef: MutableRefObject<((size: number) => void) | null>
  viewSizeOptions: number[]
  viewSizeLabel: (size: number) => string
  maxViewSize: number
  showLabels: boolean
  setShowLabels: (updater: boolean | ((prev: boolean) => boolean)) => void
  showCitizens: boolean
  setShowCitizens: (updater: boolean | ((prev: boolean) => boolean)) => void
  showSettlements: boolean
  setShowSettlements: (updater: boolean | ((prev: boolean) => boolean)) => void
}

const MapControlsContext = createContext<MapControlsValue | null>(null)

/**
 * Map view-tier and layer-toggle state, lifted out of WorldStage so
 * MapNavRail (a permanent rail to the left of the whole map column) can
 * drive it directly instead of the state living behind a popover only
 * WorldStage itself could open. Pan offset stays local to WorldStage - only
 * the rail-controlled settings live here.
 */
export function MapControlsProvider({ children }: { children: ReactNode }) {
  const [viewSize, setViewSize] = useLocalStorageState<number>('garden.map.viewSize', 64)
  const [showLabels, setShowLabels] = useLocalStorageState<boolean>('garden.map.showLabels', false)
  const [showCitizens, setShowCitizens] = useLocalStorageState<boolean>('garden.map.showCitizens', true)
  const [showSettlements, setShowSettlements] = useLocalStorageState<boolean>('garden.map.showSettlements', true)

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

  const changeViewSizeRef = useRef<((size: number) => void) | null>(null)

  const value = useMemo<MapControlsValue>(
    () => ({
      viewSize,
      setViewSize,
      changeViewSizeRef,
      viewSizeOptions,
      viewSizeLabel: (size: number) => (size >= maxViewSize ? 'Planet' : (VIEW_SIZE_LABELS[size] ?? `${size}`)),
      maxViewSize,
      showLabels,
      setShowLabels,
      showCitizens,
      setShowCitizens,
      showSettlements,
      setShowSettlements,
    }),
    [viewSize, setViewSize, viewSizeOptions, maxViewSize, showLabels, setShowLabels, showCitizens, setShowCitizens, showSettlements, setShowSettlements]
  )

  return <MapControlsContext.Provider value={value}>{children}</MapControlsContext.Provider>
}

export function useMapControls() {
  const ctx = useContext(MapControlsContext)
  if (!ctx) throw new Error('useMapControls must be used within a MapControlsProvider')
  return ctx
}

import { Sparkle, UsersThree, HouseLine, MagnifyingGlassPlus, MagnifyingGlassMinus } from '@phosphor-icons/react'
import { useMapControls } from '@/lib/mapControls'
import { cn } from '@/lib/utils'

/**
 * Permanent vertical rail along the far left edge of the app, spanning the
 * full height of the map column (header through footer) - the map's own
 * zoom tier and layer toggles, previously hidden behind a popover only
 * reachable from a single button floating over the map. A narrow rail with
 * always-visible controls is more discoverable than a menu that has to be
 * opened first, at the cost of a fixed width budget - hence single-letter
 * tier abbreviations with a full name in the tooltip.
 */
export function MapNavRail() {
  const { viewSize, viewSizeOptions, viewSizeLabel, changeViewSizeRef, showLabels, setShowLabels, showCitizens, setShowCitizens, showSettlements, setShowSettlements } =
    useMapControls()

  const currentIndex = viewSizeOptions.indexOf(viewSize)

  function step(direction: 1 | -1) {
    const nextIndex = direction === 1 ? Math.max(0, currentIndex - 1) : Math.min(viewSizeOptions.length - 1, currentIndex + 1)
    const size = viewSizeOptions[nextIndex]
    if (size !== undefined) changeViewSizeRef.current?.(size)
  }

  return (
    <div className="flex w-14 shrink-0 flex-col items-center gap-1 border-r border-border/70 bg-panel/60 py-3 backdrop-blur-md">
      <button
        onClick={() => step(1)}
        disabled={currentIndex <= 0}
        className="flex h-8 w-8 items-center justify-center rounded-full text-muted-foreground transition-colors hover:bg-accent hover:text-accent-foreground disabled:pointer-events-none disabled:opacity-30"
        title="Zoom in"
      >
        <MagnifyingGlassPlus size={15} weight="bold" />
      </button>

      <div className="my-1 flex flex-col items-center gap-1">
        {viewSizeOptions.map((size) => {
          const active = size === viewSize
          return (
            <button
              key={size}
              onClick={() => changeViewSizeRef.current?.(size)}
              title={viewSizeLabel(size)}
              aria-pressed={active}
              className={cn(
                'flex h-8 w-8 items-center justify-center rounded-full font-display text-[11px] font-semibold transition-colors',
                active ? 'bg-primary text-primary-foreground shadow-atlas' : 'text-muted-foreground hover:bg-accent hover:text-accent-foreground'
              )}
            >
              {viewSizeLabel(size).charAt(0)}
            </button>
          )
        })}
      </div>

      <button
        onClick={() => step(-1)}
        disabled={currentIndex >= viewSizeOptions.length - 1}
        className="flex h-8 w-8 items-center justify-center rounded-full text-muted-foreground transition-colors hover:bg-accent hover:text-accent-foreground disabled:pointer-events-none disabled:opacity-30"
        title="Zoom out"
      >
        <MagnifyingGlassMinus size={15} weight="bold" />
      </button>

      <div className="my-2 h-px w-8 bg-border" />

      <button
        onClick={() => setShowLabels((v) => !v)}
        className={cn(
          'flex h-8 w-8 items-center justify-center rounded-full transition-colors',
          showLabels ? 'bg-primary text-primary-foreground' : 'text-muted-foreground hover:bg-accent'
        )}
        title="Terrain labels"
      >
        <Sparkle size={15} weight={showLabels ? 'fill' : 'regular'} />
      </button>
      <button
        onClick={() => setShowSettlements((v) => !v)}
        className={cn(
          'flex h-8 w-8 items-center justify-center rounded-full transition-colors',
          showSettlements ? 'bg-status-thriving text-status-thriving-foreground' : 'text-muted-foreground hover:bg-accent'
        )}
        title="Settlements layer"
      >
        <HouseLine size={15} weight={showSettlements ? 'fill' : 'regular'} />
      </button>
      <button
        onClick={() => setShowCitizens((v) => !v)}
        className={cn(
          'flex h-8 w-8 items-center justify-center rounded-full transition-colors',
          showCitizens ? 'bg-status-water text-status-water-foreground' : 'text-muted-foreground hover:bg-accent'
        )}
        title="Citizens layer"
      >
        <UsersThree size={15} weight={showCitizens ? 'fill' : 'regular'} />
      </button>
    </div>
  )
}

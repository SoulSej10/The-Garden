import { UsersThree, HouseLine, Crown, Scroll, CloudSun, Wrench } from '@phosphor-icons/react'
import { usePanelState, type PanelId } from '@/lib/usePanelState'
import { cn } from '@/lib/utils'

const ITEMS: { id: PanelId; icon: typeof UsersThree; label: string }[] = [
  { id: 'citizens', icon: UsersThree, label: 'Citizens' },
  { id: 'settlements', icon: HouseLine, label: 'Settlements' },
  { id: 'civilization', icon: Crown, label: 'Civilization' },
  { id: 'chronicle', icon: Scroll, label: 'Chronicle' },
  { id: 'almanac', icon: CloudSun, label: 'Almanac' },
  { id: 'steward', icon: Wrench, label: "Steward's Desk" },
]

/**
 * Sidebar tab menu, docked above the persistent display window. Unlike the
 * old slide-in-panel model, selecting a tab here doesn't layer anything over
 * the map - it swaps the content of the always-visible pane beside it.
 * Clicking the already-active tab returns to the Overview (panel = null)
 * rather than doing nothing, so there's always a way back to the default
 * civilization readout without a dedicated "home" button.
 */
export function CompassRail() {
  const { panel, openPanel, closePanel } = usePanelState()

  return (
    <div className="grid grid-cols-3 gap-0.5 border-b border-border/70 bg-panel/60 p-1.5 backdrop-blur-md sm:grid-cols-6">
      {ITEMS.map((item) => {
        const Icon = item.icon
        const active = panel === item.id
        return (
          <button
            key={item.id}
            onClick={() => (active ? closePanel() : openPanel(item.id!))}
            title={item.label}
            aria-label={item.label}
            aria-pressed={active}
            className={cn(
              'flex flex-col items-center gap-1 rounded-xl px-1.5 py-2 font-display text-[10px] font-medium transition-colors',
              active
                ? 'bg-primary text-primary-foreground shadow-atlas'
                : 'text-muted-foreground hover:bg-accent hover:text-accent-foreground'
            )}
          >
            <Icon size={17} weight={active ? 'fill' : 'regular'} />
            <span className="truncate leading-none">{item.label}</span>
          </button>
        )
      })}
    </div>
  )
}

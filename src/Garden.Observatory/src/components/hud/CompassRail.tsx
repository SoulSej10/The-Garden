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
 * Bottom-right icon dock — the only "navigation" left in the app. No labels
 * by default (icon + tooltip), so it reads as a HUD control cluster rather
 * than a sidebar. Opening a panel never navigates away from the world; it
 * just adds a floating layer on top of it.
 */
export function CompassRail() {
  const { panel, openPanel, closePanel } = usePanelState()

  return (
    <div className="pointer-events-none absolute bottom-4 right-4 z-30 md:bottom-6 md:right-6">
      <div className="pointer-events-auto flex items-center gap-1 rounded-full border border-border/70 bg-panel/90 p-1.5 shadow-atlas-lg backdrop-blur-md">
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
                'flex h-10 w-10 items-center justify-center rounded-full transition-colors',
                active
                  ? 'bg-primary text-primary-foreground shadow-atlas'
                  : 'text-muted-foreground hover:bg-accent hover:text-accent-foreground'
              )}
            >
              <Icon size={18} weight={active ? 'fill' : 'regular'} />
            </button>
          )
        })}
      </div>
    </div>
  )
}

import { useEffect } from 'react'
import { UsersThree, HouseLine, Crown, Scroll, CloudSun, Wrench, X } from '@phosphor-icons/react'
import { usePanelState, type PanelId } from '@/lib/usePanelState'
import CitizensPanel from './CitizensPanel'
import SettlementsPanel from './SettlementsPanel'
import CivilizationPanel from './CivilizationPanel'
import ChroniclePanel from './ChroniclePanel'
import AlmanacPanel from './AlmanacPanel'
import StewardsDeskPanel from './StewardsDeskPanel'

const PANEL_META: Record<PanelId, { title: string; icon: typeof UsersThree; content: React.ComponentType }> = {
  citizens: { title: 'Citizens', icon: UsersThree, content: CitizensPanel },
  settlements: { title: 'Settlements', icon: HouseLine, content: SettlementsPanel },
  civilization: { title: 'Civilization', icon: Crown, content: CivilizationPanel },
  chronicle: { title: 'Chronicle', icon: Scroll, content: ChroniclePanel },
  almanac: { title: 'Almanac', icon: CloudSun, content: AlmanacPanel },
  steward: { title: "Steward's Desk", icon: Wrench, content: StewardsDeskPanel },
}

/**
 * Generic slide-in container for every content panel. Replaces the old
 * Sheet primitive, which had no enter transition and no Escape handling.
 * Deliberately plain conditional rendering + a CSS keyframe on mount rather
 * than Motion's AnimatePresence - a Motion `animate` transition here was
 * found to get permanently stuck at its `initial` state in this app's dev
 * environment (panel rendered at opacity:0, offset 48px, indefinitely),
 * which silently made every panel invisible. CSS animations always resolve
 * to their end state, so there's no such failure mode. The world
 * (WorldStage) is always mounted underneath and stays interactive - closing
 * a panel never re-fetches or remounts the map.
 */
export function PanelHost() {
  const { panel, closePanel } = usePanelState()
  const meta = panel ? PANEL_META[panel] : null

  useEffect(() => {
    if (!panel) return
    const onKey = (e: KeyboardEvent) => {
      if (e.key === 'Escape') closePanel()
    }
    window.addEventListener('keydown', onKey)
    return () => window.removeEventListener('keydown', onKey)
  }, [panel, closePanel])

  if (!meta) return null

  return (
    <>
      <div
        className="animate-backdrop fixed inset-0 z-40 bg-background-deep/35 backdrop-blur-[2px]"
        onClick={closePanel}
      />
      <div
        key={panel}
        className="animate-panel-slide fixed inset-y-0 right-0 z-40 flex h-full w-full max-w-md flex-col md:inset-y-4 md:right-4 md:max-w-lg"
      >
        <div className="panel-carved-r flex h-full flex-col overflow-hidden border border-border/70 bg-panel shadow-atlas-lg">
          <div className="flex shrink-0 items-center justify-between border-b border-border/60 px-5 py-4">
            <div className="flex items-center gap-2.5">
              <meta.icon size={19} weight="duotone" className="text-primary" />
              <h2 className="font-display text-lg font-semibold">{meta.title}</h2>
            </div>
            <button
              onClick={closePanel}
              className="flex h-8 w-8 items-center justify-center rounded-full text-muted-foreground transition-colors hover:bg-accent hover:text-accent-foreground"
              aria-label="Close panel"
            >
              <X size={16} />
            </button>
          </div>
          <div className="scroll-atlas flex-1 overflow-y-auto p-5">
            <meta.content />
          </div>
        </div>
      </div>
    </>
  )
}

import { UsersThree, HouseLine, Crown, Scroll, CloudSun, Wrench, Compass } from '@phosphor-icons/react'
import { usePanelState, type PanelId } from '@/lib/usePanelState'
import { CivilizationOverview } from '@/components/hud/CivilizationOverview'
import { LiveEventTicker } from '@/components/hud/LiveEventTicker'
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

function OverviewTab() {
  return (
    <div>
      <CivilizationOverview />
      <LiveEventTicker />
    </div>
  )
}

/**
 * The sidebar's persistent "display window" - always showing something,
 * never a slide-in overlay the map has to be interrupted for. With no tab
 * selected this shows the Overview (civilization trends + world activity);
 * selecting a tab in CompassRail swaps this content in place, the same way
 * a game's side panel changes with the selected menu item rather than
 * popping a dialog over the viewport.
 */
export function PanelHost() {
  const { panel } = usePanelState()
  const meta = panel ? PANEL_META[panel] : null

  return (
    <div key={panel ?? 'overview'} className="animate-view-swap">
      {meta ? (
        <div>
          <div className="flex items-center gap-2.5 border-b border-border/60 px-3 py-3">
            <meta.icon size={17} weight="duotone" className="text-primary" />
            <h2 className="font-display text-sm font-semibold">{meta.title}</h2>
          </div>
          <div className="p-3">
            <meta.content />
          </div>
        </div>
      ) : (
        <div>
          <div className="flex items-center gap-2.5 border-b border-border/60 px-3 py-3">
            <Compass size={17} weight="duotone" className="text-primary" />
            <h2 className="font-display text-sm font-semibold">Overview</h2>
          </div>
          <OverviewTab />
        </div>
      )}
    </div>
  )
}

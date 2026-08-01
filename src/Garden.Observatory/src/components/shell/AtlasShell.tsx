import { useState } from 'react'
import { WorldStage } from './WorldStage'
import { VitalsCluster } from '@/components/hud/VitalsCluster'
import { TimeHud } from '@/components/hud/TimeHud'
import { TopRightCluster } from '@/components/hud/TopRightCluster'
import { CompassRail } from '@/components/hud/CompassRail'
import { MapFooterBar } from '@/components/hud/MapFooterBar'
import { PanelHost } from '@/components/panels/PanelHost'
import GlobalSearch from '@/components/GlobalSearch'
import NotificationArea from '@/components/NotificationArea'
import { useNotificationBus } from '@/components/NotificationToast'

/**
 * The permanent stage - a fixed two-pane layout (map column + sidebar
 * column) rather than the earlier full-bleed-map-with-floating-chrome
 * model. The map stays the largest, always-visible element, but is now a
 * bounded pane with a header/footer instead of the whole viewport; the
 * sidebar is a permanent tab strip over a persistent display window
 * (PanelHost) instead of a slide-in overlay, so world state (population,
 * recent events, whichever tab is open) reads at a glance without any
 * panel needing to be opened first.
 */
export default function AtlasShell() {
  const [searchOpen, setSearchOpen] = useState(false)
  const notifications = useNotificationBus()

  return (
    <div className="h-dvh w-dvw overflow-hidden bg-background-deep font-sans text-foreground">
      <div className="flex h-full w-full flex-col lg:flex-row">
        {/* Map column */}
        <div className="relative flex min-h-0 min-w-0 flex-1 flex-col">
          <VitalsCluster />
          <div className="relative min-h-0 flex-1">
            <WorldStage />
            <TimeHud />
          </div>
          <MapFooterBar />
        </div>

        {/* Sidebar column */}
        <div className="flex h-[70dvh] w-full shrink-0 flex-col border-t border-border/70 lg:h-full lg:w-[26rem] lg:border-l lg:border-t-0">
          <TopRightCluster onOpenSearch={() => setSearchOpen(true)} />
          <CompassRail />
          <div className="scroll-atlas min-h-0 flex-1 overflow-y-auto">
            <PanelHost />
          </div>
        </div>
      </div>

      <GlobalSearch open={searchOpen} onOpenChange={setSearchOpen} />
      <NotificationArea notifications={notifications} />
    </div>
  )
}

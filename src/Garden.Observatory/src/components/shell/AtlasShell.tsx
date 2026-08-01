import { useState } from 'react'
import { WorldStage } from './WorldStage'
import { VitalsCluster } from '@/components/hud/VitalsCluster'
import { TimeHud } from '@/components/hud/TimeHud'
import { TopRightCluster } from '@/components/hud/TopRightCluster'
import { CompassRail } from '@/components/hud/CompassRail'
import { MapNavRail } from '@/components/hud/MapNavRail'
import { MapStatusPanel } from '@/components/hud/MapStatusPanel'
import { MapFooterBar } from '@/components/hud/MapFooterBar'
import { PanelHost } from '@/components/panels/PanelHost'
import GlobalSearch from '@/components/GlobalSearch'
import NotificationArea from '@/components/NotificationArea'
import { useNotificationBus } from '@/components/NotificationToast'
import { MapControlsProvider } from '@/lib/mapControls'

/**
 * The permanent stage - a fixed three-region layout: a narrow map-control
 * rail, the bounded map column (header, map, status strip, footer), and a
 * sidebar column (search/utility row, docked tab menu, persistent display
 * window). Replaces the earlier full-bleed-map-with-floating-chrome model:
 * the map is now a bounded pane rather than the whole viewport, and
 * selecting a sidebar tab swaps content in place instead of sliding an
 * overlay panel over the map.
 */
export default function AtlasShell() {
  const [searchOpen, setSearchOpen] = useState(false)
  const notifications = useNotificationBus()

  return (
    <div className="h-dvh w-dvw overflow-hidden bg-background-deep font-sans text-foreground">
      <div className="flex h-full w-full flex-col lg:flex-row">
        <MapControlsProvider>
          <div className="hidden lg:block">
            <MapNavRail />
          </div>

          {/* Map column */}
          <div className="relative flex min-h-0 min-w-0 flex-1 flex-col">
            <VitalsCluster />
            <div className="relative min-h-0 flex-1">
              <WorldStage />
              <TimeHud />
            </div>
            <MapStatusPanel />
            <MapFooterBar />
          </div>
        </MapControlsProvider>

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

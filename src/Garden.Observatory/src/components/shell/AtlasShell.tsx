import { useState } from 'react'
import { WorldStage } from './WorldStage'
import { VitalsCluster } from '@/components/hud/VitalsCluster'
import { TimeHud } from '@/components/hud/TimeHud'
import { TopRightCluster } from '@/components/hud/TopRightCluster'
import { CompassRail } from '@/components/hud/CompassRail'
import { PanelHost } from '@/components/panels/PanelHost'
import GlobalSearch from '@/components/GlobalSearch'
import NotificationArea from '@/components/NotificationArea'
import { useNotificationBus } from '@/components/NotificationToast'

/**
 * The permanent stage. Unlike a routed dashboard, this component never
 * unmounts and never gets replaced by a "page" - the world underneath is
 * always there; everything else is a floating HUD layer on top of it. See
 * the plan doc's "Living Atlas" concept for the full rationale.
 */
export default function AtlasShell() {
  const [searchOpen, setSearchOpen] = useState(false)
  const notifications = useNotificationBus()

  return (
    <div className="relative h-dvh w-dvw overflow-hidden bg-background-deep font-sans text-foreground">
      <WorldStage />
      <VitalsCluster />
      <TopRightCluster onOpenSearch={() => setSearchOpen(true)} />
      <TimeHud />
      <CompassRail />
      <PanelHost />
      <GlobalSearch open={searchOpen} onOpenChange={setSearchOpen} />
      <NotificationArea notifications={notifications} />
    </div>
  )
}

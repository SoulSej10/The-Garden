import { useEffect, useState } from 'react'
import { Outlet, useLocation } from 'react-router-dom'
import Sidebar from './Sidebar.tsx'
import TopBar from './TopBar.tsx'
import GlobalSearch from './GlobalSearch.tsx'
import NotificationArea from './NotificationArea.tsx'
import { useNotificationBus } from './NotificationToast.tsx'

export default function Layout() {
  const [searchOpen, setSearchOpen] = useState(false)
  const [sidebarCollapsed, setSidebarCollapsed] = useState(false)
  // Rebalancing audit finding 9: below the md breakpoint the sidebar's
  // containing div was plain `hidden`, with no alternative navigation
  // surface at all - every route past the initial page became unreachable
  // through the UI on any phone/narrow window. This adds the standard
  // responsive-sidebar pattern: a hamburger-triggered slide-over drawer
  // rendering the same Sidebar, rather than a second nav list to maintain.
  const [mobileNavOpen, setMobileNavOpen] = useState(false)
  const notifications = useNotificationBus()
  const location = useLocation()

  useEffect(() => {
    setMobileNavOpen(false)
  }, [location.pathname])

  return (
    <div className="flex h-screen flex-col">
      <TopBar
        onToggleSearch={() => setSearchOpen((v) => !v)}
        onToggleMobileNav={() => setMobileNavOpen((v) => !v)}
      />
      {searchOpen && <GlobalSearch />}
      <div className="flex flex-1 overflow-hidden">
        <div className={`${sidebarCollapsed ? 'w-0 md:w-14' : 'w-56'} transition-all duration-200 hidden md:block`}>
          <Sidebar collapsed={sidebarCollapsed} onToggle={() => setSidebarCollapsed((v) => !v)} />
        </div>
        <main className="flex-1 overflow-y-auto p-4 md:p-6">
          <Outlet />
        </main>
      </div>
      <NotificationArea notifications={notifications} />
      {sidebarCollapsed === false && (
        <button
          onClick={() => setSidebarCollapsed(true)}
          className="fixed bottom-4 left-4 z-40 hidden md:flex h-8 w-8 items-center justify-center rounded-md border bg-background text-xs text-muted-foreground hover:bg-accent"
          title="Collapse sidebar"
        >
          ◀
        </button>
      )}
      {mobileNavOpen && (
        <div className="fixed inset-0 z-50 flex md:hidden">
          <div
            className="absolute inset-0 bg-black/40"
            onClick={() => setMobileNavOpen(false)}
            aria-hidden="true"
          />
          <div className="relative z-10 h-full w-64 bg-background shadow-xl">
            <Sidebar collapsed={false} />
          </div>
        </div>
      )}
    </div>
  )
}

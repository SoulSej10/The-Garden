import { useState } from 'react'
import { Outlet } from 'react-router-dom'
import Sidebar from './Sidebar.tsx'
import TopBar from './TopBar.tsx'
import GlobalSearch from './GlobalSearch.tsx'
import NotificationArea from './NotificationArea.tsx'
import { useNotificationBus } from './NotificationToast.tsx'

export default function Layout() {
  const [searchOpen, setSearchOpen] = useState(false)
  const [sidebarCollapsed, setSidebarCollapsed] = useState(false)
  const notifications = useNotificationBus()

  return (
    <div className="flex h-screen flex-col">
      <TopBar onToggleSearch={() => setSearchOpen((v) => !v)} />
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
    </div>
  )
}

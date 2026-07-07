import { NavLink } from 'react-router-dom'

const navItems = [
  { to: '/', label: 'Dashboard', icon: '⊞' },
  { to: '/environment', label: 'Environment', icon: '🌍' },
  { to: '/map', label: 'World Map', icon: '🗺' },
  { to: '/citizens', label: 'Citizens', icon: '👥' },
  { to: '/settlements', label: 'Settlements', icon: '🏘' },
  { to: '/economy', label: 'Economy', icon: '📊' },
  { to: '/civilization', label: 'Civilization', icon: '🏛' },
  { to: '/history', label: 'History', icon: '📜' },
  { to: '/production', label: 'Production', icon: '🖥' },
  { to: '/diagnostics', label: 'Diagnostics', icon: '⚙' },
]

export default function Sidebar({ collapsed }: { collapsed: boolean; onToggle?: () => void }) {
  return (
    <aside className="flex h-full flex-col border-r bg-muted/30">
      <nav className="flex flex-col gap-1 p-3">
        {navItems.map((item) => (
          <NavLink
            key={item.to}
            to={item.to}
            end={item.to === '/'}
            className={({ isActive }) =>
              `flex items-center gap-3 rounded-md px-3 py-2 text-sm transition-colors ${
                isActive
                  ? 'bg-accent text-accent-foreground font-medium'
                  : 'text-muted-foreground hover:bg-accent/50 hover:text-accent-foreground'
              }`
            }
            title={collapsed ? item.label : undefined}
          >
            <span className="w-5 text-center shrink-0">{item.icon}</span>
            {!collapsed && <span className="truncate">{item.label}</span>}
          </NavLink>
        ))}
      </nav>
    </aside>
  )
}

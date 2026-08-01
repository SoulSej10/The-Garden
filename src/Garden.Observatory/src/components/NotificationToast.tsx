import { useEffect, useState } from 'react'

interface Notification {
  id: number
  title: string
  description: string
  category: string
  severity: string
}

let nextId = 0
let addFn: ((n: Omit<Notification, 'id'>) => void) | null = null

export function pushNotification(title: string, description: string, category: string, severity: string) {
  addFn?.({ title, description, category, severity })
}

export function useNotificationBus() {
  const [notifications, setNotifications] = useState<Notification[]>([])

  useEffect(() => {
    addFn = (n) => {
      const id = nextId++
      setNotifications((prev) => [...prev, { ...n, id }])
      setTimeout(() => {
        setNotifications((prev) => prev.filter((x) => x.id !== id))
      }, 5000)
    }
    return () => { addFn = null }
  }, [])

  return notifications
}

const severityAccent: Record<string, string> = {
  High: 'bg-status-danger',
  Normal: 'bg-status-water',
  Info: 'bg-status-thriving',
}

export function NotificationToast({ notification }: { notification: Notification }) {
  const [visible, setVisible] = useState(false)

  useEffect(() => {
    requestAnimationFrame(() => setVisible(true))
    const timer = setTimeout(() => setVisible(false), 4500)
    return () => clearTimeout(timer)
  }, [])

  return (
    <div
      className={`animate-shine panel-carved relative overflow-hidden border border-border/70 bg-panel/95 p-3 pl-4 shadow-atlas-lg backdrop-blur-md transition-all duration-300 ${
        visible ? 'translate-x-0 opacity-100' : 'translate-x-6 opacity-0'
      }`}
    >
      <span className={`absolute inset-y-0 left-0 w-1 ${severityAccent[notification.severity] ?? 'bg-muted-foreground'}`} />
      <p className="font-display text-xs font-semibold">{notification.title}</p>
      <p className="mt-0.5 text-xs text-muted-foreground">{notification.description}</p>
      <span className="mt-1 inline-block text-[10px] uppercase tracking-wide text-muted-foreground/70">{notification.category}</span>
    </div>
  )
}

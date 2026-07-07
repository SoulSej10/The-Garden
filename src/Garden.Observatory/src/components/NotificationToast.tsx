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

const severityColors: Record<string, string> = {
  High: 'border-red-500 bg-red-50 dark:bg-red-950',
  Normal: 'border-blue-500 bg-blue-50 dark:bg-blue-950',
  Info: 'border-green-500 bg-green-50 dark:bg-green-950',
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
      className={`border-l-4 p-3 rounded-r-md shadow-md transition-all duration-300 ${
        visible ? 'opacity-100 translate-x-0' : 'opacity-0 translate-x-full'
      } ${severityColors[notification.severity] ?? 'border-gray-500 bg-gray-50'}`}
    >
      <p className="text-xs font-semibold">{notification.title}</p>
      <p className="text-xs text-muted-foreground">{notification.description}</p>
      <span className="text-[10px] text-muted-foreground/60">{notification.category}</span>
    </div>
  )
}

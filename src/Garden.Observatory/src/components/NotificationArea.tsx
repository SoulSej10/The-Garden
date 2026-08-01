import { NotificationToast } from './NotificationToast.tsx'

interface Notification {
  id: number
  title: string
  description: string
  category: string
  severity: string
}

export default function NotificationArea({ notifications }: { notifications: Notification[] }) {
  if (notifications.length === 0) return null

  return (
    <div className="pointer-events-none fixed right-4 top-20 z-50 flex w-72 flex-col gap-2 md:right-6 md:top-24">
      {notifications.map((n) => (
        <NotificationToast key={n.id} notification={n} />
      ))}
    </div>
  )
}

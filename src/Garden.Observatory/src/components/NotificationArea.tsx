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
    <div className="fixed bottom-4 right-4 z-50 flex w-80 flex-col gap-2">
      {notifications.map((n) => (
        <NotificationToast key={n.id} notification={n} />
      ))}
    </div>
  )
}

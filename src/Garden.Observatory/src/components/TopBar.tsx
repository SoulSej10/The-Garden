export default function TopBar() {
  return (
    <header className="flex h-14 items-center border-b px-6">
      <div className="flex items-center gap-2">
        <span className="text-lg font-semibold">The Garden</span>
      </div>
      <div className="ml-auto flex items-center gap-4">
        <span className="text-sm text-muted-foreground">
          Observing...
        </span>
      </div>
    </header>
  )
}

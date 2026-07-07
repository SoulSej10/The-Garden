import * as React from "react"

import { cn } from "@/lib/utils"

const SheetContext = React.createContext<{ onClose: () => void }>({ onClose: () => {} })

function Sheet({ open, onClose, children }: {
  open: boolean
  onClose: () => void
  children: React.ReactNode
}) {
  if (!open) return null

  return (
    <SheetContext.Provider value={{ onClose }}>
      <div className="fixed inset-0 z-50 bg-black/50" onClick={onClose} />
      <div
        className={cn(
          "fixed inset-y-0 right-0 z-50 flex h-full w-full max-w-md flex-col border-l bg-background shadow-lg"
        )}
      >
        <div className="flex items-center justify-between border-b px-4 py-3">
          <SheetTitle />
          <button
            onClick={onClose}
            className="rounded-md p-1 text-muted-foreground hover:bg-accent hover:text-accent-foreground"
          >
            ✕
          </button>
        </div>
        <div className="flex-1 overflow-y-auto p-4">
          {children}
        </div>
      </div>
    </SheetContext.Provider>
  )
}

function SheetContent({ children, className }: { children: React.ReactNode; className?: string }) {
  return <div className={cn("space-y-6", className)}>{children}</div>
}

function SheetHeader({ children }: { children: React.ReactNode }) {
  return <div className="space-y-1">{children}</div>
}

function SheetTitle({ children }: { children?: React.ReactNode }) {
  if (!children) return null
  return <h2 className="text-lg font-semibold">{children}</h2>
}

function SheetDescription({ children }: { children: React.ReactNode }) {
  return <p className="text-sm text-muted-foreground">{children}</p>
}

export { Sheet, SheetContent, SheetHeader, SheetTitle, SheetDescription }

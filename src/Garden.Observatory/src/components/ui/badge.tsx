import * as React from "react"
import { cva, type VariantProps } from "class-variance-authority"

import { cn } from "@/lib/utils"

/**
 * The five `status-*` variants are the app's fixed semantic vocabulary
 * (health / hunger / danger / thriving / water) - reserved for status
 * meaning only, never used decoratively. Keep this set closed rather than
 * adding one-off colors per panel.
 */
const badgeVariants = cva(
  "inline-flex items-center gap-1 rounded-full border px-2.5 py-0.5 text-xs font-medium transition-colors focus:outline-none focus:ring-2 focus:ring-ring focus:ring-offset-2",
  {
    variants: {
      variant: {
        default: "border-transparent bg-primary text-primary-foreground",
        secondary: "border-transparent bg-secondary text-secondary-foreground",
        destructive: "border-transparent bg-destructive text-destructive-foreground",
        outline: "border-border text-foreground",
        "status-health": "border-transparent bg-status-health text-status-health-foreground",
        "status-hunger": "border-transparent bg-status-hunger text-status-hunger-foreground",
        "status-danger": "border-transparent bg-status-danger text-status-danger-foreground",
        "status-thriving": "border-transparent bg-status-thriving text-status-thriving-foreground",
        "status-water": "border-transparent bg-status-water text-status-water-foreground",
      },
    },
    defaultVariants: {
      variant: "default",
    },
  }
)

export interface BadgeProps
  extends React.HTMLAttributes<HTMLDivElement>,
    VariantProps<typeof badgeVariants> {}

function Badge({ className, variant, ...props }: BadgeProps) {
  return (
    <div className={cn(badgeVariants({ variant }), className)} {...props} />
  )
}

export { Badge, badgeVariants }

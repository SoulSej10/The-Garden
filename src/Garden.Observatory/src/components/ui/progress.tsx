import * as React from "react"

import { cn } from "@/lib/utils"

/**
 * The filled portion (the actual current value) is controlled by
 * `indicatorClassName`. `className` only styles the track - the empty
 * remainder behind the fill, always a neutral color.
 *
 * Previously `className` was applied to the track while the fill was
 * hardcoded to `bg-primary` regardless of what callers passed in, so
 * severity colors (red/yellow/green) ended up on the empty portion of the
 * bar instead of the filled portion showing the actual value - callers
 * couldn't tell which section represented "how much," just that colors
 * changed somewhere on the bar.
 */
function Progress({
  className,
  indicatorClassName,
  value,
  ...props
}: React.ComponentProps<"div"> & { value?: number; indicatorClassName?: string }) {
  return (
    <div
      data-slot="progress"
      role="progressbar"
      aria-valuemin={0}
      aria-valuemax={100}
      aria-valuenow={value}
      className={cn(
        "relative h-2 w-full overflow-hidden rounded-full bg-muted",
        className
      )}
      {...props}
    >
      <div
        className={cn("h-full w-full flex-1 bg-primary transition-all", indicatorClassName)}
        style={{ transform: `translateX(-${100 - (value ?? 0)}%)` }}
      />
    </div>
  )
}

export { Progress }

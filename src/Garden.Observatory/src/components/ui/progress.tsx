import * as React from "react"

import { cn } from "@/lib/utils"

/**
 * The filled portion (the actual current value) is controlled by
 * `indicatorClassName`. `className` only styles the track - the empty
 * remainder behind the fill, always a neutral color. Callers pass a
 * status-* background (see badge.tsx variants) on the indicator so severity
 * reads consistently with the rest of the app's status vocabulary.
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
        "relative h-2.5 w-full overflow-hidden rounded-full bg-muted",
        className
      )}
      {...props}
    >
      <div
        className={cn(
          "h-full w-full flex-1 rounded-full bg-primary transition-all duration-500 ease-out",
          indicatorClassName
        )}
        style={{ transform: `translateX(-${100 - (value ?? 0)}%)` }}
      />
    </div>
  )
}

export { Progress }

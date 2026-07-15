import { useState } from 'react'

// Rebalancing audit finding 3: view settings (map layer toggles, viewport
// position/zoom) previously lived in plain useState with no persistence at
// all - any remount (route navigation away and back, or a page reload)
// silently reset them to hardcoded defaults. localStorage is the right
// scope for this: it's a view preference, not simulation state, so it
// belongs client-side rather than round-tripping through the API.
export function useLocalStorageState<T>(key: string, defaultValue: T) {
  const [state, setState] = useState<T>(() => {
    try {
      const stored = window.localStorage.getItem(key)
      return stored !== null ? (JSON.parse(stored) as T) : defaultValue
    } catch {
      return defaultValue
    }
  })

  const setPersistedState = (value: T | ((prev: T) => T)) => {
    setState((prev) => {
      const next = typeof value === 'function' ? (value as (prev: T) => T)(prev) : value
      try {
        window.localStorage.setItem(key, JSON.stringify(next))
      } catch {
        // Storage unavailable (private browsing, quota) - state still
        // updates in-memory for this session, it just won't persist.
      }
      return next
    })
  }

  return [state, setPersistedState] as const
}

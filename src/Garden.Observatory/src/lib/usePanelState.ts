import { useCallback } from 'react'
import { useSearchParams } from 'react-router-dom'

export type PanelId = 'citizens' | 'settlements' | 'civilization' | 'chronicle' | 'almanac' | 'steward'

/**
 * Single source of truth for "which panel floats over the world right now."
 * State lives in the URL (?panel=citizens&selected=<id>) rather than in
 * routes, so deep links / the browser back button / GlobalSearch's
 * navigate-to-entity behavior keep working without the world ever needing to
 * unmount - opening a panel never replaces the map, it just adds a param.
 */
export function usePanelState() {
  const [searchParams, setSearchParams] = useSearchParams()
  const panel = searchParams.get('panel') as PanelId | null
  const selected = searchParams.get('selected')
  const tab = searchParams.get('tab')

  const openPanel = useCallback(
    (id: PanelId, extra?: Record<string, string | undefined>) => {
      setSearchParams((prev) => {
        const next = new URLSearchParams(prev)
        next.set('panel', id)
        if (extra) {
          for (const [k, v] of Object.entries(extra)) {
            if (v === undefined) next.delete(k)
            else next.set(k, v)
          }
        }
        return next
      })
    },
    [setSearchParams]
  )

  const closePanel = useCallback(() => {
    setSearchParams((prev) => {
      const next = new URLSearchParams(prev)
      next.delete('panel')
      next.delete('selected')
      next.delete('tab')
      return next
    })
  }, [setSearchParams])

  const setSelected = useCallback(
    (id: string | null) => {
      setSearchParams((prev) => {
        const next = new URLSearchParams(prev)
        if (id) next.set('selected', id)
        else next.delete('selected')
        return next
      })
    },
    [setSearchParams]
  )

  const setTab = useCallback(
    (value: string) => {
      setSearchParams((prev) => {
        const next = new URLSearchParams(prev)
        next.set('tab', value)
        return next
      })
    },
    [setSearchParams]
  )

  return { panel, selected, tab, openPanel, closePanel, setSelected, setTab }
}

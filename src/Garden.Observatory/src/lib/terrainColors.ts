export interface TerrainColorEntry {
  label: string
  color: string
  highlight?: string
}

export const TERRAIN_PALETTE: Record<string, TerrainColorEntry> = {
  Ocean: { label: 'Ocean', color: '#16436b' },
  Coast: { label: 'Coast', color: '#e3d3a4' },
  Plains: { label: 'Plains', color: '#cddc8f' },
  Grassland: { label: 'Grassland', color: '#4f9e42' },
  Forest: { label: 'Forest', color: '#26492b' },
  Hills: { label: 'Hills', color: '#a67c4d' },
  Mountains: { label: 'Mountains', color: '#918d86', highlight: '#c9c5bd' },
  Swamp: { label: 'Swamp', color: '#3c4f47' },
  River: { label: 'River', color: '#2f9fe0' },
  Lake: { label: 'Lake', color: '#4a7fbf' },
}

export const TERRAIN_LABELS: Record<string, string> = {
  Ocean: '~',
  Coast: '.',
  Plains: ',',
  Grassland: '"',
  Forest: '#',
  Hills: '^',
  Mountains: 'A',
  Swamp: '%',
  River: '~',
  Lake: '~',
}

export const TERRAIN_ORDER = [
  'Ocean',
  'Coast',
  'Plains',
  'Grassland',
  'Forest',
  'Hills',
  'Mountains',
  'Swamp',
  'River',
  'Lake',
]

export function getTerrainColor(terrain: string, isRiver: boolean, isLake: boolean): TerrainColorEntry {
  if (isRiver) return TERRAIN_PALETTE.River
  if (isLake || terrain === 'Lake') return TERRAIN_PALETTE.Lake
  return TERRAIN_PALETTE[terrain] ?? { label: terrain, color: '#94a3b8' }
}

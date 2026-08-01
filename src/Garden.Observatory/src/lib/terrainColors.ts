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
  // Layered geological generator additions (WorldGenerator.GenerateSpecialTerrain)
  Desert: { label: 'Desert', color: '#e0b96a' },
  Badlands: { label: 'Badlands', color: '#a8543a', highlight: '#c97a52' },
  Canyon: { label: 'Canyon', color: '#8a3f2e', highlight: '#b8613f' },
  Glacier: { label: 'Glacier', color: '#dceef5', highlight: '#ffffff' },
  Fjord: { label: 'Fjord', color: '#3d5a73' },
  VolcanicIsland: { label: 'Volcanic Island', color: '#3a2a28', highlight: '#e0592a' },
  GeothermalField: { label: 'Geothermal Field', color: '#8a6d3f', highlight: '#e8943a' },
  CoralReef: { label: 'Coral Reef', color: '#2dbfa8' },
  Delta: { label: 'Delta', color: '#6b8f4e' },
  Crater: { label: 'Crater', color: '#5c5850', highlight: '#2e2b27' },
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
  Desert: ':',
  Badlands: '=',
  Canyon: 'V',
  Glacier: '*',
  Fjord: '~',
  VolcanicIsland: '^',
  GeothermalField: '!',
  CoralReef: 'o',
  Delta: 'y',
  Crater: 'O',
}

export const TERRAIN_ORDER = [
  'Ocean',
  'Coast',
  'CoralReef',
  'Fjord',
  'Delta',
  'Plains',
  'Grassland',
  'Forest',
  'Swamp',
  'Desert',
  'Badlands',
  'Hills',
  'Mountains',
  'Canyon',
  'Glacier',
  'VolcanicIsland',
  'GeothermalField',
  'Crater',
  'River',
  'Lake',
]

export function getTerrainColor(terrain: string, isRiver: boolean, isLake: boolean): TerrainColorEntry {
  if (isRiver) return TERRAIN_PALETTE.River
  if (isLake || terrain === 'Lake') return TERRAIN_PALETTE.Lake
  return TERRAIN_PALETTE[terrain] ?? { label: terrain, color: '#94a3b8' }
}

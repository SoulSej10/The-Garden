export interface TerrainColorEntry {
  label: string
  color: string
  highlight?: string
}

// A deliberately punchier, more saturated palette than realistic cartography
// muted tones - leans cool (blues/teals/violets dominate) with warm accents
// reserved for desert/volcanic/lava contrast, closer to a strategy-game map
// (Civilization/Humankind) than a muted topographic reference.
export const TERRAIN_PALETTE: Record<string, TerrainColorEntry> = {
  Ocean: { label: 'Ocean', color: '#0f5fa8' },
  Coast: { label: 'Coast', color: '#f2cf85' },
  Plains: { label: 'Plains', color: '#c8e05a' },
  Grassland: { label: 'Grassland', color: '#3ec95e' },
  Forest: { label: 'Forest', color: '#0f6b4a' },
  Hills: { label: 'Hills', color: '#c98a3f' },
  Mountains: { label: 'Mountains', color: '#8577a8', highlight: '#c4b8e8' },
  Swamp: { label: 'Swamp', color: '#1f5c52' },
  River: { label: 'River', color: '#22c3f0' },
  Lake: { label: 'Lake', color: '#2f8fe0' },
  // Layered geological generator additions (WorldGenerator.GenerateSpecialTerrain)
  Desert: { label: 'Desert', color: '#f0b43e' },
  Badlands: { label: 'Badlands', color: '#c1512f', highlight: '#e67a4a' },
  Canyon: { label: 'Canyon', color: '#8a2f1f', highlight: '#c94f2f' },
  Glacier: { label: 'Glacier', color: '#c8f0fa', highlight: '#ffffff' },
  Fjord: { label: 'Fjord', color: '#1f5878' },
  VolcanicIsland: { label: 'Volcanic Island', color: '#241a1c', highlight: '#ff6a2e' },
  GeothermalField: { label: 'Geothermal Field', color: '#7a5a35', highlight: '#ffb238' },
  CoralReef: { label: 'Coral Reef', color: '#1fd6b8' },
  Delta: { label: 'Delta', color: '#5ba847' },
  Crater: { label: 'Crater', color: '#5a5468', highlight: '#2b2733' },
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

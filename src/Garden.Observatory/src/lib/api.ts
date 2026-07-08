import axios from 'axios'

const isDev = import.meta.env.DEV

const apiUrl = import.meta.env.VITE_API_URL ?? (isDev ? 'http://localhost:5088' : '/')

const api = axios.create({
  baseURL: apiUrl,
  timeout: 10000,
})

export interface WorldStatus {
  width: number
  height: number
  totalTiles: number
  tileCounts: Record<string, number>
  isInitialized: boolean
}

export interface TileData {
  x: number
  y: number
  terrain: string
  biome: string
  climate: string
  elevation: number
  moisture: number
  temperature: number
  isRiver: boolean
  isLake: boolean
  resources: Array<{
    type: string
    quantity: number
    maxCapacity: number
    regenerationRate: number
  }>
  occupancy: null | {
    occupiedBy: string
    structureType: string
  }
}

export interface MapData {
  offsetX: number
  offsetY: number
  width: number
  height: number
  totalReturned: number
  tiles: Array<{
    x: number
    y: number
    terrain: string
    biome: string
    elevation: number
    isRiver: boolean
    isLake: boolean
  }>
}

export interface WeatherData {
  condition: string
  intensity: number
  remainingDuration: number
  temperatureModifier: number
  windStrength: number
  humidityModifier: number
}

export interface ClimateData {
  zones: Array<{
    zone: string
    tileCount: number
    avgTemperature: number
    avgMoisture: number
    avgElevation: number
  }>
  derived: boolean
}

export interface ResourcesData {
  summary: Record<string, {
    total: number
    totalCapacity: number
    deposits: number
  }>
  totalResources: number
}

export interface EnvironmentEvent {
  tick: number
  eventType: string
  severity: string
  x: number | null
  y: number | null
  time: string
}

export interface EventsData {
  total: number
  events: EnvironmentEvent[]
}

export async function fetchWorldStatus(): Promise<WorldStatus> {
  const { data } = await api.get('/world')
  return data
}

export async function fetchMap(offsetX = 0, offsetY = 0, width = 50, height = 50): Promise<MapData> {
  const { data } = await api.get('/world/map', {
    params: { regionX: offsetX, regionY: offsetY, width, height },
  })
  return data
}

export async function fetchTile(x: number, y: number): Promise<TileData> {
  const { data } = await api.get(`/world/tiles/${x}/${y}`)
  return data
}

export async function fetchWeather(): Promise<WeatherData> {
  const { data } = await api.get('/environment/weather')
  return data
}

export async function fetchClimate(): Promise<ClimateData> {
  const { data } = await api.get('/environment/climate')
  return data
}

export async function fetchResources(): Promise<ResourcesData> {
  const { data } = await api.get('/environment/resources')
  return data
}

export async function fetchEvents(limit = 50): Promise<EventsData> {
  const { data } = await api.get('/environment/events', {
    params: { limit },
  })
  return data
}

export async function fetchSimulationStatus() {
  const { data } = await api.get('/simulation/status')
  return data
}

export async function startSimulation() {
  const { data } = await api.post('/simulation/start')
  return data
}

export async function pauseSimulation() {
  const { data } = await api.post('/simulation/pause')
  return data
}

export interface CitizenSummary {
  id: string
  name: string
  age: number
  stage: string
  currentActivity: string
  currentGoal: string
  tileX: number
  tileY: number
  health: number
  hunger: number
  thirst: number
  energy: number
  warmth: number
}

export interface CitizensData {
  total: number
  page: number
  pageSize: number
  totalPages: number
  citizens: CitizenSummary[]
}

export interface PopulationData {
  total: number
  alive: number
  dead: number
  totalBirths: number
  totalDeaths: number
  averageAge: number
}

export interface CitizenDetail {
  id: string
  firstName: string
  lastName: string
  age: number
  stage: string
  biologicalSex: string
  isAlive: boolean
  currentActivity: string
  currentGoal: string
  tileX: number
  tileY: number
  attributes: {
    strength: number
    endurance: number
    intelligence: number
    dexterity: number
    perception: number
  }
  personality: {
    curiosity: number
    patience: number
    aggression: number
    compassion: number
    diligence: number
    introversion: number
  }
  needs: {
    hunger: number
    thirst: number
    energy: number
    warmth: number
    health: number
  }
}

export interface CitizenDetailData {
  citizen: CitizenDetail
  recentEvents: Array<{
    tick: number
    eventType: string
    description: string
  }>
}

export async function fetchCitizens(
  page = 1,
  pageSize = 50,
  sortBy = 'name',
  search = ''
): Promise<CitizensData> {
  const { data } = await api.get('/citizens', {
    params: { page, pageSize, sortBy, search: search || undefined },
  })
  return data
}

export async function fetchPopulation(): Promise<PopulationData> {
  const { data } = await api.get('/citizens/population')
  return data
}

export async function fetchCitizenDetail(id: string): Promise<CitizenDetailData> {
  const { data } = await api.get(`/citizens/${id}`)
  return data
}

export interface SettlementSummary {
  id: string
  name: string
  population: number
  tileX: number
  tileY: number
  territoryRadius: number
  completedBuildings: number
  underConstruction: number
  totalBuildings: number
  foodReserves: number
  waterReserves: number
  woodReserves: number
  stoneReserves: number
  members: number
}

export interface SettlementDetail {
  id: string
  name: string
  population: number
  tileX: number
  tileY: number
  territoryRadius: number
  foundedTick: number
  storage: Array<{ itemType: string; quantity: number; weight: number }>
  buildings: Array<{
    id: string
    buildingType: string
    status: string
    buildProgress: number
    tileX: number
    tileY: number
    buildTimeRequired: number
    assignedWorkerId: string | null
  }>
  members: Array<{
    id: string
    firstName: string
    lastName: string
    age: number
    currentActivity: string
    currentGoal: string
    tileX: number
    tileY: number
    isAlive: boolean
  }>
}

export interface EconomyData {
  tick: number
  settlements: Array<{
    name: string
    population: number
    foundedTick: number
    totalFood: number
    totalWater: number
    totalWood: number
    totalStone: number
    buildingCount: number
    buildingsByType: Record<string, number>
  }>
  globalGoodsCrafted: number
  globalTradeCount: number
  totalSettlements: number
}

export async function fetchSettlements(): Promise<SettlementSummary[]> {
  const { data } = await api.get('/settlements')
  return data
}

export async function fetchSettlementDetail(id: string): Promise<SettlementDetail> {
  const { data } = await api.get(`/settlements/${id}`)
  return data
}

export async function fetchSettlementBuildings(id: string) {
  const { data } = await api.get(`/settlements/${id}/buildings`)
  return data
}

export async function fetchEconomy(): Promise<EconomyData> {
  const { data } = await api.get('/economy')
  return data
}

export async function fetchEconomyResources(): Promise<Record<string, number>> {
  const { data } = await api.get('/economy/resources')
  return data
}

export interface HistoryRecord {
  id: string
  tick: number
  year: number
  day: number
  season: string
  eventType: string
  category: string
  title: string
  description: string
  locationX: number
  locationY: number
  locationName: string
  participantNames: string[]
  severity: number
  importance: string
}

export interface TimelineResult {
  page: number
  pageSize: number
  totalRecords: number
  totalPages: number
  entries: HistoryRecord[]
}

export interface HistoryStats {
  totalRecords: number
  births: number
  deaths: number
  eventTypeCounts: Record<string, number>
  memoryStats: Record<string, number>
  storyCount: number
}

export interface StorySummary {
  id: string
  tick: number
  category: string
  title: string
  summary: string
  participantNames: string[]
  generatedAtTick: number
}

export interface StoryDetail {
  id: string
  tick: number
  category: string
  title: string
  summary: string
  narrative: string
  participantNames: string[]
  relatedRecordIds: string[]
  generatedAtTick: number
}

export interface StoriesData {
  page: number
  pageSize: number
  totalStories: number
  stories: StorySummary[]
}

export interface MemoriesData {
  citizenId: string
  citizenName: string
  totalMemories: number
  memories: Array<{
    id: string
    tick: number
    eventType: string
    title: string
    description: string
    confidence: number
    emotionalImpact: number
  }>
}

export async function fetchHistory(): Promise<{ totalRecords: number; records: HistoryRecord[] }> {
  const { data } = await api.get('/history')
  return data
}

export async function fetchHistoryTimeline(
  page = 1,
  pageSize = 50,
  category?: string
): Promise<TimelineResult> {
  const { data } = await api.get('/history/timeline', {
    params: { page, pageSize, category },
  })
  return data
}

export async function searchHistory(
  q: string,
  page = 1,
  pageSize = 50
): Promise<{ page: number; pageSize: number; totalRecords: number; totalPages: number; records: HistoryRecord[] }> {
  const { data } = await api.get('/history/search', {
    params: { q, page, pageSize },
  })
  return data
}

export async function fetchHistoryStats(): Promise<HistoryStats> {
  const { data } = await api.get('/history/stats')
  return data
}

export async function fetchStories(page = 1, pageSize = 50): Promise<StoriesData> {
  const { data } = await api.get('/stories', { params: { page, pageSize } })
  return data
}

export async function fetchStoryDetail(id: string): Promise<StoryDetail> {
  const { data } = await api.get(`/stories/${id}`)
  return data
}

export async function fetchCitizenMemories(id: string): Promise<MemoriesData> {
  const { data } = await api.get(`/citizens/${id}/memories`)
  return data
}

export interface DashboardSummary {
  simulation: {
    status: string
    tick: number
    speed: number
    worldAge: string
  }
  population: {
    total: number
    alive: number
    dead: number
    births: number
    deaths: number
    averageAge: number
  }
  environment: {
    season: string
    temperature: number
    weather: string
  }
  settlements: {
    total: number
    totalBuildings: number
    totalFood: number
    totalPopulation: number
  }
  history: {
    totalRecords: number
    latestStory: { title: string; summary: string; category: string } | null
  }
}

export interface DiagnosticsData {
  simulation: {
    tickDurationMs: number
    tickRate: number
    uptimeMs: number
    isRunning: boolean
    targetSpeed: number
    currentTick: number
    registeredSystems: number
  }
  memory: {
    workingSetMB: number
    privateMemoryMB: number
    pagedMemoryMB: number
  }
  world: {
    totalCitizens: number
    aliveCitizens: number
    deadCitizens: number
    totalSettlements: number
    mapWidth: number
    mapHeight: number
  }
  process: {
    processName: string
    threads: number
    handleCount: number
    startTime: string
  }
}

export interface SearchResults {
  query: string
  citizens: Array<{ type: string; id: string; label: string; subLabel: string; location: { tileX: number; tileY: number } }>
  settlements: Array<{ type: string; id: string; label: string; subLabel: string; location: { tileX: number; tileY: number } }>
  total: number
  results: Array<{ type: string; id: string; label: string; subLabel: string; location: { tileX: number; tileY: number } }>
}

export async function fetchDashboardSummary(): Promise<DashboardSummary> {
  const { data } = await api.get('/dashboard/summary')
  return data
}

export async function fetchDashboardActivity(limit = 50) {
  const { data } = await api.get('/dashboard/activity', { params: { limit } })
  return data
}

export async function fetchDashboardPerformance() {
  const { data } = await api.get('/dashboard/performance')
  return data
}

export async function fetchDiagnostics(): Promise<DiagnosticsData> {
  const { data } = await api.get('/diagnostics')
  return data
}

export async function globalSearch(q: string, limit = 20): Promise<SearchResults> {
  const { data } = await api.get('/search', { params: { q, limit } })
  return data
}

export async function setSimulationSpeed(speed: number) {
  const { data } = await api.post('/simulation/speed', { speed })
  return data
}

export async function simulationStep() {
  const { data } = await api.post('/simulation/step')
  return data
}

export interface SystemHealth {
  status: string
  uptime: number
  timestamp: string
  version: string
  simulationRunning: boolean
  databaseConnected: boolean
}

export interface SystemStatistics {
  simulation: { tick: number; year: number; day: number; season: string; isRunning: boolean; speed: number; tickDurationMs: number; uptimeMs: number }
  world: { totalCitizens: number; aliveCitizens: number; deadCitizens: number; totalSettlements: number; activeKingdoms: number; totalBuildings: number; activeTradeRoutes: number; technologiesDiscovered: number; religions: number; historyRecords: number }
  performance: { workingSetMB: number; privateMemoryMB: number; threadCount: number; handleCount: number; cpuTime: number }
}

export interface SystemSaves {
  saves: Array<{ name: string; sizeBytes: number; lastModified: string }>
  count: number
}

export interface SystemBackups {
  backups: Array<{ name: string; type: string; citizenCount: number; settlementCount: number; createdAt: string }>
  count: number
}

export interface AssistantSummary {
  tick: number
  year: number
  day: number
  season: string
  narrative: string
  statistics: {
    totalCitizens: number
    aliveCitizens: number
    deadCitizens: number
    totalSettlements: number
    totalKingdoms: number
    totalBuildings: number
    totalTradeRoutes: number
    technologiesDiscovered: number
    historyRecordCount: number
  }
}

export async function fetchSystemHealth(): Promise<SystemHealth> {
  const { data } = await api.get('/system/health')
  return data
}

export async function fetchSystemStatistics(): Promise<SystemStatistics> {
  const { data } = await api.get('/system/statistics')
  return data
}

export async function fetchSystemSaves(): Promise<SystemSaves> {
  const { data } = await api.get('/system/saves')
  return data
}

export async function fetchSystemBackups(): Promise<SystemBackups> {
  const { data } = await api.get('/system/backups')
  return data
}

export async function saveWorld(name?: string) {
  const { data } = await api.post('/system/save', { name })
  return data
}

export async function loadWorld(name: string) {
  const { data } = await api.post('/system/load', { name })
  return data
}

export async function deleteSave(name: string) {
  const { data } = await api.delete(`/system/saves/${encodeURIComponent(name)}`)
  return data
}

export interface ResetWorldResult {
  message: string
  seed: number
  citizenCount: number
}

export async function resetWorld(password: string): Promise<ResetWorldResult> {
  const { data } = await api.post('/system/reset', { password })
  return data
}

export async function fetchAssistantSummary(): Promise<AssistantSummary> {
  const { data } = await api.get('/assistant/summary')
  return data
}

export async function askQuestion(question: string): Promise<{ question: string; answer: string; timestamp: string }> {
  const { data } = await api.post('/assistant/question', { question })
  return data
}

export interface CivilizationSummary {
  kingdomCount: number
  governmentCounts: Record<string, number>
  tradeRouteCount: number
  technologiesDiscovered: number
  cultureCount: number
  religionCount: number
}

export interface KingdomSummary {
  id: string
  name: string
  capitalName: string
  leaderName: string
  leaderAge: number
  population: number
  governmentType: string
  settlementCount: number
  territoryRadius: number
  stability: number
  foundedTick: number
}

export interface GovernmentEntry {
  id: string
  name: string
  governmentType: string
  leaderName: string
  population: number
  foundedTick: number
  buildingCount: number
}

export interface LeaderEntry {
  leaderId: string
  leaderName: string
  age: number
  biologicalSex: string
  contributionScore: number
  reputation: number
  intelligence: number
  compassion: number
  diligence: number
  aggression: number
  settlementId: string
  settlementName: string
  governmentType: string
}

export interface DiplomacyEntry {
  id: string
  entityA: string
  entityB: string
  relation: string
  relationScore: number
  hasTradeAgreement: boolean
  isAlliance: boolean
  lastInteractionTick: number
  establishedTick: number
}

export interface TradeRouteEntry {
  id: string
  fromSettlementName: string
  toSettlementName: string
  primaryGood: string
  totalVolumeTransported: number
  tripCount: number
  distance: number
  economicValue: number
  establishedTick: number
  lastTripTick: number
}

export interface TechnologyEntry {
  id: string
  name: string
  category: string
  description: string
  discoveredTick?: number
  settlementName?: string
  citizenName?: string
}

export interface TechnologyProgress {
  id: string
  name: string
  category: string
  description: string
  progress: number
  progressRequired: number
  currentProgress: number
}

export interface CultureEntry {
  id: string
  name: string
  population: number
  traits: Array<{
    id: string
    name: string
    description: string
    category: string
    strength: number
    establishedTick: number
  }>
  religionName: string
}

export interface ReligionEntry {
  id: string
  name: string
  description: string
  coreValue: string
  tenets: string[]
  followerCount: number
  originSettlementName: string
  culturalInfluence: number
  establishedTick: number
}

export async function fetchCivilizationSummary(): Promise<CivilizationSummary> {
  const { data } = await api.get('/civilization/summary')
  return data
}

export async function fetchKingdoms(): Promise<KingdomSummary[]> {
  const { data } = await api.get('/civilization/kingdoms')
  return data
}

export async function fetchKingdomDetail(id: string) {
  const { data } = await api.get(`/civilization/kingdoms/${id}`)
  return data
}

export async function fetchGovernments(): Promise<GovernmentEntry[]> {
  const { data } = await api.get('/civilization/governments')
  return data
}

export async function fetchLeaders(): Promise<LeaderEntry[]> {
  const { data } = await api.get('/civilization/leaders')
  return data
}

export async function fetchDiplomacy(): Promise<DiplomacyEntry[]> {
  const { data } = await api.get('/civilization/diplomacy')
  return data
}

export async function fetchTradeRoutes(): Promise<TradeRouteEntry[]> {
  const { data } = await api.get('/civilization/trade-routes')
  return data
}

export async function fetchTechnology(): Promise<{ discovered: TechnologyEntry[]; inProgress: TechnologyProgress[] }> {
  const { data } = await api.get('/civilization/technology')
  return data
}

export async function fetchCulture(): Promise<CultureEntry[]> {
  const { data } = await api.get('/civilization/culture')
  return data
}

export async function fetchReligion(): Promise<ReligionEntry[]> {
  const { data } = await api.get('/civilization/religion')
  return data
}

export async function fetchMigration() {
  const { data } = await api.get('/civilization/migration')
  return data
}

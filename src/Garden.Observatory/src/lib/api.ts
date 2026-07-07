import axios from 'axios'

const api = axios.create({
  baseURL: '/',
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

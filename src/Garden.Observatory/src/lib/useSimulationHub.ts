import { useEffect, useState } from 'react'
import * as signalR from '@microsoft/signalr'

const BASE = import.meta.env.DEV ? 'http://localhost:5088' : ''

export function useSimulationHub() {
  const [connection, setConnection] = useState<signalR.HubConnection | null>(null)
  const [tick, setTick] = useState(0)
  const [isPaused, setIsPaused] = useState(false)
  const [speed, setSpeed] = useState(1)
  const [status, setStatus] = useState('Unknown')

  useEffect(() => {
    const conn = new signalR.HubConnectionBuilder()
      .withUrl(`${BASE}/simulationHub`)
      .withAutomaticReconnect()
      .build()

    conn.on('Tick', (data: { tick: number; isPaused: boolean; speed: number }) => {
      setTick(data.tick)
      setIsPaused(data.isPaused)
      setSpeed(data.speed)
    })

    conn.on('StatusChanged', (newStatus: string) => {
      setStatus(newStatus)
    })

    conn.start().then(() => {
      conn.invoke('SubscribeToSimulation')
      setConnection(conn)
    })

    return () => {
      conn.invoke('UnsubscribeFromSimulation')
      conn.stop()
    }
  }, [])

  return { connection, tick, isPaused, speed, status }
}

export function useEnvironmentHub() {
  const [connection, setConnection] = useState<signalR.HubConnection | null>(null)
  const [weather, setWeather] = useState<any>(null)
  const [season, setSeason] = useState<string | null>(null)

  useEffect(() => {
    const conn = new signalR.HubConnectionBuilder()
      .withUrl(`${BASE}/environmentHub`)
      .withAutomaticReconnect()
      .build()

    conn.on('WeatherUpdated', (data: any) => setWeather(data))
    conn.on('SeasonChanged', (s: string) => setSeason(s))

    conn.start().then(() => {
      conn.invoke('SubscribeToEnvironment')
      setConnection(conn)
    })

    return () => {
      conn.invoke('UnsubscribeFromEnvironment')
      conn.stop()
    }
  }, [])

  return { connection, weather, season }
}

export function useCitizenHub() {
  const [connection, setConnection] = useState<signalR.HubConnection | null>(null)
  const [population, setPopulation] = useState<{ total: number; alive: number; births: number; deaths: number } | null>(null)
  const [notification, setNotification] = useState<{ title: string; description: string; category: string; severity: string } | null>(null)

  useEffect(() => {
    const conn = new signalR.HubConnectionBuilder()
      .withUrl(`${BASE}/citizenHub`)
      .withAutomaticReconnect()
      .build()

    conn.on('PopulationChanged', (data: any) => setPopulation(data))
    conn.on('CitizenBorn', (data: { name: string }) => {
      setNotification({ title: `${data.name} was born`, description: `A new citizen has arrived.`, category: 'Birth', severity: 'Info' })
    })
    conn.on('CitizenDied', (data: { name: string; age: number; cause: string }) => {
      setNotification({ title: `${data.name} has passed`, description: `Died at age ${data.age} from ${data.cause}.`, category: 'Death', severity: 'High' })
    })

    conn.start().then(() => {
      conn.invoke('SubscribeToCitizens')
      setConnection(conn)
    })

    return () => {
      conn.invoke('UnsubscribeFromCitizens')
      conn.stop()
    }
  }, [])

  return { connection, population, notification, clearNotification: () => setNotification(null) }
}

export function useSettlementHub() {
  const [connection, setConnection] = useState<signalR.HubConnection | null>(null)
  const [notification, setNotification] = useState<{ title: string; description: string; category: string; severity: string } | null>(null)

  useEffect(() => {
    const conn = new signalR.HubConnectionBuilder()
      .withUrl(`${BASE}/settlementHub`)
      .withAutomaticReconnect()
      .build()

    conn.on('SettlementFounded', (data: { name: string; tileX: number; tileY: number }) => {
      setNotification({ title: `Settlement Founded: ${data.name}`, description: `A new settlement at (${data.tileX}, ${data.tileY}).`, category: 'Settlement', severity: 'High' })
    })
    conn.on('BuildingCompleted', (data: { settlementName: string; buildingType: string }) => {
      setNotification({ title: `Building Completed`, description: `${data.buildingType} completed in ${data.settlementName}.`, category: 'Building', severity: 'Normal' })
    })

    conn.start().then(() => {
      conn.invoke('SubscribeToSettlements')
      setConnection(conn)
    })

    return () => {
      conn.invoke('UnsubscribeFromSettlements')
      conn.stop()
    }
  }, [])

  return { connection, notification, clearNotification: () => setNotification(null) }
}

export function useHistoryHub() {
  const [connection, setConnection] = useState<signalR.HubConnection | null>(null)
  const [notification, setNotification] = useState<{ title: string; description: string; category: string; severity: string } | null>(null)

  useEffect(() => {
    const conn = new signalR.HubConnectionBuilder()
      .withUrl(`${BASE}/historyHub`)
      .withAutomaticReconnect()
      .build()

    conn.on('SignificantEvent', (data: { title: string; description: string; category: string; severity: string }) => {
      setNotification(data)
    })

    conn.start().then(() => {
      conn.invoke('SubscribeToHistory')
      setConnection(conn)
    })

    return () => {
      conn.invoke('UnsubscribeFromHistory')
      conn.stop()
    }
  }, [])

  return { connection, notification, clearNotification: () => setNotification(null) }
}

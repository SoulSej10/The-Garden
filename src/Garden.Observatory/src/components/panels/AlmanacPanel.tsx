import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { Badge } from '@/components/ui/badge'
import { Tabs, TabsList, TabsTrigger, TabsContent } from '@/components/ui/tabs'
import { fetchWeather, fetchClimate, fetchResources, fetchEvents, fetchSimulationStatus, fetchEconomy } from '@/lib/api'

/**
 * Environment + Economy merged into one "world knowledge" reference. The
 * live feel now belongs to WorldStage's own ambient weather layer (see
 * map-polish task) - this panel is where you go to read the numbers behind
 * what you're already seeing on the map, not the primary way to notice it.
 */
export default function AlmanacPanel() {
  const [tab, setTab] = useState('weather')

  const { data: status } = useQuery({ queryKey: ['simulation-status'], queryFn: fetchSimulationStatus, refetchInterval: 5000 })
  const { data: weather } = useQuery({ queryKey: ['weather'], queryFn: fetchWeather, refetchInterval: 3000 })
  const { data: climate } = useQuery({ queryKey: ['climate'], queryFn: fetchClimate, refetchInterval: 10000 })
  const { data: resources } = useQuery({ queryKey: ['resources'], queryFn: fetchResources, refetchInterval: 5000 })
  const { data: events } = useQuery({ queryKey: ['environment-events'], queryFn: () => fetchEvents(20), refetchInterval: 5000 })
  const { data: economy } = useQuery({ queryKey: ['economy'], queryFn: fetchEconomy, refetchInterval: 3000 })

  const weatherBadgeVariant =
    weather?.condition === 'Clear' ? 'default' : weather?.condition === 'Storm' || weather?.condition === 'HeavyRain' ? 'destructive' : 'secondary'

  const avgTemp = climate?.zones?.length
    ? (
        climate.zones.reduce((s, z) => s + z.avgTemperature * z.tileCount, 0) /
        climate.zones.reduce((s, z) => s + z.tileCount, 0)
      ).toFixed(1)
    : null

  return (
    <div className="space-y-5">
      <p className="text-sm text-muted-foreground">{status?.time ?? 'Reading the sky…'}</p>

      <div className="grid grid-cols-3 gap-2">
        <MiniStat label="Season" value={status?.time?.split(',')[1]?.trim().split(' ')[0] ?? '–'} />
        <MiniStat label="Avg temp" value={avgTemp ? `${avgTemp}°` : '–'} />
        <MiniStat label="Events" value={events?.total ?? '–'} />
      </div>

      <Tabs value={tab} onValueChange={setTab}>
        <TabsList>
          <TabsTrigger value="weather">Weather</TabsTrigger>
          <TabsTrigger value="climate">Climate</TabsTrigger>
          <TabsTrigger value="resources">Resources</TabsTrigger>
          <TabsTrigger value="events">Events</TabsTrigger>
          <TabsTrigger value="economy">Economy</TabsTrigger>
        </TabsList>

        <TabsContent value="weather">
          {weather ? (
            <div className="space-y-3">
              <div className="flex items-center gap-2">
                <Badge variant={weatherBadgeVariant}>{weather.condition}</Badge>
                <span className="text-xs text-muted-foreground">×{weather.intensity.toFixed(1)} intensity</span>
              </div>
              <div className="grid grid-cols-2 gap-2 text-sm">
                <InfoRow label="Duration" value={`${weather.remainingDuration}h`} />
                <InfoRow label="Temp modifier" value={`${weather.temperatureModifier > 0 ? '+' : ''}${weather.temperatureModifier}°C`} />
                <InfoRow label="Wind" value={`${(weather.windStrength * 100).toFixed(0)}%`} />
                <InfoRow label="Humidity" value={`${(weather.humidityModifier * 100).toFixed(0)}%`} />
              </div>
            </div>
          ) : (
            <Empty>Not initialized.</Empty>
          )}
        </TabsContent>

        <TabsContent value="climate">
          {climate?.zones?.length ? (
            <div className="space-y-1.5">
              {climate.zones.map((zone) => (
                <div key={zone.zone} className="flex items-center justify-between rounded-xl border border-border/70 px-3 py-2 text-sm">
                  <div>
                    <p className="font-medium">{zone.zone}</p>
                    <p className="text-xs text-muted-foreground">{zone.tileCount} tiles</p>
                  </div>
                  <div className="text-right text-xs">
                    <p className="font-medium text-foreground">{zone.avgTemperature.toFixed(1)}°C</p>
                    <p className="text-muted-foreground">{(zone.avgMoisture * 100).toFixed(0)}% moisture</p>
                  </div>
                </div>
              ))}
            </div>
          ) : (
            <Empty>Not initialized.</Empty>
          )}
        </TabsContent>

        <TabsContent value="resources">
          {resources?.summary ? (
            <div className="space-y-1.5">
              {Object.entries(resources.summary).map(([name, data]) => (
                <div key={name} className="rounded-xl border border-border/70 px-3 py-2 text-sm">
                  <div className="flex items-center justify-between">
                    <p className="font-medium">{name}</p>
                    <p className="text-xs text-muted-foreground">{data.total.toFixed(0)} / {data.totalCapacity.toFixed(0)}</p>
                  </div>
                  <div className="mt-1.5 h-1.5 w-full overflow-hidden rounded-full bg-muted">
                    <div className="h-full rounded-full bg-status-thriving" style={{ width: `${(data.total / data.totalCapacity) * 100}%` }} />
                  </div>
                  <p className="mt-1 text-[10px] text-muted-foreground">{data.deposits} deposits</p>
                </div>
              ))}
            </div>
          ) : (
            <Empty>Not initialized.</Empty>
          )}
        </TabsContent>

        <TabsContent value="events">
          {events?.events?.length ? (
            <div className="space-y-1">
              {events.events.map((evt, i) => (
                <div key={`${evt.tick}-${i}`} className="flex items-center gap-2 rounded-lg px-2 py-1.5 text-sm hover:bg-accent/40">
                  <span className="w-14 shrink-0 font-mono text-[10px] text-muted-foreground">T{evt.tick}</span>
                  <Badge variant="outline" className="text-[10px]">{evt.eventType}</Badge>
                  <span className="ml-auto text-[10px] text-muted-foreground">{evt.severity}</span>
                </div>
              ))}
            </div>
          ) : (
            <Empty>No events recorded.</Empty>
          )}
        </TabsContent>

        <TabsContent value="economy" className="space-y-3">
          {!economy ? (
            <Empty>Reading the ledgers…</Empty>
          ) : (
            <>
              <p className="text-xs text-muted-foreground">Tick {economy.tick} · {economy.totalSettlements} settlements</p>
              <div className="grid grid-cols-2 gap-2">
                <MiniStat label="Goods crafted" value={economy.globalGoodsCrafted} />
                <MiniStat label="Trades" value={economy.globalTradeCount} />
              </div>
              {economy.settlements.length === 0 ? (
                <Empty>No settlements founded yet.</Empty>
              ) : (
                <div className="space-y-2">
                  {economy.settlements.map((settlement) => (
                    <div key={settlement.name} className="panel-carved border border-border/70 bg-card p-3">
                      <div className="flex items-center justify-between">
                        <span className="font-display text-sm font-semibold">{settlement.name}</span>
                        <Badge variant="secondary">{settlement.population} pop</Badge>
                      </div>
                      <div className="mt-2 grid grid-cols-4 gap-1.5 text-center text-xs">
                        <MiniBlock label="Food" value={Math.round(settlement.totalFood)} />
                        <MiniBlock label="Water" value={Math.round(settlement.totalWater)} />
                        <MiniBlock label="Wood" value={Math.round(settlement.totalWood)} />
                        <MiniBlock label="Stone" value={Math.round(settlement.totalStone)} />
                      </div>
                      {Object.entries(settlement.buildingsByType).length > 0 && (
                        <div className="mt-2 flex flex-wrap gap-1">
                          {Object.entries(settlement.buildingsByType).map(([type, count]) => (
                            <Badge key={type} variant="outline" className="text-[10px]">{type}: {count}</Badge>
                          ))}
                        </div>
                      )}
                    </div>
                  ))}
                </div>
              )}
            </>
          )}
        </TabsContent>
      </Tabs>
    </div>
  )
}

function MiniStat({ label, value }: { label: string; value: string | number }) {
  return (
    <div className="rounded-2xl border border-border/70 bg-card px-2 py-2 text-center">
      <p className="font-display text-lg font-semibold">{value}</p>
      <p className="text-[10px] uppercase tracking-wide text-muted-foreground">{label}</p>
    </div>
  )
}

function MiniBlock({ label, value }: { label: string; value: number }) {
  return (
    <div className="rounded-xl bg-muted/60 px-1.5 py-1.5">
      <p className="font-display text-sm font-semibold">{value}</p>
      <p className="text-[9px] text-muted-foreground">{label}</p>
    </div>
  )
}

function InfoRow({ label, value }: { label: string; value: string }) {
  return (
    <div className="rounded-xl border border-border/70 px-3 py-1.5">
      <p className="text-[10px] text-muted-foreground">{label}</p>
      <p className="text-sm font-medium">{value}</p>
    </div>
  )
}

function Empty({ children }: { children: React.ReactNode }) {
  return <p className="py-6 text-center text-sm text-muted-foreground">{children}</p>
}

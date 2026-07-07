import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Skeleton } from '@/components/ui/skeleton'
import {
  fetchWeather,
  fetchClimate,
  fetchResources,
  fetchEvents,
  fetchSimulationStatus,
} from '@/lib/api'

export default function EnvironmentPage() {
  const [activeTab, setActiveTab] = useState('weather')

  const { data: status, isLoading: statusLoading } = useQuery({
    queryKey: ['simulation-status'],
    queryFn: fetchSimulationStatus,
    refetchInterval: 5000,
  })

  const { data: weather, isLoading: weatherLoading } = useQuery({
    queryKey: ['weather'],
    queryFn: fetchWeather,
    refetchInterval: 3000,
  })

  const { data: climate, isLoading: climateLoading } = useQuery({
    queryKey: ['climate'],
    queryFn: fetchClimate,
    refetchInterval: 10000,
  })

  const { data: resources, isLoading: resourcesLoading } = useQuery({
    queryKey: ['resources'],
    queryFn: fetchResources,
    refetchInterval: 5000,
  })

  const { data: events, isLoading: eventsLoading } = useQuery({
    queryKey: ['environment-events'],
    queryFn: () => fetchEvents(20),
    refetchInterval: 5000,
  })

  const weatherBadgeVariant = weather?.condition === 'Clear'
    ? 'default' as const
    : weather?.condition === 'Storm' || weather?.condition === 'HeavyRain'
      ? 'destructive' as const
      : 'secondary' as const

  const avgTemp = climate?.zones?.length
    ? (climate.zones.reduce((s, z) => s + z.avgTemperature * z.tileCount, 0) /
        climate.zones.reduce((s, z) => s + z.tileCount, 0)).toFixed(1)
    : null

  return (
    <div className="mx-auto max-w-[1600px] space-y-6">
      <div>
        <h1 className="text-2xl font-semibold">Environment</h1>
        <p className="text-sm text-muted-foreground">
          World overview &mdash; {status?.time ?? '—'}
        </p>
      </div>

      <div className="grid grid-cols-1 gap-4 md:grid-cols-2 lg:grid-cols-4">
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium text-muted-foreground">
              Season
            </CardTitle>
          </CardHeader>
          <CardContent>
            {statusLoading ? (
              <Skeleton className="h-7 w-24" />
            ) : (
              <p className="text-2xl font-semibold">
                {status?.time?.split(',')[1]?.trim().split(' ')[0] ?? '—'}
              </p>
            )}
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium text-muted-foreground">
              Weather
            </CardTitle>
          </CardHeader>
          <CardContent>
            {weatherLoading ? (
              <Skeleton className="h-7 w-20" />
            ) : weather ? (
              <div className="flex items-center gap-2">
                <Badge variant={weatherBadgeVariant}>
                  {weather.condition}
                </Badge>
                <span className="text-xs text-muted-foreground">
                  x{weather.intensity.toFixed(1)}
                </span>
              </div>
            ) : (
              <p className="text-2xl font-semibold">—</p>
            )}
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium text-muted-foreground">
              Avg Temperature
            </CardTitle>
          </CardHeader>
          <CardContent>
            {climateLoading ? (
              <Skeleton className="h-7 w-20" />
            ) : avgTemp ? (
              <p className="text-2xl font-semibold">{avgTemp}&deg;C</p>
            ) : (
              <p className="text-2xl font-semibold">—</p>
            )}
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium text-muted-foreground">
              Active Events
            </CardTitle>
          </CardHeader>
          <CardContent>
            {eventsLoading ? (
              <Skeleton className="h-7 w-12" />
            ) : (
              <p className="text-2xl font-semibold">
                {events?.total ?? '—'}
              </p>
            )}
          </CardContent>
        </Card>
      </div>

      <TabsView activeTab={activeTab} onTabChange={setActiveTab}>
        <div data-tab="weather">
          <Card>
            <CardHeader>
              <CardTitle>Weather Details</CardTitle>
            </CardHeader>
            <CardContent>
              {weatherLoading ? (
                <Skeleton className="h-32 w-full" />
              ) : weather ? (
                <div className="grid grid-cols-2 gap-4 md:grid-cols-3">
                  <div>
                    <p className="text-xs text-muted-foreground">Condition</p>
                    <p className="font-medium">{weather.condition}</p>
                  </div>
                  <div>
                    <p className="text-xs text-muted-foreground">Intensity</p>
                    <p className="font-medium">{weather.intensity.toFixed(2)}</p>
                  </div>
                  <div>
                    <p className="text-xs text-muted-foreground">Duration Remaining</p>
                    <p className="font-medium">{weather.remainingDuration}h</p>
                  </div>
                  <div>
                    <p className="text-xs text-muted-foreground">Temp Modifier</p>
                    <p className="font-medium">{weather.temperatureModifier > 0 ? '+' : ''}{weather.temperatureModifier}&deg;C</p>
                  </div>
                  <div>
                    <p className="text-xs text-muted-foreground">Wind</p>
                    <p className="font-medium">{(weather.windStrength * 100).toFixed(0)}%</p>
                  </div>
                  <div>
                    <p className="text-xs text-muted-foreground">Humidity</p>
                    <p className="font-medium">{(weather.humidityModifier * 100).toFixed(0)}%</p>
                  </div>
                </div>
              ) : (
                <p className="text-sm text-muted-foreground">Not initialized.</p>
              )}
            </CardContent>
          </Card>
        </div>

        <div data-tab="climate">
          <Card>
            <CardHeader>
              <CardTitle>Climate Zones</CardTitle>
            </CardHeader>
            <CardContent>
              {climateLoading ? (
                <Skeleton className="h-40 w-full" />
              ) : climate?.zones?.length ? (
                <div className="space-y-3">
                  {climate.zones.map((zone) => (
                    <div key={zone.zone} className="flex items-center justify-between rounded-lg border p-3">
                      <div>
                        <p className="font-medium">{zone.zone}</p>
                        <p className="text-xs text-muted-foreground">
                          {zone.tileCount} tiles
                        </p>
                      </div>
                      <div className="text-right text-sm">
                        <p>{zone.avgTemperature.toFixed(1)}&deg;C</p>
                        <p className="text-muted-foreground">Moisture: {(zone.avgMoisture * 100).toFixed(0)}%</p>
                      </div>
                    </div>
                  ))}
                </div>
              ) : (
                <p className="text-sm text-muted-foreground">Not initialized.</p>
              )}
            </CardContent>
          </Card>
        </div>

        <div data-tab="resources">
          <Card>
            <CardHeader>
              <CardTitle>Resource Summary</CardTitle>
            </CardHeader>
            <CardContent>
              {resourcesLoading ? (
                <Skeleton className="h-40 w-full" />
              ) : resources?.summary ? (
                <div className="space-y-3">
                  {Object.entries(resources.summary).map(([name, data]) => (
                    <div key={name} className="flex items-center justify-between rounded-lg border p-3">
                      <div>
                        <p className="font-medium">{name}</p>
                        <p className="text-xs text-muted-foreground">
                          {data.deposits} deposits
                        </p>
                      </div>
                      <div className="text-right text-sm">
                        <p>
                          {data.total.toFixed(0)} / {data.totalCapacity.toFixed(0)}
                        </p>
                        <div className="mt-1 h-1.5 w-24 rounded-full bg-muted">
                          <div
                            className="h-full rounded-full bg-primary"
                            style={{
                              width: `${(data.total / data.totalCapacity) * 100}%`,
                            }}
                          />
                        </div>
                      </div>
                    </div>
                  ))}
                </div>
              ) : (
                <p className="text-sm text-muted-foreground">Not initialized.</p>
              )}
            </CardContent>
          </Card>
        </div>

        <div data-tab="events">
          <Card>
            <CardHeader>
              <CardTitle>Environmental Events</CardTitle>
            </CardHeader>
            <CardContent>
              {eventsLoading ? (
                <Skeleton className="h-40 w-full" />
              ) : events?.events?.length ? (
                <div className="space-y-1">
                  {events.events.map((evt, i) => (
                    <div key={`${evt.tick}-${i}`} className="flex items-center gap-3 rounded-md px-2 py-1.5 text-sm hover:bg-muted/50">
                      <span className="w-16 shrink-0 font-mono text-xs text-muted-foreground">
                        T{evt.tick}
                      </span>
                      <Badge variant="outline" className="text-xs">
                        {evt.eventType}
                      </Badge>
                      <span className="ml-auto text-xs text-muted-foreground">
                        {evt.severity}
                      </span>
                    </div>
                  ))}
                </div>
              ) : (
                <p className="text-sm text-muted-foreground">No events recorded.</p>
              )}
            </CardContent>
          </Card>
        </div>
      </TabsView>
    </div>
  )
}

function TabsView({
  activeTab,
  onTabChange,
  children,
}: {
  activeTab: string
  onTabChange: (tab: string) => void
  children: React.ReactElement<{ 'data-tab': string }> | React.ReactElement<{ 'data-tab': string }>[]
}) {
  const tabs = Array.isArray(children) ? children : [children]
  const labels: Record<string, string> = {
    weather: 'Weather',
    climate: 'Climate',
    resources: 'Resources',
    events: 'Events',
  }

  return (
    <div className="flex flex-col gap-4">
      <div className="inline-flex h-9 items-center justify-center rounded-lg bg-muted p-1 text-muted-foreground self-start">
        {tabs.map((tab) => {
          const tabValue = tab.props['data-tab']
          return (
            <button
              key={tabValue}
              data-state={activeTab === tabValue ? 'active' : 'inactive'}
              className="inline-flex items-center justify-center whitespace-nowrap rounded-md px-3 py-1 text-sm font-medium ring-offset-background transition-all focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:pointer-events-none disabled:opacity-50 data-[state=active]:bg-background data-[state=active]:text-foreground data-[state=active]:shadow"
              onClick={() => onTabChange(tabValue)}
            >
              {labels[tabValue] ?? tabValue}
            </button>
          )
        })}
      </div>
      {tabs
        .filter((tab) => tab.props['data-tab'] === activeTab)
        .map((tab) => (
          <div key={tab.props['data-tab']} className="mt-2 ring-offset-background">
            {tab}
          </div>
        ))}
    </div>
  )
}

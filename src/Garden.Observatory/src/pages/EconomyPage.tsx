import { useQuery } from '@tanstack/react-query'
import {
  Card,
  CardContent,
  CardHeader,
  CardTitle,
} from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { fetchEconomy } from '@/lib/api'

export default function EconomyPage() {
  const { data: economy, isLoading } = useQuery({
    queryKey: ['economy'],
    queryFn: fetchEconomy,
    refetchInterval: 3000,
  })

  if (isLoading || !economy) {
    return (
      <div className="mx-auto max-w-[1600px] space-y-6">
        <h1 className="text-2xl font-semibold">Economy</h1>
        <p className="text-sm text-muted-foreground">Loading...</p>
      </div>
    )
  }

  const totalResources = economy.settlements.reduce(
    (a, s) => ({
      food: a.food + s.totalFood,
      wood: a.wood + s.totalWood,
      stone: a.stone + s.totalStone,
      water: a.water + s.totalWater,
    }),
    { food: 0, wood: 0, stone: 0, water: 0 }
  )

  return (
    <div className="mx-auto max-w-[1600px] space-y-6">
      <div>
        <h1 className="text-2xl font-semibold">Economy</h1>
        <p className="text-sm text-muted-foreground">
          Tick {economy.tick} &middot; {economy.totalSettlements} settlements
        </p>
      </div>

      <div className="grid grid-cols-1 gap-4 md:grid-cols-2 lg:grid-cols-4">
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium text-muted-foreground">Total Food</CardTitle>
          </CardHeader>
          <CardContent>
            <p className="text-2xl font-semibold">{Math.round(totalResources.food)}</p>
          </CardContent>
        </Card>
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium text-muted-foreground">Total Wood</CardTitle>
          </CardHeader>
          <CardContent>
            <p className="text-2xl font-semibold">{Math.round(totalResources.wood)}</p>
          </CardContent>
        </Card>
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium text-muted-foreground">Goods Crafted</CardTitle>
          </CardHeader>
          <CardContent>
            <p className="text-2xl font-semibold">{economy.globalGoodsCrafted}</p>
          </CardContent>
        </Card>
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium text-muted-foreground">Trades Completed</CardTitle>
          </CardHeader>
          <CardContent>
            <p className="text-2xl font-semibold">{economy.globalTradeCount}</p>
          </CardContent>
        </Card>
      </div>

      {economy.settlements.length === 0 ? (
        <Card>
          <CardHeader>
            <CardTitle>Settlement Economy</CardTitle>
          </CardHeader>
          <CardContent>
            <p className="text-sm text-muted-foreground">
              No settlements founded yet. Citizens will establish settlements as they explore the world.
            </p>
          </CardContent>
        </Card>
      ) : (
        economy.settlements.map((settlement) => (
          <Card key={settlement.name}>
            <CardHeader>
              <CardTitle className="flex items-center justify-between">
                <span>{settlement.name}</span>
                <Badge variant="secondary">{settlement.population} pop</Badge>
              </CardTitle>
            </CardHeader>
            <CardContent>
              <div className="grid grid-cols-2 gap-4 md:grid-cols-4">
                <div>
                  <p className="text-xs text-muted-foreground">Food</p>
                  <p className="text-lg font-semibold">{Math.round(settlement.totalFood)}</p>
                </div>
                <div>
                  <p className="text-xs text-muted-foreground">Water</p>
                  <p className="text-lg font-semibold">{Math.round(settlement.totalWater)}</p>
                </div>
                <div>
                  <p className="text-xs text-muted-foreground">Wood</p>
                  <p className="text-lg font-semibold">{Math.round(settlement.totalWood)}</p>
                </div>
                <div>
                  <p className="text-xs text-muted-foreground">Stone</p>
                  <p className="text-lg font-semibold">{Math.round(settlement.totalStone)}</p>
                </div>
              </div>
              <div className="mt-4">
                <p className="text-xs text-muted-foreground mb-2">Buildings</p>
                <div className="flex flex-wrap gap-2">
                  {Object.entries(settlement.buildingsByType).length === 0 ? (
                    <span className="text-xs text-muted-foreground">None</span>
                  ) : (
                    Object.entries(settlement.buildingsByType).map(([type, count]) => (
                      <Badge key={type} variant="outline">
                        {type}: {count}
                      </Badge>
                    ))
                  )}
                </div>
              </div>
            </CardContent>
          </Card>
        ))
      )}
    </div>
  )
}

import { useQuery } from '@tanstack/react-query'
import { useState } from 'react'
import {
  fetchCivilizationSummary, fetchKingdoms, fetchGovernments, fetchLeaders,
  fetchDiplomacy, fetchTradeRoutes, fetchTechnology, fetchCulture,
  fetchReligion, fetchMigration,
  type KingdomSummary, type GovernmentEntry, type LeaderEntry,
  type DiplomacyEntry, type TradeRouteEntry, type TechnologyEntry,
  type TechnologyProgress, type CultureEntry, type ReligionEntry
} from '../lib/api'

type Tab = 'kingdoms' | 'governments' | 'leaders' | 'diplomacy' | 'trade' | 'technology' | 'culture' | 'religion' | 'migration'

export default function CivilizationPage() {
  const [tab, setTab] = useState<Tab>('kingdoms')

  const { data: summary } = useQuery({
    queryKey: ['civilization-summary'],
    queryFn: fetchCivilizationSummary,
    refetchInterval: 5000
  })

  const { data: kingdoms } = useQuery({
    queryKey: ['kingdoms'],
    queryFn: fetchKingdoms,
    refetchInterval: 5000
  })

  const { data: governments } = useQuery({
    queryKey: ['governments'],
    queryFn: fetchGovernments,
    refetchInterval: 5000
  })

  const { data: leaders } = useQuery({
    queryKey: ['leaders'],
    queryFn: fetchLeaders,
    refetchInterval: 5000
  })

  const { data: diplomacy } = useQuery({
    queryKey: ['diplomacy'],
    queryFn: fetchDiplomacy,
    refetchInterval: 5000
  })

  const { data: tradeRoutes } = useQuery({
    queryKey: ['trade-routes'],
    queryFn: fetchTradeRoutes,
    refetchInterval: 5000
  })

  const { data: technology } = useQuery({
    queryKey: ['technology'],
    queryFn: fetchTechnology,
    refetchInterval: 5000
  })

  const { data: culture } = useQuery({
    queryKey: ['culture'],
    queryFn: fetchCulture,
    refetchInterval: 5000
  })

  const { data: religion } = useQuery({
    queryKey: ['religion'],
    queryFn: fetchReligion,
    refetchInterval: 5000
  })

  const tabs: { key: Tab; label: string }[] = [
    { key: 'kingdoms', label: `Kingdoms (${summary?.kingdomCount ?? '...'})` },
    { key: 'governments', label: 'Governments' },
    { key: 'leaders', label: 'Leaders' },
    { key: 'diplomacy', label: 'Diplomacy' },
    { key: 'trade', label: 'Trade Routes' },
    { key: 'technology', label: `Tech (${summary?.technologiesDiscovered ?? '...'})` },
    { key: 'culture', label: 'Culture' },
    { key: 'religion', label: 'Religion' },
    { key: 'migration', label: 'Migration' },
  ]

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold tracking-tight">Civilization</h1>
        <p className="text-sm text-muted-foreground">
          Advanced societal systems emerging from citizen interactions
        </p>
      </div>

      {summary && (
        <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-6 gap-3">
          <div className="rounded-lg border p-3">
            <div className="text-xs text-muted-foreground">Kingdoms</div>
            <div className="text-2xl font-bold">{summary.kingdomCount}</div>
          </div>
          {Object.entries(summary.governmentCounts).map(([type, count]) => (
            <div key={type} className="rounded-lg border p-3">
              <div className="text-xs text-muted-foreground">{type}</div>
              <div className="text-2xl font-bold">{count}</div>
            </div>
          ))}
          <div className="rounded-lg border p-3">
            <div className="text-xs text-muted-foreground">Trade Routes</div>
            <div className="text-2xl font-bold">{summary.tradeRouteCount}</div>
          </div>
          <div className="rounded-lg border p-3">
            <div className="text-xs text-muted-foreground">Technologies</div>
            <div className="text-2xl font-bold">{summary.technologiesDiscovered}</div>
          </div>
          <div className="rounded-lg border p-3">
            <div className="text-xs text-muted-foreground">Religions</div>
            <div className="text-2xl font-bold">{summary.religionCount}</div>
          </div>
        </div>
      )}

      <div className="flex flex-wrap gap-1 border-b pb-1">
        {tabs.map(t => (
          <button
            key={t.key}
            onClick={() => setTab(t.key)}
            className={`px-3 py-1.5 text-sm rounded-t-md transition-colors ${
              tab === t.key
                ? 'bg-accent text-accent-foreground font-medium border-b-2 border-primary'
                : 'text-muted-foreground hover:text-foreground hover:bg-accent/50'
            }`}
          >
            {t.label}
          </button>
        ))}
      </div>

      <div className="space-y-6">
        {tab === 'kingdoms' && <KingdomsTab kingdoms={kingdoms ?? []} />}
        {tab === 'governments' && <GovernmentsTab data={governments ?? []} />}
        {tab === 'leaders' && <LeadersTab data={leaders ?? []} />}
        {tab === 'diplomacy' && <DiplomacyTab data={diplomacy ?? []} />}
        {tab === 'trade' && <TradeRoutesTab data={tradeRoutes ?? []} />}
        {tab === 'technology' && <TechnologyTab data={technology} />}
        {tab === 'culture' && <CultureTab data={culture ?? []} />}
        {tab === 'religion' && <ReligionTab data={religion ?? []} />}
        {tab === 'migration' && <MigrationTab />}
      </div>
    </div>
  )
}

function KingdomsTab({ kingdoms }: { kingdoms: KingdomSummary[] }) {
  if (kingdoms.length === 0) return <p className="text-sm text-muted-foreground">No kingdoms have formed yet. Kingdoms emerge when settlements develop stable leadership and form alliances.</p>

  return (
    <div className="space-y-4">
      {kingdoms.map(k => {
        const stabilityColor = k.stability >= 70 ? 'text-green-500' : k.stability >= 40 ? 'text-yellow-500' : 'text-red-500'
        return (
          <div key={k.id} className="rounded-lg border p-4">
            <div className="flex items-start justify-between">
              <div>
                <h3 className="font-semibold text-lg">{k.name}</h3>
                <p className="text-sm text-muted-foreground">
                  Capital: {k.capitalName} | Ruler: {k.leaderName} ({k.leaderAge} yrs)
                </p>
              </div>
              <div className="text-right">
                <div className="text-sm font-medium">{k.governmentType}</div>
                <div className={`text-sm ${stabilityColor}`}>Stability: {k.stability.toFixed(0)}%</div>
              </div>
            </div>
            <div className="mt-3 grid grid-cols-4 gap-2 text-center">
              <div className="rounded bg-muted/30 p-2">
                <div className="text-lg font-bold">{k.population}</div>
                <div className="text-xs text-muted-foreground">Population</div>
              </div>
              <div className="rounded bg-muted/30 p-2">
                <div className="text-lg font-bold">{k.settlementCount}</div>
                <div className="text-xs text-muted-foreground">Settlements</div>
              </div>
              <div className="rounded bg-muted/30 p-2">
                <div className="text-lg font-bold">{k.territoryRadius}</div>
                <div className="text-xs text-muted-foreground">Territory</div>
              </div>
              <div className="rounded bg-muted/30 p-2">
                <div className="text-lg font-bold">#{k.foundedTick}</div>
                <div className="text-xs text-muted-foreground">Founded Tick</div>
              </div>
            </div>
          </div>
        )
      })}
    </div>
  )
}

function GovernmentsTab({ data }: { data: GovernmentEntry[] }) {
  if (data.length === 0) return <p className="text-sm text-muted-foreground">No formal governments yet. Governments form as settlements grow and develop leadership.</p>

  return (
    <div className="overflow-x-auto rounded-lg border">
      <table className="w-full text-sm">
        <thead className="bg-muted/50">
          <tr>
            <th className="text-left p-3 font-medium">Settlement</th>
            <th className="text-left p-3 font-medium">Government</th>
            <th className="text-left p-3 font-medium">Leader</th>
            <th className="text-right p-3 font-medium">Population</th>
            <th className="text-right p-3 font-medium">Buildings</th>
          </tr>
        </thead>
        <tbody>
          {data.map(g => (
            <tr key={g.id} className="border-t hover:bg-muted/20">
              <td className="p-3 font-medium">{g.name}</td>
              <td className="p-3">
                <span className="rounded bg-muted/30 px-2 py-0.5 text-xs">{g.governmentType}</span>
              </td>
              <td className="p-3">{g.leaderName}</td>
              <td className="p-3 text-right">{g.population}</td>
              <td className="p-3 text-right">{g.buildingCount}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}

function LeadersTab({ data }: { data: LeaderEntry[] }) {
  if (data.length === 0) return <p className="text-sm text-muted-foreground">No leaders have emerged yet. Leaders earn recognition through contributions and experience.</p>

  return (
    <div className="overflow-x-auto rounded-lg border">
      <table className="w-full text-sm">
        <thead className="bg-muted/50">
          <tr>
            <th className="text-left p-3 font-medium">Name</th>
            <th className="text-left p-3 font-medium">Settlement</th>
            <th className="text-left p-3 font-medium">Government</th>
            <th className="text-right p-3 font-medium">Age</th>
            <th className="text-right p-3 font-medium">Contributions</th>
            <th className="text-right p-3 font-medium">Reputation</th>
            <th className="text-right p-3 font-medium">Intelligence</th>
          </tr>
        </thead>
        <tbody>
          {data.map(l => (
            <tr key={l.leaderId} className="border-t hover:bg-muted/20">
              <td className="p-3 font-medium">{l.leaderName}</td>
              <td className="p-3">{l.settlementName}</td>
              <td className="p-3">
                <span className="rounded bg-muted/30 px-2 py-0.5 text-xs">{l.governmentType}</span>
              </td>
              <td className="p-3 text-right">{l.age}</td>
              <td className="p-3 text-right">{l.contributionScore.toFixed(0)}</td>
              <td className="p-3 text-right">{l.reputation.toFixed(0)}</td>
              <td className="p-3 text-right">{l.intelligence.toFixed(1)}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}

function DiplomacyTab({ data }: { data: DiplomacyEntry[] }) {
  if (data.length === 0) return <p className="text-sm text-muted-foreground">No diplomatic relations yet. Relations form as settlements interact with each other.</p>

  const relationColor = (r: string) => {
    switch (r) {
      case 'Allied': return 'text-green-500'
      case 'Friendly': return 'text-blue-500'
      case 'Neutral': return 'text-muted-foreground'
      case 'Suspicious': return 'text-yellow-500'
      case 'Hostile': return 'text-red-500'
      default: return ''
    }
  }

  return (
    <div className="overflow-x-auto rounded-lg border">
      <table className="w-full text-sm">
        <thead className="bg-muted/50">
          <tr>
            <th className="text-left p-3 font-medium">Entity A</th>
            <th className="text-left p-3 font-medium">Relation</th>
            <th className="text-left p-3 font-medium">Entity B</th>
            <th className="text-right p-3 font-medium">Score</th>
            <th className="text-center p-3 font-medium">Trade</th>
            <th className="text-center p-3 font-medium">Alliance</th>
          </tr>
        </thead>
        <tbody>
          {data.map(r => (
            <tr key={r.id} className="border-t hover:bg-muted/20">
              <td className="p-3 font-medium">{r.entityA}</td>
              <td className={`p-3 font-medium ${relationColor(r.relation)}`}>{r.relation}</td>
              <td className="p-3 font-medium">{r.entityB}</td>
              <td className="p-3 text-right">{r.relationScore.toFixed(0)}</td>
              <td className="p-3 text-center">{r.hasTradeAgreement ? '✓' : '—'}</td>
              <td className="p-3 text-center">{r.isAlliance ? '✓' : '—'}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}

function TradeRoutesTab({ data }: { data: TradeRouteEntry[] }) {
  if (data.length === 0) return <p className="text-sm text-muted-foreground">No trade routes yet. Trade emerges when settlements produce surpluses and demand exists elsewhere.</p>

  return (
    <div className="space-y-3">
      {data.map(r => (
        <div key={r.id} className="rounded-lg border p-4">
          <div className="flex items-start justify-between">
            <div>
              <div className="font-medium">
                {r.fromSettlementName} ↔ {r.toSettlementName}
              </div>
              <div className="text-sm text-muted-foreground">
                Primary good: {r.primaryGood} | Distance: {r.distance.toFixed(0)} tiles
              </div>
            </div>
            <div className="text-right text-sm">
              <div className="font-medium">{r.tripCount} trips</div>
              <div className="text-muted-foreground">{r.totalVolumeTransported.toFixed(0)} units</div>
            </div>
          </div>
          <div className="mt-2 flex gap-4 text-xs text-muted-foreground">
            <span>Economic value: {r.economicValue.toFixed(1)}</span>
            <span>Established: tick {r.establishedTick}</span>
            <span>Last trip: tick {r.lastTripTick}</span>
          </div>
        </div>
      ))}
    </div>
  )
}

function TechnologyTab({ data }: { data: { discovered: TechnologyEntry[]; inProgress: TechnologyProgress[] } | undefined }) {
  const categories = [...new Set(data?.discovered.map(t => t.category) ?? [])]

  return (
    <div className="space-y-6">
      <div>
        <h3 className="font-semibold mb-2">Discovered Technologies ({data?.discovered.length ?? 0})</h3>
        {categories.length === 0 ? (
          <p className="text-sm text-muted-foreground">No technologies discovered yet. Technology advances as settlements accumulate experience.</p>
        ) : categories.map(cat => (
          <div key={cat} className="mb-4">
            <h4 className="text-sm font-medium text-muted-foreground mb-2">{cat}</h4>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-2">
              {data?.discovered.filter(t => t.category === cat).map(t => (
                <div key={t.id} className="rounded border p-3">
                  <div className="font-medium">{t.name}</div>
                  <div className="text-xs text-muted-foreground">{t.description}</div>
                  <div className="text-xs text-muted-foreground mt-1">
                    Discovered by {t.settlementName ?? 'unknown'}{t.citizenName ? ` (${t.citizenName})` : ''}
                  </div>
                </div>
              ))}
            </div>
          </div>
        ))}
      </div>

      <div>
        <h3 className="font-semibold mb-2">In Progress</h3>
        {(data?.inProgress.length ?? 0) === 0 ? (
          <p className="text-sm text-muted-foreground">All technologies discovered.</p>
        ) : (
          <div className="grid grid-cols-1 md:grid-cols-2 gap-2">
            {data?.inProgress.map(t => (
              <div key={t.id} className="rounded border p-3">
                <div className="flex items-center justify-between">
                  <div>
                    <div className="font-medium text-sm">{t.name}</div>
                    <div className="text-xs text-muted-foreground">{t.category}</div>
                  </div>
                  <div className="text-sm font-medium">{t.progress.toFixed(0)}%</div>
                </div>
                <div className="mt-2 h-2 w-full rounded-full bg-muted">
                  <div className="h-2 rounded-full bg-primary transition-all" style={{ width: `${t.progress}%` }} />
                </div>
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  )
}

function CultureTab({ data }: { data: CultureEntry[] }) {
  if (data.length === 0) return <p className="text-sm text-muted-foreground">No cultural identities have developed yet. Culture emerges as settlements gain traditions and shared experiences.</p>

  return (
    <div className="space-y-4">
      {data.map(s => (
        <div key={s.id} className="rounded-lg border p-4">
          <div className="flex items-start justify-between">
            <div>
              <h3 className="font-semibold">{s.name}</h3>
              <p className="text-sm text-muted-foreground">Population: {s.population}{s.religionName ? ` | Religion: ${s.religionName}` : ''}</p>
            </div>
          </div>
          <div className="mt-3 flex flex-wrap gap-2">
            {s.traits.map(t => (
              <div key={t.id} className="rounded bg-muted/30 px-3 py-1.5 text-sm">
                <div className="font-medium">{t.name}</div>
                <div className="text-xs text-muted-foreground">{t.category}</div>
              </div>
            ))}
          </div>
        </div>
      ))}
    </div>
  )
}

function ReligionTab({ data }: { data: ReligionEntry[] }) {
  if (data.length === 0) return <p className="text-sm text-muted-foreground">No religions have formed yet. Belief systems emerge from shared values and community traditions.</p>

  return (
    <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
      {data.map(r => (
        <div key={r.id} className="rounded-lg border p-4">
          <div className="font-semibold text-lg">{r.name}</div>
          <div className="text-sm text-muted-foreground mb-2">{r.description}</div>
          <div className="text-sm">
            <span className="font-medium">Core Value:</span> {r.coreValue}
          </div>
          <div className="mt-2 flex flex-wrap gap-1">
            {r.tenets.map((tenet, i) => (
              <span key={i} className="rounded bg-muted/30 px-2 py-0.5 text-xs">{tenet}</span>
            ))}
          </div>
          <div className="mt-3 grid grid-cols-3 gap-2 text-center text-sm">
            <div>
              <div className="font-bold">{r.followerCount}</div>
              <div className="text-xs text-muted-foreground">Followers</div>
            </div>
            <div>
              <div className="font-bold">{(r.culturalInfluence * 100).toFixed(0)}%</div>
              <div className="text-xs text-muted-foreground">Influence</div>
            </div>
            <div>
              <div className="font-bold">{r.originSettlementName}</div>
              <div className="text-xs text-muted-foreground">Origin</div>
            </div>
          </div>
        </div>
      ))}
    </div>
  )
}

function MigrationTab() {
  const { data, isLoading } = useQuery({
    queryKey: ['migration'],
    queryFn: fetchMigration,
    refetchInterval: 5000
  })

  if (isLoading) return <p className="text-sm text-muted-foreground">Loading migration data...</p>

  const migrants = data?.currentMigrants ?? []

  return (
    <div>
      <p className="text-sm text-muted-foreground mb-4">
        Citizens migrate between settlements in search of better conditions.
      </p>
      {migrants.length === 0 ? (
        <p className="text-sm text-muted-foreground">No active migrations currently.</p>
      ) : (
        <div className="overflow-x-auto rounded-lg border">
          <table className="w-full text-sm">
            <thead className="bg-muted/50">
              <tr>
                <th className="text-left p-3 font-medium">Name</th>
                <th className="text-left p-3 font-medium">Activity</th>
                <th className="text-right p-3 font-medium">Location</th>
              </tr>
            </thead>
            <tbody>
              {migrants.map((m: Record<string, unknown>) => (
                <tr key={m.id as string} className="border-t hover:bg-muted/20">
                  <td className="p-3 font-medium">{m.name as string}</td>
                  <td className="p-3">{m.currentActivity as string}</td>
                  <td className="p-3 text-right">({m.tileX as number}, {m.tileY as number})</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  )
}

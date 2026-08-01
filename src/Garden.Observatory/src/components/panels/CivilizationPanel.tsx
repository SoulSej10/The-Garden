import { useQuery } from '@tanstack/react-query'
import { useState } from 'react'
import { Badge } from '@/components/ui/badge'
import { Progress } from '@/components/ui/progress'
import { Tabs, TabsList, TabsTrigger, TabsContent } from '@/components/ui/tabs'
import {
  fetchCivilizationSummary, fetchKingdoms, fetchGovernments, fetchLeaders,
  fetchDiplomacy, fetchTradeRoutes, fetchTechnology, fetchCulture,
  fetchReligion, fetchMigration, fetchLegends,
  type KingdomSummary, type GovernmentEntry, type LeaderEntry,
  type DiplomacyEntry, type TradeRouteEntry, type TechnologyEntry,
  type TechnologyProgress, type CultureEntry, type ReligionEntry,
  type LegendEntry
} from '@/lib/api'

type Tab = 'kingdoms' | 'governments' | 'leaders' | 'diplomacy' | 'trade' | 'technology' | 'culture' | 'religion' | 'migration' | 'legends'

/**
 * The "reference wiki" panel - ten sub-domains of emergent society. Every
 * raw <table> from the old page becomes a stacked card row here, since a
 * ~500px floating panel can't fit 6-7 table columns; the underlying data
 * and derived logic (relationColor, technology category grouping, the
 * lazily-fetched migration tab) carry over unchanged.
 */
export default function CivilizationPanel() {
  const [tab, setTab] = useState<Tab>('kingdoms')

  const { data: summary } = useQuery({ queryKey: ['civilization-summary'], queryFn: fetchCivilizationSummary, refetchInterval: 5000 })
  const { data: kingdoms } = useQuery({ queryKey: ['kingdoms'], queryFn: fetchKingdoms, refetchInterval: 5000 })
  const { data: governments } = useQuery({ queryKey: ['governments'], queryFn: fetchGovernments, refetchInterval: 5000 })
  const { data: leaders } = useQuery({ queryKey: ['leaders'], queryFn: fetchLeaders, refetchInterval: 5000 })
  const { data: diplomacy } = useQuery({ queryKey: ['diplomacy'], queryFn: fetchDiplomacy, refetchInterval: 5000 })
  const { data: tradeRoutes } = useQuery({ queryKey: ['trade-routes'], queryFn: fetchTradeRoutes, refetchInterval: 5000 })
  const { data: technology } = useQuery({ queryKey: ['technology'], queryFn: fetchTechnology, refetchInterval: 5000 })
  const { data: culture } = useQuery({ queryKey: ['culture'], queryFn: fetchCulture, refetchInterval: 5000 })
  const { data: religion } = useQuery({ queryKey: ['religion'], queryFn: fetchReligion, refetchInterval: 5000 })
  const { data: legends } = useQuery({ queryKey: ['legends'], queryFn: fetchLegends, refetchInterval: 5000 })

  return (
    <div className="space-y-5">
      <div className="grid grid-cols-3 gap-2">
        <MiniStat label="Kingdoms" value={summary?.kingdomCount} />
        <MiniStat label="Trade routes" value={summary?.tradeRouteCount} />
        <MiniStat label="Technologies" value={summary?.technologiesDiscovered} />
      </div>

      <Tabs value={tab} onValueChange={(v) => setTab(v as Tab)}>
        <TabsList>
          <TabsTrigger value="kingdoms">Kingdoms</TabsTrigger>
          <TabsTrigger value="governments">Govts</TabsTrigger>
          <TabsTrigger value="leaders">Leaders</TabsTrigger>
          <TabsTrigger value="diplomacy">Diplomacy</TabsTrigger>
          <TabsTrigger value="trade">Trade</TabsTrigger>
          <TabsTrigger value="technology">Tech</TabsTrigger>
          <TabsTrigger value="culture">Culture</TabsTrigger>
          <TabsTrigger value="religion">Religion</TabsTrigger>
          <TabsTrigger value="migration">Migration</TabsTrigger>
          <TabsTrigger value="legends">Legends</TabsTrigger>
        </TabsList>

        <TabsContent value="kingdoms"><KingdomsTab kingdoms={kingdoms ?? []} /></TabsContent>
        <TabsContent value="governments"><GovernmentsTab data={governments ?? []} /></TabsContent>
        <TabsContent value="leaders"><LeadersTab data={leaders ?? []} /></TabsContent>
        <TabsContent value="diplomacy"><DiplomacyTab data={diplomacy ?? []} /></TabsContent>
        <TabsContent value="trade"><TradeRoutesTab data={tradeRoutes ?? []} /></TabsContent>
        <TabsContent value="technology"><TechnologyTab data={technology} /></TabsContent>
        <TabsContent value="culture"><CultureTab data={culture ?? []} /></TabsContent>
        <TabsContent value="religion"><ReligionTab data={religion ?? []} /></TabsContent>
        <TabsContent value="migration"><MigrationTab active={tab === 'migration'} /></TabsContent>
        <TabsContent value="legends"><LegendsTab data={legends ?? []} /></TabsContent>
      </Tabs>
    </div>
  )
}

function MiniStat({ label, value }: { label: string; value?: number }) {
  return (
    <div className="rounded-2xl border border-border/70 bg-card px-2 py-2 text-center">
      <p className="font-display text-lg font-semibold">{value ?? '–'}</p>
      <p className="text-[10px] uppercase tracking-wide text-muted-foreground">{label}</p>
    </div>
  )
}

function Empty({ children }: { children: React.ReactNode }) {
  return <p className="py-6 text-center text-sm text-muted-foreground">{children}</p>
}

function KingdomsTab({ kingdoms }: { kingdoms: KingdomSummary[] }) {
  if (kingdoms.length === 0) return <Empty>No kingdoms have formed yet. Kingdoms emerge when settlements develop stable leadership and form alliances.</Empty>
  return (
    <div className="space-y-3">
      {kingdoms.map((k) => {
        const stabilityColor = k.stability >= 70 ? 'text-status-health' : k.stability >= 40 ? 'text-status-hunger' : 'text-status-danger'
        return (
          <div key={k.id} className="panel-carved border border-border/70 bg-card p-3.5">
            <div className="flex items-start justify-between gap-2">
              <div>
                <h3 className="font-display font-semibold">{k.name}</h3>
                <p className="text-xs text-muted-foreground">Capital {k.capitalName} · Ruler {k.leaderName} ({k.leaderAge}y)</p>
              </div>
              <div className="shrink-0 text-right">
                <p className="text-xs font-medium">{k.governmentType}</p>
                <p className={`text-xs ${stabilityColor}`}>{k.stability.toFixed(0)}% stable</p>
              </div>
            </div>
            <div className="mt-2.5 grid grid-cols-3 gap-1.5 text-center text-xs">
              <MiniBlock label="Pop" value={k.population} />
              <MiniBlock label="Settlements" value={k.settlementCount} />
              <MiniBlock label="Territory" value={k.territoryRadius} />
            </div>
          </div>
        )
      })}
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

function GovernmentsTab({ data }: { data: GovernmentEntry[] }) {
  if (data.length === 0) return <Empty>No formal governments yet. Governments form as settlements grow and develop leadership.</Empty>
  return (
    <div className="space-y-1.5">
      {data.map((g) => (
        <div key={g.id} className="flex items-center justify-between rounded-xl border border-border/70 px-3 py-2 text-sm">
          <div>
            <p className="font-medium">{g.name}</p>
            <p className="text-xs text-muted-foreground">{g.leaderName}</p>
          </div>
          <div className="text-right">
            <Badge variant="outline" className="text-[10px]">{g.governmentType}</Badge>
            <p className="mt-0.5 text-xs text-muted-foreground">{g.population} pop · {g.buildingCount} built</p>
          </div>
        </div>
      ))}
    </div>
  )
}

function LeadersTab({ data }: { data: LeaderEntry[] }) {
  if (data.length === 0) return <Empty>No leaders have emerged yet. Leaders earn recognition through contributions and experience.</Empty>
  return (
    <div className="space-y-1.5">
      {data.map((l) => (
        <div key={l.leaderId} className="rounded-xl border border-border/70 px-3 py-2 text-sm">
          <div className="flex items-center justify-between">
            <p className="font-medium">{l.leaderName}</p>
            <Badge variant="outline" className="text-[10px]">{l.governmentType}</Badge>
          </div>
          <p className="text-xs text-muted-foreground">{l.settlementName} · Age {l.age}</p>
          <div className="mt-1 flex gap-3 text-[11px] text-muted-foreground">
            <span>Contribution {l.contributionScore.toFixed(0)}</span>
            <span>Reputation {l.reputation.toFixed(0)}</span>
            <span>Intelligence {l.intelligence.toFixed(1)}</span>
          </div>
        </div>
      ))}
    </div>
  )
}

function relationColor(r: string) {
  switch (r) {
    case 'Allied': return 'text-status-health'
    case 'Friendly': return 'text-status-water'
    case 'Neutral': return 'text-muted-foreground'
    case 'Suspicious': return 'text-status-hunger'
    case 'Hostile': return 'text-status-danger'
    default: return ''
  }
}

function DiplomacyTab({ data }: { data: DiplomacyEntry[] }) {
  if (data.length === 0) return <Empty>No diplomatic relations yet. Relations form as settlements interact with each other.</Empty>
  return (
    <div className="space-y-1.5">
      {data.map((r) => (
        <div key={r.id} className="rounded-xl border border-border/70 px-3 py-2 text-sm">
          <div className="flex items-center justify-between">
            <span className="font-medium">{r.entityA} ↔ {r.entityB}</span>
            <span className={`text-xs font-medium ${relationColor(r.relation)}`}>{r.relation}</span>
          </div>
          <div className="mt-1 flex items-center gap-3 text-[11px] text-muted-foreground">
            <span>Score {r.relationScore.toFixed(0)}</span>
            {r.hasTradeAgreement && <Badge variant="outline" className="px-1.5 py-0 text-[10px]">Trade</Badge>}
            {r.isAlliance && <Badge variant="outline" className="px-1.5 py-0 text-[10px]">Alliance</Badge>}
          </div>
        </div>
      ))}
    </div>
  )
}

function TradeRoutesTab({ data }: { data: TradeRouteEntry[] }) {
  if (data.length === 0) return <Empty>No trade routes yet. Trade emerges when settlements produce surpluses and demand exists elsewhere.</Empty>
  return (
    <div className="space-y-2">
      {data.map((r) => (
        <div key={r.id} className="panel-carved border border-border/70 bg-card p-3">
          <p className="text-sm font-medium">{r.fromSettlementName} ↔ {r.toSettlementName}</p>
          <p className="text-xs text-muted-foreground">{r.primaryGood} · {r.distance.toFixed(0)} tiles</p>
          <div className="mt-1.5 flex flex-wrap gap-x-3 gap-y-0.5 text-[11px] text-muted-foreground">
            <span>{r.tripCount} trips</span>
            <span>{r.totalVolumeTransported.toFixed(0)} units</span>
            <span>value {r.economicValue.toFixed(1)}</span>
          </div>
        </div>
      ))}
    </div>
  )
}

function TechnologyTab({ data }: { data: { discovered: TechnologyEntry[]; inProgress: TechnologyProgress[] } | undefined }) {
  const categories = [...new Set(data?.discovered.map((t) => t.category) ?? [])]
  return (
    <div className="space-y-5">
      <div>
        <p className="mb-2 font-display text-xs font-semibold uppercase tracking-wide text-muted-foreground">
          Discovered ({data?.discovered.length ?? 0})
        </p>
        {categories.length === 0 ? (
          <Empty>No technologies discovered yet. Technology advances as settlements accumulate experience.</Empty>
        ) : (
          categories.map((cat) => (
            <div key={cat} className="mb-3">
              <p className="mb-1.5 text-xs font-medium text-muted-foreground">{cat}</p>
              <div className="space-y-1.5">
                {data?.discovered.filter((t) => t.category === cat).map((t) => (
                  <div key={t.id} className="rounded-xl border border-border/70 px-3 py-1.5 text-sm">
                    <p className="font-medium">{t.name}</p>
                    <p className="text-xs text-muted-foreground">{t.description}</p>
                    <p className="mt-0.5 text-[10px] text-muted-foreground/70">
                      {t.settlementName ?? 'Unknown'}{t.citizenName ? ` · ${t.citizenName}` : ''}
                    </p>
                  </div>
                ))}
              </div>
            </div>
          ))
        )}
      </div>

      <div>
        <p className="mb-2 font-display text-xs font-semibold uppercase tracking-wide text-muted-foreground">In Progress</p>
        {(data?.inProgress.length ?? 0) === 0 ? (
          <p className="text-sm text-muted-foreground">All technologies discovered.</p>
        ) : (
          <div className="space-y-2">
            {data?.inProgress.map((t) => (
              <div key={t.id} className="panel-carved border border-border/70 bg-card p-3">
                <div className="flex items-center justify-between">
                  <div>
                    <p className="text-sm font-medium">{t.name}</p>
                    <p className="text-xs text-muted-foreground">{t.category}</p>
                  </div>
                  <p className="text-sm font-medium tabular-nums">{t.progress.toFixed(0)}%</p>
                </div>
                <Progress value={t.progress} className="mt-2" />
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  )
}

function CultureTab({ data }: { data: CultureEntry[] }) {
  if (data.length === 0) return <Empty>No cultural identities have developed yet. Culture emerges as settlements gain traditions and shared experiences.</Empty>
  return (
    <div className="space-y-3">
      {data.map((s) => (
        <div key={s.id} className="panel-carved border border-border/70 bg-card p-3.5">
          <h3 className="font-display font-semibold">{s.name}</h3>
          <p className="text-xs text-muted-foreground">Population {s.population}{s.religionName ? ` · ${s.religionName}` : ''}</p>
          <div className="mt-2 flex flex-wrap gap-1.5">
            {s.traits.map((t) => (
              <div key={t.id} className="rounded-full bg-muted/60 px-2.5 py-1 text-xs">
                <span className="font-medium">{t.name}</span>
                <span className="ml-1 text-muted-foreground">{t.category}</span>
              </div>
            ))}
          </div>
        </div>
      ))}
    </div>
  )
}

function ReligionTab({ data }: { data: ReligionEntry[] }) {
  if (data.length === 0) return <Empty>No religions have formed yet. Belief systems emerge from shared values and community traditions.</Empty>
  return (
    <div className="space-y-3">
      {data.map((r) => (
        <div key={r.id} className="panel-carved border border-border/70 bg-card p-3.5">
          <h3 className="font-display font-semibold">{r.name}</h3>
          <p className="mb-1.5 text-xs text-muted-foreground">{r.description}</p>
          <p className="text-sm"><span className="font-medium">Core value:</span> {r.coreValue}</p>
          <div className="mt-1.5 flex flex-wrap gap-1">
            {r.tenets.map((tenet, i) => (
              <span key={i} className="rounded-full bg-muted/60 px-2 py-0.5 text-[11px]">{tenet}</span>
            ))}
          </div>
          <div className="mt-2.5 grid grid-cols-3 gap-1.5 text-center text-xs">
            <MiniBlock label="Followers" value={r.followerCount} />
            <div className="rounded-xl bg-muted/60 px-1.5 py-1.5">
              <p className="font-display text-sm font-semibold">{(r.culturalInfluence * 100).toFixed(0)}%</p>
              <p className="text-[9px] text-muted-foreground">Influence</p>
            </div>
            <div className="rounded-xl bg-muted/60 px-1.5 py-1.5">
              <p className="truncate font-display text-sm font-semibold">{r.originSettlementName}</p>
              <p className="text-[9px] text-muted-foreground">Origin</p>
            </div>
          </div>
        </div>
      ))}
    </div>
  )
}

function MigrationTab({ active }: { active: boolean }) {
  const { data, isLoading } = useQuery({
    queryKey: ['migration'],
    queryFn: fetchMigration,
    refetchInterval: 5000,
    enabled: active,
  })

  if (isLoading) return <p className="text-sm text-muted-foreground">Reading the roads…</p>
  const migrants = (data?.currentMigrants ?? []) as Record<string, unknown>[]

  return (
    <div className="space-y-3">
      <p className="text-xs text-muted-foreground">Citizens migrate between settlements in search of better conditions.</p>
      {migrants.length === 0 ? (
        <Empty>No active migrations currently.</Empty>
      ) : (
        <div className="space-y-1.5">
          {migrants.map((m) => (
            <div key={m.id as string} className="flex items-center justify-between rounded-xl border border-border/70 px-3 py-1.5 text-sm">
              <span className="font-medium">{m.name as string}</span>
              <span className="text-xs text-muted-foreground">{m.currentActivity as string} · ({m.tileX as number}, {m.tileY as number})</span>
            </div>
          ))}
        </div>
      )}
    </div>
  )
}

function LegendsTab({ data }: { data: LegendEntry[] }) {
  return (
    <div>
      <p className="mb-3 text-xs text-muted-foreground">
        Memory changes faster than history. These are distorted retellings of real, high-importance events that have aged long enough for the truth to blur — the original record is never overwritten, only accompanied.
      </p>
      {data.length === 0 ? (
        <Empty>No legends have formed yet. They emerge from significant events once enough time has passed.</Empty>
      ) : (
        <div className="space-y-3">
          {data.map((l) => (
            <div key={l.id} className="panel-carved border border-border/70 bg-card p-3.5">
              <div className="flex items-center justify-between gap-2">
                <p className="font-display font-medium">{l.title}</p>
                <span className="shrink-0 text-[10px] text-muted-foreground">{l.legendaryStatus.toFixed(0)}</span>
              </div>
              <p className="mt-1 font-chronicle text-sm italic text-muted-foreground">{l.distortedNarrative}</p>
              {l.originalTitle && (
                <p className="mt-2 border-t border-border/60 pt-2 text-[11px] text-muted-foreground">
                  What actually happened: {l.originalTitle}{l.originalDescription ? ` - ${l.originalDescription}` : ''}
                </p>
              )}
            </div>
          ))}
        </div>
      )}
    </div>
  )
}

import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { CaretLeft, Bread, Tree, Mountains } from '@phosphor-icons/react'
import { Badge } from '@/components/ui/badge'
import { Progress } from '@/components/ui/progress'
import { Skeleton } from '@/components/ui/skeleton'
import { Tabs, TabsList, TabsTrigger, TabsContent } from '@/components/ui/tabs'
import { fetchSettlements, fetchSettlementDetail, type SettlementSummary, type SettlementDetail } from '@/lib/api'
import { usePanelState } from '@/lib/usePanelState'

function formatTier(tier: string) {
  return tier.replace(/([a-z])([A-Z])/g, '$1 $2')
}

const TIER_DESCRIPTIONS: Record<string, string> = {
  Hamlet: 'A small cluster of under 10 residents, just getting started.',
  Village: 'A growing community of 10-29 residents.',
  Town: 'An established settlement of 30-74 residents with real infrastructure.',
  City: 'A major settlement of 75-149 residents.',
  RegionalCapital: 'A regional power of 150-299 residents.',
  Metropolis: 'A dominant metropolis of 300 or more residents.',
}

const AUTHORITY_DESCRIPTIONS: Record<string, string> = {
  Competence: "Whoever contributes most to the settlement naturally leads - no formal structure yet.",
  Election: 'A small council shares authority collectively.',
  Tradition: 'Authority rests with customary, elder-based leadership.',
}

export default function SettlementsPanel() {
  const { selected, setSelected } = usePanelState()

  if (selected) {
    return (
      <div key={selected} className="animate-view-swap">
        <SettlementLedger id={selected} onBack={() => setSelected(null)} />
      </div>
    )
  }

  return (
    <div className="animate-view-swap">
      <SettlementList onSelect={setSelected} />
    </div>
  )
}

function SettlementList({ onSelect }: { onSelect: (id: string) => void }) {
  const { data: settlements, isLoading } = useQuery({
    queryKey: ['settlements'],
    queryFn: fetchSettlements,
    refetchInterval: 3000,
  })

  if (isLoading) {
    return (
      <div className="space-y-2">
        {Array.from({ length: 3 }).map((_, i) => (
          <Skeleton key={i} className="h-20 w-full rounded-2xl" />
        ))}
      </div>
    )
  }

  if (!settlements?.length) {
    return (
      <p className="py-8 text-center text-sm text-muted-foreground">
        No settlements yet. Citizens will found one as they explore the world.
      </p>
    )
  }

  const totalPop = settlements.reduce((a, s) => a + s.population, 0)
  const totalBuildings = settlements.reduce((a, s) => a + s.completedBuildings, 0)

  return (
    <div className="space-y-4">
      <div className="grid grid-cols-3 gap-2">
        <MiniStat label="Settlements" value={settlements.length} />
        <MiniStat label="Population" value={totalPop} />
        <MiniStat label="Buildings" value={totalBuildings} />
      </div>

      <div className="space-y-2">
        {settlements.map((s: SettlementSummary) => (
          <button
            key={s.id}
            onClick={() => onSelect(s.id)}
            className="w-full rounded-2xl border border-transparent px-3 py-2.5 text-left transition-colors hover:border-border hover:bg-accent/50"
          >
            <div className="flex items-center justify-between">
              <span className="font-display text-sm font-semibold">{s.name}</span>
              <Badge variant="outline" title={TIER_DESCRIPTIONS[s.tier]}>{formatTier(s.tier)}</Badge>
            </div>
            <div className="mt-1.5 flex items-center gap-3 text-xs text-muted-foreground">
              <span>{s.population} residents</span>
              <span>·</span>
              <span>{s.completedBuildings}/{s.totalBuildings} built</span>
              <span>·</span>
              <span>({s.tileX}, {s.tileY})</span>
            </div>
            <div className="mt-2 flex items-center gap-4 text-xs">
              <ResourceChip icon={<Bread size={12} weight="fill" />} value={Math.round(s.foodReserves)} />
              <ResourceChip icon={<Tree size={12} weight="fill" />} value={Math.round(s.woodReserves)} />
              <ResourceChip icon={<Mountains size={12} weight="fill" />} value={Math.round(s.stoneReserves)} />
            </div>
          </button>
        ))}
      </div>
    </div>
  )
}

function MiniStat({ label, value }: { label: string; value: number }) {
  return (
    <div className="rounded-2xl border border-border/70 bg-card px-2.5 py-2 text-center">
      <p className="font-display text-lg font-semibold">{value}</p>
      <p className="text-[10px] uppercase tracking-wide text-muted-foreground">{label}</p>
    </div>
  )
}

function ResourceChip({ icon, value }: { icon: React.ReactNode; value: number }) {
  return (
    <span className="flex items-center gap-1 text-muted-foreground">
      <span className="text-status-thriving">{icon}</span>
      <span className="tabular-nums text-foreground">{value}</span>
    </span>
  )
}

function SettlementLedger({ id, onBack }: { id: string; onBack: () => void }) {
  const { data: detail, isLoading } = useQuery({
    queryKey: ['settlement', id],
    queryFn: () => fetchSettlementDetail(id),
    refetchInterval: 3000,
  })
  const [tab, setTab] = useState('overview')

  if (isLoading || !detail) {
    return (
      <div className="space-y-3">
        <Skeleton className="h-10 w-40" />
        <Skeleton className="h-48 w-full" />
      </div>
    )
  }

  return (
    <div className="space-y-5">
      <button onClick={onBack} className="flex items-center gap-1 text-xs font-medium text-muted-foreground hover:text-foreground">
        <CaretLeft size={12} /> Settlements
      </button>

      <div>
        <h3 className="font-display text-lg font-semibold">{detail.name}</h3>
        <p className="text-sm text-muted-foreground">
          Founded tick {detail.foundedTick} · ({detail.tileX}, {detail.tileY})
        </p>
      </div>

      <Tabs value={tab} onValueChange={setTab}>
        <TabsList>
          <TabsTrigger value="overview">Overview</TabsTrigger>
          <TabsTrigger value="civic">Civic</TabsTrigger>
          <TabsTrigger value="nature">Nature</TabsTrigger>
          <TabsTrigger value="trade">Trade &amp; Defense</TabsTrigger>
          <TabsTrigger value="ledger">Ledger</TabsTrigger>
        </TabsList>

        <TabsContent value="overview" className="space-y-5">
          <div>
            <div className="grid grid-cols-2 gap-2 text-sm">
              <InfoRow label="Tier" value={formatTier(detail.tier)} />
              <InfoRow label="Leader" value={detail.leaderName || 'None'} />
              <InfoRow label="Government" value={detail.governmentType} />
              <InfoRow label="Authority" value={detail.authoritySource} />
              <InfoRow label="Religion" value={detail.religionName || 'None'} />
              <InfoRow label="Technology" value={`${detail.technologyProgress.toFixed(0)}%`} />
            </div>
            <p className="mt-2 text-xs text-muted-foreground">
              {TIER_DESCRIPTIONS[detail.tier] ?? ''}
              {detail.authoritySource && AUTHORITY_DESCRIPTIONS[detail.authoritySource] ? ` ${AUTHORITY_DESCRIPTIONS[detail.authoritySource]}` : ''}
            </p>
          </div>

          <Section title="Legitimacy" hint="How secure this settlement's government is — low legitimacy makes it a shakier diplomatic partner.">
            <WellbeingBar label="Overall" value={detail.legitimacy} goodHigh />
            {detail.legitimacyBreakdown && (
              <>
                <WellbeingBar label="Competence" value={detail.legitimacyBreakdown.competence} goodHigh />
                <WellbeingBar label="Trust" value={detail.legitimacyBreakdown.publicTrust} goodHigh />
                <WellbeingBar label="Stability" value={detail.legitimacyBreakdown.stability} goodHigh />
              </>
            )}
          </Section>

          <div className="grid grid-cols-2 gap-2">
            <StatBlock label="Population" value={detail.population} />
            <StatBlock label="Territory" value={`${detail.territoryRadius} tiles`} />
            <StatBlock label="Buildings" value={detail.buildings.length} />
            <StatBlock label="Members" value={detail.members.length} />
          </div>

          {detail.currentProblems.length > 0 && (
            <Section title="Current Problems">
              {detail.currentProblems.map((p) => (
                <WarningRow key={p}>{p}</WarningRow>
              ))}
            </Section>
          )}

          {detail.wellbeing && (
            <Section title="Wellbeing" hint="Averaged across living members.">
              <WellbeingBar label="Health" value={detail.wellbeing.averageHealth} goodHigh />
              <WellbeingBar label="Hunger" value={detail.wellbeing.averageHunger} goodHigh={false} />
              <WellbeingBar label="Thirst" value={detail.wellbeing.averageThirst} goodHigh={false} />
              <WellbeingBar label="Energy" value={detail.wellbeing.averageEnergy} goodHigh />
            </Section>
          )}
        </TabsContent>

        <TabsContent value="civic" className="space-y-5">
          {detail.ongoingProjects.length > 0 && (
            <Section title="Ongoing Projects">
              {detail.ongoingProjects.map((p, i) => (
                <div key={i} className="panel-carved border border-border/70 bg-card p-3">
                  <div className="flex items-center justify-between">
                    <span className="text-sm font-medium">{p.buildingType}</span>
                    <Badge variant="secondary">{p.status}</Badge>
                  </div>
                  <Progress value={p.buildTimeRequired > 0 ? (p.buildProgress / p.buildTimeRequired) * 100 : 0} className="mt-2 h-1.5" />
                </div>
              ))}
            </Section>
          )}

          {detail.legalCases.open + detail.legalCases.resolved + detail.legalCases.failed > 0 && (
            <Section title="Justice" hint="Disputes between residents, resolved informally by the settlement's leader.">
              <div className="grid grid-cols-3 gap-2 text-sm">
                <StatBlock label="Open" value={detail.legalCases.open} />
                <StatBlock label="Resolved" value={detail.legalCases.resolved} />
                <StatBlock label="Failed" value={detail.legalCases.failed} />
              </div>
            </Section>
          )}

          <Section title="Territory" hint="Regional influence, derived from population and legitimacy.">
            <BarRow value={detail.territorialInfluence} label={detail.territorialInfluence.toFixed(0)} />
            {detail.borderDisputes.map((d) => (
              <WarningRow key={d.otherSettlementId}>Border dispute with {d.otherSettlementName}</WarningRow>
            ))}
          </Section>

          <Section title="Population" hint="Carrying capacity — how many members this settlement's food and housing can sustain.">
            <BarRow
              value={detail.carryingCapacity > 0 ? Math.min(100, (detail.population / detail.carryingCapacity) * 100) : 0}
              label={`${detail.population} / ${detail.carryingCapacity.toFixed(0)}`}
            />
          </Section>
        </TabsContent>

        <TabsContent value="nature" className="space-y-5">
          {detail.nearbyResources.length > 0 && (
            <Section title="Nearby Resources">
              <div className="grid grid-cols-2 gap-1.5">
                {detail.nearbyResources.map((r) => (
                  <InfoRow key={r.type} label={r.type} value={String(r.total)} />
                ))}
              </div>
            </Section>
          )}

          <Section title="Health" hint="Active infections — overcrowding-driven, tracked per citizen.">
            {detail.activeInfections > 0 ? (
              <WarningRow>
                {detail.activeInfections} {detail.activeInfections === 1 ? 'citizen is' : 'citizens are'} currently infected
              </WarningRow>
            ) : (
              <p className="text-xs text-muted-foreground">No active infections.</p>
            )}
          </Section>

          {detail.attributeAverages && (
            <Section title="Adaptation" hint="Average attributes across living members — drift here reflects real inheritance and survival.">
              <div className="grid grid-cols-3 gap-2 text-xs">
                <InfoRow label="Strength" value={String(detail.attributeAverages.strength)} />
                <InfoRow label="Endurance" value={String(detail.attributeAverages.endurance)} />
                <InfoRow label="Intelligence" value={String(detail.attributeAverages.intelligence)} />
                <InfoRow label="Dexterity" value={String(detail.attributeAverages.dexterity)} />
                <InfoRow label="Perception" value={String(detail.attributeAverages.perception)} />
              </div>
            </Section>
          )}

          <Section title="Ecology" hint="Soil health is depleted by farming and restored by decomposition. Wildlife is an aggregate estimate from forest habitat.">
            <BarRow value={detail.soilHealth} label={`Soil ${detail.soilHealth.toFixed(0)}`} />
            <p className="text-xs">
              <span className="font-medium">{detail.wildlifePopulation.toFixed(0)}</span>{' '}
              <span className="text-muted-foreground">wildlife (aggregate estimate)</span>
            </p>
          </Section>
        </TabsContent>

        <TabsContent value="trade" className="space-y-5">
          <Section title="Military" hint="Strength is derived from population and legitimacy.">
            <p className="text-xs text-muted-foreground">Strength: {detail.militaryStrength.toFixed(0)}</p>
            {detail.activeWars.map((w) => (
              <div key={w.otherSettlementId} className="rounded-xl border border-status-danger/40 bg-status-danger/10 px-3 py-1.5 text-sm text-status-danger">
                At war with {w.otherSettlementName} — {w.battlesFought} battle{w.battlesFought === 1 ? '' : 's'} fought
              </div>
            ))}
          </Section>

          <Section title="Infrastructure" hint="Route quality grows with sustained trade traffic and decays when idle. At 50 quality a route becomes a road.">
            {detail.tradeRoutes.length === 0 ? (
              <p className="text-xs text-muted-foreground">No trade routes.</p>
            ) : (
              detail.tradeRoutes.map((r) => (
                <div key={r.otherSettlementId} className="flex items-center justify-between rounded-xl border border-border/70 px-3 py-1.5 text-sm">
                  <span>{r.otherSettlementName} ({r.primaryGood}){!r.isActive && ' — inactive'}</span>
                  <span className="flex items-center gap-2">
                    <Progress value={r.infrastructureQuality} className="w-16" />
                    <span className="w-8 text-right text-xs tabular-nums">{r.infrastructureQuality.toFixed(0)}</span>
                  </span>
                </div>
              ))
            )}
          </Section>

          {detail.languageDivergence.length > 0 && (
            <Section title="Language" hint="How far this settlement's speech has drifted from settlements it has contact with.">
              {detail.languageDivergence.map((d) => (
                <div key={d.otherSettlementId} className="flex items-center justify-between rounded-xl border border-border/70 px-3 py-1.5 text-sm">
                  <span>{d.otherSettlementName}</span>
                  <span className="flex items-center gap-2">
                    {d.dialectFormed && <Badge variant="secondary">Dialect</Badge>}
                    <span className="font-medium tabular-nums">{d.divergence.toFixed(0)}</span>
                  </span>
                </div>
              ))}
            </Section>
          )}
        </TabsContent>

        <TabsContent value="ledger" className="space-y-5">
          <Section title="Storage">
            {detail.storage.length === 0 ? (
              <p className="text-xs text-muted-foreground">Empty.</p>
            ) : (
              detail.storage.map((item) => <InfoRow key={item.itemType} label={item.itemType} value={String(Math.round(item.quantity))} />)
            )}
          </Section>

          <Section title="Buildings">
            {detail.buildings.map((b) => (
              <div key={b.id} className="panel-carved border border-border/70 bg-card p-3">
                <div className="flex items-center justify-between">
                  <div className="flex items-center gap-2">
                    <span className="text-sm font-medium">{b.buildingType}</span>
                    <Badge variant={b.status === 'Completed' ? 'default' : b.status === 'UnderConstruction' ? 'secondary' : 'outline'}>
                      {b.status}
                    </Badge>
                  </div>
                  <span className="text-xs text-muted-foreground">({b.tileX}, {b.tileY})</span>
                </div>
                {b.status !== 'Completed' && (
                  <Progress value={b.buildTimeRequired > 0 ? (b.buildProgress / b.buildTimeRequired) * 100 : 0} className="mt-2 h-1.5" />
                )}
              </div>
            ))}
          </Section>

          <Section title="Members">
            {detail.members.map((m) => (
              <div key={m.id} className="flex items-center justify-between rounded-xl border border-border/70 px-3 py-1.5 text-sm">
                <span>{m.firstName} {m.lastName}</span>
                <span className="text-xs text-muted-foreground">{m.currentActivity} · Age {m.age}</span>
              </div>
            ))}
          </Section>

          <p className="text-xs text-muted-foreground">
            Families and security aren't modeled in the simulation yet — this ledger will show them here once those systems exist.
          </p>
        </TabsContent>
      </Tabs>
    </div>
  )
}

function Section({ title, hint, children }: { title: string; hint?: string; children: React.ReactNode }) {
  return (
    <div>
      <p className="mb-1 font-display text-xs font-semibold uppercase tracking-wide text-muted-foreground">{title}</p>
      {hint && <p className="mb-2 text-xs text-muted-foreground">{hint}</p>}
      <div className="space-y-1.5">{children}</div>
    </div>
  )
}

function InfoRow({ label, value }: { label: string; value: string }) {
  return (
    <div className="flex items-center justify-between rounded-xl border border-border/70 px-3 py-1.5 text-sm">
      <span className="text-muted-foreground">{label}</span>
      <span className="font-medium">{value}</span>
    </div>
  )
}

function StatBlock({ label, value }: { label: string; value: string | number }) {
  return (
    <div className="panel-carved border border-border/70 bg-card p-3">
      <p className="text-xs text-muted-foreground">{label}</p>
      <p className="font-display text-xl font-semibold">{value}</p>
    </div>
  )
}

function WarningRow({ children }: { children: React.ReactNode }) {
  return (
    <div className="rounded-xl border border-status-hunger/40 bg-status-hunger/10 px-3 py-1.5 text-sm text-status-hunger">
      {children}
    </div>
  )
}

function BarRow({ value, label }: { value: number; label: string }) {
  return (
    <div className="flex items-center gap-3">
      <Progress value={value} />
      <span className="w-16 shrink-0 text-right text-xs tabular-nums">{label}</span>
    </div>
  )
}

function WellbeingBar({ label, value, goodHigh }: { label: string; value: number; goodHigh: boolean }) {
  const effective = goodHigh ? value : 100 - value
  const color = effective >= 60 ? 'bg-status-health' : effective >= 35 ? 'bg-status-hunger' : 'bg-status-danger'
  const icon = effective >= 60 ? '✓' : effective >= 35 ? '!' : '✗'
  const statusLabel = effective >= 60 ? 'good' : effective >= 35 ? 'warning' : 'poor'

  return (
    <div className="flex items-center gap-2.5">
      <span className="w-16 shrink-0 text-xs text-muted-foreground">{label}</span>
      <span className="w-3 shrink-0 text-xs" aria-hidden="true">{icon}</span>
      <Progress value={value} indicatorClassName={color} aria-label={`${label}: ${value.toFixed(0)} (${statusLabel})`} />
      <span className="w-7 shrink-0 text-right text-xs tabular-nums">{value.toFixed(0)}</span>
    </div>
  )
}

export type { SettlementDetail }

import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { CaretLeft, CaretRight, MagnifyingGlass, Heart, ForkKnife, Lightning } from '@phosphor-icons/react'
import { Badge } from '@/components/ui/badge'
import { Progress } from '@/components/ui/progress'
import { Avatar, AvatarFallback } from '@/components/ui/avatar'
import { Skeleton } from '@/components/ui/skeleton'
import {
  fetchCitizens,
  fetchPopulation,
  fetchCitizenDetail,
  fetchCitizenRelationships,
  fetchCitizenFamily,
  type CitizenSummary,
  type CitizenDetail,
  type CitizenRelationship,
  type CitizenFamilyMember,
} from '@/lib/api'
import { usePanelState } from '@/lib/usePanelState'

function getInitials(name: string) {
  return name.split(' ').map((n) => n[0]).join('').toUpperCase().slice(0, 2)
}

function needColor(value: number, warning: number, critical: number) {
  if (value >= critical) return 'bg-status-danger'
  if (value >= warning) return 'bg-status-hunger'
  return 'bg-status-health'
}

function healthColor(value: number) {
  if (value <= 20) return 'bg-status-danger'
  if (value <= 40) return 'bg-status-hunger'
  return 'bg-status-health'
}

const PAGE_SIZE = 30
const SORT_OPTIONS: { value: string; label: string }[] = [
  { value: 'name', label: 'Name' },
  { value: 'age', label: 'Age' },
  { value: 'activity', label: 'Activity' },
]

/**
 * Roster + inspector as two views of the same panel (rather than a second
 * overlay stacked on top), since we're already inside PanelHost's slide-in
 * layer. Selection is kept in the URL (?selected=id) via usePanelState so
 * GlobalSearch's deep-link and the browser back button both work.
 */
export default function CitizensPanel() {
  const { selected, setSelected } = usePanelState()
  const [page, setPage] = useState(1)
  const [sortBy, setSortBy] = useState('name')
  const [search, setSearch] = useState('')

  const { data: population } = useQuery({
    queryKey: ['population'],
    queryFn: fetchPopulation,
    refetchInterval: 5000,
  })

  if (selected) {
    return (
      <div key={selected} className="animate-view-swap">
        <CitizenInspector id={selected} onBack={() => setSelected(null)} />
      </div>
    )
  }

  return (
    <div className="animate-view-swap space-y-4">
      <div className="grid grid-cols-4 gap-2">
        <MiniStat label="Alive" value={population?.alive} />
        <MiniStat label="Dead" value={population?.dead} muted />
        <MiniStat label="Avg age" value={population?.averageAge} />
        <MiniStat label="Deaths" value={population?.totalDeaths} muted />
      </div>

      <div className="relative">
        <MagnifyingGlass size={14} className="pointer-events-none absolute left-3 top-1/2 -translate-y-1/2 text-muted-foreground" />
        <input
          type="text"
          placeholder="Search citizens…"
          value={search}
          onChange={(e) => {
            setSearch(e.target.value)
            setPage(1)
          }}
          className="h-9 w-full rounded-full border border-border bg-card pl-8 pr-3 text-sm outline-none transition-colors placeholder:text-muted-foreground focus-visible:ring-2 focus-visible:ring-ring"
        />
      </div>

      <div className="flex items-center gap-1.5 text-xs text-muted-foreground">
        <span>Sort</span>
        {SORT_OPTIONS.map((opt) => (
          <button
            key={opt.value}
            onClick={() => {
              setSortBy(opt.value)
              setPage(1)
            }}
            className={
              sortBy === opt.value
                ? 'rounded-full bg-primary px-2.5 py-1 font-display text-[11px] font-semibold text-primary-foreground'
                : 'rounded-full px-2.5 py-1 font-display text-[11px] font-medium hover:bg-accent'
            }
          >
            {opt.label}
          </button>
        ))}
      </div>

      <RosterList page={page} setPage={setPage} sortBy={sortBy} search={search} onSelect={setSelected} />
    </div>
  )
}

function MiniStat({ label, value, muted }: { label: string; value?: number; muted?: boolean }) {
  return (
    <div className="rounded-2xl border border-border/70 bg-card px-2.5 py-2 text-center">
      <p className={muted ? 'font-display text-lg font-semibold text-muted-foreground' : 'font-display text-lg font-semibold'}>
        {value ?? '–'}
      </p>
      <p className="text-[10px] uppercase tracking-wide text-muted-foreground">{label}</p>
    </div>
  )
}

function RosterList({
  page,
  setPage,
  sortBy,
  search,
  onSelect,
}: {
  page: number
  setPage: (updater: (p: number) => number) => void
  sortBy: string
  search: string
  onSelect: (id: string) => void
}) {
  const { data, isLoading } = useQuery({
    queryKey: ['citizens', page, PAGE_SIZE, sortBy, search],
    queryFn: () => fetchCitizens(page, PAGE_SIZE, sortBy, search),
    refetchInterval: 5000,
  })

  if (isLoading) {
    return (
      <div className="space-y-2">
        {Array.from({ length: 6 }).map((_, i) => (
          <Skeleton key={i} className="h-14 w-full rounded-2xl" />
        ))}
      </div>
    )
  }

  if (!data?.citizens?.length) {
    return <p className="py-8 text-center text-sm text-muted-foreground">No citizens match that search.</p>
  }

  return (
    <div className="space-y-3">
      <div className="space-y-1.5">
        {data.citizens.map((c: CitizenSummary) => (
          <button
            key={c.id}
            onClick={() => onSelect(c.id)}
            className="flex w-full items-center gap-3 rounded-2xl border border-transparent px-2.5 py-2 text-left transition-colors hover:border-border hover:bg-accent/50"
          >
            <Avatar className="h-9 w-9 shrink-0">
              <AvatarFallback>{getInitials(c.name)}</AvatarFallback>
            </Avatar>
            <div className="min-w-0 flex-1">
              <div className="flex items-center gap-1.5">
                <p className="truncate text-sm font-medium">{c.name}</p>
                <span className="shrink-0 text-[10px] text-muted-foreground">{c.age}</span>
              </div>
              <div className="mt-1 flex items-center gap-2.5">
                <NeedDot icon={<Heart size={10} weight="fill" />} value={c.health} color={healthColor(c.health)} />
                <NeedDot icon={<ForkKnife size={10} weight="fill" />} value={100 - c.hunger} color={needColor(c.hunger, 60, 80)} />
                <NeedDot icon={<Lightning size={10} weight="fill" />} value={c.energy} color={needColor(100 - c.energy, 70, 85)} />
                <Badge variant="outline" className="ml-auto shrink-0 px-1.5 py-0 text-[10px]">
                  {c.currentActivity}
                </Badge>
              </div>
            </div>
          </button>
        ))}
      </div>

      {data.totalPages > 1 && (
        <div className="flex items-center justify-between pt-1">
          <button
            disabled={page <= 1}
            onClick={() => setPage((p) => Math.max(1, p - 1))}
            className="flex h-7 w-7 items-center justify-center rounded-full text-muted-foreground transition-colors hover:bg-accent disabled:opacity-30"
          >
            <CaretLeft size={13} />
          </button>
          <span className="text-xs text-muted-foreground">
            Page {data.page} of {data.totalPages} · {data.total} citizens
          </span>
          <button
            disabled={page >= data.totalPages}
            onClick={() => setPage((p) => p + 1)}
            className="flex h-7 w-7 items-center justify-center rounded-full text-muted-foreground transition-colors hover:bg-accent disabled:opacity-30"
          >
            <CaretRight size={13} />
          </button>
        </div>
      )}
    </div>
  )
}

function NeedDot({ icon, value, color }: { icon: React.ReactNode; value: number; color: string }) {
  return (
    <span className="flex items-center gap-1 text-[10px] text-muted-foreground" title={`${value.toFixed(0)}`}>
      <span className={`flex h-3.5 w-3.5 items-center justify-center rounded-full text-white ${color}`}>{icon}</span>
    </span>
  )
}

const FAMILY_RELATION_ORDER = [
  'Father', 'Mother', 'Husband', 'Wife', 'Son', 'Daughter',
  'Brother', 'Sister', 'Grandfather', 'Grandmother', 'Grandson', 'Granddaughter',
]

function CitizenInspector({ id, onBack }: { id: string; onBack: () => void }) {
  const { data: detail, isLoading } = useQuery({
    queryKey: ['citizen', id],
    queryFn: () => fetchCitizenDetail(id),
  })
  const { data: relationships } = useQuery({
    queryKey: ['citizen-relationships', id],
    queryFn: () => fetchCitizenRelationships(id),
  })
  const { data: family } = useQuery({
    queryKey: ['citizen-family', id],
    queryFn: () => fetchCitizenFamily(id),
  })

  if (isLoading) {
    return (
      <div className="space-y-3">
        <Skeleton className="h-10 w-32" />
        <Skeleton className="h-48 w-full" />
      </div>
    )
  }

  if (!detail?.citizen) {
    return <p className="text-sm text-muted-foreground">Citizen not found.</p>
  }

  const data = detail.citizen
  const sortedFamily = [...(family ?? [])].sort(
    (a: CitizenFamilyMember, b: CitizenFamilyMember) =>
      FAMILY_RELATION_ORDER.indexOf(a.relation) - FAMILY_RELATION_ORDER.indexOf(b.relation)
  )

  return (
    <div className="space-y-6">
      <button onClick={onBack} className="flex items-center gap-1 text-xs font-medium text-muted-foreground hover:text-foreground">
        <CaretLeft size={12} /> Roster
      </button>

      <div className="flex items-center gap-3">
        <Avatar className="h-14 w-14">
          <AvatarFallback className="text-lg">{getInitials(`${data.firstName} ${data.lastName}`)}</AvatarFallback>
        </Avatar>
        <div>
          <h3 className="font-display text-lg font-semibold">
            {data.firstName} {data.lastName}
          </h3>
          <p className="text-sm text-muted-foreground">
            {data.stage} · {data.age} years · {data.biologicalSex}
          </p>
        </div>
      </div>

      <Section title="Activity">
        <div className="panel-carved border border-border/70 bg-card p-3">
          <Badge>{data.currentActivity}</Badge>
          <p className="mt-1.5 text-xs text-muted-foreground">
            Goal: {data.currentGoal} · Tile ({data.tileX},{data.tileY})
          </p>
        </div>
      </Section>

      <Section title="Needs">
        <div className="space-y-2">
          {[
            { label: 'Health', value: data.needs.health, color: healthColor(data.needs.health) },
            { label: 'Hunger', value: data.needs.hunger, color: needColor(data.needs.hunger, 60, 80) },
            { label: 'Thirst', value: data.needs.thirst, color: needColor(data.needs.thirst, 60, 80) },
            { label: 'Energy', value: data.needs.energy, color: needColor(100 - data.needs.energy, 70, 85) },
            { label: 'Warmth', value: data.needs.warmth, color: needColor(100 - data.needs.warmth, 70, 85) },
          ].map((n) => (
            <div key={n.label} className="flex items-center gap-3">
              <span className="w-14 text-xs text-muted-foreground">{n.label}</span>
              <Progress value={n.value} indicatorClassName={n.color} />
              <span className="w-7 text-right text-xs tabular-nums">{n.value.toFixed(0)}</span>
            </div>
          ))}
        </div>
      </Section>

      <StatGrid title="Attributes" items={[
        ['Strength', data.attributes.strength], ['Endurance', data.attributes.endurance],
        ['Intelligence', data.attributes.intelligence], ['Dexterity', data.attributes.dexterity],
        ['Perception', data.attributes.perception],
      ]} />

      <StatGrid title="Personality" items={[
        ['Curiosity', data.personality.curiosity], ['Patience', data.personality.patience],
        ['Aggression', data.personality.aggression], ['Compassion', data.personality.compassion],
        ['Diligence', data.personality.diligence], ['Introversion', data.personality.introversion],
      ]} />

      <StatGrid title="Emotions" items={[
        ['Fear', data.emotions.fear], ['Joy', data.emotions.joy],
        ['Sadness', data.emotions.sadness], ['Trust', data.emotions.trust],
        ['Curiosity', data.emotions.curiosity], ['Loneliness', data.emotions.loneliness],
      ]} />

      {data.apprenticeship && (
        <Section title="Apprenticeship">
          <div className="panel-carved border border-border/70 bg-card px-3 py-2 text-sm">
            {data.apprenticeship.role === 'Mentor'
              ? `Mentoring ${data.apprenticeship.otherCitizenName}`
              : `Learning from ${data.apprenticeship.otherCitizenName}`}
          </div>
        </Section>
      )}

      {sortedFamily.length > 0 && (
        <Section title="Family">
          <div className="space-y-1">
            {sortedFamily.map((f) => (
              <div key={f.citizenId} className="flex items-center justify-between rounded-xl border border-border/70 px-2.5 py-1.5 text-sm">
                <span className={f.isAlive ? 'font-medium' : 'font-medium text-muted-foreground line-through'}>{f.name}</span>
                <Badge variant="outline">{f.relation}</Badge>
              </div>
            ))}
          </div>
        </Section>
      )}

      {(relationships?.length ?? 0) > 0 && (
        <Section title="Relationships">
          <div className="space-y-1.5">
            {relationships!.map((r: CitizenRelationship) => (
              <div key={r.otherCitizenId} className="rounded-xl border border-border/70 px-2.5 py-1.5 text-sm">
                <div className="flex items-center justify-between">
                  <span className="font-medium">{r.otherCitizenName}</span>
                  <span className="text-xs text-muted-foreground">{r.interactionCount} interactions</span>
                </div>
                <div className="mt-1 flex gap-3 text-xs text-muted-foreground">
                  <span>Trust {r.trust.toFixed(0)}</span>
                  <span>Affection {r.affection.toFixed(0)}</span>
                  <span>Distance {r.socialDistance.toFixed(0)}</span>
                </div>
              </div>
            ))}
          </div>
        </Section>
      )}

      <Section title="What This Citizen Knows">
        {data.knownEvents.length > 0 ? (
          <ul className="space-y-1">
            {data.knownEvents.map((e) => (
              <li key={e.key} className="rounded-xl border border-border/70 px-2.5 py-1.5 text-sm">
                {e.title}
              </li>
            ))}
          </ul>
        ) : (
          <p className="text-sm text-muted-foreground">Hasn't heard about any civilization events yet.</p>
        )}
      </Section>
    </div>
  )
}

function Section({ title, children }: { title: string; children: React.ReactNode }) {
  return (
    <div>
      <p className="mb-2 font-display text-xs font-semibold uppercase tracking-wide text-muted-foreground">{title}</p>
      {children}
    </div>
  )
}

function StatGrid({ title, items }: { title: string; items: [string, number][] }) {
  return (
    <Section title={title}>
      <div className="grid grid-cols-2 gap-2 text-sm">
        {items.map(([label, value]) => (
          <div key={label} className="rounded-xl border border-border/70 px-2.5 py-1.5">
            <p className="text-xs text-muted-foreground">{label}</p>
            <p className="font-medium tabular-nums">{value.toFixed(1)}</p>
          </div>
        ))}
      </div>
    </Section>
  )
}

// Type-only re-exports so callers touching these shapes don't need to reach
// into lib/api directly.
export type { CitizenDetail }

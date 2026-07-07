using Garden.Core.Interfaces;
using Garden.Engine.Services;
using Garden.World.Collections;
using Microsoft.Extensions.Logging;

namespace Garden.Engine.Systems;

public class CivilizationSystem : IScheduledSystem
{
    private readonly WorldState _worldState;
    private readonly LeadershipService _leadershipService;
    private readonly GovernanceService _governanceService;
    private readonly KingdomService _kingdomService;
    private readonly DiplomacyService _diplomacyService;
    private readonly MigrationService _migrationService;
    private readonly TradeRouteService _tradeRouteService;
    private readonly TechnologyService _technologyService;
    private readonly CultureService _cultureService;
    private readonly ReligionService _religionService;
    private readonly ILogger<CivilizationSystem> _logger;
    private long _nextExecutionTick;
    private long _lastDailyTick;
    private long _lastWeeklyTick;
    private long _lastMonthlyTick;
    private long _lastYearlyTick;

    public string Name => "CivilizationSystem";
    public long IntervalTicks => 1;
    public long NextExecutionTick => _nextExecutionTick;

    public CivilizationSystem(
        WorldState worldState,
        LeadershipService leadershipService,
        GovernanceService governanceService,
        KingdomService kingdomService,
        DiplomacyService diplomacyService,
        MigrationService migrationService,
        TradeRouteService tradeRouteService,
        TechnologyService technologyService,
        CultureService cultureService,
        ReligionService religionService,
        ILogger<CivilizationSystem> logger)
    {
        _worldState = worldState;
        _leadershipService = leadershipService;
        _governanceService = governanceService;
        _kingdomService = kingdomService;
        _diplomacyService = diplomacyService;
        _migrationService = migrationService;
        _tradeRouteService = tradeRouteService;
        _technologyService = technologyService;
        _cultureService = cultureService;
        _religionService = religionService;
        _logger = logger;
    }

    public void Execute()
    {
        var tick = _worldState.CurrentTime.Tick;
        if (tick == 0) { _nextExecutionTick = tick + IntervalTicks; return; }

        if (tick - _lastDailyTick >= 1)
        {
            foreach (var settlement in _worldState.Settlements)
            {
                _leadershipService.EvaluateLeadership(settlement, tick);
            }
            _lastDailyTick = tick;
        }

        if (tick - _lastWeeklyTick >= 7)
        {
            _tradeRouteService.EvaluateTradeRoutes(tick);
            _diplomacyService.EvaluateDiplomacy(tick);
            _lastWeeklyTick = tick;
        }

        if (tick - _lastMonthlyTick >= 28)
        {
            _migrationService.EvaluateMigration(tick);
            foreach (var settlement in _worldState.Settlements)
            {
                _governanceService.EvaluateGovernance(settlement, tick);
            }
            _lastMonthlyTick = tick;
        }

        if (tick - _lastYearlyTick >= 336)
        {
            _technologyService.EvaluateTechnology(tick);
            _cultureService.EvaluateCulture(tick);
            _religionService.EvaluateReligion(tick);
            _religionService.SpreadReligion(tick);
            _kingdomService.EvaluateKingdomFormation(tick);
            _kingdomService.EvaluateKingdomDissolution(tick);
            _lastYearlyTick = tick;
        }

        _nextExecutionTick = tick + IntervalTicks;
    }
}

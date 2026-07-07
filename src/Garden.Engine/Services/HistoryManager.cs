using Garden.World.Collections;
using Garden.World.Entities;

namespace Garden.Engine.Services;

public class HistoryManager
{
    private readonly HistoricalArchive _archive;
    private readonly SignificanceEvaluator _evaluator;
    private readonly TimelineService _timeline;
    private readonly MemoryService _memory;
    private readonly StoryEngine _storyEngine;

    public HistoricalArchive Archive => _archive;
    public SignificanceEvaluator Evaluator => _evaluator;
    public TimelineService Timeline => _timeline;
    public MemoryService Memory => _memory;
    public StoryEngine StoryEngine => _storyEngine;

    public HistoryManager(
        HistoricalArchive archive,
        SignificanceEvaluator evaluator,
        TimelineService timeline,
        MemoryService memory,
        StoryEngine storyEngine)
    {
        _archive = archive;
        _evaluator = evaluator;
        _timeline = timeline;
        _memory = memory;
        _storyEngine = storyEngine;
    }

    public HistoricalRecord? GetRecord(Guid id) => _archive.GetById(id);
}

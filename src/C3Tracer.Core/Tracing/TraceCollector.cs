using System.Collections.Immutable;

namespace C3Tracer.Core.Tracing;

internal sealed class TraceCollector(int cap)
{
    private readonly ImmutableArray<TraceEvent>.Builder events = ImmutableArray.CreateBuilder<TraceEvent>();
    public long Attempted { get; private set; }
    public long Dropped => Attempted - events.Count;
    public void Emit(Func<long, TraceEvent> create)
    {
        Attempted++;
        if (events.Count < cap) events.Add(create(Attempted));
    }
    public ImmutableArray<TraceEvent> Snapshot() => events.ToImmutable();
}

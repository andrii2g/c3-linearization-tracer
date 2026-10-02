using System.Collections.Immutable;

namespace C3Tracer.Core.Linearization;

public sealed record SequenceSnapshot(string Id, string Kind, string? BaseName, ImmutableArray<string> Items, int Cursor);

internal sealed class MergeSequenceCursor(string id, string kind, string? baseName, ImmutableArray<string> items)
{
    public string Id { get; } = id;
    public ImmutableArray<string> Items { get; } = items;
    public int Cursor { get; private set; }
    public bool Exhausted => Cursor == Items.Length;
    public string Head => Items[Cursor];
    public void Advance() => Cursor++;
    public SequenceSnapshot Snapshot() => new(Id, kind, baseName, Items, Cursor);
}

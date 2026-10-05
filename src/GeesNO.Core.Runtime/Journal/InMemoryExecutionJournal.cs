namespace GeesNO.Core.Runtime.Journal;

internal sealed class InMemoryExecutionJournal : IExecutionJournalSink
{
    private readonly List<ExecutionJournalEntry> _entries = [];
    private readonly HashSet<long> _sequences = [];
    private readonly object _sync = new();

    public void Append(ExecutionJournalEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        lock (_sync)
        {
            if (!_sequences.Add(entry.Sequence))
            {
                throw new InvalidOperationException(
                    $"The journal already contains sequence {entry.Sequence}.");
            }

            _entries.Add(entry);
        }
    }

    internal IReadOnlyList<ExecutionJournalEntry> GetEntries()
    {
        lock (_sync)
        {
            var snapshot = _entries
                .OrderBy(static entry => entry.Sequence)
                .ToArray();

            return Array.AsReadOnly(snapshot);
        }
    }
}

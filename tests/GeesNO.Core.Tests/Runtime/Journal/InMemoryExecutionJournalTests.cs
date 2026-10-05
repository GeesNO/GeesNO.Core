using GeesNO.Core.Runtime.Journal;
using GeesNO.Core.Tests.Runtime.Observability;

namespace GeesNO.Core.Tests.Runtime.Journal;

public sealed class InMemoryExecutionJournalTests
{
    [Fact]
    public void Append_rejects_null()
    {
        var journal = new InMemoryExecutionJournal();

        Assert.Throws<ArgumentNullException>(() => journal.Append(null!));
    }

    [Fact]
    public void Append_rejects_duplicate_sequence()
    {
        var journal = new InMemoryExecutionJournal();
        journal.Append(ObservabilityTestData.JournalEntry(1));

        Assert.Throws<InvalidOperationException>(() =>
            journal.Append(ObservabilityTestData.JournalEntry(1)));
    }

    [Fact]
    public void Entries_are_returned_in_sequence_order()
    {
        var journal = new InMemoryExecutionJournal();
        journal.Append(ObservabilityTestData.JournalEntry(3));
        journal.Append(ObservabilityTestData.JournalEntry(1));
        journal.Append(ObservabilityTestData.JournalEntry(2));

        Assert.Equal([1, 2, 3], journal.GetEntries().Select(entry => entry.Sequence));
    }

    [Fact]
    public void Snapshot_is_defensive_read_only_and_never_null()
    {
        var journal = new InMemoryExecutionJournal();
        var empty = journal.GetEntries();
        journal.Append(ObservabilityTestData.JournalEntry(1));
        var snapshot = journal.GetEntries();
        journal.Append(ObservabilityTestData.JournalEntry(2));

        Assert.Empty(empty);
        Assert.Single(snapshot);
        Assert.Throws<NotSupportedException>(() =>
            ((IList<ExecutionJournalEntry>)snapshot).Add(ObservabilityTestData.JournalEntry(3)));
    }

    [Fact]
    public void Journal_supports_concurrent_unique_appends()
    {
        var journal = new InMemoryExecutionJournal();

        Parallel.For(1, 257, sequence =>
            journal.Append(ObservabilityTestData.JournalEntry(sequence)));

        var entries = journal.GetEntries();
        Assert.Equal(256, entries.Count);
        Assert.Equal(Enumerable.Range(1, 256).Select(value => (long)value),
            entries.Select(entry => entry.Sequence));
    }

    [Fact]
    public void Sink_interface_has_append_only()
    {
        var methods = typeof(IExecutionJournalSink).GetMethods();
        var append = Assert.Single(methods);

        Assert.Equal("Append", append.Name);
        Assert.Equal(typeof(void), append.ReturnType);
        Assert.Single(append.GetParameters());
        Assert.Equal(typeof(ExecutionJournalEntry), append.GetParameters()[0].ParameterType);
    }
}

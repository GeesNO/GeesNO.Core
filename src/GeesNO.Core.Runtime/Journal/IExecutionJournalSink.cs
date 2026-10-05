namespace GeesNO.Core.Runtime.Journal;

internal interface IExecutionJournalSink
{
    void Append(ExecutionJournalEntry entry);
}

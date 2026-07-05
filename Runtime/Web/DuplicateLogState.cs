namespace CoradoLog.Web
{
    internal sealed class DuplicateLogState
    {
        public DuplicateLogState(float windowStartedAt, CoLoggerEntry firstEntry)
        {
            WindowStartedAt = windowStartedAt;
            LastEntry = firstEntry;
            SentCount = 1;
        }

        public float WindowStartedAt;
        public int SentCount;
        public int SuppressedCount;
        public CoLoggerEntry LastEntry;
    }
}

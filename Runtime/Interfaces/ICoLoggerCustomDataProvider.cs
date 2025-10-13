namespace CoradoLog.Interfaces
{
    public interface ICoLoggerCustomDataProvider
    {
        string GetCustomDataFormat(object customData);
    }
}
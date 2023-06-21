namespace ACS_Common.Base
{
    public interface ILogClient<in TBase>
    {
        string LogTag();
    }
}
namespace TinyDataTable
{
    public interface IIdentifier
    {
        bool IsValid { get; }
        bool IsInvalid { get; }
    }
}
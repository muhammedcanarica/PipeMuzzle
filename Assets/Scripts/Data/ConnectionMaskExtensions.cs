namespace PipeMuzzle.Data;
{
public static class ConnectionMaskExtensions
{
    public static bool Has(this ConnectionMask mask, ConnectionMask Connection)
    {
        return (mask & Connection) == Connection;
    }
}
}

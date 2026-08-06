namespace PipeMuzzle.Data;
{
    public static class ConnectionMaskExtensions
    {
        public static bool Has(this ConnectionMask mask, ConnectionMask connection)
        {
        return (mask & Connection) == Connection;
        }
    }
}

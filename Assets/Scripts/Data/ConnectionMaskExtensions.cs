namespace PipeMuzzle.Data;
{
    public static class ConnectionMaskExtensions
    {
        public static bool Has(this ConnectionMask mask, ConnectionMask connection)
        {
        return (mask & Connection) == Connection;
        }
        
        public static ConnectionMask RotateClockWise(this ConnectionMask mask)
        {   
            ConnectionMask rotated = ConnectionMask.None;
            
            if (mask.Has(ConnectionMask.North))
            {
                rotated |ConnectionMask.East;
            }
                                                        
            if (mask.Has(ConnectionMask.East))
            {
                rotated | ConnectionMask.South;
            }
            
            if (mask.Has(ConnectionMask.South))
            {
                rotated | ConnectionMask.West;
            }
            
            if (mask.Has(ConnectionMask.West))
            {
                rotated | ConnectionMask.North;
            }

            return rotated;
        }
    }
}
// | ne işe yarıyor?  Mevcut bağlantıları temsil eden bir bit maskesi üzerinde bit düzeyinde OR işlemi yapar. Bu, mevcut bağlantı maskesine yeni bir bağlantı eklemek için kullanılır. 
// Örneğin, `rotated | ConnectionMask.East` ifadesi, `rotated` maskesine `East` bağlantısını ekler.

// Bu kod, bir `ConnectionMask` enum'ı üzerinde bit düzeyinde işlemler yaparak bağlantıları temsil eden bir bit maskesi ile çalışır. 
//`Has` metodu, belirli bir bağlantının mevcut maskede olup olmadığını kontrol ederken, `RotateClockWise` metodu mevcut bağlantıları saat yönünde döndürür.
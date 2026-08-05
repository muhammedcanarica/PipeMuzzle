using System;

namespace PipeMuzzle.Data
{
    //bitleri kullandığımız için bağlantıları birleştirebiliriz kolaylıkla North + East = 0011 olur mesela

    [Flags] // System kütüphanesinde 

    public enum ConnectionMask
    {
        None = 0,  //0000
        North = 1, //0001  
        East = 2,  //0010
        South = 4, //0100
        West = 8, //1000
    }
}
//Flags'i kullanma sebebimiz ConnectionMask mask = ConnectionMask.North | ConnectionMask.East şeklinde kullanabilmemizi sağlıyomuş 
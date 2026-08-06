namespace PipeMuzzle.Data
{
    public static class TileShapeExtension
    {
        public static ConnectionMask GetBaseConnections(this TileShape shape)
        {
            return shape switch
            {
                TileShape.Empty => ConnectionMask.None,

                TileShape.Straight => ConnectionMask.North | ConnectionMask.South,

                TileShape.Corner => ConnectionMask.North | ConnectionMask.East,

                TileShape.ThreeWay => ConnectionMask.North | ConnectionMask.East | ConnectionMask.South,

                TileShape.Cross => ConnectionMask.North | ConnectionMask.East | ConnectionMask.South | ConnectionMask.West,

                _ => ConnectionMask.None 
            }
        }
    }
}
// Bu scriptte TileShape enum'ine göre her bir şeklin hangi yönlere bağlantı sağladığını belirleyen bir GetBaseConnections metodu tanımlanmıştır. 
//Bu metod, TileShape değerine göre uygun ConnectionMask değerini döndürür.
using KFrame.Sources;

namespace KFrame
{
    public interface IReferenceDbSource : IReferenceSource
    {
        //IEnumerable<object> Read(SqlMapper.GridReader s);
        DbSourceTable Table { get; }
        string SqlKey(string select);
        string SqlMax(string date, string dateL);
        string Sql(string date = null, string dateL = null);
    }
}

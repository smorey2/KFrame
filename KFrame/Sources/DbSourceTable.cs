namespace KFrame.Sources
{
    public class DbSourceTable
    {
        public enum TableKeyType
        {
            @int = 0,
            nvarchar,
            uniqueidentifer
        }

        public DbSourceTable(string name, string key, TableKeyType keyType)
        {
            Name = name;
            Key = key;
            KeyType = keyType;
        }

        public string Name { get; set; }
        public string Key { get; set; }
        public TableKeyType KeyType { get; set; }

        public string Id => $"Id{(int)KeyType}";
    }
}

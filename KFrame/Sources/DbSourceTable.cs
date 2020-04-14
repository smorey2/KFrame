namespace KFrame.Sources
{
    /// <summary>
    /// Class DbSourceTable.
    /// </summary>
    public class DbSourceTable
    {
        /// <summary>
        /// Enum TableKeyType
        /// </summary>
        public enum TableKeyType
        {
            /// <summary>
            /// The int
            /// </summary>
            @int = 0,
            /// <summary>
            /// The nvarchar
            /// </summary>
            nvarchar,
            /// <summary>
            /// The uniqueidentifer
            /// </summary>
            uniqueidentifer
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DbSourceTable"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="key">The key.</param>
        /// <param name="keyType">Type of the key.</param>
        public DbSourceTable(string name, string key, TableKeyType keyType = TableKeyType.@int)
        {
            Name = name;
            Key = key;
            KeyType = keyType;
        }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the key.
        /// </summary>
        /// <value>The key.</value>
        public string Key { get; set; }

        /// <summary>
        /// Gets or sets the type of the key.
        /// </summary>
        /// <value>The type of the key.</value>
        public TableKeyType KeyType { get; set; }

        /// <summary>
        /// Gets the identifier.
        /// </summary>
        /// <value>The identifier.</value>
        public string Id => $"Id{(int)KeyType}";

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>A <see cref="System.String" /> that represents this instance.</returns>
        public override string ToString() => Name;
    }
}

using KFrame.Sources;

namespace KFrame
{
    /// <summary>
    /// Class KFrameOptions.
    /// </summary>
    public class KFrameOptions
    {
        /// <summary>
        /// Gets or sets the request path.
        /// </summary>
        /// <value>The request path.</value>
        public string RequestPath { get; set; } = "/@frame";
        /// <summary>
        /// Gets or sets the access token.
        /// </summary>
        /// <value>The access token.</value>
        public string AccessToken { get; set; }
        /// <summary>
        /// Gets or sets the database source.
        /// </summary>
        /// <value>The database source.</value>
        public DbSourceAbstract DbSource { get; set; }
        /// <summary>
        /// Gets or sets the kv source.
        /// </summary>
        /// <value>The kv source.</value>
        public KvSourceAbstract KvSource { get; set; }
    }
}

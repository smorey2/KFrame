using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KFrame.Sources
{
    /// <summary>
    /// Class SourceAbstract.
    /// </summary>
    public abstract class SourceAbstract
    {
        /// <summary>
        /// Gets or sets the prefix.
        /// </summary>
        /// <value>The prefix.</value>
        public string NameFormat { get; set; } = "{0}{1}";

        /// <summary>
        /// Formats the name.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>System.String.</returns>
        public string FormatName(string chapter, string name) => string.Format(NameFormat, chapter, name);

        /// <summary>
        /// Gets the i frame asynchronous.
        /// </summary>
        /// <param name="chapter">The chapter.</param>
        /// <param name="sources">The sources.</param>
        /// <returns>Task&lt;dynamic&gt;.</returns>
        public abstract Task<dynamic> GetIFrameAsync(string chapter, IEnumerable<IKFrameSource> sources);

        /// <summary>
        /// Gets the p frame asynchronous.
        /// </summary>
        /// <param name="chapter">The chapter.</param>
        /// <param name="sources">The sources.</param>
        /// <param name="iframe">The kframe.</param>
        /// <param name="expand">if set to <c>true</c> [expand].</param>
        /// <returns>Task&lt;System.ValueTuple&lt;dynamic, KFrameRepository.Check, System.String&gt;&gt;.</returns>
        public abstract Task<(dynamic data, KFrameRepository.Check check, string etag)> GetPFrameAsync(string chapter, IEnumerable<IKFrameSource> sources, DateTime iframe, bool expand);

        /// <summary>
        /// Clears the asynchronous.
        /// </summary>
        /// <param name="chapter">The chapter.</param>
        /// <param name="sources">The sources.</param>
        /// <returns>Task&lt;System.String&gt;.</returns>
        public abstract Task<string> ClearAsync(string chapter, IEnumerable<IKFrameSource> sources);

        /// <summary>
        /// Installs the asynchronous.
        /// </summary>
        /// <param name="chapter">The chapter.</param>
        /// <param name="sources">The sources.</param>
        /// <returns>Task&lt;System.String&gt;.</returns>
        public abstract Task<string> InstallAsync(string chapter, IEnumerable<IKFrameSource> sources);

        /// <summary>
        /// Uninstalls the asynchronous.
        /// </summary>
        /// <param name="chapter">The chapter.</param>
        /// <param name="sources">The sources.</param>
        /// <returns>Task&lt;System.String&gt;.</returns>
        public abstract Task<string> UninstallAsync(string chapter, IEnumerable<IKFrameSource> sources);
    }
}

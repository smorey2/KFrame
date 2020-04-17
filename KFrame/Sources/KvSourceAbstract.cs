using KFrame.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KFrame.Sources
{
    /// <summary>
    /// Class KvSourceAbstract.
    /// </summary>
    public abstract class KvSourceAbstract : SourceAbstract
    {
        /// <summary>
        /// Gets or sets the connection string.
        /// </summary>
        /// <value>The connection string.</value>
        public string ConnectionString { get; set; }

        /// <summary>
        /// Gets or sets the kv service.
        /// </summary>
        /// <value>The kv service.</value>
        public IKvService KvService { get; set; } = new KvService();

        /// <summary>
        /// get i frame as an asynchronous operation.
        /// </summary>
        /// <param name="chapter">The chapter.</param>
        /// <param name="sources">The sources.</param>
        /// <returns>Task&lt;dynamic&gt;.</returns>
        /// <exception cref="ArgumentNullException">KvService</exception>
        /// <exception cref="ArgumentNullException">ConnectionString</exception>
        public override Task<dynamic> GetIFrameAsync(string chapter, IEnumerable<IKFrameSource> sources)
        {
            if (KvService == null)
                throw new ArgumentNullException(nameof(KvService));
            if (string.IsNullOrEmpty(ConnectionString))
                throw new ArgumentNullException(nameof(ConnectionString));
            using (var ctx = KvService.GetConnection(ConnectionString))
                return Task.FromResult<dynamic>(null);
        }

        /// <summary>
        /// Gets the p frame asynchronous.
        /// </summary>
        /// <param name="chapeter">The chapeter.</param>
        /// <param name="sources">The sources.</param>
        /// <param name="iframe">The kframe.</param>
        /// <param name="expand">if set to <c>true</c> [expand].</param>
        /// <returns>Task&lt;System.ValueTuple&lt;dynamic, KFrameRepository.Check, System.String&gt;&gt;.</returns>
        /// <exception cref="ArgumentNullException">KvService</exception>
        /// <exception cref="ArgumentNullException">ConnectionString</exception>
        public override Task<(dynamic data, KFrameRepository.Check check, string etag)> GetPFrameAsync(string chapeter, IEnumerable<IKFrameSource> sources, DateTime iframe, bool expand)
        {
            if (KvService == null)
                throw new ArgumentNullException(nameof(KvService));
            if (string.IsNullOrEmpty(ConnectionString))
                throw new ArgumentNullException(nameof(ConnectionString));
            var kframeL = iframe.ToLocalTime();
            using (var ctx = KvService.GetConnection(ConnectionString))
                return Task.FromResult<(dynamic data, KFrameRepository.Check check, string etag)>((null, null, null));
        }

        /// <summary>
        /// Clears the asynchronous.
        /// </summary>
        /// <param name="b">The b.</param>
        /// <param name="ctx">The CTX.</param>
        /// <param name="chapter">The chapter.</param>
        /// <param name="sources">The sources.</param>
        /// <returns>Task.</returns>
        protected abstract Task ClearAsync(StringBuilder b, object ctx, string chapter, IEnumerable<IKFrameKvSource> sources);

        /// <summary>
        /// clear as an asynchronous operation.
        /// </summary>
        /// <param name="chapter">The chapter.</param>
        /// <param name="sources">The sources.</param>
        /// <returns>Task&lt;System.String&gt;.</returns>
        /// <exception cref="ArgumentNullException">KvService</exception>
        /// <exception cref="ArgumentNullException">ConnectionString</exception>
        public override async Task<string> ClearAsync(string chapter, IEnumerable<IKFrameSource> sources)
        {
            if (KvService == null)
                throw new ArgumentNullException(nameof(KvService));
            if (string.IsNullOrEmpty(ConnectionString))
                throw new ArgumentNullException(nameof(ConnectionString));
            var b = new StringBuilder();
            using (var ctx = KvService.GetConnection(ConnectionString))
                await InstallAsync(b, ctx, chapter, sources.Cast<IKFrameKvSource>());
            b.AppendLine("DONE");
            return b.ToString();
        }

        /// <summary>
        /// Kvs the install asynchronous.
        /// </summary>
        /// <param name="b">The b.</param>
        /// <param name="ctx">The CTX.</param>
        /// <param name="chapter">The chapter.</param>
        /// <param name="sources">The sources.</param>
        /// <returns>Task.</returns>
        protected abstract Task InstallAsync(StringBuilder b, object ctx, string chapter, IEnumerable<IKFrameKvSource> sources);

        /// <summary>
        /// kv install as an asynchronous operation.
        /// </summary>
        /// <param name="chapter">The chapter.</param>
        /// <param name="sources">The sources.</param>
        /// <returns>Task&lt;System.String&gt;.</returns>
        /// <exception cref="ArgumentNullException">KvService</exception>
        /// <exception cref="ArgumentNullException">ConnectionString</exception>
        public override async Task<string> InstallAsync(string chapter, IEnumerable<IKFrameSource> sources)
        {
            if (KvService == null)
                throw new ArgumentNullException(nameof(KvService));
            if (string.IsNullOrEmpty(ConnectionString))
                throw new ArgumentNullException(nameof(ConnectionString));
            var b = new StringBuilder();
            using (var ctx = KvService.GetConnection(ConnectionString))
                await InstallAsync(b, ctx, chapter, sources.Cast<IKFrameKvSource>());
            b.AppendLine("DONE");
            return b.ToString();
        }

        /// <summary>
        /// Kvs the uninstall asynchronous.
        /// </summary>
        /// <param name="b">The b.</param>
        /// <param name="ctx">The CTX.</param>
        /// <param name="chapter">The chapter.</param>
        /// <param name="sources">The sources.</param>
        /// <returns>Task.</returns>
        protected abstract Task UninstallAsync(StringBuilder b, object ctx, string chapter, IEnumerable<IKFrameKvSource> sources);

        /// <summary>
        /// kv uninstall as an asynchronous operation.
        /// </summary>
        /// <param name="chapter">The chapter.</param>
        /// <param name="sources">The sources.</param>
        /// <returns>Task&lt;System.String&gt;.</returns>
        /// <exception cref="ArgumentNullException">KvService</exception>
        /// <exception cref="ArgumentNullException">ConnectionString</exception>
        public override async Task<string> UninstallAsync(string chapter, IEnumerable<IKFrameSource> sources)
        {
            if (KvService == null)
                throw new ArgumentNullException(nameof(KvService));
            if (string.IsNullOrEmpty(ConnectionString))
                throw new ArgumentNullException(nameof(ConnectionString));
            var b = new StringBuilder();
            using (var ctx = KvService.GetConnection(ConnectionString))
                await UninstallAsync(b, ctx, chapter, sources.Cast<IKFrameKvSource>());
            b.AppendLine("DONE");
            return b.ToString();
        }
    }
}

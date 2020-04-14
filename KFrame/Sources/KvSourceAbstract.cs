using KFrame.Services;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using System;
using Microsoft.Extensions.Caching.Memory;

namespace KFrame.Sources
{
    /// <summary>
    /// Class KvSourceAbstract.
    /// </summary>
    public abstract class KvSourceAbstract
    {
        /// <summary>
        /// Gets or sets the connection string.
        /// </summary>
        /// <value>The connection string.</value>
        public string ConnectionString { get; set; }

        /// <summary>
        /// Gets or sets the prefix.
        /// </summary>
        /// <value>The prefix.</value>
        public string Prefix { get; set; }

        /// <summary>
        /// Gets or sets the kv service.
        /// </summary>
        /// <value>The kv service.</value>
        public IKvService KvService { get; set; } = new KvService();

        /// <summary>
        /// get k frame as an asynchronous operation.
        /// </summary>
        /// <param name="sources">The sources.</param>
        /// <returns>Task&lt;dynamic&gt;.</returns>
        public Task<dynamic> GetKFrameAsync(IReferenceKvSource[] sources)
        {
            using (var ctx = KvService.GetConnection(ConnectionString))
            {
                return Task.FromResult<dynamic>(null);
            }
        }

        /// <summary>
        /// get i frame as an asynchronous operation.
        /// </summary>
        /// <param name="sources">The sources.</param>
        /// <param name="kframe">The kframe.</param>
        /// <param name="expand">if set to <c>true</c> [expand].</param>
        /// <returns>Task&lt;MemoryCacheResult&gt;.</returns>
        public Task<MemoryCacheResult> GetIFrameAsync(IReferenceKvSource[] sources, DateTime kframe, bool expand)
        {
            var kframeL = kframe.ToLocalTime();
            using (var ctx = KvService.GetConnection(ConnectionString))
            {
                return Task.FromResult(new MemoryCacheResult(null)
                {
                });
            }
        }

        /// <summary>
        /// Kvs the install asynchronous.
        /// </summary>
        /// <param name="b">The b.</param>
        /// <param name="ctx">The CTX.</param>
        /// <param name="sources">The sources.</param>
        /// <returns>Task.</returns>
        protected abstract Task KvInstallAsync(StringBuilder b, object ctx, IReferenceKvSource[] sources);

        /// <summary>
        /// kv install as an asynchronous operation.
        /// </summary>
        /// <param name="sources">The sources.</param>
        /// <returns>Task&lt;System.String&gt;.</returns>
        public async Task<string> KvInstallAsync(IReferenceKvSource[] sources)
        {
            var b = new StringBuilder();
            using (var ctx = KvService.GetConnection(ConnectionString))
                await KvInstallAsync(b, ctx, sources.Cast<IReferenceKvSource>().Where(x => x != null).ToArray());
            b.AppendLine("DONE");
            return b.ToString();
        }

        /// <summary>
        /// Kvs the uninstall asynchronous.
        /// </summary>
        /// <param name="b">The b.</param>
        /// <param name="ctx">The CTX.</param>
        /// <param name="sources">The sources.</param>
        /// <returns>Task.</returns>
        protected abstract Task KvUninstallAsync(StringBuilder b, object ctx, IReferenceKvSource[] sources);

        /// <summary>
        /// kv uninstall as an asynchronous operation.
        /// </summary>
        /// <param name="sources">The sources.</param>
        /// <returns>Task&lt;System.String&gt;.</returns>
        public async Task<string> KvUninstallAsync(IReferenceKvSource[] sources)
        {
            var b = new StringBuilder();
            using (var ctx = KvService.GetConnection(ConnectionString))
                await KvUninstallAsync(b, ctx, sources.Cast<IReferenceKvSource>().Where(x => x != null).ToArray());
            b.AppendLine("DONE");
            return b.ToString();
        }
    }
}

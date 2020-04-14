using System;
using System.Text;
using System.Threading.Tasks;

namespace KFrame.Sources
{
    /// <summary>
    /// Class KvSourceRedis.
    /// Implements the <see cref="KFrame.Sources.KvSourceAbstract" />
    /// </summary>
    /// <seealso cref="KFrame.Sources.KvSourceAbstract" />
    public class KvSourceRedis : KvSourceAbstract
    {
        /// <summary>
        /// Kvs the install asynchronous.
        /// </summary>
        /// <param name="b">The b.</param>
        /// <param name="ctx">The CTX.</param>
        /// <param name="sources">The sources.</param>
        /// <returns>Task.</returns>
        /// <exception cref="System.NotImplementedException"></exception>
        protected override Task KvInstallAsync(StringBuilder b, object ctx, IReferenceKvSource[] sources) => throw new NotImplementedException();

        /// <summary>
        /// Kvs the uninstall asynchronous.
        /// </summary>
        /// <param name="b">The b.</param>
        /// <param name="ctx">The CTX.</param>
        /// <param name="sources">The sources.</param>
        /// <returns>Task.</returns>
        /// <exception cref="System.NotImplementedException"></exception>
        protected override Task KvUninstallAsync(StringBuilder b, object ctx, IReferenceKvSource[] sources) => throw new NotImplementedException();
    }
}

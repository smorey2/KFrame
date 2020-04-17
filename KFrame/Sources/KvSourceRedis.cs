using System;
using System.Collections.Generic;
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
        /// Clears the asynchronous.
        /// </summary>
        /// <param name="b">The b.</param>
        /// <param name="ctx">The CTX.</param>
        /// <param name="chapter">The chapter.</param>
        /// <param name="sources">The sources.</param>
        /// <returns>Task.</returns>
        /// <exception cref="NotImplementedException"></exception>
        protected override Task ClearAsync(StringBuilder b, object ctx, string chapter, IEnumerable<IKFrameKvSource> sources) => throw new NotImplementedException();

        /// <summary>
        /// Kvs the install asynchronous.
        /// </summary>
        /// <param name="b">The b.</param>
        /// <param name="ctx">The CTX.</param>
        /// <param name="chapter">The chapter.</param>
        /// <param name="sources">The sources.</param>
        /// <returns>Task.</returns>
        /// <exception cref="NotImplementedException"></exception>
        protected override Task InstallAsync(StringBuilder b, object ctx, string chapter, IEnumerable<IKFrameKvSource> sources) => throw new NotImplementedException();

        /// <summary>
        /// Kvs the uninstall asynchronous.
        /// </summary>
        /// <param name="b">The b.</param>
        /// <param name="ctx">The CTX.</param>
        /// <param name="chapter">The chapter.</param>
        /// <param name="sources">The sources.</param>
        /// <returns>Task.</returns>
        /// <exception cref="NotImplementedException"></exception>
        protected override Task UninstallAsync(StringBuilder b, object ctx, string chapter, IEnumerable<IKFrameKvSource> sources) => throw new NotImplementedException();
    }
}

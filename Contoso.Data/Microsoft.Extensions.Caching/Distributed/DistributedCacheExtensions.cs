using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.IO;

namespace Microsoft.Extensions.Caching.Distributed
{
    public static class DistributedCacheExtensions
    {
        public static IEnumerable<IChangeToken> CreateCacheEntryChangeTokens(this IDistributedCache cache, IEnumerable<string> keys)
        {
            return null;
        }

        public static bool Contains(this IDistributedCache cache, string key) => throw new NotSupportedException();

        /// <summary>
        /// Expire the cache entry if the given <see cref="IChangeToken"/> expires.
        /// </summary>
        /// <param name="options">The <see cref="MemoryCacheEntryOptions"/>.</param>
        /// <param name="expirationToken">The <see cref="IChangeToken"/> that causes the cache entry to expire.</param>
        /// <returns>The <see cref="MemoryCacheEntryOptions"/> so that additional calls can be chained.</returns>
        public static DistributedCacheEntryOptions AddExpirationToken(
            this DistributedCacheEntryOptions2 options,
            IChangeToken expirationToken)
        {
            if (expirationToken == null)
                throw new ArgumentNullException(nameof(expirationToken));
            options.ExpirationTokens.Add(expirationToken);
            return options;
        }

        public static void Set<TItem>(this IDistributedCache cache, string key, byte[] value, FileCacheDependency dependency)
        {
            var fileInfo = new FileInfo(dependency.FileName);
            var fileProvider = new PhysicalFileProvider(fileInfo.DirectoryName);
            cache.Set(key, value, new DistributedCacheEntryOptions2().AddExpirationToken(fileProvider.Watch(fileInfo.Name)));
        }
    }
}

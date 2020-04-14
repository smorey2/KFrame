using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;
using System.Collections.Generic;
using System.IO;

namespace Microsoft.Extensions.Caching.Memory
{
    public static class MemoryCacheExtensions
    {
        public static IEnumerable<IChangeToken> CreateCacheEntryChangeTokens(this IMemoryCache cache, IEnumerable<string> keys)
        {
            return null;
        }

        public static bool Contains(this IMemoryCache cache, string key) => cache.TryGetValue(key, out var dummy);

        public static void Set<TItem>(this IMemoryCache cache, string key, TItem value, FileCacheDependency dependency)
        {
            var fileInfo = new FileInfo(dependency.FileName);
            var fileProvider = new PhysicalFileProvider(fileInfo.DirectoryName);
            cache.Set(key, value, new MemoryCacheEntryOptions().AddExpirationToken(fileProvider.Watch(fileInfo.Name)));
        }
    }
}

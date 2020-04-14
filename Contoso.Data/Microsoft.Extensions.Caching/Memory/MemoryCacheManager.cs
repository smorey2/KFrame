using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.Caching.Memory
{
    public static class MemoryCacheManager
    {
        static readonly ReaderWriterLockSlim _rwLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
        const string RegionMarker = "@";
        readonly static object EmptyValue = new object();

        static class CacheFile
        {
            static string GetFilePathForName(string name) => !string.IsNullOrEmpty(FileCacheDependency.Directory) ? Path.Combine(FileCacheDependency.Directory, name + ".txt") : null;
            static void WriteBodyForName(string name, string path) => File.WriteAllText(path, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "\r\n");

            public static void Touch(string name)
            {
                if (string.IsNullOrEmpty(FileCacheDependency.Directory))
                    return;
                lock (_rwLock)
                {
                    var filePath = GetFilePathForName(name);
                    if (filePath == null)
                        return;
                    try { WriteBodyForName(name, filePath); }
                    catch { };
                }
            }

            public static IEnumerable<IChangeToken> MakeChangeTokens(IEnumerable<string> names)
            {
                if (string.IsNullOrEmpty(FileCacheDependency.Directory))
                    return null;
                if (!Directory.Exists(FileCacheDependency.Directory))
                    Directory.CreateDirectory(FileCacheDependency.Directory);
                var fileProvider = new PhysicalFileProvider(FileCacheDependency.Directory);
                lock (_rwLock)
                {
                    var changeTokens = new List<IChangeToken>();
                    foreach (var name in names)
                    {
                        var filePath = GetFilePathForName(name);
                        if (filePath == null)
                            continue;
                        try
                        {
                            var filePathAsDirectory = Path.GetDirectoryName(filePath);
                            if (!Directory.Exists(filePathAsDirectory))
                                Directory.CreateDirectory(filePathAsDirectory);
                            if (!File.Exists(filePath))
                                WriteBodyForName(name, filePath);
                        }
                        catch { };
                        changeTokens.Add(fileProvider.Watch(filePath));
                    }
                    return changeTokens;
                }
            }
        }

        public static void Remove(this IMemoryCache cache, MemoryCacheRegistration key, params object[] values) => cache.Remove(key.GetNamespace(values));
        public static bool Contains(this IMemoryCache cache, MemoryCacheRegistration key, params object[] values) => cache.Contains(key.GetNamespace(values)); 

        public static void Touch(this IMemoryCache cache, string[] names)
        {
            if (names == null || names.Length == 0)
                return;
            foreach (var name in names)
            {
                if (name.StartsWith("#"))
                {
                    CacheFile.Touch(name);
                    continue;
                }
                cache.Set(name, EmptyValue, new MemoryCacheEntryOptions().SetAbsoluteExpiration(DateTimeOffset.MaxValue));
            }
        }

        public static T Get<T>(this IMemoryCache cache, MemoryCacheRegistration key, object tag, params object[] values) => GetOrCreateUsingLock<T>(cache, key ?? throw new ArgumentNullException(nameof(key)), tag, values);
        public static MemoryCacheResult GetResult(this IMemoryCache cache, MemoryCacheRegistration key, object tag, params object[] values) => Get<MemoryCacheResult>(cache, key, tag, values);
        public static async Task<T> GetAsync<T>(this IMemoryCache cache, MemoryCacheRegistration key, object tag, params object[] values) => await GetOrCreateUsingLockAsync<T>(cache, key ?? throw new ArgumentNullException(nameof(key)), tag, values);
        public static async Task<MemoryCacheResult> GetResultAsync(this IMemoryCache cache, MemoryCacheRegistration key, object tag, params object[] values) => await GetAsync<MemoryCacheResult>(cache, key, tag, values);

        public static void Set(this IMemoryCache cache, string name, object value, Action<MemoryCacheEntryOptions> entryOptions)
        {
            var rwLock = _rwLock;
            rwLock.EnterWriteLock();
            try
            {
                if (value is MemoryCacheResult valueResult)
                {
                    entryOptions?.Invoke(valueResult.EntryOptions);
                    SetInsideLock(cache, name, valueResult);
                }
                else throw new InvalidOperationException("Not Service Cache Result");
            }
            finally { rwLock.ExitWriteLock(); }
        }

        static IEnumerable<IChangeToken> MakeChangeTokens(IMemoryCache cache, object tag, IEnumerable<string> names)
        {
            return names.Select(x =>
            {
                if (x.StartsWith("#")) return new { name = x.Substring(1), regionName = "#" };
                var name = x;
                // add anchor name if not exists
                if (cache.Get(name) == null) cache.Set(name, EmptyValue, new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.MaxValue));
                return new { name, regionName = string.Empty };
            }).GroupBy(x => x.regionName).SelectMany(x =>
            {
                if (x.Key == "#") return CacheFile.MakeChangeTokens(x.Select(y => y.name));
                return cache.CreateCacheEntryChangeTokens(x.Select(y => y.name));
            }).Where(x => x != null).ToList();
        }


        static T GetOrCreateUsingLock<T>(IMemoryCache cache, MemoryCacheRegistration key, object tag, object[] values)
        {
            var rwLock = key._rwLock ?? _rwLock;
            var name = key.GetNamespace(values);
            rwLock.EnterUpgradeableReadLock();
            var notCacheResult = typeof(T) != typeof(MemoryCacheResult);
            try
            {
                // double lock test
                var value = cache.Get(name);
                if (value != null)
                    return notCacheResult && value is MemoryCacheResult cacheResult ? (T)cacheResult.Result : (T)value;
                rwLock.EnterWriteLock();
                try
                {
                    value = cache.Get(name);
                    if (value != null)
                        return notCacheResult && value is MemoryCacheResult cacheResult ? (T)cacheResult.Result : (T)value;
                    // create value
                    value = key.BuilderAsync != null ? Task.Run(() => CreateValueAsync<T>(key, tag, values)).Result : CreateValue<T>(key, tag, values);
                    //var entryOptions = key.EntryOptions is MemoryCacheEntryOptions2 entryOptions2 ? entryOptions2.ToEntryOptions() : key.EntryOptions;
                    var entryOptions = key.EntryOptions;
                    if (key.CacheTags != null)
                    {
                        var tags = key.CacheTags(tag, values);
                        if (tags != null && tags.Any())
                            ((List<IChangeToken>)entryOptions.ExpirationTokens).AddRange(MakeChangeTokens(cache, tag, tags));
                    }
                    // add value
                    var valueAsResult = value is MemoryCacheResult result ? result : new MemoryCacheResult(value);
                    valueAsResult.WeakTag = new WeakReference(tag);
                    valueAsResult.Key = key;
                    valueAsResult.EntryOptions = entryOptions;
                    SetInsideLock(cache, name, valueAsResult);
                    return (T)value;
                }
                finally { rwLock.ExitWriteLock(); }
            }
            finally { rwLock.ExitUpgradeableReadLock(); }
        }

        static Task<T> GetOrCreateUsingLockAsync<T>(IMemoryCache cache, MemoryCacheRegistration key, object tag, object[] values)
        {
            var rwLock = key._rwLock ?? _rwLock;
            var name = key.GetNamespace(values);
            rwLock.EnterUpgradeableReadLock();
            var notCacheResult = typeof(T) != typeof(MemoryCacheResult);
            try
            {
                // double lock test
                var value = cache.Get(name);
                if (value != null)
                    return Task.FromResult(notCacheResult && value is MemoryCacheResult cacheResult ? (T)cacheResult.Result : (T)value);
                rwLock.EnterWriteLock();
                try
                {
                    value = cache.Get(name);
                    if (value != null)
                        return Task.FromResult(notCacheResult && value is MemoryCacheResult cacheResult ? (T)cacheResult.Result : (T)value);
                    // create value
                    value = key.BuilderAsync != null ? Task.Run(() => CreateValueAsync<T>(key, tag, values)).Result : CreateValue<T>(key, tag, values);
                    //var entryOptions = key.EntryOptions is MemoryCacheEntryOptions2 entryOptions2 ? entryOptions2.ToEntryOptions() : key.EntryOptions;
                    var entryOptions = key.EntryOptions;
                    if (key.CacheTags != null)
                    {
                        var tags = key.CacheTags(tag, values);
                        if (tags != null && tags.Any())
                            ((List<IChangeToken>)entryOptions.ExpirationTokens).AddRange(MakeChangeTokens(cache, tag, tags));
                    }
                    // add value
                    var valueAsResult = value is MemoryCacheResult result ? result : new MemoryCacheResult(value);
                    valueAsResult.WeakTag = new WeakReference(tag);
                    valueAsResult.Key = key;
                    valueAsResult.EntryOptions = entryOptions;
                    SetInsideLock(cache, name, valueAsResult);
                    return Task.FromResult((T)value);
                }
                finally { rwLock.ExitWriteLock(); }
            }
            finally { rwLock.ExitUpgradeableReadLock(); }
        }

        static void SetInsideLock(IMemoryCache cache, string name, MemoryCacheResult value)
        {
            try { cache.Set(name, value, value.EntryOptions); }
            catch (InvalidOperationException) { }
            catch (Exception e) { Console.WriteLine(e); }
            finally
            {
                if (!string.IsNullOrEmpty(value.ETag))
                {
                    var etagName = value.Key.GetNamespace(new[] { value.ETag });
                    var etagEntryOptions = new MemoryCacheEntryOptions().SetAbsoluteExpiration(DateTimeOffset.MaxValue);
                    ((List<IChangeToken>)etagEntryOptions.ExpirationTokens).AddRange(cache.CreateCacheEntryChangeTokens(new[] { name }));
                    // ensure base is still exists, then add
                    var baseValue = cache.Get(name);
                    if (baseValue != null)
                        cache.Set(etagName, name, etagEntryOptions);
                }
            }
        }

        static T CreateValue<T>(MemoryCacheRegistration key, object tag, object[] values) => (T)key.Builder(tag, values);
        static async Task<T> CreateValueAsync<T>(MemoryCacheRegistration key, object tag, object[] values) => (T)await key.BuilderAsync(tag, values);
    }
}

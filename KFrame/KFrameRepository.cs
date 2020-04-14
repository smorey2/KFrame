using Contoso.Data.Services;
using Dapper;
using KFrame.Sources;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace KFrame
{
    /// <summary>
    /// Interface IKFrameRepository
    /// </summary>
    public interface IKFrameRepository
    {
        /// <summary>
        /// Databases the install asynchronous.
        /// </summary>
        /// <returns>Task&lt;System.String&gt;.</returns>
        Task<string> DbInstallAsync();
        /// <summary>
        /// Databases the uninstall asynchronous.
        /// </summary>
        /// <returns>Task&lt;System.String&gt;.</returns>
        Task<string> DbUninstallAsync();
        /// <summary>
        /// Kvs the install asynchronous.
        /// </summary>
        /// <returns>Task&lt;System.String&gt;.</returns>
        Task<string> KvInstallAsync();
        /// <summary>
        /// Kvs the uninstall asynchronous.
        /// </summary>
        /// <returns>Task&lt;System.String&gt;.</returns>
        Task<string> KvUninstallAsync();
        /// <summary>
        /// Gets the k frame asynchronous.
        /// </summary>
        /// <returns>Task&lt;System.Object&gt;.</returns>
        Task<object> GetKFrameAsync();
        /// <summary>
        /// Gets the i frame asynchronous.
        /// </summary>
        /// <param name="kframe">The kframe.</param>
        /// <returns>Task&lt;MemoryCacheResult&gt;.</returns>
        Task<MemoryCacheResult> GetIFrameAsync(long kframe);
        /// <summary>
        /// Determines whether [has i frame] [the specified etag].
        /// </summary>
        /// <param name="etag">The etag.</param>
        /// <returns><c>true</c> if [has i frame] [the specified etag]; otherwise, <c>false</c>.</returns>
        bool HasIFrame(string etag);
    }

    /// <summary>
    /// Class KFrameRepository.
    /// Implements the <see cref="KFrame.IKFrameRepository" />
    /// </summary>
    /// <seealso cref="KFrame.IKFrameRepository" />
    public class KFrameRepository : IKFrameRepository
    {
        readonly IMemoryCache _cache;

        /// <summary>
        /// Initializes a new instance of the <see cref="KFrameRepository" /> class.
        /// </summary>
        /// <param name="cache">The cache.</param>
        /// <param name="options">The options.</param>
        /// <param name="assemblys">The assemblys.</param>
        public KFrameRepository(IMemoryCache cache, KFrameOptions options, IEnumerable<Assembly> assemblys)
            : this(cache, options, FindSourcesFromAssembly(assemblys)) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="KFrameRepository"/> class.
        /// </summary>
        /// <param name="cache">The cache.</param>
        /// <param name="options">The options.</param>
        /// <param name="sources">The sources.</param>
        public KFrameRepository(IMemoryCache cache, KFrameOptions options, IReferenceSource[] sources)
        {
            _cache = cache;
            Options = options;
            Sources = sources;
        }

        #region Cache

        /// <summary>
        /// Class _del_.
        /// Implements the <see cref="KFrame.KFrameRepository.IKey" />
        /// </summary>
        /// <seealso cref="KFrame.KFrameRepository.IKey" />
        public class _del_ : Reference.IKey
        {
            public string id { get; set; }
            public string t { get; set; }
        }

        readonly static MemoryCacheRegistration KFrame = new MemoryCacheRegistration(nameof(KFrame), new MemoryCacheEntryOptions
        {
            AbsoluteExpiration = DateTime.Today.AddDays(1),
        }, async (tag, values) =>
        {
            var parent = (KFrameRepository)tag;
            // db-source
            var dbSources = parent.Sources.OfType<IReferenceDbSource>().ToArray();
            if (dbSources.Length > 0)
            {
                var dbSource = parent.Options.DbSource ?? throw new InvalidOperationException($"{nameof(KFrameOptions.DbSource)} not set");
                return await dbSource.GetKFrameAsync(dbSources);
            }
            // kv-source
            var kvSources = parent.Sources.OfType<IReferenceKvSource>().ToArray();
            if (kvSources.Length > 0)
            {
                var kvSource = parent.Options.KvSource ?? throw new InvalidOperationException($"{nameof(KFrameOptions.KvSource)} not set");
                return await kvSource.GetKFrameAsync(kvSources);
            }
            // none found
            throw new InvalidOperationException($"IReferenceSource(s) not found");
        }, "#KFrame");

        readonly static MemoryCacheRegistration IFrame = new MemoryCacheRegistration(nameof(IFrame), AddRemovedCallback(new MemoryCacheEntryOptions
        {
            AbsoluteExpiration = DateTime.Now.AddMinutes(1),
        }), async (tag, values) =>
        {
            var parent = (KFrameRepository)tag;
            var kframe = new DateTime((long)values[0]);
            // db-source
            var dbSources = parent.Sources.OfType<IReferenceDbSource>().ToArray();
            if (dbSources.Length > 0)
            {
                var dbSource = parent.Options.DbSource ?? throw new InvalidOperationException($"{nameof(KFrameOptions.DbSource)} not set");
                return await dbSource.GetIFrameAsync(dbSources, kframe, true);
            }
            // kv-source
            var kvSources = parent.Sources.OfType<IReferenceKvSource>().ToArray();
            if (kvSources.Length > 0)
            {
                var kvSource = parent.Options.KvSource ?? throw new InvalidOperationException($"{nameof(KFrameOptions.KvSource)} not set");
                return await kvSource.GetIFrameAsync(kvSources, kframe, true);
            }
            // none found
            throw new InvalidOperationException($"IReferenceSource(s) not found");
        }, "#KFrame");

        /// <summary>
        /// Struct TagCheck
        /// </summary>
        public struct TagCheck
        {
            public DateTime KFrame;
            public int[] Keys;
            public DateTime MaxDate;

            public override bool Equals(object obj) => obj is TagCheck b ? Keys.SequenceEqual(b.Keys) && MaxDate == b.MaxDate : false;
            public override int GetHashCode() => KFrame.GetHashCode() ^ Keys.GetHashCode() ^ MaxDate.GetHashCode();
            public static bool operator ==(TagCheck a, TagCheck b) => a.Equals(b);
            public static bool operator !=(TagCheck a, TagCheck b) => !a.Equals(b);
        }

        static MemoryCacheEntryOptions AddRemovedCallback(MemoryCacheEntryOptions options)
        {
            options.RegisterPostEvictionCallback(async (key, value, reason, state) =>
            {
                var parent = (KFrameRepository)state;
                var result = value as MemoryCacheResult;
                if (parent == null || result == null || result.Tag == null)
                    return;
                var tagCheck = (TagCheck)result.Tag;
                // db-source
                var dbSources = parent.Sources.OfType<IReferenceDbSource>().ToArray();
                if (dbSources.Length > 0)
                {
                    var dbSource = parent.Options.DbSource ?? throw new InvalidOperationException($"{nameof(KFrameOptions.DbSource)} not set");
                    var tagCheck2 = (TagCheck)(await dbSource.GetIFrameAsync(dbSources, tagCheck.KFrame, false)).Tag;
                    if (tagCheck == tagCheck2)
                        parent._cache.Set(key, value, new MemoryCacheEntryOptions().SetAbsoluteExpiration(DateTime.Now.AddMinutes(1)));
                    return;
                }
                // kv-source
                var kvSources = parent.Sources.OfType<IReferenceKvSource>().ToArray();
                if (kvSources.Length > 0)
                {
                    var kvSource = parent.Options.KvSource ?? throw new InvalidOperationException($"{nameof(KFrameOptions.KvSource)} not set");
                    var tagCheck2 = (TagCheck)(await kvSource.GetIFrameAsync(kvSources, tagCheck.KFrame, false)).Tag;
                    if (tagCheck == tagCheck2)
                        parent._cache.Set(key, value, new MemoryCacheEntryOptions().SetAbsoluteExpiration(DateTime.Now.AddMinutes(1)));
                    return;
                }
            }, null);
            return options;
        }

        readonly static MemoryCacheRegistration MergedFrame = new MemoryCacheRegistration(nameof(MergedFrame), 10, (tag, values) =>
        {
            var parent = (KFrameRepository)tag;
            var kframe = (IDictionary<string, object>)parent._cache.Get<dynamic>(KFrame, parent);
            var iframe = (IDictionary<string, object>)parent._cache.GetResult(IFrame, parent, (long)kframe["frame"]).Result;
            var idels = (List<_del_>)iframe["del"];
            var result = (IDictionary<string, object>)new ExpandoObject();
            foreach (var source in parent.Sources)
            {
                var kps = ((IEnumerable<object>)kframe[source.Param.key]).Cast<Reference.IKey>().ToList();
                var ips = ((IEnumerable<object>)iframe[source.Param.key]).Cast<Reference.IKey>().ToList();
                if (kps.Count == 0 && ips.Count == 0)
                    continue;
                var ipsdelsById = idels.Where(x => x.t == source.Param.key).ToDictionary(x => x.id);
                var ipsById = ips.ToDictionary(x => x.id);
                var p = kps.Where(x => !ipsdelsById.ContainsKey(x.id) && !ipsById.ContainsKey(x.id)).Union(ips).ToList();
                result.Add(source.Param.key, p.ToDictionary(x => x.id));
            }
            return (dynamic)result;
        }, "#KFrame");

        #endregion

        /// <summary>
        /// Gets or sets the sources.
        /// </summary>
        /// <value>The sources.</value>
        public IReferenceSource[] Sources { get; set; }

        /// <summary>
        /// Gets or sets the options.
        /// </summary>
        /// <value>The options.</value>
        public KFrameOptions Options { get; set; }

        /// <summary>
        /// database install as an asynchronous operation.
        /// </summary>
        /// <returns>Task&lt;System.String&gt;.</returns>
        public async Task<string> DbInstallAsync() => await Options.DbSource.DbInstallAsync(Sources.OfType<IReferenceDbSource>().ToArray());

        /// <summary>
        /// database uninstall as an asynchronous operation.
        /// </summary>
        /// <returns>Task&lt;System.String&gt;.</returns>
        public async Task<string> DbUninstallAsync() => await Options.DbSource.DbUninstallAsync(Sources.OfType<IReferenceDbSource>().ToArray());

        /// <summary>
        /// kv install as an asynchronous operation.
        /// </summary>
        /// <returns>Task&lt;System.String&gt;.</returns>
        public async Task<string> KvInstallAsync() => await Options.KvSource.KvInstallAsync(Sources.OfType<IReferenceKvSource>().ToArray());

        /// <summary>
        /// kv uninstall as an asynchronous operation.
        /// </summary>
        /// <returns>Task&lt;System.String&gt;.</returns>
        public async Task<string> KvUninstallAsync() => await Options.KvSource.KvUninstallAsync(Sources.OfType<IReferenceKvSource>().ToArray());

        /// <summary>
        /// get k frame as an asynchronous operation.
        /// </summary>
        /// <returns>Task&lt;dynamic&gt;.</returns>
        public async Task<dynamic> GetKFrameAsync() => await _cache.GetAsync<dynamic>(KFrame, this);

        /// <summary>
        /// get i frame as an asynchronous operation.
        /// </summary>
        /// <param name="kframe">The kframe.</param>
        /// <returns>Task&lt;MemoryCacheResult&gt;.</returns>
        public async Task<MemoryCacheResult> GetIFrameAsync(long kframe) => await _cache.GetResultAsync(IFrame, this, kframe);

        /// <summary>
        /// Determines whether [has i frame] [the specified etag].
        /// </summary>
        /// <param name="etag">The etag.</param>
        /// <returns><c>true</c> if [has i frame] [the specified etag]; otherwise, <c>false</c>.</returns>
        public bool HasIFrame(string etag) => _cache.Contains(IFrame, etag);

        /// <summary>
        /// get merged frame as an asynchronous operation.
        /// </summary>
        /// <returns>Task&lt;dynamic&gt;.</returns>
        public async Task<dynamic> GetMergedFrameAsync() => await _cache.GetAsync<dynamic>(MergedFrame, this);

        /// <summary>
        /// Finds the sources from assembly.
        /// </summary>
        /// <param name="assemblysToScan">The assemblys to scan.</param>
        /// <param name="condition">The condition.</param>
        /// <returns>IReferenceSource[].</returns>
        public static IReferenceSource[] FindSourcesFromAssembly(IEnumerable<Assembly> assemblysToScan, Predicate<Type> condition) =>
            assemblysToScan.SelectMany(a => a.GetTypes().Where(t => condition(t))
                .Select(t => (IReferenceSource)Activator.CreateInstance(t))).ToArray();

        /// <summary>
        /// Finds the sources from assembly.
        /// </summary>
        /// <param name="assemblysToScan">The assemblys to scan.</param>
        /// <param name="excludes">The excludes.</param>
        /// <returns>IReferenceSource[].</returns>
        public static IReferenceSource[] FindSourcesFromAssembly(IEnumerable<Assembly> assemblysToScan, params Type[] excludes) =>
            FindSourcesFromAssembly(assemblysToScan, x => !x.IsAbstract && !x.IsInterface && typeof(IReferenceSource).IsAssignableFrom(x) && !excludes.Contains(x));
    }
}

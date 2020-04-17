using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace KFrame
{
    /// <summary>
    /// Interface IKFrameRepository
    /// </summary>
    public interface IKFrameRepository
    {
        /// <summary>
        /// Installs the asynchronous.
        /// </summary>
        /// <param name="accessCode">The access code.</param>
        /// <returns>Task&lt;System.String&gt;.</returns>
        Task<string> ClearAsync(string accessCode);
        /// <summary>
        /// Installs the asynchronous.
        /// </summary>
        /// <param name="accessCode">The access code.</param>
        /// <returns>Task&lt;System.String&gt;.</returns>
        Task<string> InstallAsync(string accessCode);
        /// <summary>
        /// Uninstalls the asynchronous.
        /// </summary>
        /// <param name="accessCode">The access code.</param>
        /// <returns>Task&lt;System.String&gt;.</returns>
        Task<string> UninstallAsync(string accessCode);
        /// <summary>
        /// Reinstalls the asynchronous.
        /// </summary>
        /// <param name="accessCode">The access code.</param>
        /// <returns>Task&lt;System.String&gt;.</returns>
        Task<string> ReinstallAsync(string accessCode);
        /// <summary>
        /// Gets the i frame asynchronous.
        /// </summary>
        /// <returns>Task&lt;System.Object&gt;.</returns>
        Task<object> GetIFrameAsync();
        /// <summary>
        /// Gets the p frame asynchronous.
        /// </summary>
        /// <param name="iframe">The iframe.</param>
        /// <returns>Task&lt;MemoryCacheResult&gt;.</returns>
        Task<MemoryCacheResult> GetPFrameAsync(long iframe);
        /// <summary>
        /// Determines whether [has i frame] [the specified etag].
        /// </summary>
        /// <param name="etag">The etag.</param>
        /// <returns><c>true</c> if [has i frame] [the specified etag]; otherwise, <c>false</c>.</returns>
        bool HasPFrame(string etag);
    }

    /// <summary>
    /// Class KFrameRepository.
    /// Implements the <see cref="KFrame.IKFrameRepository" />
    /// </summary>
    /// <seealso cref="KFrame.IKFrameRepository" />
    public class KFrameRepository : IKFrameRepository
    {
        readonly IMemoryCache _cache;
        IKFrameSource[] _sources;
        List<KFrameNode> _nodes;

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
        public KFrameRepository(IMemoryCache cache, KFrameOptions options, IKFrameSource[] sources)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            Options = options ?? throw new ArgumentNullException(nameof(options));
            Sources = sources ?? throw new ArgumentNullException(nameof(sources));
        }

        #region Cache

        /// <summary>
        /// Class _del_.
        /// Implements the <see cref="KFrame.KFrameRepository.IKey" />
        /// </summary>
        /// <seealso cref="KFrame.KFrameRepository.IKey" />
        public class _del_ : Source.IKey
        {
            public object id { get; set; }
            public string t { get; set; }
        }

        readonly static MemoryCacheRegistration IFrame = new MemoryCacheRegistration(nameof(IFrame), new MemoryCacheEntryOptions
        {
            AbsoluteExpiration = KFrameTiming.IFrameAbsoluteExpiration(),
        }, async (tag, values) =>
        {
            var parent = (KFrameRepository)tag;
            var results = new List<object>();
            foreach (var node in parent.Nodes)
                results.Add(await node.Source.GetIFrameAsync(node.Chapter, node.FrameSources));
            return results;
        }, "KFrame");

        readonly static MemoryCacheRegistration PFrame = new MemoryCacheRegistration(nameof(PFrame), AddRemovedCallback(new MemoryCacheEntryOptions
        {
            AbsoluteExpiration = KFrameTiming.PFrameAbsoluteExpiration(),
        }), async (tag, values) =>
        {
            var parent = (KFrameRepository)tag;
            var iframe = new DateTime((long)values[0]);
            var results = new List<object>();
            var checks = new Queue<Check>();
            var etags = new List<string>();
            foreach (var node in parent.Nodes)
            {
                var (data, check, etag) = await node.Source.GetPFrameAsync(node.Chapter, node.FrameSources, iframe, true);
                results.Add(data);
                checks.Enqueue(check);
                etags.Add(etag);
            }
            return new MemoryCacheResult(results.ToArray())
            {
                Tag = checks,
                ETag = etags.Count != 0 ? $"\"{string.Join(" ", etags)}\"" : null,
            };
        }, "KFrame");

        /// <summary>
        /// Class Check.
        /// </summary>
        public class Check
        {
            public DateTime IFrame;
            public int[] Keys;
            public DateTime MaxDate;

            public override bool Equals(object obj) => obj is Check b ? Keys.SequenceEqual(b.Keys) && MaxDate == b.MaxDate : false;
            public override int GetHashCode() => IFrame.GetHashCode() ^ Keys.GetHashCode() ^ MaxDate.GetHashCode();
            public static bool operator ==(Check a, Check b) => a.Equals(b);
            public static bool operator !=(Check a, Check b) => !a.Equals(b);
        }

        static MemoryCacheEntryOptions AddRemovedCallback(MemoryCacheEntryOptions options)
        {
            options.RegisterPostEvictionCallback(async (key, value, reason, state) =>
            {
                var parent = (KFrameRepository)state;
                var result = value as MemoryCacheResult;
                if (parent == null || result == null || result.Tag == null)
                    return;
                var checks = (Queue<Check>)result.Tag;
                foreach (var node in parent.Nodes)
                {
                    var check = checks.Dequeue();
                    var (data2, check2, etag2) = await node.Source.GetPFrameAsync(node.Chapter, node.FrameSources, check.IFrame, false);
                    if (check != check2)
                        return;
                }
                parent._cache.Set(key, value, new MemoryCacheEntryOptions().SetAbsoluteExpiration(KFrameTiming.PFramePolling()));
            }, null);
            return options;
        }

        readonly static MemoryCacheRegistration MergedFrame = new MemoryCacheRegistration(nameof(MergedFrame), 10, (tag, values) =>
        {
            var parent = (KFrameRepository)tag;
            var iframe = (IDictionary<string, object>)parent._cache.Get<dynamic>(IFrame, parent);
            var pframe = (IDictionary<string, object>)parent._cache.GetResult(PFrame, parent, (long)iframe["frame"]).Result;
            var dels = (List<_del_>)pframe["del"];
            var result = (IDictionary<string, object>)new ExpandoObject();
            foreach (var source in parent.Sources)
            {
                var kps = ((IEnumerable<object>)iframe[source.Param.key]).Cast<Source.IKey>().ToList();
                var ips = ((IEnumerable<object>)pframe[source.Param.key]).Cast<Source.IKey>().ToList();
                if (kps.Count == 0 && ips.Count == 0)
                    continue;
                var ipsdelsById = dels.Where(x => x.t == source.Param.key).ToDictionary(x => x.id);
                var ipsById = ips.ToDictionary(x => x.id);
                var p = kps.Where(x => !ipsdelsById.ContainsKey(x.id) && !ipsById.ContainsKey(x.id)).Union(ips).ToList();
                result.Add(source.Param.key, p.ToDictionary(x => x.id));
            }
            return (dynamic)result;
        }, "KFrame");

        #endregion

        /// <summary>
        /// Gets or sets the sources.
        /// </summary>
        /// <value>The sources.</value>
        public IKFrameSource[] Sources
        {
            get => _sources;
            set
            {
                _sources = value ?? throw new ArgumentNullException(nameof(value));
                _nodes = null;
            }
        }

        List<KFrameNode> Nodes
        {
            get
            {
                if (_nodes != null)
                    return _nodes;
                var nodes = new List<KFrameNode>();
                // db-source
                var dbFrameSources = Sources.OfType<IKFrameDbSource>().ToArray();
                if (dbFrameSources.Length > 0)
                {
                    var dbSource = Options.DbSource ?? throw new InvalidOperationException($"{nameof(KFrameOptions.DbSource)} not set");
                    nodes.Add(new KFrameNode(dbSource, dbFrameSources));
                }
                // kv-source
                var kvFrameSources = Sources.OfType<IKFrameKvSource>().ToArray();
                if (kvFrameSources.Length > 0)
                {
                    var kvSource = Options.KvSource ?? throw new InvalidOperationException($"{nameof(KFrameOptions.KvSource)} not set");
                    nodes.Add(new KFrameNode(kvSource, kvFrameSources));
                }
                return _nodes = nodes;
            }
        }

        /// <summary>
        /// Gets or sets the options.
        /// </summary>
        /// <value>The options.</value>
        public KFrameOptions Options { get; set; }

        bool ValidAccessCode(string accessCode, out string message)
        {
            if (!string.IsNullOrEmpty(Options.AccessToken) && $"/{Options.AccessToken}" != accessCode)
            {
                message = "Invalid Access Token";
                return false;
            }
            message = null;
            return true;
        }

        /// <summary>
        /// clear as an asynchronous operation.
        /// </summary>
        /// <param name="accessCode">The access code.</param>
        /// <returns>Task&lt;System.String&gt;.</returns>
        public async Task<string> ClearAsync(string accessCode)
        {
            if (!ValidAccessCode(accessCode, out var message))
                return message;
            var b = new StringBuilder();
            //foreach (var node in Nodes)
            //    b.Append(await node.Source.ClearAsync(node.Chapter, node.FrameSources));
            _cache.Touch("KFrame");
            return b.ToString();
        }

        /// <summary>
        /// install as an asynchronous operation.
        /// </summary>
        /// <param name="accessCode">The access code.</param>
        /// <returns>Task&lt;System.String&gt;.</returns>
        /// <exception cref="System.InvalidOperationException"></exception>
        /// <exception cref="System.InvalidOperationException"></exception>
        public async Task<string> InstallAsync(string accessCode)
        {
            if (!ValidAccessCode(accessCode, out var message))
                return message;
            var b = new StringBuilder();
            foreach (var node in Nodes)
                b.Append(await node.Source.InstallAsync(node.Chapter, node.FrameSources));
            return b.ToString();
        }

        /// <summary>
        /// uninstall as an asynchronous operation.
        /// </summary>
        /// <param name="accessCode">The access code.</param>
        /// <returns>Task&lt;System.String&gt;.</returns>
        /// <exception cref="System.InvalidOperationException"></exception>
        /// <exception cref="System.InvalidOperationException"></exception>
        public async Task<string> UninstallAsync(string accessCode)
        {
            if (!ValidAccessCode(accessCode, out var message))
                return message;
            var b = new StringBuilder();
            foreach (var node in Nodes)
                b.Append(await node.Source.UninstallAsync(node.Chapter, node.FrameSources));
            return b.ToString();
        }

        /// <summary>
        /// reinstall as an asynchronous operation.
        /// </summary>
        /// <param name="accessCode">The access code.</param>
        /// <returns>Task&lt;System.String&gt;.</returns>
        public async Task<string> ReinstallAsync(string accessCode)
        {
            if (!ValidAccessCode(accessCode, out var message))
                return message;
            var b = new StringBuilder();
            b.Append(await UninstallAsync(accessCode));
            b.Append(await InstallAsync(accessCode));
            return b.ToString();
        }

        /// <summary>
        /// get i frame as an asynchronous operation.
        /// </summary>
        /// <returns>Task&lt;dynamic&gt;.</returns>
        public async Task<dynamic> GetIFrameAsync() => await _cache.GetAsync<dynamic>(IFrame, this);

        /// <summary>
        /// get p frame as an asynchronous operation.
        /// </summary>
        /// <param name="iframe">The kframe.</param>
        /// <returns>Task&lt;MemoryCacheResult&gt;.</returns>
        public async Task<MemoryCacheResult> GetPFrameAsync(long iframe) => await _cache.GetResultAsync(PFrame, this, iframe);

        /// <summary>
        /// Determines whether [has p frame] [the specified etag].
        /// </summary>
        /// <param name="etag">The etag.</param>
        /// <returns><c>true</c> if [has p frame] [the specified etag]; otherwise, <c>false</c>.</returns>
        public bool HasPFrame(string etag) => _cache.Contains(PFrame, etag);

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
        public static IKFrameSource[] FindSourcesFromAssembly(IEnumerable<Assembly> assemblysToScan, Predicate<Type> condition) =>
            assemblysToScan.SelectMany(a => a.GetTypes().Where(t => condition(t))
                .Select(t => (IKFrameSource)Activator.CreateInstance(t))).ToArray();

        /// <summary>
        /// Finds the sources from assembly.
        /// </summary>
        /// <param name="assemblysToScan">The assemblys to scan.</param>
        /// <param name="excludes">The excludes.</param>
        /// <returns>IReferenceSource[].</returns>
        public static IKFrameSource[] FindSourcesFromAssembly(IEnumerable<Assembly> assemblysToScan, params Type[] excludes) =>
            FindSourcesFromAssembly(assemblysToScan, x => !x.IsAbstract && !x.IsInterface && typeof(IKFrameSource).IsAssignableFrom(x) && !excludes.Contains(x));
    }
}

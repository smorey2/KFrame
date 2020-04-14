using Contoso.Data.Services;
using Dapper;
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
    public interface IKFrameRepository
    {
        string DbInstall();
        string DbUninstall();
        string KvInstall();
        string KvUninstall();
        Task<object> GetKFrameAsync();
        Task<MemoryCacheResult> GetIFrameAsync(long kframe);
        bool HasIFrame(string etag);
    }

    public class KFrameRepository : IKFrameRepository
    {
        readonly IMemoryCache _cache;
        readonly KFrameSettings _settings;
        readonly IDbService _dbService;

        public KFrameRepository(IMemoryCache cache, KFrameSettings settings, IEnumerable<Assembly> assemblys)
            : this(cache, settings, new DbService(), ReferenceSourceManager.FindSourcesFromAssembly(assemblys)) { }
        public KFrameRepository(IMemoryCache cache, KFrameSettings settings, IDbService dbService, IEnumerable<Assembly> assemblys)
            : this(cache, settings, dbService, ReferenceSourceManager.FindSourcesFromAssembly(assemblys)) { }
        public KFrameRepository(IMemoryCache cache, KFrameSettings settings, IDbService dbService, IReferenceSource[] sources)
        {
            _cache = cache;
            _settings = settings;
            _dbService = dbService;
            Sources = sources;
            Schema = "dbo";
        }

        #region Cache

        public interface IKey
        {
            string id { get; }
        }

        public class DelData
        {
            public int Id0 { get; set; }
            public string Id1 { get; set; }
            public Guid Id2 { get; set; }
            public string Param { get; set; }
        }

        public class _del_ : IKey
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
            using (var ctx = parent._dbService.GetConnection())
            {
                var s = await ctx.QueryMultipleAsync($"[{parent.Schema}].[p_GetKFrame]", null, commandType: CommandType.StoredProcedure);
                var f = s.Read().Single(); var frame = (DateTime)f.Frame; var frameId = (int?)f.FrameId;
                var r = (IDictionary<string, object>)new ExpandoObject();
                r.Add("frame", frame.Ticks);
                foreach (var source in parent.Sources)
                    r.Add(source.Param, source.Read(s));
                return (dynamic)r;
            }
        }, "#KFrame");

        static MemoryCacheEntryOptions AddRemovedCallback(MemoryCacheEntryOptions options)
        {
            options.RegisterPostEvictionCallback((key, value, reason, state) =>
            {
                var parent = (KFrameRepository)state;
                var cache = parent._cache;
                var result = value as MemoryCacheResult;
                if (parent == null || result == null || result.Tag == null)
                    return;
                var valueTag = (Tuple<DateTime, int[], DateTime>)result.Tag;
                var kframe = valueTag.Item1; var kframeL = kframe.ToLocalTime();
                using (var ctx = parent._dbService.GetConnection())
                {
                    var s = ctx.QueryMultiple($"[{parent.Schema}].[p_GetIFrame]", new { kframe, kframeL, expand = false }, commandType: CommandType.StoredProcedure);
                    var f = s.Read().Single(); var frameId = (int?)f.FrameId;
                    if (frameId == null)
                        return;
                    var ddel = s.Read<int>().Single();
                    var maxDate = DateTime.MinValue;
                    foreach (var source in parent.Sources)
                    {
                        var date = s.Read<DateTime?>().Single();
                        if (date != null && date.Value > maxDate) maxDate = date.Value;
                    }
                    var check = new Tuple<DateTime, int[], DateTime>(kframe, new[] { frameId.Value, ddel }, maxDate);
                    if (check.Item2.SequenceEqual(valueTag.Item2) && check.Item3 == valueTag.Item3)
                        cache.Set(key, value, new MemoryCacheEntryOptions().SetAbsoluteExpiration(DateTime.Now.AddMinutes(1)));

                }
            }, null);
            return options;
        }

        readonly static MemoryCacheRegistration IFrame = new MemoryCacheRegistration(nameof(IFrame), AddRemovedCallback(new MemoryCacheEntryOptions
        {
            AbsoluteExpiration = DateTime.Now.AddMinutes(1),
        }), async (tag, values) =>
        {
            var parent = (KFrameRepository)tag;
            var kframe = new DateTime((long)values[0]); var kframeL = kframe.ToLocalTime();
            using (var ctx = parent._dbService.GetConnection())
            {
                var s = await ctx.QueryMultipleAsync($"[{parent.Schema}].[p_GetIFrame]", new { kframe, kframeL, expand = true }, commandType: CommandType.StoredProcedure);
                var f = s.Read().Single(); var frameId = (int?)f.FrameId;
                var etag = $"\"{Convert.ToBase64String(BitConverter.GetBytes(((DateTime)f.Frame).Ticks))}\"";
                if (frameId == null)
                    return new MemoryCacheResult(null)
                    {
                        Tag = null,
                        ETag = etag
                    };
                var ddel = s.Read<int>().Single(); var del = s.Read<DelData>().Select(x => new _del_ { id = (x.Id0 != -1 ? x.Id0.ToString() : string.Empty) + x.Id1 + (x.Id2 != Guid.Empty ? x.Id2.ToString() : string.Empty), t = x.Param }).ToList();
                var maxDate = DateTime.MinValue;
                var r = (IDictionary<string, object>)new ExpandoObject();
                r.Add("del", del);
                foreach (var source in parent.Sources)
                {
                    var date = s.Read<DateTime?>().Single();
                    if (date != null && date.Value > maxDate) maxDate = date.Value;
                    r.Add(source.Param, source.Read(s));
                }
                var check = new Tuple<DateTime, int[], DateTime>(kframe, new[] { frameId.Value, ddel }, maxDate);
                return new MemoryCacheResult((dynamic)r)
                {
                    Tag = check,
                    ETag = etag
                };
            }
        }, "#KFrame");

        readonly static MemoryCacheRegistration MergedFrame = new MemoryCacheRegistration(nameof(MergedFrame), 10, (tag, values) =>
        {
            var parent = (KFrameRepository)tag;
            var kframe = (IDictionary<string, object>)parent._cache.Get<dynamic>(KFrame, parent);
            var iframe = (IDictionary<string, object>)parent._cache.GetResult(IFrame, parent, (long)kframe["frame"]).Result;
            var idels = (List<_del_>)iframe["del"];
            var r = (IDictionary<string, object>)new ExpandoObject();
            foreach (var source in parent.Sources)
            {
                var kps = ((IEnumerable<object>)kframe[source.Param]).Cast<IKey>().ToList();
                var ips = ((IEnumerable<object>)iframe[source.Param]).Cast<IKey>().ToList();
                if (kps.Count == 0 && ips.Count == 0)
                    continue;
                var ipsdelsById = idels.Where(x => x.t == source.Param).ToDictionary(x => x.id);
                var ipsById = ips.ToDictionary(x => x.id);
                var p = kps.Where(x => !ipsdelsById.ContainsKey(x.id) && !ipsById.ContainsKey(x.id)).Union(ips).ToList();
                r.Add(source.Param, p.ToDictionary(x => x.id));
            }
            return (dynamic)r;
        }, "#KFrame");

        #endregion

        public IReferenceSource[] Sources { get; set; }
        public string Schema { get; set; }
        public string DbInstall() => ReferenceSourceManager.DbInstall(_settings.DbSource, Sources);
        public string DbUninstall() => ReferenceSourceManager.DbUninstall(_settings.DbSource, Sources);
        public string KvInstall() => ReferenceSourceManager.KvInstall(_settings.KvSource, Sources);
        public string KvUninstall() => ReferenceSourceManager.KvUninstall(_settings.KvSource, Sources);
        public async Task<dynamic> GetKFrameAsync() => await _cache.GetAsync<dynamic>(KFrame, this);
        public async Task<MemoryCacheResult> GetIFrameAsync(long kframe) => await _cache.GetResultAsync(IFrame, this, kframe);
        public bool HasIFrame(string etag) => _cache.Contains(IFrame, etag);
        public async Task<dynamic> GetMergedFrameAsync() => await _cache.GetAsync<dynamic>(MergedFrame, this);
    }
}

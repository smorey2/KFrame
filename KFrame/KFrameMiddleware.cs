using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using System;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;

namespace KFrame
{
    /// <summary>
    /// Class KFrameMiddleware.
    /// </summary>
    public class KFrameMiddleware
    {
        readonly RequestDelegate _next;
        readonly KFrameOptions _options;
        readonly IMemoryCache _cache;
        readonly KFrameRepository _repository;

        /// <summary>
        /// Initializes a new instance of the <see cref="KFrameMiddleware"/> class.
        /// </summary>
        /// <param name="next">The next.</param>
        /// <param name="options">The options.</param>
        /// <param name="cache">The cache.</param>
        public KFrameMiddleware(RequestDelegate next, KFrameOptions options, IMemoryCache cache)
        {
            _next = next;
            _options = options;
            _cache = cache;
            _repository = new KFrameRepository(_cache, _options, new Assembly[] { });
        }

        /// <summary>
        /// invoke as an asynchronous operation.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns>Task.</returns>
        public async Task InvokeAsync(HttpContext context)
        {
            var req = context.Request;
            var res = context.Response;
            if (req.Path.StartsWithSegments(_options.RequestPath, StringComparison.OrdinalIgnoreCase, out var remaining))
            {
                if (remaining.StartsWithSegments("/k", StringComparison.OrdinalIgnoreCase)) await KFrameAsync(req, res);
                else if (remaining.StartsWithSegments("/i", StringComparison.OrdinalIgnoreCase, out var kframe)) await IFrameAsync(req, res, kframe);
                else if (remaining.StartsWithSegments("/dbinstall", StringComparison.OrdinalIgnoreCase)) await DbInstallAsync(req, res);
                else if (remaining.StartsWithSegments("/dbuninstall", StringComparison.OrdinalIgnoreCase)) await DbUninstallAsync(req, res);
                else if (remaining.StartsWithSegments("/kvinstall", StringComparison.OrdinalIgnoreCase)) await KvInstallAsync(req, res);
                else if (remaining.StartsWithSegments("/kvuninstall", StringComparison.OrdinalIgnoreCase)) await KvUninstallAsync(req, res);
                else await _next(context);
                return;
            }
            await _next(context);
        }

        async Task KFrameAsync(HttpRequest req, HttpResponse res)
        {
            res.Clear();
            var r = await _repository.GetKFrameAsync();
            res.StatusCode = (int)HttpStatusCode.OK;
            res.Headers.Add("Access-Control-Allow-Origin", "*");
            res.ContentType = "application/json";
            var typedHeaders = res.GetTypedHeaders();
            typedHeaders.CacheControl = new CacheControlHeaderValue { Public = true, MaxAge = DateTime.Today.AddDays(1) - DateTime.Now };
            typedHeaders.Expires = DateTime.Today.ToUniversalTime().AddDays(1);
            var json = JsonConvert.SerializeObject((object)r);
            await res.WriteAsync(json);
        }

        async Task IFrameAsync(HttpRequest req, HttpResponse res, PathString kframe)
        {
            if (kframe == null || !long.TryParse(kframe, out var kframeAsLong))
                kframeAsLong = 0;
            res.Clear();
            var firstEtag = req.Headers["If-None-Match"];
            if (!string.IsNullOrEmpty(firstEtag) && _repository.HasIFrame(firstEtag))
            {
                res.StatusCode = (int)HttpStatusCode.NotModified;
                return;
            }
            var r = await _repository.GetIFrameAsync(kframeAsLong);
            res.StatusCode = (int)HttpStatusCode.OK;
            res.ContentType = "application/json";
            res.Headers.Add("Access-Control-Allow-Origin", "*");
            var typedHeaders = res.GetTypedHeaders();
            typedHeaders.CacheControl = new CacheControlHeaderValue { Private = true };
            typedHeaders.ETag = new EntityTagHeaderValue(r.ETag);
            var json = JsonConvert.SerializeObject(r.Result);
            await res.WriteAsync(json);
        }

        async Task DbInstallAsync(HttpRequest req, HttpResponse res) => await res.WriteAsync(await _repository.KvInstallAsync());

        async Task DbUninstallAsync(HttpRequest req, HttpResponse res) => await res.WriteAsync(await _repository.KvUninstallAsync());

        async Task KvInstallAsync(HttpRequest req, HttpResponse res) => await res.WriteAsync(await _repository.KvInstallAsync());

        async Task KvUninstallAsync(HttpRequest req, HttpResponse res) => await res.WriteAsync(await _repository.KvUninstallAsync());
    }
}

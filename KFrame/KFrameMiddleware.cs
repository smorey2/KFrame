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
        public KFrameMiddleware(RequestDelegate next, KFrameOptions options, IMemoryCache cache, IKFrameSource[] sources)
        {
            _next = next;
            _options = options;
            _cache = cache;
            _repository = new KFrameRepository(_cache, _options, sources);
        }

        /// <summary>
        /// invoke as an asynchronous operation.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns>Task.</returns>
        public async Task InvokeAsync(HttpContext context)
        {
            var req = context.Request; var res = context.Response;
            if (req.Path.StartsWithSegments(_options.RequestPath, StringComparison.OrdinalIgnoreCase, out var remaining))
            {
                if (remaining.StartsWithSegments("/i", StringComparison.OrdinalIgnoreCase, out var remaining2)) await IFrameAsync(req, res, remaining2);
                else if (remaining.StartsWithSegments("/p", StringComparison.OrdinalIgnoreCase, out remaining2)) await PFrameAsync(req, res, remaining2);
                else if (remaining.StartsWithSegments("/clear", StringComparison.OrdinalIgnoreCase, out remaining2)) await ClearAsync(req, res, remaining2);
                else if (remaining.StartsWithSegments("/install", StringComparison.OrdinalIgnoreCase, out remaining2)) await InstallAsync(req, res, remaining2);
                else if (remaining.StartsWithSegments("/uninstall", StringComparison.OrdinalIgnoreCase, out remaining2)) await UninstallAsync(req, res, remaining2);
                else if (remaining.StartsWithSegments("/reinstall", StringComparison.OrdinalIgnoreCase, out remaining2)) await ReinstallAsync(req, res, remaining2);
                else await _next(context);
                return;
            }
            await _next(context);
        }

        async Task IFrameAsync(HttpRequest req, HttpResponse res, string remaining)
        {
            res.Clear();
            var etag = req.Headers["If-None-Match"];
            if (!string.IsNullOrEmpty(etag) && etag == "\"iframe\"")
            {
                res.StatusCode = (int)HttpStatusCode.NotModified;
                return;
            }
            var result = await _repository.GetIFrameAsync();
            res.StatusCode = (int)HttpStatusCode.OK;
            res.Headers.Add("Access-Control-Allow-Origin", "*");
            res.ContentType = "application/json";
            var typedHeaders = res.GetTypedHeaders();
            typedHeaders.CacheControl = new CacheControlHeaderValue { Public = true, MaxAge = KFrameTiming.IFrameCacheMaxAge() };
            typedHeaders.Expires = KFrameTiming.IFrameCacheExpires();
            typedHeaders.ETag = new EntityTagHeaderValue("\"iframe\"");
            var json = JsonConvert.SerializeObject((object)result);
            await res.WriteAsync(json);
        }

        async Task PFrameAsync(HttpRequest req, HttpResponse res, string remaining)
        {
            res.Clear();
            if (string.IsNullOrEmpty(remaining) || remaining[0] != '/')
            {
                res.StatusCode = (int)HttpStatusCode.NotFound;
                return;
            }
            if (!long.TryParse(remaining.Substring(1), out var iframe))
            {
                res.StatusCode = (int)HttpStatusCode.NotFound;
                return;
            }
            var etag = req.Headers["If-None-Match"];
            if (!string.IsNullOrEmpty(etag) && _repository.HasPFrame(etag))
            {
                res.StatusCode = (int)HttpStatusCode.NotModified;
                return;
            }
            var result = await _repository.GetPFrameAsync(iframe);
            res.StatusCode = (int)HttpStatusCode.OK;
            res.ContentType = "application/json";
            res.Headers.Add("Access-Control-Allow-Origin", "*");
            var typedHeaders = res.GetTypedHeaders();
            typedHeaders.CacheControl = new CacheControlHeaderValue { NoCache = true };
            //typedHeaders.Expires = DateTime.Today.ToUniversalTime().AddDays(1);
            typedHeaders.ETag = new EntityTagHeaderValue(result.ETag);
            var json = JsonConvert.SerializeObject(result.Result);
            await res.WriteAsync(json);
        }

        async Task ClearAsync(HttpRequest req, HttpResponse res, string remaining) => await res.WriteAsync(await _repository.ClearAsync(remaining));

        async Task InstallAsync(HttpRequest req, HttpResponse res, string remaining) => await res.WriteAsync(await _repository.InstallAsync(remaining));

        async Task UninstallAsync(HttpRequest req, HttpResponse res, string remaining) => await res.WriteAsync(await _repository.UninstallAsync(remaining));

        async Task ReinstallAsync(HttpRequest req, HttpResponse res, string remaining) => await res.WriteAsync(await _repository.ReinstallAsync(remaining));
    }
}

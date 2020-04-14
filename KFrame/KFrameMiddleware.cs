using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using System.Threading.Tasks;

namespace KFrame
{
    public class KFrameMiddleware
    {
        readonly RequestDelegate _next;
        readonly IMemoryCache _cache;

        public KFrameMiddleware(RequestDelegate next, IMemoryCache cache)
        {
            _next = next;
            _cache = cache;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            //var cultureQuery = context.Request.Query["culture"];
            //if (!string.IsNullOrWhiteSpace(cultureQuery))
            //{
            //    //var culture = new CultureInfo(cultureQuery);
            //    //CultureInfo.CurrentCulture = culture;
            //    //CultureInfo.CurrentUICulture = culture;
            //}
            await _next(context);
        }
    }
}

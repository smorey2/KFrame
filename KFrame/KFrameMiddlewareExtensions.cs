using Microsoft.AspNetCore.Builder;

namespace KFrame
{
    public static class KFrameMiddlewareExtensions
    {
        public static IApplicationBuilder UseKFrame(this IApplicationBuilder builder, KFrameSettings settings)
        {
            return builder.UseMiddleware<KFrameMiddleware>(settings);
        }
    }
}

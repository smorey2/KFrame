using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System;
using System.Reflection;

namespace KFrame
{
    /// <summary>
    /// Class KFrameMiddlewareExtensions.
    /// </summary>
    public static class KFrameMiddlewareExtensions
    {
        /// <summary>
        /// Uses the k frame.
        /// </summary>
        /// <param name="app">The application.</param>
        /// <param name="assemblys">The assemblys.</param>
        /// <returns>IApplicationBuilder.</returns>
        /// <exception cref="System.ArgumentNullException">app</exception>
        public static IApplicationBuilder UseKFrame(this IApplicationBuilder app, params Assembly[] assemblys)
        {
            if (app == null)
                throw new ArgumentNullException(nameof(app));
            return app.UseMiddleware<KFrameMiddleware>(new KFrameOptions { });
        }

        /// <summary>
        /// Uses the k frame.
        /// </summary>
        /// <param name="app">The application.</param>
        /// <param name="requestPath">The request path.</param>
        /// <param name="assemblys">The assemblys.</param>
        /// <returns>IApplicationBuilder.</returns>
        /// <exception cref="System.ArgumentNullException">app</exception>
        public static IApplicationBuilder UseKFrame(this IApplicationBuilder app, string requestPath, params Assembly[] assemblys)
        {
            if (app == null)
                throw new ArgumentNullException(nameof(app));
            return app.UseMiddleware<KFrameMiddleware>(new KFrameOptions
            {
                RequestPath = new PathString(requestPath)
            });
        }

        /// <summary>
        /// Uses the k frame.
        /// </summary>
        /// <param name="app">The application.</param>
        /// <param name="options">The options.</param>
        /// <param name="assemblys">The assemblys.</param>
        /// <returns>IApplicationBuilder.</returns>
        /// <exception cref="System.ArgumentNullException">app</exception>
        /// <exception cref="System.ArgumentNullException">options</exception>
        public static IApplicationBuilder UseKFrame(this IApplicationBuilder app, KFrameOptions options, params Assembly[] assemblys)
        {
            if (app == null)
                throw new ArgumentNullException(nameof(app));
            if (options == null)
                throw new ArgumentNullException(nameof(options));
            return app.UseMiddleware<KFrameMiddleware>(options);
        }

        /// <summary>
        /// Uses the k frame.
        /// </summary>
        /// <param name="app">The application.</param>
        /// <param name="options">The options.</param>
        /// <param name="requestPath">The request path.</param>
        /// <param name="assemblys">The assemblys.</param>
        /// <returns>IApplicationBuilder.</returns>
        /// <exception cref="System.ArgumentNullException">app</exception>
        /// <exception cref="System.ArgumentNullException">options</exception>
        public static IApplicationBuilder UseKFrame(this IApplicationBuilder app, KFrameOptions options, string requestPath, params Assembly[] assemblys)
        {
            if (app == null)
                throw new ArgumentNullException(nameof(app));
            if (options == null)
                throw new ArgumentNullException(nameof(options));
            options.RequestPath = requestPath;
            return app.UseMiddleware<KFrameMiddleware>(options);
        }
    }
}

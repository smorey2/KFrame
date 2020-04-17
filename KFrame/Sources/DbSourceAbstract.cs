using Contoso.Extensions.Services;
using Dapper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KFrame.Sources
{
    /// <summary>
    /// Class DbSourceAbstract.
    /// </summary>
    public abstract class DbSourceAbstract : SourceAbstract
    {
        /// <summary>
        /// Gets or sets the name of the connection.
        /// </summary>
        /// <value>The name of the connection.</value>
        public string ConnectionName { get; set; }

        /// <summary>
        /// Gets or sets the schema.
        /// </summary>
        /// <value>The schema.</value>
        public string Schema { get; set; } = "dbo";

        /// <summary>
        /// Gets or sets the database service.
        /// </summary>
        /// <value>The database service.</value>
        public IDbService DbService { get; set; } = new DbService();

        /// <summary>
        /// Gets or sets a value indicating whether [use variant].
        /// </summary>
        /// <value><c>true</c> if [use variant]; otherwise, <c>false</c>.</value>
        public bool UseVariant { get; set; } = false;

        /// <summary>
        /// execute as an asynchronous operation.
        /// </summary>
        /// <param name="b">The b.</param>
        /// <param name="ctx">The CTX.</param>
        /// <param name="sql">The SQL.</param>
        /// <returns>Task.</returns>
        protected static async Task ExecuteAsync(StringBuilder b, IDbConnection ctx, string sql)
        {
            b.AppendLine(sql);
            try
            {
                foreach (var x in sql.Split(new[] { "GO" }, StringSplitOptions.RemoveEmptyEntries))
                    await ctx.ExecuteAsync(x);
            }
            catch (Exception e) { b.AppendLine("\nERROR"); b.Append(e); }
        }

        /// <summary>
        /// Gets the i frame procedure.
        /// </summary>
        /// <param name="chapter">The chapter.</param>
        /// <returns>System.String.</returns>
        /// <value>The i frame procedure.</value>
        protected abstract string GetIFrameProcedure(string chapter);

        /// <summary>
        /// get i frame as an asynchronous operation.
        /// </summary>
        /// <param name="chapter">The chapter.</param>
        /// <param name="sources">The sources.</param>
        /// <returns>Task&lt;dynamic&gt;.</returns>
        /// <exception cref="ArgumentNullException">DbService</exception>
        /// <exception cref="ArgumentNullException">Schema</exception>
        public override async Task<dynamic> GetIFrameAsync(string chapter, IEnumerable<IKFrameSource> sources)
        {
            if (DbService == null)
                throw new ArgumentNullException(nameof(DbService));
            if (string.IsNullOrEmpty(Schema))
                throw new ArgumentNullException(nameof(Schema));
            using (var ctx = DbService.GetConnection(ConnectionName))
            {
                var s = await ctx.QueryMultipleAsync(GetIFrameProcedure(chapter), null, commandType: CommandType.StoredProcedure);
                var f = s.Read().Single(); var frame = (DateTime)f.Frame; var frameId = (int?)f.FrameId;
                var result = (IDictionary<string, object>)new ExpandoObject();
                result.Add("frame", frame.Ticks);
                foreach (var source in sources.Cast<IKFrameDbSource>())
                    result.Add(source.Param.key, source.Read(s));
                return (dynamic)result;
            }
        }

        /// <summary>
        /// Gets the p frame procedure.
        /// </summary>
        /// <param name="chapter">The chapter.</param>
        /// <returns>System.String.</returns>
        /// <value>The p frame procedure.</value>
        protected abstract string GetPFrameProcedure(string chapter);

        class DelData
        {
            public object Id { get; set; }
            public int Id0 { get; set; }
            public string Id1 { get; set; }
            public Guid Id2 { get; set; }
            public string Param { get; set; }
        }

        /// <summary>
        /// get p frame as an asynchronous operation.
        /// </summary>
        /// <param name="chapter">The chapter.</param>
        /// <param name="sources">The sources.</param>
        /// <param name="iframe">The iframe.</param>
        /// <param name="expand">if set to <c>true</c> [expand].</param>
        /// <returns>Task&lt;System.ValueTuple&lt;dynamic, KFrameRepository.Check, System.String&gt;&gt;.</returns>
        /// <exception cref="ArgumentNullException">DbService</exception>
        /// <exception cref="ArgumentNullException">Schema</exception>
        public override async Task<(dynamic data, KFrameRepository.Check check, string etag)> GetPFrameAsync(string chapter, IEnumerable<IKFrameSource> sources, DateTime iframe, bool expand)
        {
            if (DbService == null)
                throw new ArgumentNullException(nameof(DbService));
            if (string.IsNullOrEmpty(Schema))
                throw new ArgumentNullException(nameof(Schema));
            var iframeL = iframe.ToLocalTime();
            using (var ctx = DbService.GetConnection(ConnectionName))
            {
                var s = await ctx.QueryMultipleAsync(GetPFrameProcedure(chapter), new { iframe, iframeL, expand }, commandType: CommandType.StoredProcedure);
                var f = s.Read().Single(); var frameId = (int?)f.FrameId;
                var etag = Convert.ToBase64String(BitConverter.GetBytes(((DateTime)f.Frame).Ticks));
                if (frameId == null)
                    return (null, null, etag);
                var ddel = s.Read<int>().Single();
                var del = expand ? s.Read<DelData>().Select(x => new KFrameRepository._del_
                {
                    id = UseVariant ? $"{x.Id}" : $"{(x.Id0 != -1 ? x.Id0.ToString() : string.Empty)}{x.Id1}{(x.Id2 != Guid.Empty ? x.Id2.ToString() : string.Empty)}",
                    t = x.Param
                }).ToList() : null;
                var maxDate = DateTime.MinValue;
                var result = (IDictionary<string, object>)(expand ? new ExpandoObject() : null);
                result?.Add("del", del);
                foreach (var source in sources.Cast<IKFrameDbSource>())
                {
                    var date = s.Read<DateTime?>().Single();
                    if (date != null && date.Value > maxDate)
                        maxDate = date.Value;
                    result?.Add(source.Param.key, source.Read(s));
                }
                return ((dynamic)result, new KFrameRepository.Check { IFrame = iframe, Keys = new[] { frameId.Value, ddel }, MaxDate = maxDate }, etag);
            }
        }

        /// <summary>
        /// Clears the asynchronous.
        /// </summary>
        /// <param name="b">The b.</param>
        /// <param name="ctx">The CTX.</param>
        /// <param name="chapter">The chapter.</param>
        /// <param name="sources">The sources.</param>
        /// <returns>Task.</returns>
        protected abstract Task ClearAsync(StringBuilder b, IDbConnection ctx, string chapter, IEnumerable<IKFrameDbSource> sources);

        /// <summary>
        /// clear as an asynchronous operation.
        /// </summary>
        /// <param name="chapter">The chapter.</param>
        /// <param name="sources">The sources.</param>
        /// <returns>Task&lt;System.String&gt;.</returns>
        /// <exception cref="ArgumentNullException">DbService</exception>
        /// <exception cref="ArgumentNullException">Schema</exception>
        public override async Task<string> ClearAsync(string chapter, IEnumerable<IKFrameSource> sources)
        {
            if (DbService == null)
                throw new ArgumentNullException(nameof(DbService));
            if (string.IsNullOrEmpty(Schema))
                throw new ArgumentNullException(nameof(Schema));
            var b = new StringBuilder();
            using (var ctx = DbService.GetConnection(ConnectionName))
                await ClearAsync(b, ctx, chapter, sources.Cast<IKFrameDbSource>());
            b.AppendLine("DONE");
            return b.ToString();
        }

        /// <summary>
        /// Databases the install asynchronous.
        /// </summary>
        /// <param name="b">The b.</param>
        /// <param name="ctx">The CTX.</param>
        /// <param name="chapter">The chapter.</param>
        /// <param name="sources">The sources.</param>
        /// <returns>Task.</returns>
        protected abstract Task InstallAsync(StringBuilder b, IDbConnection ctx, string chapter, IEnumerable<IKFrameDbSource> sources);

        /// <summary>
        /// database install as an asynchronous operation.
        /// </summary>
        /// <param name="chapter">The chapter.</param>
        /// <param name="sources">The sources.</param>
        /// <returns>Task&lt;System.String&gt;.</returns>
        /// <exception cref="ArgumentNullException">DbService</exception>
        /// <exception cref="ArgumentNullException">Schema</exception>
        public override async Task<string> InstallAsync(string chapter, IEnumerable<IKFrameSource> sources)
        {
            if (DbService == null)
                throw new ArgumentNullException(nameof(DbService));
            if (string.IsNullOrEmpty(Schema))
                throw new ArgumentNullException(nameof(Schema));
            var b = new StringBuilder();
            using (var ctx = DbService.GetConnection(ConnectionName))
                await InstallAsync(b, ctx, chapter, sources.Cast<IKFrameDbSource>());
            b.AppendLine("DONE");
            return b.ToString();
        }

        /// <summary>
        /// Databases the uninstall asynchronous.
        /// </summary>
        /// <param name="b">The b.</param>
        /// <param name="ctx">The CTX.</param>
        /// <param name="chapter">The chapter.</param>
        /// <param name="sources">The sources.</param>
        /// <returns>Task.</returns>
        protected abstract Task UninstallAsync(StringBuilder b, IDbConnection ctx, string chapter, IEnumerable<IKFrameDbSource> sources);

        /// <summary>
        /// database uninstall as an asynchronous operation.
        /// </summary>
        /// <param name="chapter">The chapter.</param>
        /// <param name="sources">The sources.</param>
        /// <returns>Task&lt;System.String&gt;.</returns>
        /// <exception cref="ArgumentNullException">DbService</exception>
        /// <exception cref="ArgumentNullException">Schema</exception>
        /// <exception cref="ArgumentNullException">DbService</exception>
        public override async Task<string> UninstallAsync(string chapter, IEnumerable<IKFrameSource> sources)
        {
            if (DbService == null)
                throw new ArgumentNullException(nameof(DbService));
            if (string.IsNullOrEmpty(Schema))
                throw new ArgumentNullException(nameof(Schema));
            var b = new StringBuilder();
            using (var ctx = DbService.GetConnection(ConnectionName))
                await UninstallAsync(b, ctx, chapter, sources.Cast<IKFrameDbSource>());
            b.AppendLine("DONE");
            return b.ToString();
        }
    }
}

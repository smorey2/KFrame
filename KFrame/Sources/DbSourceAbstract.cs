using Contoso.Data.Services;
using Dapper;
using Microsoft.Extensions.Caching.Memory;
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
    public abstract class DbSourceAbstract
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
        public string Schema { get; set; }

        /// <summary>
        /// Gets or sets the prefix.
        /// </summary>
        /// <value>The prefix.</value>
        public string Prefix { get; set; }

        /// <summary>
        /// Gets or sets the database service.
        /// </summary>
        /// <value>The database service.</value>
        public IDbService DbService { get; set; } = new DbService();

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
        /// Gets the k frame procedure.
        /// </summary>
        /// <value>The k frame procedure.</value>
        protected abstract string KFrameProcedure { get; }

        /// <summary>
        /// get k frame as an asynchronous operation.
        /// </summary>
        /// <param name="sources">The sources.</param>
        /// <returns>Task&lt;dynamic&gt;.</returns>
        public async Task<dynamic> GetKFrameAsync(IReferenceDbSource[] sources)
        {
            using (var ctx = DbService.GetConnection(ConnectionName))
            {
                var s = await ctx.QueryMultipleAsync(KFrameProcedure, null, commandType: CommandType.StoredProcedure);
                var f = s.Read().Single(); var frame = (DateTime)f.Frame; var frameId = (int?)f.FrameId;
                var result = (IDictionary<string, object>)new ExpandoObject();
                result.Add("frame", frame.Ticks);
                foreach (var source in sources)
                    result.Add(source.Param.key, source.Read(s));
                return (dynamic)result;
            }
        }

        /// <summary>
        /// Gets the i frame procedure.
        /// </summary>
        /// <value>The i frame procedure.</value>
        protected abstract string IFrameProcedure { get; }

        class DelData
        {
            public int Id0 { get; set; }
            public string Id1 { get; set; }
            public Guid Id2 { get; set; }
            public string Param { get; set; }
        }

        /// <summary>
        /// get i frame as an asynchronous operation.
        /// </summary>
        /// <param name="sources">The sources.</param>
        /// <param name="kframe">The kframe.</param>
        /// <param name="expand">if set to <c>true</c> [expand].</param>
        /// <returns>Task&lt;MemoryCacheResult&gt;.</returns>
        public async Task<MemoryCacheResult> GetIFrameAsync(IReferenceDbSource[] sources, DateTime kframe, bool expand)
        {
            var kframeL = kframe.ToLocalTime();
            using (var ctx = DbService.GetConnection(ConnectionName))
            {
                var s = await ctx.QueryMultipleAsync(IFrameProcedure, new { kframe, kframeL, expand }, commandType: CommandType.StoredProcedure);
                var f = s.Read().Single(); var frameId = (int?)f.FrameId;
                var etag = $"\"{Convert.ToBase64String(BitConverter.GetBytes(((DateTime)f.Frame).Ticks))}\"";
                if (frameId == null)
                    return new MemoryCacheResult(null)
                    {
                        Tag = null,
                        ETag = etag
                    };
                var ddel = s.Read<int>().Single();
                var del = expand ? s.Read<DelData>().Select(x => new KFrameRepository._del_
                {
                    id = $"{(x.Id0 != -1 ? x.Id0.ToString() : string.Empty)}{x.Id1}{(x.Id2 != Guid.Empty ? x.Id2.ToString() : string.Empty)}",
                    t = x.Param
                }).ToList() : null;
                var maxDate = DateTime.MinValue;
                var result = (IDictionary<string, object>)(expand ? new ExpandoObject() : null);
                result?.Add("del", del);
                foreach (var source in sources)
                {
                    var date = s.Read<DateTime?>().Single();
                    if (date != null && date.Value > maxDate)
                        maxDate = date.Value;
                    result?.Add(source.Param.key, source.Read(s));
                }
                return new MemoryCacheResult((dynamic)result)
                {
                    Tag = new KFrameRepository.TagCheck { KFrame = kframe, Keys = new[] { frameId.Value, ddel }, MaxDate = maxDate },
                    ETag = etag
                };
            }
        }

        /// <summary>
        /// Databases the install asynchronous.
        /// </summary>
        /// <param name="b">The b.</param>
        /// <param name="ctx">The CTX.</param>
        /// <param name="sources">The sources.</param>
        /// <returns>Task.</returns>
        protected abstract Task DbInstallAsync(StringBuilder b, IDbConnection ctx, IReferenceDbSource[] sources);

        /// <summary>
        /// database install as an asynchronous operation.
        /// </summary>
        /// <param name="sources">The sources.</param>
        /// <returns>Task&lt;System.String&gt;.</returns>
        public async Task<string> DbInstallAsync(IReferenceDbSource[] sources)
        {
            var b = new StringBuilder();
            using (var ctx = DbService.GetConnection(ConnectionName))
                await DbInstallAsync(b, ctx, sources);
            b.AppendLine("DONE");
            return b.ToString();
        }

        /// <summary>
        /// Databases the uninstall asynchronous.
        /// </summary>
        /// <param name="b">The b.</param>
        /// <param name="ctx">The CTX.</param>
        /// <param name="sources">The sources.</param>
        /// <returns>Task.</returns>
        protected abstract Task DbUninstallAsync(StringBuilder b, IDbConnection ctx, IReferenceDbSource[] sources);

        /// <summary>
        /// database uninstall as an asynchronous operation.
        /// </summary>
        /// <param name="sources">The sources.</param>
        /// <returns>Task&lt;System.String&gt;.</returns>
        public async Task<string> DbUninstallAsync(IReferenceDbSource[] sources)
        {
            var b = new StringBuilder();
            using (var ctx = DbService.GetConnection(ConnectionName))
                await DbUninstallAsync(b, ctx, sources);
            b.AppendLine("DONE");
            return b.ToString();
        }
    }
}

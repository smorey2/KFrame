using Contoso.Data.Services;
using Dapper;
using System;
using System.Data;
using System.Text;

namespace KFrame.Sources
{
    public abstract class DbSourceAbstract
    {
        public string ConnectionName { get; set; }
        public string Schema { get; set; }
        public string Prefix { get; set; }
        public IDbService DbService { get; set; } = new DbService();

        protected static void Execute(StringBuilder b, IDbConnection ctx, string sql)
        {
            b.AppendLine(sql);
            try
            {
                foreach (var x in sql.Split(new[] { "GO" }, StringSplitOptions.RemoveEmptyEntries))
                    ctx.Execute(x);
            }
            catch (Exception e) { b.AppendLine("\nERROR"); b.Append(e); }
        }

        public abstract void DbInstall(StringBuilder b, IDbConnection ctx, IReferenceDbSource[] sources);
        public abstract void DbUninstall(StringBuilder b, IDbConnection ctx, IReferenceDbSource[] sources);
    }
}

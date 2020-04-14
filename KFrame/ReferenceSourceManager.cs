using KFrame.Sources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace KFrame
{
    public static class ReferenceSourceManager
    {
        public static IReferenceSource[] FindSourcesFromAssembly(IEnumerable<Assembly> assemblysToScan, Predicate<Type> condition) =>
            assemblysToScan.SelectMany(a => a.GetTypes().Where(t => condition(t))
                .Select(t => (IReferenceSource)Activator.CreateInstance(t))).ToArray();

        public static IReferenceSource[] FindSourcesFromAssembly(IEnumerable<Assembly> assemblysToScan, params Type[] excludes) =>
            FindSourcesFromAssembly(assemblysToScan, x => !x.IsAbstract && !x.IsInterface && typeof(IReferenceSource).IsAssignableFrom(x) && !excludes.Contains(x));

        public static string DbInstall(DbSourceAbstract db, IReferenceSource[] sources)
        {
            var b = new StringBuilder();
            using (var ctx = db.DbService.GetConnection(db.ConnectionName))
                db.DbInstall(b, ctx, sources.Cast<IReferenceDbSource>().Where(x => x != null).ToArray());
            b.AppendLine("DONE");
            return b.ToString();
        }

        public static string DbUninstall(DbSourceAbstract db, IReferenceSource[] sources)
        {
            var b = new StringBuilder();
            using (var ctx = db.DbService.GetConnection(db.ConnectionName))
                db.DbUninstall(b, ctx, sources.Cast<IReferenceDbSource>().Where(x => x != null).ToArray());
            b.AppendLine("DONE");
            return b.ToString();
        }

        public static string KvInstall(KvSourceAbstract kv, IReferenceSource[] sources)
        {
            var b = new StringBuilder();
            using (var ctx = kv.KvService.GetConnection(kv.ConnectionString))
                kv.KvInstall(b, ctx, sources.Cast<IReferenceKvSource>().Where(x => x != null).ToArray());
            b.AppendLine("DONE");
            return b.ToString();
        }

        public static string KvUninstall(KvSourceAbstract kv, IReferenceSource[] sources)
        {
            var b = new StringBuilder();
            using (var ctx = kv.KvService.GetConnection(kv.ConnectionString))
                kv.KvUninstall(b, ctx, sources.Cast<IReferenceKvSource>().Where(x => x != null).ToArray());
            b.AppendLine("DONE");
            return b.ToString();
        }
    }
}

using KFrame.Services;
using System.Text;

namespace KFrame.Sources
{
    public abstract class KvSourceAbstract
    {
        public string ConnectionString { get; set; }
        public string Prefix { get; set; }
        public IKvService KvService { get; set; } = new KvService();
        public abstract void KvInstall(StringBuilder b, object ctx, IReferenceKvSource[] sources);
        public abstract void KvUninstall(StringBuilder b, object ctx, IReferenceKvSource[] sources);
    }
}

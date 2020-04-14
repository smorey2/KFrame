using KFrame.Sources;

namespace KFrame
{
    public class KFrameSettings
    {
        public DbSourceAbstract DbSource { get; set; }
        public KvSourceAbstract KvSource { get; set; }
    }
}

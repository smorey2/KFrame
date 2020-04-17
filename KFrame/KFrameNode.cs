using KFrame.Sources;
using System.Collections.Generic;

namespace KFrame
{
    internal class KFrameNode
    {
        public KFrameNode(SourceAbstract source, IEnumerable<IKFrameSource> frameSources)
        {
            Chapter = "Primary";
            Source = source;
            FrameSources = frameSources;
        }

        public readonly string Chapter;
        public readonly SourceAbstract Source;
        public readonly IEnumerable<IKFrameSource> FrameSources;
    }
}

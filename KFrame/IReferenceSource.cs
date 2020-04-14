using System.Collections.Generic;

namespace KFrame
{
    public interface IReferenceSource
    {
        string Name { get; }
        string Param { get; }
        IEnumerable<object> Read(object s);
    }
}

using System;

namespace Microsoft.Extensions.Caching
{
    public class FileCacheDependency
    {
        public FileCacheDependency(string filename)
        {
            FileName = filename;
        }

        public string FileName { get; }

        static string _directory;
        public static string Directory
        {
            get => _directory;
            set
            {
                if (string.IsNullOrEmpty(value))
                    throw new ArgumentNullException(nameof(value));
                _directory = value.EndsWith("\\") ? value : value + "\\";
            }
        }
    }
}

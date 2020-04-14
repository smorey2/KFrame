using System;

namespace KFrame.Services
{
    public interface IKvService
    {
        IDisposable GetConnection(string connectionString);
    }

    public class KvService : IKvService
    {
        public IDisposable GetConnection(string connectionString) => throw new NotImplementedException();
    }
}

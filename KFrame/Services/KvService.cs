using System;

namespace KFrame.Services
{
    /// <summary>
    /// Interface IKvService
    /// </summary>
    public interface IKvService
    {
        /// <summary>
        /// Gets the connection.
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        /// <returns>IDisposable.</returns>
        IDisposable GetConnection(string connectionString);
    }

    /// <summary>
    /// Class KvService.
    /// Implements the <see cref="KFrame.Services.IKvService" />
    /// </summary>
    /// <seealso cref="KFrame.Services.IKvService" />
    public class KvService : IKvService
    {
        /// <summary>
        /// Gets the connection.
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        /// <returns>IDisposable.</returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public IDisposable GetConnection(string connectionString) => throw new NotImplementedException();
    }
}

using Dapper;
using KFrame.Sources;
using System;
using System.Collections.Generic;

namespace KFrame
{
    /// <summary>
    /// Interface IKFrameDbSource
    /// Implements the <see cref="KFrame.IKFrameSource" />
    /// </summary>
    /// <seealso cref="KFrame.IKFrameSource" />
    public interface IKFrameDbSource : IKFrameSource
    {
        /// <summary>
        /// Gets the table.
        /// </summary>
        /// <value>The table.</value>
        DbSourceTable Table { get; }
        /// <summary>
        /// Gets the build.
        /// </summary>
        /// <value>The build.</value>
        (Func<string, string> key, Func<Source.X, string> max, Func<Source.X, string> body) Build { get; }
        /// <summary>
        /// Reads the specified s.
        /// </summary>
        /// <param name="s">The s.</param>
        /// <returns>IEnumerable&lt;System.Object&gt;.</returns>
        IEnumerable<object> Read(SqlMapper.GridReader s);
    }
}

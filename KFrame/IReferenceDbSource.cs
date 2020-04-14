using Dapper;
using KFrame.Sources;
using System;
using System.Collections.Generic;

namespace KFrame
{
    /// <summary>
    /// Interface IReferenceDbSource
    /// Implements the <see cref="KFrame.IReferenceSource" />
    /// </summary>
    /// <seealso cref="KFrame.IReferenceSource" />
    public interface IReferenceDbSource : IReferenceSource
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
        (Func<string, string> key, Func<Reference.X, string> max, Func<Reference.X, string> body) Build { get; }
        /// <summary>
        /// Reads the specified s.
        /// </summary>
        /// <param name="s">The s.</param>
        /// <returns>IEnumerable&lt;System.Object&gt;.</returns>
        IEnumerable<object> Read(SqlMapper.GridReader s);
    }
}

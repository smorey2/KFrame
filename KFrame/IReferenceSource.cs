namespace KFrame
{
    /// <summary>
    /// Interface IReferenceSource
    /// </summary>
    public interface IReferenceSource
    {
        /// <summary>
        /// Gets the parameter.
        /// </summary>
        /// <value>The parameter.</value>
        (string key, string name) Param { get; }
    }
}

namespace KFrame
{
    /// <summary>
    /// Interface IKFrameSource
    /// </summary>
    public interface IKFrameSource
    {
        IKFrameChapter Chapter { get; }
        /// <summary>
        /// Gets the parameter.
        /// </summary>
        /// <value>The parameter.</value>
        (string key, string name) Param { get; }
    }
}

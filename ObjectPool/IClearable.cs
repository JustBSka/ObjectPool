namespace ObjectPool
{
    /// <summary>
    /// Interface of the pooled object which can be cleared before returning to the pool.
    /// </summary>
    public interface IClearable
    {
        /// <summary>
        /// Clear the object before returning to the pool.
        /// </summary>
        void Clear();
    }
}

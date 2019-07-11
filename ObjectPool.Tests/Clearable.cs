namespace ObjectPool.Tests
{
    internal class Clearable
        : IClearable
    {
        public bool IsCleared { get; private set; }

        public void Clear()
        {
            IsCleared = true;
        }
    }
}

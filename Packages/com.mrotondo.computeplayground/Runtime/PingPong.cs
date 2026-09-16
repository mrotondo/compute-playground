namespace Mrotondo.ComputePlayground
{
    /// <summary>
    /// A read/write pair you swap between steps. Works for RenderTexture, GraphicsBuffer,
    /// or anything else double-buffered.
    /// </summary>
    public class PingPong<T> where T : class
    {
        public T Read;
        public T Write;

        public PingPong(T read, T write)
        {
            Read = read;
            Write = write;
        }

        public void Swap() => (Read, Write) = (Write, Read);
    }
}

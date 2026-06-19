namespace FluxionDrawAndAnimate.Core.Raster;

/// <summary>
/// Reuses fixed-length RGBA tile buffers so painting does not allocate (and
/// thrash the GC) on every dab. Returned buffers are zeroed for reuse.
/// </summary>
public sealed class TileBufferPool
{
    private readonly int _bufferLength;
    private readonly Stack<byte[]> _free = new();

    public TileBufferPool(int bufferLength)
    {
        if (bufferLength <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(bufferLength));
        }

        _bufferLength = bufferLength;
    }

    public int BufferLength => _bufferLength;

    public byte[] Rent()
    {
        return _free.Count > 0 ? _free.Pop() : new byte[_bufferLength];
    }

    public void Return(byte[] buffer)
    {
        if (buffer.Length != _bufferLength)
        {
            return;
        }

        Array.Clear(buffer);
        _free.Push(buffer);
    }
}

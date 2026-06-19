namespace FluxionDrawAndAnimate.Core.Geometry;

public readonly record struct DocumentRect(double X, double Y, double Width, double Height)
{
    public static DocumentRect Empty { get; } = new(0, 0, 0, 0);

    public double Right => X + Width;
    public double Bottom => Y + Height;
    public bool IsEmpty => Width <= 0 || Height <= 0;

    public DocumentRect Inflate(double amount)
    {
        if (IsEmpty)
        {
            return this;
        }

        return new DocumentRect(X - amount, Y - amount, Width + amount * 2, Height + amount * 2);
    }

    public DocumentRect Clamp(double width, double height)
    {
        if (IsEmpty)
        {
            return Empty;
        }

        var x = Math.Clamp(X, 0, width);
        var y = Math.Clamp(Y, 0, height);
        var right = Math.Clamp(Right, 0, width);
        var bottom = Math.Clamp(Bottom, 0, height);
        return new DocumentRect(x, y, Math.Max(0, right - x), Math.Max(0, bottom - y));
    }
}

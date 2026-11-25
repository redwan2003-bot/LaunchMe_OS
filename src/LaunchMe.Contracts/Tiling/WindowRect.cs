namespace LaunchMe.Contracts.Tiling;

/// <summary>
/// Represents an integer bounding rectangle for a desktop window or screen area.
/// </summary>
public record WindowRect(int X, int Y, int Width, int Height)
{
    public int Right => X + Width;
    public int Bottom => Y + Height;
    public int CenterX => X + (Width / 2);
    public int CenterY => Y + (Height / 2);
    public long Area => (long)Math.Max(0, Width) * Math.Max(0, Height);

    public bool Contains(int px, int py) =>
        px >= X && px < Right && py >= Y && py < Bottom;

    public bool IntersectsWith(WindowRect other) =>
        X < other.Right && Right > other.X && Y < other.Bottom && Bottom > other.Y;

    public WindowRect Inset(int dx, int dy) =>
        new(X + dx, Y + dy, Math.Max(0, Width - (dx * 2)), Math.Max(0, Height - (dy * 2)));

    public static WindowRect Empty => new(0, 0, 0, 0);
}

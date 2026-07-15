namespace Snaptic.Core.Display;

/// <summary>Vùng chữ nhật tính bằng pixel vật lý, toạ độ desktop ảo toàn cục.</summary>
public readonly record struct PhysicalRect(int X, int Y, int Width, int Height)
{
    public bool IsEmpty => Width <= 0 || Height <= 0;
}

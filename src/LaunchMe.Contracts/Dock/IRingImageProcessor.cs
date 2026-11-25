namespace LaunchMe.Contracts.Dock;

public sealed record RingStyleConfig(
    int RingCount = 12,
    double RingThickness = 16.0,
    double Spacing = 8.0,
    double Brightness = 1.0,
    double Saturation = 1.0);

public interface IRingImageProcessor
{
    byte[] ProcessImageToRings(byte[] inputImageBytes, RingStyleConfig config);
}

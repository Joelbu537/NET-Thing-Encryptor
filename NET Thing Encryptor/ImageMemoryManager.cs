using System.Diagnostics;
using ImageMagick;

namespace NET_Thing_Encryptor
{
    internal static class ImageMemoryManager
    {
        private const ulong MagickMemoryLimit = 512UL * 1024 * 1024;
        private const ulong MaximumSingleAllocation = 256UL * 1024 * 1024;
        private static int _configured;

        public static void Configure()
        {
            if (Interlocked.Exchange(ref _configured, 1) != 0)
                return;

            // ImageMagick otherwise defaults to half of all available system RAM.
            // That is excessive for a viewer which ultimately displays one screen-sized image.
            ResourceLimits.Memory = MagickMemoryLimit;
            ResourceLimits.MaxMemoryRequest = MaximumSingleAllocation;
            ResourceLimits.Thread = 2;
            OpenCL.IsEnabled = false;
        }

        public static void Trim()
        {
            // ImageMagick currently exposes native heap trimming only on non-Windows
            // platforms. Windows still benefits from the strict cache limit and from
            // disposing/downscaling every decoded image.
            if (OperatingSystem.IsWindows())
                return;

            try
            {
                _ = ResourceLimits.TrimMemory();
            }
            catch (Exception ex)
            {
                // Trimming is an optimization and must never break image playback.
                Debug.WriteLine($"ImageMagick could not trim unused native memory: {ex}");
            }
        }
    }
}

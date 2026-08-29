using System.Runtime;

namespace NET_Thing_Encryptor
{
    internal static class MemoryMaintenance
    {
        private const long LargeReleaseThreshold = 32L * 1024 * 1024;
        private static long _pendingReleasedBytes;
        private static int _collectionScheduled;

        public static void NotifyLargeBufferReleased(long byteCount)
        {
            if (byteCount < LargeReleaseThreshold)
                return;

            Interlocked.Add(ref _pendingReleasedBytes, byteCount);
            if (Interlocked.Exchange(ref _collectionScheduled, 1) != 0)
                return;

            ThreadPool.QueueUserWorkItem(static _ => CompactReleasedBuffers());
        }

        private static void CompactReleasedBuffers()
        {
            try
            {
                // Give form disposal and native callbacks a moment to drop their last references.
                Thread.Sleep(100);
                if (Interlocked.Exchange(ref _pendingReleasedBytes, 0) < LargeReleaseThreshold)
                    return;

                GCSettings.LargeObjectHeapCompactionMode =
                    GCLargeObjectHeapCompactionMode.CompactOnce;
                GC.Collect(
                    GC.MaxGeneration,
                    GCCollectionMode.Aggressive,
                    blocking: true,
                    compacting: true);
            }
            finally
            {
                Volatile.Write(ref _collectionScheduled, 0);
                if (Volatile.Read(ref _pendingReleasedBytes) >= LargeReleaseThreshold &&
                    Interlocked.Exchange(ref _collectionScheduled, 1) == 0)
                {
                    ThreadPool.QueueUserWorkItem(static _ => CompactReleasedBuffers());
                }
            }
        }
    }
}

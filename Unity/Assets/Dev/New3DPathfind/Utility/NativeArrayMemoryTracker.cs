namespace Candy.Pathfind3D
{
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using UnityEngine;

    public static class NativeArrayMemoryTracker
    {
        /// <summary>
        /// NativeArray가 사용하는 총 메모리 크기를 byte 단위로 반환합니다.
        /// </summary>
        public static long GetMemorySizeBytes<T>(NativeArray<T> array) where T : struct
        {
            if (!array.IsCreated)
                return 0;

            int elementSize = UnsafeUtility.SizeOf<T>();
            long totalSize = elementSize * array.Length;
            return totalSize;
        }

        /// <summary>
        /// NativeArray의 메모리 사용량을 로그로 출력합니다.
        /// </summary>
        public static void LogMemoryUsage<T>(NativeArray<T> array, string arrayName = "NativeArray") where T : struct
        {
            long sizeInBytes = GetMemorySizeBytes(array);
            Debug.Log($"{arrayName} 메모리 사용량: {FormatBytes(sizeInBytes)} ({sizeInBytes} bytes)");
        }
        
        /// <summary>
        /// NativeArray의 메모리 사용량 로그를 string으로 반환
        /// </summary>
        public static string GetMemoryUsageMessage<T>(NativeArray<T> array, out long sizeInBytes, string arrayName = "NativeArray") where T : struct
        {
            sizeInBytes = GetMemorySizeBytes(array);
            return $"{arrayName} 메모리 사용량: {FormatBytes(sizeInBytes)} ({sizeInBytes} bytes)";
        }

        /// <summary>
        /// Byte 수를 사람이 읽기 좋은 형태(KB, MB 등)로 포맷합니다.
        /// </summary>
        public static string FormatBytes(long bytes)
        {
            if (bytes >= 1073741824)
                return $"{(bytes / 1073741824f):F2} GB";
            if (bytes >= 1048576)
                return $"{(bytes / 1048576f):F2} MB";
            if (bytes >= 1024)
                return $"{(bytes / 1024f):F2} KB";
            return $"{bytes} Bytes";
        }
    }
}
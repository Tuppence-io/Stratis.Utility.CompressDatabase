using System;
using System.IO;

namespace Stratis.Utility.CompressDatabase
{
    public static class FileInfoExtensions
    {
        public static void CopyToDirectory(this FileInfo fileInfo, string path, Action<FileInfo, long, long> progress)
        {
            // Get the filename as we are keeping it that same.
            var targetFilename = Path.Combine(path, fileInfo.Name);

            // Copy the file
            fileInfo.CopyTo(targetFilename, progress);
        }

        private static void CopyTo(this FileInfo sourceFileInfo, string filename, Action<FileInfo, long, long> progress)
        {
            const int bufferSize = 1024 * 1024 * 100; // 100 MB
            var buffer = new byte[bufferSize];

            var sourceSize = sourceFileInfo.Length;
            long bytesCopied = 0;

            var targetFileInfo = new FileInfo(filename);

            using (var sourceStream = sourceFileInfo.OpenRead())
            using (var targetStream = targetFileInfo.OpenWrite())
            {
                // This will speed things up a bit by allocating the full stream size so the 
                // stream doesn't have to keep allocating more space as we write.
                targetStream.SetLength(sourceSize);

                int bytesRead;
                while ((bytesRead = sourceStream.Read(buffer, 0, bufferSize)) != 0)
                {
                    targetStream.Write(buffer, 0, bytesRead);

                    // Notify the call of the progress
                    bytesCopied += bytesRead;
                    progress?.Invoke(targetFileInfo, bytesCopied, sourceSize);
                }
            }
        }
    }
}

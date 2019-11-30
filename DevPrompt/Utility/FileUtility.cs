using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace DevPrompt.Utility
{
    /// <summary>
    /// Helpers for dealing with the file system
    /// </summary>
    internal static class FileUtility
    {
        public const int DefaultStreamCopyBufferSize = 81920;

        public static bool TryDeleteFile(string file)
        {
            if (string.IsNullOrEmpty(file) || !File.Exists(file))
            {
                return false;
            }

            try
            {
                File.Delete(file);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool TryMoveFile(string source, string dest)
        {
            if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(dest) || !File.Exists(source))
            {
                return false;
            }

            FileUtility.TryDeleteFile(dest);

            try
            {
                File.Move(source, dest);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static async Task<string> DownloadFileAsync(HttpContent content, CancellationToken cancelToken)
        {
            string tempFile = null;

            try
            {
                tempFile = Path.GetTempFileName();

                using (FileStream tempFileStream = File.OpenWrite(tempFile))
                using (Stream downloadStream = await content.ReadAsStreamAsync())
                {
                    await downloadStream.CopyToAsync(tempFileStream, FileUtility.DefaultStreamCopyBufferSize, cancelToken);
                    return tempFile;
                }
            }
            catch
            {
                FileUtility.TryDeleteFile(tempFile);
                return null;
            }
        }
    }
}

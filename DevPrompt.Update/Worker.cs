using DevPrompt.Update.Utility;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DevPrompt.Update
{
    internal sealed class Worker : PropertyNotifier
    {
        public IReadOnlyList<WorkerStage> Stages => this.stages;
        public WorkerStage CurrentStage => this.stages.FirstOrDefault(s => s.Stage == this.CurrentStageType);
        private WorkerStateType state;
        private readonly ObservableCollection<WorkerStage> stages;
        private readonly string folderPath;
        private readonly string exePath;
        private string failureText;
        private string failureDetails;

        private const string ProcessName = "DevPrompt";
        private const string ExeName = "DevPrompt.exe";
        private const int WaitForProcessExitMilliseconds = 100;

        public Worker()
        {
            this.failureText = string.Empty;
            this.failureDetails = string.Empty;
            this.folderPath = Path.GetFullPath(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location));
            this.exePath = Path.GetFullPath(Path.Combine(this.folderPath, Worker.ExeName));

            this.stages = new ObservableCollection<WorkerStage>()
            {
                new WorkerStage(this, WorkerStageType.ClosingAppWindows),
                new WorkerStage(this, WorkerStageType.Downloading),
                new WorkerStage(this, WorkerStageType.Installing),
                new WorkerStage(this, WorkerStageType.Done),
            };
        }

        public string FailureText
        {
            get => this.failureText;
            set => this.SetPropertyValue(ref this.failureText, value ?? string.Empty);
        }

        public string FailureDetails
        {
            get => this.failureDetails;
            set => this.SetPropertyValue(ref this.failureDetails, value ?? string.Empty);
        }

        public WorkerStateType State
        {
            get => this.state;
            set
            {
                WorkerStageType oldStage = this.CurrentStageType;
                if (this.SetPropertyValue(ref this.state, value) && oldStage != this.CurrentStageType)
                {
                    this.OnPropertyChanged(nameof(this.CurrentStageType));
                    this.OnPropertyChanged(nameof(this.CurrentStage));
                }
            }
        }

        public WorkerStageType CurrentStageType
        {
            get
            {
                switch (this.State)
                {
                    default:
                    case WorkerStateType.None:
                    case WorkerStateType.ClosingWindows:
                        return WorkerStageType.ClosingAppWindows;

                    case WorkerStateType.DownloadingHeaders:
                    case WorkerStateType.Downloading:
                        return WorkerStageType.Downloading;

                    case WorkerStateType.Extracting:
                        return WorkerStageType.Installing;

                    case WorkerStateType.DoneSuccess:
                    case WorkerStateType.DoneCancel:
                    case WorkerStateType.DoneException:
                        return WorkerStageType.Done;
                }
            }
        }

        public async Task<bool> UpdateAsync(CancellationToken cancelToken)
        {
            try
            {
                await this.CloseWindows(cancelToken);

                using (HttpClient client = new HttpClient())
                using (HttpResponseMessage response = await this.DownloadZipFile(client, cancelToken))
                {
                    response.EnsureSuccessStatusCode();
                    await this.ExtractZipFile(response, cancelToken);
                }

                await this.RunNewProcess(cancelToken);
            }
            catch (TaskCanceledException)
            {
                this.State = WorkerStateType.DoneCancel;
            }
            catch (Exception ex)
            {
                this.State = WorkerStateType.DoneException;
                this.FailureText = $"{ex.GetType()}: {ex.Message}";
                this.FailureDetails = ex.StackTrace;
            }

            return this.State != WorkerStateType.DoneException;
        }

        [DllImport("Kernel32", SetLastError = true)]
        static extern bool QueryFullProcessImageName([In] IntPtr hProcess, [In] int dwFlags, [Out] StringBuilder lpExeName, ref uint lpdwSize);

        private static string GetProcessExePath(Process process)
        {
            uint bufferSize = 1024;
            StringBuilder buffer = new StringBuilder((int)bufferSize);
            Worker.QueryFullProcessImageName(process.Handle, 0, buffer, ref bufferSize);
            return Path.GetFullPath(buffer.ToString(0, (int)bufferSize));
        }

        private async Task CloseWindows(CancellationToken cancelToken)
        {
            this.State = WorkerStateType.ClosingWindows;

            await this.WaitForOwnerProcessToExit(cancelToken);

            await Task.Run(() =>
            {
                while (true)
                {
                    Process[] processes = Process.GetProcessesByName(Worker.ProcessName).Where(p =>
                    {
                        try
                        {
                            return !p.HasExited && string.Compare(Worker.GetProcessExePath(p), this.exePath, true) == 0;
                        }
                        catch
                        {
                            return false;
                        }
                    }).ToArray();

                    if (processes.Length == 0)
                    {
                        break;
                    }

                    foreach (Process process in processes)
                    {
                        cancelToken.ThrowIfCancellationRequested();

                        try
                        {
                            if (!process.CloseMainWindow())
                            {
                                process.Kill();
                            }

                            while (true)
                            {
                                cancelToken.ThrowIfCancellationRequested();

                                if (process.WaitForExit(Worker.WaitForProcessExitMilliseconds))
                                {
                                    break;
                                }
                            }
                        }
                        catch (InvalidOperationException) when (process.HasExited)
                        {
                            // Already exited
                        }
                    }
                }
            }, cancelToken);
        }

        private Task WaitForOwnerProcessToExit(CancellationToken cancelToken)
        {
            return Task.Run(() =>
            {
                string[] args = Environment.GetCommandLineArgs();
                for (int i = 0; i < args.Length; i++)
                {
                    if (args[i] == "/waitfor")
                    {
                        int pid = int.Parse(args[++i]);
                        Process process = null;
                        try
                        {
                            process = Process.GetProcessById(pid);
                        }
                        catch
                        {
                            // doesn't matter if this fails, we'll try to close the window later
                        }

                        if (process != null)
                        {
                            while (true)
                            {
                                cancelToken.ThrowIfCancellationRequested();

                                if (process.WaitForExit(Worker.WaitForProcessExitMilliseconds))
                                {
                                    break;
                                }
                            }
                        }
                    }
                }
            }, cancelToken);
        }

        private Task<HttpResponseMessage> DownloadZipFile(HttpClient client, CancellationToken cancelToken)
        {
            this.State = WorkerStateType.DownloadingHeaders;

            return client.GetAsync(Resources.ZipFileUrl, HttpCompletionOption.ResponseHeadersRead, cancelToken);
        }

        private struct FileInfo
        {
            public string DestFile;
            public string NewFile;
            public string BackupFile;
        }

        private async Task ExtractZipFile(HttpResponseMessage response, CancellationToken cancelToken)
        {
            this.State = WorkerStateType.Extracting;

            Guid tempGuid = Guid.NewGuid();
            string tempRoot = Path.Combine(this.folderPath, $"Update.{tempGuid}");
            string backupRoot = Path.Combine(this.folderPath, $"Backup.{tempGuid}");
            List<FileInfo> replacedFileInfos = new List<FileInfo>();

            await Task.Run(() =>
            {
                Directory.CreateDirectory(tempRoot);
                Directory.CreateDirectory(backupRoot);
            }, cancelToken);

            try
            {
                List<FileInfo> fileInfos = await Task.Run(async () =>
                {
                    List<FileInfo> taskFiles = new List<FileInfo>();

                    using (Stream zipStream = await response.Content.ReadAsStreamAsync())
                    using (ZipArchive zip = new ZipArchive(zipStream, ZipArchiveMode.Read, leaveOpen: true))
                    {
                        foreach (ZipArchiveEntry entry in zip.Entries)
                        {
                            cancelToken.ThrowIfCancellationRequested();

                            FileInfo info = new FileInfo()
                            {
                                DestFile = Path.Combine(this.folderPath, entry.FullName),
                                NewFile = Path.Combine(tempRoot, entry.FullName),
                                BackupFile = Path.Combine(backupRoot, entry.FullName),
                            };

                            Directory.CreateDirectory(Path.GetDirectoryName(info.DestFile));
                            Directory.CreateDirectory(Path.GetDirectoryName(info.NewFile));
                            Directory.CreateDirectory(Path.GetDirectoryName(info.BackupFile));

                            using (Stream entryStream = entry.Open())
                            using (FileStream fileStream = File.Create(info.NewFile))
                            {
                                await entryStream.CopyToAsync(fileStream, 81920, cancelToken);
                                taskFiles.Add(info);
                            }
                        }
                    }

                    return taskFiles;
                }, cancelToken);

                await Task.Run(() =>
                {
                    foreach (FileInfo info in fileInfos)
                    {
                        cancelToken.ThrowIfCancellationRequested();

                        using (File.OpenWrite(info.DestFile))
                        {
                            // Probably good to replace this file
                        }
                    }
                }, cancelToken);

                await Task.Run(() =>
                {
                    foreach (FileInfo info in fileInfos)
                    {
                        cancelToken.ThrowIfCancellationRequested();
                        File.Replace(info.NewFile, info.DestFile, info.BackupFile, ignoreMetadataErrors: true);
                        replacedFileInfos.Add(info);
                    }
                }, cancelToken);
            }
            catch
            {
                // For any failure or cancel, restore the original files from the backup

                foreach (FileInfo info in replacedFileInfos)
                {
                    try
                    {
                        File.Replace(info.BackupFile, info.DestFile, null, ignoreMetadataErrors: true);
                    }
                    catch
                    {
                        // Not good, but have to keep going to replace the rest of the files
                    }
                }

                throw;
            }
            finally
            {
                // Afterwards the temp update and backup folders should be deleted

                try
                {
                    await Task.Run(() =>
                    {
                        Directory.Delete(tempRoot, recursive: true);
                        Directory.Delete(backupRoot, recursive: true);
                    });
                }
                catch
                {
                    // Not the end of the world if the temp directory sticks around
                }
            }
        }

        private Task RunNewProcess(CancellationToken cancelToken)
        {
            return Task.Run(() =>
            {
                if (Process.Start(this.exePath) is Process process)
                {
                    this.State = WorkerStateType.DoneSuccess;
                }
                else
                {
                    throw new Exception(string.Format(CultureInfo.CurrentCulture, Resources.Error_FailedToStart, this.exePath));
                }
            }, cancelToken);
        }
    }
}

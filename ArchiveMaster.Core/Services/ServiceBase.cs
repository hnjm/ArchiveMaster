using System.Collections;
using System.Diagnostics;
using ArchiveMaster.Configs;
using ArchiveMaster.Enums;
using ArchiveMaster.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using SimpleFileInfo = ArchiveMaster.ViewModels.FileSystem.SimpleFileInfo;

namespace ArchiveMaster.Services
{
    public abstract class ServiceBase<TConfig>(AppConfig appConfig)
        where TConfig : ConfigBase
    {
        public event EventHandler<MessageUpdateEventArgs> MessageUpdate;

        public event EventHandler<ProgressUpdateEventArgs> ProgressUpdate;
        public TConfig Config { get; set; }

        protected void NotifyMessage(string message)
        {
            MessageUpdate?.Invoke(this, new MessageUpdateEventArgs(message));
            Debug.WriteLine(message);
        }

        protected void NotifyProgress(double percent)
        {
            ProgressUpdate?.Invoke(this, new ProgressUpdateEventArgs(percent));
            Debug.WriteLine($"{percent * 100:0.00}%");
        }

        protected void NotifyProgressIndeterminate()
        {
            NotifyProgress(double.NaN);
        }

        protected FilesLoopStates TryForFiles<T>(IEnumerable<T> files, Action<T, FilesLoopStates> body,
            CancellationToken cancellationToken,
            FilesLoopOptions options = null)
            where T : SimpleFileInfo
        {
            options ??= FilesLoopOptions.DoNothing();
            var states = new FilesLoopStates(options);

            PreProcessStatistic(files, options, states);

            switch (options.Threads)
            {
                case 1:
                    ForEachFileCore(files, body, cancellationToken, options, states);
                    break;
                case >= 2 or 0:
                    ParallelForEachFileCore(files, body, cancellationToken, options, states);
                    break;
                default:
                    throw new ArgumentOutOfRangeException("Threads应为大于等于0的整数，其中1表示单线程，0表示自动多线程，>=2表示指定线程数");
            }

            return states;
        }

        protected async Task<FilesLoopStates> TryForFilesAsync<T>(IEnumerable<T> files, Action<T, FilesLoopStates> body,
            CancellationToken cancellationToken, FilesLoopOptions options = null)
            where T : SimpleFileInfo
        {
            FilesLoopStates states = null;
            await Task.Run(() => { states = TryForFiles(files, body, cancellationToken, options); }, cancellationToken);
            return states;
        }

        protected async Task<FilesLoopStates> TryForFilesAsync<T>(IEnumerable<T> files,
            Func<T, FilesLoopStates, Task> asyncBody,
            CancellationToken cancellationToken, FilesLoopOptions options = null)
            where T : SimpleFileInfo
        {
            options ??= FilesLoopOptions.DoNothing();
            var states = new FilesLoopStates(options);

            PreProcessStatistic(files, options, states);

            switch (options.Threads)
            {
                case 1:
                    await ForEachFileCoreAsync(files, asyncBody, cancellationToken, options, states);
                    break;
                case >= 2 or 0:
                    await ParallelForEachFileCoreAsync(files, asyncBody, cancellationToken, options, states);
                    break;
                default:
                    throw new ArgumentOutOfRangeException("Threads应为大于等于0的整数，其中1表示单线程，0表示自动多线程，>=2表示指定线程数");
            }

            return states;
        }

        private static void PreProcessStatistic<T>(IEnumerable<T> files, FilesLoopOptions options,
            FilesLoopStates states)
            where T : SimpleFileInfo
        {
            if (options.AutoApplyProgress == AutoApplyProgressMode.FileLength && options.TotalLength == 0)
            {
                states.CanAccessTotalLength = true;

                foreach (var file in files)
                {
                    if (file is not SimpleFileInfo f)
                    {
                        states.CanAccessTotalLength = false;
                        throw new ArgumentException(
                            "集合内的元素必须都为SimpleFileInfo时，才可以使用AutoApplyProgressMode.FileLength",
                            nameof(files));
                    }

                    states.TotalLength += f.Length;
                }
            }

            if (options.TotalCount == 0)
            {
                if (files is ICollection collection)
                {
                    states.FileCount = collection.Count;
                }
                else
                {
                    if (options.AutoApplyProgress == AutoApplyProgressMode.FileNumber)
                    {
                        throw new ArgumentException(
                            "必须为集合，才可以使用AutoApplyProgressMode.FileNumber",
                            nameof(files));
                    }
                }
            }
        }

        private static void ProcessAutoApplyStatus<T>(FilesLoopOptions options, T file) where T : SimpleFileInfo
        {
            if (options.AutoApplyStatus)
            {
                if (file.Status == ProcessStatus.Ready)
                {
                    file.Complete();
                }
            }
        }

        private static void ProcessException<T>(FilesLoopOptions options, T file, Exception ex) where T : SimpleFileInfo
        {
            file.Error(ex);
            options.CatchAction?.Invoke(file, ex);
            if (options.ThrowExceptions)
            {
                throw ex;
            }
        }

        private void ForEachFileCore<T>(IEnumerable<T> files, Action<T, FilesLoopStates> body,
            CancellationToken cancellationToken,
            FilesLoopOptions options, FilesLoopStates states) where T : SimpleFileInfo
        {
            foreach (var file in files)
            {
                TryForFilesSingle(body, cancellationToken, options, file, states);
                if (states.NeedBroken)
                {
                    break;
                }
            }
        }

        private async Task ForEachFileCoreAsync<T>(IEnumerable<T> files, Func<T, FilesLoopStates, Task> asyncBody,
            CancellationToken cancellationToken, FilesLoopOptions options, FilesLoopStates states)
            where T : SimpleFileInfo
        {
            foreach (var file in files)
            {
                await TryForFilesSingleAsync(asyncBody, cancellationToken, options, file, states);
                if (states.NeedBroken)
                {
                    break;
                }
            }
        }

        private void ParallelForEachFileCore<T>(IEnumerable<T> files, Action<T, FilesLoopStates> body,
            CancellationToken cancellationToken,
            FilesLoopOptions options, FilesLoopStates states) where T : SimpleFileInfo
        {
            Parallel.ForEach(files, new ParallelOptions()
            {
                MaxDegreeOfParallelism = options.Threads <= 0 ? -1 : options.Threads,
                CancellationToken = cancellationToken
            }, (file, s) =>
            {
                TryForFilesSingle(body, cancellationToken, options, file, states);
                if (states.NeedBroken)
                {
                    s.Break();
                }
            });
        }


        private Task ParallelForEachFileCoreAsync<T>(IEnumerable<T> files,
            Func<T, FilesLoopStates, Task> asyncBody,
            CancellationToken cancellationToken, FilesLoopOptions options, FilesLoopStates states)
            where T : SimpleFileInfo
        {
            return Parallel.ForEachAsync(files, new ParallelOptions()
            {
                MaxDegreeOfParallelism = options.Threads <= 0 ? -1 : options.Threads,
                CancellationToken = cancellationToken
            }, async (file, c) => { await TryForFilesSingleAsync(asyncBody, c, options, file, states); });
        }

        private void ProcessFinally<T>(FilesLoopOptions options, T file, FilesLoopStates states)
            where T : SimpleFileInfo
        {
            if (states.CanAccessTotalLength)
            {
                states.IncreaseFileLength(file.Length);
            }

            states.IncreaseFileIndex();

            switch (options.AutoApplyProgress)
            {
                case AutoApplyProgressMode.FileLength:
                    NotifyProgress(1.0 * states.AccumulatedLength / states.TotalLength);
                    break;
                case AutoApplyProgressMode.FileNumber:
                    NotifyProgress(1.0 * states.FileIndex / states.FileCount);
                    break;
            }

            options.FinallyAction?.Invoke(file);
        }

        private void TryForFilesSingle<T>(Action<T, FilesLoopStates> body, CancellationToken cancellationToken,
            FilesLoopOptions options, T file,
            FilesLoopStates states) where T : SimpleFileInfo
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                body(file, states);
                ProcessAutoApplyStatus(options, file);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                ProcessException(options, file, ex);
            }
            finally
            {
                ProcessFinally(options, file, states);
            }


            if (appConfig.DebugMode && appConfig.DebugModeLoopDelay > 0)
            {
                Thread.Sleep(appConfig.DebugModeLoopDelay);
            }
        }

        private async Task TryForFilesSingleAsync<T>(Func<T, FilesLoopStates, Task> asyncBody,
            CancellationToken cancellationToken,
            FilesLoopOptions options, T file,
            FilesLoopStates states) where T : SimpleFileInfo
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                await asyncBody(file, states);
                ProcessAutoApplyStatus(options, file);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                ProcessException(options, file, ex);
            }
            finally
            {
                ProcessFinally(options, file, states);
            }


            if (appConfig.DebugMode && appConfig.DebugModeLoopDelay > 0)
            {
                await Task.Delay(appConfig.DebugModeLoopDelay);
            }
        }
    }
}
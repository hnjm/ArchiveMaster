namespace ArchiveMaster.ViewModels.FileSystem
{
    public class SlimmingFilesInfo
    {
        private List<SimpleFileInfo> processingFiles = new List<SimpleFileInfo>();

        private IList<string> processingFilesRelativePaths;

        private string rootDir;

        private List<SimpleFileInfo> skippedFiles = new List<SimpleFileInfo>();

        public SlimmingFilesInfo(string rootDir)
        {
            this.rootDir = rootDir;
        }

        public IReadOnlyList<SimpleFileInfo> ProcessingFiles => processingFiles.AsReadOnly();

        public long ProcessingFilesLength { get; private set; } = 0;

        public IList<string> ProcessingFilesRelativePaths => processingFilesRelativePaths ??
                                                             throw new Exception(
                                                                 $"还未调用{nameof(CreateRelativePathsAsync)}方法");

        public IReadOnlyList<SimpleFileInfo> SkippedFiles => skippedFiles.AsReadOnly();

        public void Add(SimpleFileInfo file)
        {
            processingFiles.Add(file);
            if (!file.IsDir)
            {
                ProcessingFilesLength += file.Length;
            }
        }

        public void AddSkipped(SimpleFileInfo file)
        {
            skippedFiles.Add(file);
        }

        public void Clear()
        {
            processingFiles = null;
            skippedFiles = null;
            ProcessingFilesLength = 0;
        }

        public Task CreateRelativePathsAsync()
        {
            return Task.Run(() =>
            {
                processingFilesRelativePaths = processingFiles
                    .Select(p =>p.RelativePath + (p.IsDir ? Path.DirectorySeparatorChar : ""))
                    .ToList();
            });
        }
    }
}
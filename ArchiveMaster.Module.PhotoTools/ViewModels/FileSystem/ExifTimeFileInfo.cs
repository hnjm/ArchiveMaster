namespace ArchiveMaster.ViewModels.FileSystem;

public class ExifTimeFileInfo : SimpleFileInfo
{
    public ExifTimeFileInfo(FileInfo file, string topDir) : base(file, topDir)
    {
    }

    public DateTime? ExifTime { get; set; }
}
using ArchiveMaster.Enums;

namespace ArchiveMaster.Configs;

public class GlobalConfigs
{
    public static GlobalConfigs Instance { get; internal set; } = new GlobalConfigs();
    public FilenameCasePolicy FileNameCase { get; set; } = FilenameCasePolicy.Auto;
}
using CommunityToolkit.Mvvm.ComponentModel;

namespace ArchiveMaster.Configs
{
    public partial class FileCopyTestConfig : ConfigBase
    {
        [ObservableProperty]
        private string sourceDir;

        [ObservableProperty]
        private string destinationDir;
        
        public override void Check()
        {
        }
    }
}
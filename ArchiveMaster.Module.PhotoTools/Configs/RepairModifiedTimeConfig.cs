using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ArchiveMaster.Configs
{
    public partial class RepairModifiedTimeConfig : ConfigBase
    {
        [ObservableProperty]
        private string dir;

        [ObservableProperty]
        private int threadCount = 2;

        [ObservableProperty]
        private TimeSpan maxDurationTolerance = TimeSpan.FromSeconds(1);

        public override void Check()
        {
            CheckDir(Dir,"目录");
        }
    }
}

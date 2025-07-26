using System;
using ArchiveMaster.Enums;
using CommunityToolkit.Mvvm.ComponentModel;
using FzLib.IO;

namespace ArchiveMaster.Configs
{
    public partial class BatchCommandLineConfig : ConfigBase
    {
        [ObservableProperty]
        private string arguments;

        [ObservableProperty]
        private string autoCreateDir;

        [ObservableProperty]
        private string dir;

        [ObservableProperty]
        private int level = 1;

        [ObservableProperty]
        private FileFilterRule filter = new FileFilterRule();

        [ObservableProperty]
        private string program;

        [ObservableProperty]
        private BatchTarget target;

        public override void Check()
        {
            CheckDir(Dir, "目录");
            CheckEmpty(Program, "程序");
            CheckEmpty(Arguments, "参数");

            if (Target is BatchTarget.SpecialLevelDirs or BatchTarget.SpecialLevelElements
                or BatchTarget.SpecialLevelFiles)
            {
                if (Level is not (>= 1 and <= 100))
                {
                    throw new Exception("层数应当在1-100之间");
                }
            }
        }
    }
}
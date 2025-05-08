using ArchiveMaster.Configs;
using ArchiveMaster.ViewModels;
using ArchiveMaster.Views;
using Avalonia;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ArchiveMaster.Models;
using ArchiveMaster.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ArchiveMaster
{
    public class PhotoToolsModuleInfo : IModuleInfo
    {
        private readonly string baseUrl = "avares://ArchiveMaster.Module.PhotoTools/Assets/";
        public IList<Type> BackgroundServices { get; }
        public IList<ConfigMetadata> Configs =>
        [
            new ConfigMetadata(typeof(RepairModifiedTimeConfig)),
            new ConfigMetadata(typeof(PhotoSlimmingConfig)),
        ];

        public string ModuleName => "照片工具";

        public int Order => 2;
        public IList<Type> SingletonServices { get; }

        public IList<Type> TransientServices { get; } =
        [
            typeof(PhotoSlimmingService),
            typeof(RepairModifiedTimeService),
        ];

        public ToolPanelGroupInfo Views => new ToolPanelGroupInfo()
        {
            Panels =
            {
                                new ToolPanelInfo(typeof(RepairModifiedTimePanel), typeof(RepairModifiedTimeViewModel), "修复照片修改时间",
                    "寻找EXIF信息中的拍摄时间与照片修改时间不同的文件，将修改时间更新闻EXIF时间", baseUrl + "time.svg"),
                new ToolPanelInfo(typeof(PhotoSlimmingPanel), typeof(PhotoSlimmingViewModel), "创建照片集合副本",
                    "复制或压缩照片，用于生成更小的照片集副本", baseUrl + "zip.svg"),
            },
            GroupName = ModuleName
        };
    }
}
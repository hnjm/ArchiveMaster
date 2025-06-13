using CommunityToolkit.Mvvm.ComponentModel;

namespace ArchiveMaster.Configs
{
    public partial class FileFilterConfig : ObservableObject
    {
        private static readonly string DefaultExcludeFiles = $"Thumbs.db{Environment.NewLine}Thumb.db{Environment.NewLine}desktop.ini";
        
        private static readonly string DefaultExcludeFilesR = @"^(Thumbs?\.db)|(desktop.ini)$";
        
        private static readonly string DefaultExcludeFolders = "$*";
        
        private static readonly string DefaultExcludeFoldersR = @"\$.*";
        
        [ObservableProperty]
        private string excludeFiles = DefaultExcludeFiles;

        [ObservableProperty]
        private string excludeFolders = DefaultExcludeFolders;

        [ObservableProperty]
        private string excludePaths = "";

        [ObservableProperty]
        private string includeFiles = "*";

        [ObservableProperty]
        private string includeFolders = "*";

        [ObservableProperty]
        private string includePaths = "*";

        [ObservableProperty]
        private bool useRegex;

        partial void OnUseRegexChanged(bool value)
        {
            if (value)
            {
                IncludeFiles = IncludeFiles == "*" ? ".*" : IncludeFiles;
                IncludeFolders = IncludeFolders == "*" ? ".*" : IncludeFolders;
                IncludePaths = IncludePaths == "*" ? ".*" : IncludePaths;
                ExcludeFiles = ExcludeFiles == DefaultExcludeFiles ? DefaultExcludeFilesR : ExcludeFiles;
                ExcludeFolders = ExcludeFolders == DefaultExcludeFolders ? DefaultExcludeFoldersR : ExcludeFolders;
                

                IncludeFiles = IncludeFiles.Replace(Environment.NewLine, "");
                IncludeFolders = IncludeFolders.Replace(Environment.NewLine, "");
                IncludePaths = IncludePaths.Replace(Environment.NewLine, "");
                ExcludeFiles = ExcludeFiles.Replace(Environment.NewLine, "");
                ExcludeFolders = ExcludeFolders.Replace(Environment.NewLine, "");
                ExcludePaths = ExcludePaths.Replace(Environment.NewLine, "");
            }
            else
            {
                IncludeFiles = IncludeFiles == ".*" ? "*" : IncludeFiles;
                IncludeFolders = IncludeFolders == ".*" ? "*" : IncludeFolders;
                IncludePaths = IncludePaths == ".*" ? "*" : IncludePaths;
                ExcludeFiles = ExcludeFiles == DefaultExcludeFilesR ? DefaultExcludeFiles : ExcludeFiles;
                ExcludeFolders = ExcludeFolders == DefaultExcludeFoldersR ? DefaultExcludeFolders : ExcludeFolders;
            }
        }
    }
}
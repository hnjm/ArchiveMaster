using System.ComponentModel;

namespace ArchiveMaster.Enums;

public enum RenameMode
{
    [Description("替换关键词")]
    ReplaceMatched,
    
    [Description("替换扩展名")]
    ReplaceExtension,
    
    [Description("替换文件名")]
    ReplaceName,
    
    [Description("替换全名")]
    ReplaceAll,

    [Description("保留匹配值")]
    RetainMatched,

    [Description("保留匹配值和扩展名")]
    RetainMatchedExtension,
    
    [Description("高级（使用C#代码）")]
    Csharp
}
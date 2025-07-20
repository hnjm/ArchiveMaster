﻿using ArchiveMaster.Configs;
using ArchiveMaster.Enums;
using ArchiveMaster.ViewModels.FileSystem;
using FzLib.IO;

namespace ArchiveMaster.Helpers;

public static class FileHelper
{
    public static FileFilterRule ImageFileFilterRule => new FileFilterRule()
    {
        IncludeFiles = """
                       *.heic
                       *.heif
                       *.jpg
                       *.jpeg
                       *.dng
                       *.arw
                       """
    };

    public static FileFilterRule NoRawImageFileFilterRule => new FileFilterRule()
    {
        IncludeFiles = """
                       *.heic
                       *.heif
                       *.jpg
                       *.jpeg
                       """
    };

    public static void DeleteByConfig(string path)
    {
        if (GlobalConfigs.Instance.PreferDeleteToRecycleBin)
        {
            try
            {
                FileDeleteHelper.DeleteToRecycleBin(path);
            }
            catch
            {
                FileDeleteHelper.DirectlyDelete(path);
            }
        }
        else
        {
            FileDeleteHelper.DirectlyDelete(path);
        }
    }

    public static bool IsMatched(this FileFilterHelper fileFilterHelper, SimpleFileInfo file)
    {
        return fileFilterHelper.IsMatched(file.Path);
    }


    public static StringComparer GetStringComparer()
    {
        switch (GlobalConfigs.Instance.FileNameCase)
        {
            case FilenameCasePolicy.Auto:
                if (OperatingSystem.IsWindows())
                {
                    return StringComparer.OrdinalIgnoreCase;
                }
                else if (OperatingSystem.IsLinux())
                {
                    return StringComparer.Ordinal;
                }

                return StringComparer.OrdinalIgnoreCase;
            case FilenameCasePolicy.Ignore:
                return StringComparer.OrdinalIgnoreCase;
            case FilenameCasePolicy.Sensitive:
                return StringComparer.Ordinal;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public static int GetOptimalBufferSize(long fileLength)
    {
        return fileLength switch
        {
            < 1 * 1024 * 1024 => 16 * 1024, // 小文件（<1MB）：16KB
            < 32 * 1024 * 1024 => 1 * 1024 * 1024, // 中等文件（1MB~32MB）：1MB
            _ => 4 * 1024 * 1024 // 大文件（>32MB）：4MB
        };
    }
}
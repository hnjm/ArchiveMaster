using MetadataExtractor;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using Rational = SixLabors.ImageSharp.Rational;

namespace ArchiveMaster.Helpers;

public static class ExifHelper
{
    public static DateTime? FindExifTime(string file)
    {
        IReadOnlyList<MetadataExtractor.Directory> directories = ImageMetadataReader.ReadMetadata(file);

        foreach (var dir in directories.Where(p => p.Name == "Exif SubIFD"))
        {
            if (dir.TryGetDateTime(36867, out DateTime time1))
            {
                return time1;
            }

            if (dir.TryGetDateTime(36868, out DateTime time2))
            {
                return time2;
            }
        }

        MetadataExtractor.Directory dir2 = null;
        if ((dir2 = directories.FirstOrDefault(p => p.Name == "Exif IFD0")) != null)
        {
            if (dir2.TryGetDateTime(306, out DateTime time))
            {
                return time;
            }
        }

        return null;
    }

    public static void WriteGpsToImage(string filePath, double lat, double lon)
    {
        using var image = Image.Load(filePath);
        var exifProfile = image.Metadata.ExifProfile ?? new ExifProfile();

        // 写入纬度
        exifProfile.SetValue(ExifTag.GPSLatitude, ToRationalDegrees(Math.Abs(lat)));
        exifProfile.SetValue(ExifTag.GPSLatitudeRef, lat >= 0 ? "N" : "S");

        // 写入经度
        exifProfile.SetValue(ExifTag.GPSLongitude, ToRationalDegrees(Math.Abs(lon)));
        exifProfile.SetValue(ExifTag.GPSLongitudeRef, lon >= 0 ? "E" : "W");

        // 保存修改
        image.Save(filePath);
    }

    private static Rational[] ToRationalDegrees(double decimalDegrees)
    {
        int degrees = (int)decimalDegrees;
        double remaining = (decimalDegrees - degrees) * 60;
        int minutes = (int)remaining;
        double seconds = (remaining - minutes) * 60;

        return new Rational[]
        {
            new Rational((uint)degrees, 1),     // 度
            new Rational((uint)minutes, 1),    // 分
            new Rational(seconds, true)        // 秒（自动优化精度）
        };
    }
}
using ExifLibrary;
using MetadataExtractor;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using ExifTag = ExifLibrary.ExifTag;
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
    public static (int degrees, int minutes, double seconds) ConvertToDmsTuple(double decimalDegrees)
    {
        // 取绝对值（方向由外部处理）
        decimalDegrees = Math.Abs(decimalDegrees);

        // 计算度、分、秒
        int degrees = (int)decimalDegrees;
        double remainingMinutes = (decimalDegrees - degrees) * 60;
        int minutes = (int)remainingMinutes;
        double seconds = (remainingMinutes - minutes) * 60;

        // 处理浮点误差（如 59.9999 秒进位）
        if (seconds >= 59.999)
        {
            seconds = 0;
            minutes++;
            if (minutes >= 60)
            {
                minutes = 0;
                degrees++;
            }
        }

        return (degrees, minutes, seconds);
    }

    public static void WriteGpsToImage(string filePath, double lat, double lon)
    {
        var imageFile = ImageFile.FromFile(filePath);

        // 写入纬度（需转换为 EXIF 格式：度/分/秒）
        var (d, m, s) = ConvertToDmsTuple(lat);
        imageFile.Properties.Set(ExifTag.GPSLatitude, d, m, (float)s);
        imageFile.Properties.Set(ExifTag.GPSLatitudeRef, lat >= 0 ? "N" : "S");

        // 写入经度
        (d, m, s) = ConvertToDmsTuple(lon);
        imageFile.Properties.Set(ExifTag.GPSLongitude, d, m, (float)s);
        imageFile.Properties.Set(ExifTag.GPSLongitudeRef, lon >= 0 ? "E" : "W");

        // 保存文件
        imageFile.Save(filePath);
    }
}
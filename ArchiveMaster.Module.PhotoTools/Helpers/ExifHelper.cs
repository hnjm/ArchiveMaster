using ExifLibrary;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using ExifTag = ExifLibrary.ExifTag;

namespace ArchiveMaster.Helpers;

public static class ExifHelper
{
    /// <summary>
    /// 从图片文件中读取 GPS 坐标（纬度、经度）
    /// </summary>
    /// <param name="file">图片文件路径</param>
    /// <returns>返回 (纬度, 经度) 元组，如果未找到则返回 null</returns>
    public static (double lat, double lon)? FindGps(string file)
    {
        try
        {
            // 1. 使用 MetadataExtractor 读取 GPS 信息
            var directories = ImageMetadataReader.ReadMetadata(file);
            var gpsDir = directories.OfType<GpsDirectory>().FirstOrDefault();

            if (gpsDir != null)
            {
              var gps=  gpsDir.GetGeoLocation();
             
                if (gps!=null&&!gps.IsZero)
                {
                    return (gps.Latitude,gps.Longitude);
                }
            }
            //
            // // 2. 如果 MetadataExtractor 未找到，尝试使用 ExifLibrary 作为备用方案
            // var imageFile = ImageFile.FromFile(file);
            //
            // var latRef = imageFile.Properties.Get<ExifEnumProperty<ExifLibrary.GPSLatitudeRef>>(ExifTag.GPSLatitudeRef);
            // var latArray = imageFile.Properties.Get<ExifURationalArray>(ExifTag.GPSLatitude);
            //
            // var lonRef =
            //     imageFile.Properties.Get<ExifEnumProperty<ExifLibrary.GPSLongitudeRef>>(ExifTag.GPSLongitudeRef);
            // var lonArray = imageFile.Properties.Get<ExifURationalArray>(ExifTag.GPSLongitude);
            //
            // if (latArray != null && lonArray != null)
            // {
            //     // 转换度分秒格式为十进制
            //     double lat = ConvertDmsToDecimal(
            //         latArray[0].ToDouble(),
            //         latArray[1].ToDouble(),
            //         latArray[2].ToDouble(),
            //         latRef?.Value == ExifLibrary.GPSLatitudeRef.North);
            //
            //     double lon = ConvertDmsToDecimal(
            //         lonArray[0].ToDouble(),
            //         lonArray[1].ToDouble(),
            //         lonArray[2].ToDouble(),
            //         lonRef?.Value == ExifLibrary.GPSLongitudeRef.East);
            //
            //     return (lat, lon);
            // }

            return null;
        }
        catch (Exception ex)
        {
            // 记录错误（实际项目中建议使用日志系统）
            Console.WriteLine($"读取 {file} 的 GPS 信息失败: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 将度分秒格式转换为十进制坐标
    /// </summary>
    private static double ConvertDmsToDecimal(double degrees, double minutes, double seconds, bool isPositive)
    {
        double decimalDegrees = degrees + (minutes / 60) + (seconds / 3600);
        return isPositive ? decimalDegrees : -decimalDegrees;
    }

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
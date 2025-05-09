using System.Text.RegularExpressions;
using System.Xml.Linq;
using ArchiveMaster.Configs;
using ArchiveMaster.Helpers;
using ArchiveMaster.ViewModels.FileSystem;

namespace ArchiveMaster.Services
{
    public class PhotoGeoTaggingService(AppConfig appConfig) : TwoStepServiceBase<PhotoGeoTaggingConfig>(appConfig)
    {
        public override Task ExecuteAsync(CancellationToken token = default)
        {
            var files = Files.Where(p => p.IsMatched && p.IsChecked).ToList();
            return TryForFilesAsync(files,
                (file, state) =>
                {
                    if (!file.Latitude.HasValue || !file.Longitude.HasValue || !file.ExifTime.HasValue)
                    {
                        throw new Exception("数据存在问题，经纬度或时间为空");
                    }
                    ExifHelper.WriteGpsToImage(file.Path, file.Latitude.Value, file.Longitude.Value);
                    File.SetLastWriteTime(file.Path,file.ExifTime.Value);
                },
                token, FilesLoopOptions.Builder().AutoApplyFileLengthProgress().AutoApplyStatus().Build());
        }

        public List<GpsFileInfo> Files { get; private set; }

        public override async Task InitializeAsync(CancellationToken token = default)
        {
            // 1. 准备照片扩展名正则表达式
            var rPhotos = new Regex($"\\.({string.Join('|', Config.PhotoExtensions)})$",
                RegexOptions.IgnoreCase);

            NotifyProgressIndeterminate();
            NotifyMessage("正在查找文件");

            // 2. 扫描目录中的所有文件
            List<GpsFileInfo> files = null;
            await Task.Run(() =>
            {
                files = new DirectoryInfo(Config.Dir)
                    .EnumerateFiles("*", FileEnumerateExtension.GetEnumerationOptions())
                    .ApplyFilter(token)
                    .Select(f => new GpsFileInfo(f, Config.Dir))
                    .ToList();
            }, token);

            // 3. 读取并解析GPX文件
            NotifyMessage("正在解析GPX轨迹");
            List<(double lat, double lon, DateTime time)> gpxPoints = new List<(double, double, DateTime)>();
            if (string.IsNullOrWhiteSpace(Config.GpxFile))
            {
                throw new Exception($"GPX文件不存在");
            }

            foreach (var gpxFile in FileNameHelper.GetFileNames(Config.GpxFile))
            {
                if (!File.Exists(gpxFile))
                {
                    throw new Exception($"GPX文件{gpxFile}不存在");
                }

                try
                {
                    gpxPoints.AddRange(ParseGpx(gpxFile));
                    if (gpxPoints.Count == 0)
                    {
                        throw new Exception("GPX文件未包含有效轨迹点");
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception($"GPX文件 {Path.GetFileName(Config.GpxFile)}解析失败：（{ex.Message}）", ex);
                }
            }

            // 4. 按时间排序GPX点
            gpxPoints = [.. gpxPoints.OrderBy(p => p.time)];

            // 5. 处理照片文件
            await TryForFilesAsync(files, (file, state) =>
            {
                NotifyMessage($"正在处理照片 {state.GetFileNumberMessage()}");

                if (!rPhotos.IsMatch(file.Name))
                {
                    return;
                }

                // 5.1 获取照片Exif时间
                DateTime? exifTime = ExifHelper.FindExifTime(file.Path);
                if (!exifTime.HasValue)
                {
                    file.IsMatched = false;
                    return;
                }

                file.ExifTime = exifTime.Value;
                DateTime offsetTime = file.ExifTime.Value +
                                      (Config.InverseTimeOffset ? Config.TimeOffset : -Config.TimeOffset);

                // 5.2 匹配最近的GPX点
                var (matched, lat, lon, gpsTime) = FindClosestGpxPoint(gpxPoints, offsetTime);

                // 5.3 检查时间容差
                if (matched && Math.Abs((gpsTime - offsetTime).TotalSeconds) <=
                    Config.MaxTolerance.TotalSeconds)
                {
                    file.Latitude = lat;
                    file.Longitude = lon;
                    file.GpsTime = gpsTime;
                    file.IsMatched = true;
                }
                else
                {
                    file.IsMatched = false;
                }
            }, token, FilesLoopOptions.Builder().AutoApplyFileNumberProgress().Build());

            foreach (var file in files)
            {
                file.IsChecked = file.CanCheck = file.IsMatched;
            }

            Files = files;
        }

        /// <summary>
        /// 独立GPX解析方法
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static List<(double lat, double lon, DateTime time)> ParseGpx(string filePath)
        {
            var doc = XDocument.Load(filePath);
            var trk = doc.Root.Element("trk"); // 直接找<trk>（忽略命名空间）

            if (trk == null) return new List<(double, double, DateTime)>();

            return trk.Elements("trkseg") // 严格按层级查找
                .SelectMany(seg => seg.Elements("trkpt")
                    .Select(pt => (
                        lat: (double)pt.Attribute("lat"),
                        lon: (double)pt.Attribute("lon"),
                        time: (DateTime)pt.Element("time")
                    ))
                )
                .OrderBy(p => p.time) // 按时间排序
                .ToList();
        }

        /// <summary>
        /// 双指针查找最近点
        /// </summary>
        /// <param name="points"></param>
        /// <param name="targetTime"></param>
        /// <returns></returns>
        private (bool matched, double lat, double lon, DateTime time) FindClosestGpxPoint(
            List<(double lat, double lon, DateTime time)> points, DateTime targetTime)
        {
            if (points.Count == 0) return (false, 0, 0, default);

            int left = 0, right = points.Count - 1;
            int closestIndex = 0;
            double minDiff = double.MaxValue;

            while (left <= right)
            {
                int mid = (left + right) / 2;
                var diff = Math.Abs((points[mid].time - targetTime).TotalSeconds);

                if (diff < minDiff)
                {
                    minDiff = diff;
                    closestIndex = mid;
                }

                if (points[mid].time < targetTime)
                    left = mid + 1;
                else
                    right = mid - 1;
            }

            var closest = points[closestIndex];
            return (true, closest.lat, closest.lon, closest.time);
        }
    }
}
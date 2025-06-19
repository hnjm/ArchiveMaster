using ArchiveMaster.Configs;
using ArchiveMaster.Helpers;
using ArchiveMaster.ViewModels;
using ArchiveMaster.ViewModels.FileSystem;
using FzLib;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.Index.Strtree;
using NetTopologySuite.IO.Esri;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace ArchiveMaster.Services
{
    public class PhotoGeoSorterService(AppConfig appConfig)
        : TwoStepServiceBase<PhotoGeoSorterConfig>(appConfig)
    {
        public List<GpsFileInfo> Files { get; private set; }

        public async override Task ExecuteAsync(CancellationToken token)
        {
            // return TryForFilesAsync(Files, (file, s) =>
            // {
            //     if (!file.ExifTime.HasValue)
            //     {
            //         return;
            //     }
            //
            //     NotifyMessage($"正在处理{s.GetFileNumberMessage()}：{file.Name}");
            //     File.SetLastWriteTime(file.Path, file.ExifTime.Value);
            // }, token, FilesLoopOptions.Builder().AutoApplyStatus().AutoApplyFileNumberProgress().Build());
        }

        private STRtree<IFeature> ReadShapefile()
        {
            using var shapefile = Shapefile.OpenRead(Config.VectorFile);
            if (shapefile.ShapeType is not (ShapeType.Polygon or ShapeType.PolygonM or ShapeType.PolyLineZM))
            {
                throw new Exception($"Shapefile的几何类型应当为面，实际为{shapefile.ShapeType}");
            }

            if (shapefile.Fields[Config.FieldName] == null)
            {
                throw new Exception($"没有名为{Config.FieldName}的字段");
            }

            var tree = new STRtree<IFeature>();
            foreach (var feature in shapefile)
            {
                tree.Insert(feature.Geometry.EnvelopeInternal, feature);
            }

            tree.Build();
            return tree;
        }

        public override async Task InitializeAsync(CancellationToken token)
        {
            var tree = Path.GetExtension(Config.VectorFile).ToLower() switch
            {
                ".shp" => ReadShapefile(),
                _ => throw new Exception($"矢量地理文件应当为Shapefile(*.shp)或GeoJSON(*.geojson)")
            };

            await Task.Run(async () =>
            {
                var files = new DirectoryInfo(Config.Dir)
                        .EnumerateFiles("*", FileEnumerateExtension.GetEnumerationOptions())
                        .ApplyFilter(token, Config.Filter)
                        .Select(p => new GpsFileInfo(p, Config.Dir))
                        .ToList();

                TryForFiles(files, (f, s) =>
               {
                   var gps = ExifHelper.FindGps(f.Path);
                   if (gps != null)
                   {
                       f.Longitude = gps.Value.lon;
                       f.Latitude = gps.Value.lat;
                       f.AlreadyHasGps = true;

                       Point point = new Point(f.Longitude.Value, f.Latitude.Value);
                       var candidates = tree.Query(point.EnvelopeInternal);
                       foreach (var candidate in candidates)
                       {
                           if (candidate.Geometry.Contains(point))
                           {
                               f.Region = candidate.Attributes[Config.FieldName].ToString();
                               f.IsMatched = true;
                               break;
                           }
                       }
                   }
               }, token, FilesLoopOptions.Builder().AutoApplyFileNumberProgress().Build());

                Files = files;
            });
        }
    }
}
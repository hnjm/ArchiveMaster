using ArchiveMaster.Helpers;
using CommunityToolkit.Mvvm.ComponentModel;
using FzLib.IO;

namespace ArchiveMaster.Configs;

public partial class PhotoGeoSorterConfig : ConfigBase
{
    [ObservableProperty]
    private string dir;

    [ObservableProperty]
    private string vectorFile;

    [ObservableProperty]
    private FileFilterRule filter = FileHelper.ImageFileFilterRule;

    [ObservableProperty]
    private string fieldName;

    public override void Check()
    {
        CheckDir(Dir, "目录");
        CheckFile(VectorFile, "矢量地理文件");
        if (Path.GetExtension(VectorFile).ToLower() is not (".shp" or ".geojson"))
        {
            throw new Exception($"矢量地理文件应当为Shapefile(*.shp)或GeoJSON(*.geojson)");
        }
        CheckEmpty(FieldName, "字段名");
    }
}
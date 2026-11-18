using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Xml.Linq;
using CircuitEditor.App.Services;
using CircuitEditor.App.ViewModels;
namespace CircuitEditor.Tests;
internal static class Stage4PresentationChecks
{
    internal static void Run(ResourceDictionary resources)
    {
        Application.Current.Resources.MergedDictionaries.Add(resources);
        using var e=new Stage4TestEnvironment();
        e.Project.Add(e.Project.Library.Single(d=>d.Id=="resistor"));
        e.Project.Add(e.Project.Library.Single(d=>d.Id=="lcd1602"));
        var png=new ImageExportService(e.Mockup,e.Schematic,e);
        foreach(var kind in new[]{WorkspaceKind.Mockup,WorkspaceKind.Schematic})
        {
            Assert.Null(png.ExportPng("memory.png",kind,1));
            using var memory=new MemoryStream(e.Exported!);
            var decoder=new PngBitmapDecoder(memory,BitmapCreateOptions.None,BitmapCacheOption.OnLoad);
            Assert.True(decoder.Frames[0].PixelWidth>100); Assert.True(decoder.Frames[0].PixelHeight>100);
        }
        var svg=new SvgExportService(e.Mockup,e.Schematic,e); Assert.Null(svg.ExportSchematic("memory.svg"));
        var xml=XDocument.Parse(Encoding.UTF8.GetString(e.Exported!));
        Assert.Equal("svg",xml.Root!.Name.LocalName); Assert.Contains(xml.Descendants(),n=>n.Name.LocalName=="path");
        Assert.DoesNotContain("image",Encoding.UTF8.GetString(e.Exported!));
        var window=new CircuitEditor.App.MainWindow {DataContext=null}; window.Close();
        Application.Current.Resources.MergedDictionaries.Remove(resources);
    }
}


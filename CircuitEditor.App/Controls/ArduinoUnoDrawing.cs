using System.Windows;
using System.Windows.Media;
using CircuitEditor.App.Services;
using CircuitEditor.Core.Physical;
namespace CircuitEditor.App.Controls;
internal static class ArduinoUnoDrawing
{
    internal static readonly Point BuiltinLed = new(24, 9.5);
    private const double LabelSize=1.85;
    private const double HeaderHalfWidth=1.15;
    private static readonly Brush Silk=PhysicalDrawing.Frozen(PhysicalDrawing.Ink("#E4F4EA"));
    public static void Draw(DrawingContext dc,FootprintDefinition footprint)
    {
        var body=PhysicalDrawing.Rect(footprint.Body);
        BoardDetailDrawing.Pcb(dc,body,"#087E91");
        dc.DrawRoundedRectangle(null,new Pen(PhysicalDrawing.Ink("#399BA3"),.16),new Rect(1,1,body.Width-2,body.Height-2),1,1);
        Traces(dc);
        foreach(var point in new[]{new Point(3.5,3.5),new Point(65,12),new Point(65,48.3),new Point(15.2,50)})BoardDetailDrawing.Hole(dc,point,1.4);
        Usb(dc);Power(dc);
        BoardDetailDrawing.Chip(dc,new Rect(14,18,7,7),"16U2");
        dc.DrawRoundedRectangle(PhysicalDrawing.Metal,new Pen(Brushes.DimGray,.16),new Rect(14.5,27,6,2.5),1,1);
        BoardDetailDrawing.TextCentered(dc,"16 MHz",17.5,30,1.1,Silk);
        dc.DrawRoundedRectangle(PhysicalDrawing.Metal,new Pen(Brushes.DimGray,.15),new Rect(15,11,6,4),.35,.35);
        dc.DrawRectangle(Brushes.DarkSlateGray,null,new Rect(16.3,12,3.4,2));
        PhysicalDrawing.Text(dc,"RESET",15,9.1,1.1,Silk);
        Dip(dc,new Rect(27,28,34,7.7));
        dc.DrawRoundedRectangle(PhysicalDrawing.Metal,new Pen(Brushes.Gray,.15),new Rect(23,31,2.2,5.5),.8,.8);
        PhysicalDrawing.Text(dc,"16.000",20,37.2,1.05,Silk);
        // Recognisable silkscreen and central Arduino infinity mark.
        var logoPen=new Pen(Silk,.45);
        dc.DrawEllipse(null,logoPen,new Point(29.5,19),2.6,1.7);dc.DrawEllipse(null,logoPen,new Point(34,19),2.6,1.7);
        dc.DrawLine(logoPen,new Point(28.3,19),new Point(30.7,19));
        dc.DrawLine(logoPen,new Point(32.8,19),new Point(35.2,19));dc.DrawLine(logoPen,new Point(34,17.8),new Point(34,20.2));
        PhysicalDrawing.Text(dc,"ARDUINO",39,15.5,2.9,Silk);
        PhysicalDrawing.Text(dc,"UNO",49,20,3.7,Silk);
        PhysicalDrawing.Text(dc,"REV3",58,24,1.25,Silk);
        foreach(var part in new[]{new Rect(24,14,1.5,.8),new Rect(27,14,1.5,.8),new Rect(24,24,1.5,.8),new Rect(27,24,1.5,.8),new Rect(38,24,1.5,.8),new Rect(41,24,1.5,.8)})
            Smd(dc,part,"#D4C8AA");
        Indicator(dc,BuiltinLed,"L","#DCC44E");
        Indicator(dc,new Point(10.5,27),"TX","#D7A855");Indicator(dc,new Point(10.5,30),"RX","#D7A855");
        Indicator(dc,new Point(62,39),"ON","#76BA77");
        for(var x=0;x<2;x++)for(var y=0;y<3;y++)
        {var pin=new Point(64+x*2.0,29+y*2.0);dc.DrawRectangle(Brushes.Black,null,new Rect(pin.X-.8,pin.Y-.8,1.6,1.6));dc.DrawRectangle(PhysicalDrawing.Metal,null,new Rect(pin.X-.25,pin.Y-.25,.5,.5));}
        PhysicalDrawing.Text(dc,"ICSP",62.2,36.5,1.05,Silk);
        Headers(dc,footprint);
    }
    private static void Traces(DrawingContext dc)
    {
        var ink=new Pen(PhysicalDrawing.Ink("#198394"),.18);
        for(var i=0;i<8;i++)
        {
            var x=28+i*3;dc.DrawLine(ink,new Point(x,13),new Point(x,25));
            dc.DrawLine(ink,new Point(x,25),new Point(x+1.5,26.5));
            dc.DrawEllipse(null,ink,new Point(x+1.5,26.5),.28,.28);
        }
        dc.DrawLine(ink,new Point(12,33),new Point(22,33));dc.DrawLine(ink,new Point(22,33),new Point(24,35));
    }
    private static void Headers(DrawingContext dc,FootprintDefinition footprint)
    {
        var groups=footprint.Contacts.GroupBy(c=>c.Y<20 ? c.ElectricalPinId is "D0" or "D1" or "D2" or "D3" or "D4" or "D5" or "D6" or "D7" ? "digital-low":"digital-high"
            :c.ElectricalPinId.StartsWith("A",StringComparison.Ordinal)?"analog":"power");
        foreach(var group in groups)
        {
            var pins=group.OrderBy(c=>c.X).ToArray();var y=pins[0].Y;
            var rect=new Rect(pins[0].X-HeaderHalfWidth,y-HeaderHalfWidth,pins[^1].X-pins[0].X+2*HeaderHalfWidth,2*HeaderHalfWidth);
            dc.DrawRoundedRectangle(new LinearGradientBrush(PhysicalDrawing.Ink("#414B51").Color,PhysicalDrawing.Ink("#101820").Color,90),new Pen(Brushes.Black,.2),rect,.25,.25);
            foreach(var pin in pins)
            {
                BoardDetailDrawing.Pad(dc,pin);
                var name=pin.ElectricalPinId switch {"GND2"=>"GND","RESET"=>"RESET",_=>pin.ElectricalPinId};
                if(new[]{"D3","D5","D6","D9","D10","D11"}.Contains(name))name="~"+name[1..];
                else if(name.StartsWith("D",StringComparison.Ordinal)&&int.TryParse(name.AsSpan(1),out _))name=name[1..];
                var text=WpfTextMeasurer.Format(name,LabelSize,Silk);var top=y<20;
                dc.PushTransform(new TranslateTransform(pin.X+(top?1:-1)*text.Height/2,y+(top?2:-2)));
                dc.PushTransform(new RotateTransform(top?90:-90));dc.DrawText(text,new Point(0,0));dc.Pop();dc.Pop();
            }
        }
        PhysicalDrawing.Text(dc,"DIGITAL  (PWM ~)",28,11.2,1.2,Silk);
        PhysicalDrawing.Text(dc,"POWER",23,40.5,1.05,Silk);
        PhysicalDrawing.Text(dc,"ANALOG IN",37.3,40.5,1.05,Silk);
    }
    private static void Dip(DrawingContext dc,Rect bounds)
    {
        const int Count=14;
        for(var i=0;i<Count;i++)
        {var x=bounds.X+1.1+i*2.4;dc.DrawRectangle(PhysicalDrawing.Metal,new Pen(Brushes.DimGray,.08),new Rect(x,bounds.Top-.9,.6,1.2));dc.DrawRectangle(PhysicalDrawing.Metal,new Pen(Brushes.DimGray,.08),new Rect(x,bounds.Bottom-.2,.6,1.2));}
        dc.DrawRoundedRectangle(PhysicalDrawing.Ink("#151D24"),new Pen(Brushes.Black,.2),bounds,.5,.5);
        dc.DrawLine(new Pen(PhysicalDrawing.Ink("#47505A"),.25),new Point(bounds.Left+1,bounds.Top+.8),new Point(bounds.Right-1,bounds.Top+.8));
        dc.DrawEllipse(PhysicalDrawing.Ink("#087E91"),null,new Point(bounds.Left,bounds.Top+bounds.Height/2),.7,1);
        BoardDetailDrawing.TextCentered(dc,"ATMEL  ATMEGA328P-PU",bounds.X+bounds.Width/2,bounds.Y+2.8,1.45,Brushes.LightGray);
    }
    private static void Usb(DrawingContext dc)
    {
        var rect=new Rect(0,14,11,12);BoardDetailDrawing.Usb(dc,rect);
        dc.DrawRectangle(PhysicalDrawing.Ink("#2B3640"),new Pen(Brushes.Silver,.25),new Rect(.3,16,2,8));
        dc.DrawLine(new Pen(Brushes.Silver,.25),new Point(3,14.8),new Point(9.7,14.8));
        Smd(dc,new Rect(11.5,16,1,4),"#CFBC73");
    }
    private static void Power(DrawingContext dc)
    {
        dc.DrawRoundedRectangle(PhysicalDrawing.Ink("#141A20"),new Pen(Brushes.Black,.2),new Rect(.5,35,11,9),.7,.7);
        dc.DrawRectangle(PhysicalDrawing.Ink("#364048"),null,new Rect(.5,36,3,7));
        dc.DrawEllipse(Brushes.Black,new Pen(Brushes.Gray,.25),new Point(2,39.5),1.2,2.7);
        BoardDetailDrawing.Chip(dc,new Rect(14,35,3.5,5),"REG");
        foreach(var x in new[]{13d,17d})
        {dc.DrawEllipse(PhysicalDrawing.Ink("#14252E"),new Pen(Brushes.DimGray,.15),new Point(x,43),1.7,1.7);dc.DrawEllipse(PhysicalDrawing.Metal,new Pen(Brushes.DimGray,.1),new Point(x,42.6),1.5,1.3);dc.DrawLine(new Pen(Brushes.DimGray,.12),new Point(x-1,42.6),new Point(x+1,42.6));}
    }
    private static void Smd(DrawingContext dc,Rect rect,string color)
    {dc.DrawRectangle(PhysicalDrawing.Metal,null,rect);dc.DrawRectangle(PhysicalDrawing.Ink(color),null,new Rect(rect.X+.25,rect.Y,Math.Max(.1,rect.Width-.5),rect.Height));}
    private static void Indicator(DrawingContext dc,Point point,string label,string color)
    {Smd(dc,new Rect(point.X,point.Y,1.5,.8),color);PhysicalDrawing.Text(dc,label,point.X+2,point.Y-.2,1.05,Silk);}
}

using System.Windows;
using System.Windows.Media;
using CircuitEditor.Core.Physical;

namespace CircuitEditor.App.Controls;

internal static class Lcd1602Drawing
{
    private const int Columns = 16;
    private const int Rows = 2;
    private const int GlyphColumns = 5;
    private const int GlyphRows = 8;

    public static void Draw(DrawingContext dc, FootprintDefinition footprint)
    {
        var body = PhysicalDrawing.Rect(footprint.Body);
        var i2c = footprint.Renderer == "lcd1602-i2c";
        BoardDetailDrawing.Pcb(dc, body, "#176F48");
        foreach (var x in new[] { body.Left + 2.5, body.Right - 2.5 })
            foreach (var y in new[] { body.Top + 2.5, body.Bottom - 2.5 })
                BoardDetailDrawing.Hole(dc, new Point(x, y));
        var frame = new Rect(body.Left + 5, body.Top + 8.2, body.Width - 10, 23);
        dc.DrawRoundedRectangle(PhysicalDrawing.Ink("#252E34"), new Pen(Brushes.Black, .25), frame, .6, .6);
        var screen = new Rect(frame.Left + 2, frame.Top + 3.5, frame.Width - 4, 16);
        dc.DrawRectangle(new LinearGradientBrush(PhysicalDrawing.Ink("#304EC5").Color,
            PhysicalDrawing.Ink("#112D91").Color, 90), new Pen(Brushes.DimGray, .15), screen);
        DrawCharacterCells(dc, screen);
        BoardDetailDrawing.TextCentered(dc, "1602 · 16×2", body.Left + body.Width / 2, body.Bottom - 3.6, 1.5, Brushes.WhiteSmoke);
        if (i2c) DrawBackpack(dc, body);
        foreach (var contact in footprint.Contacts)
        {
            if (i2c) PhysicalDrawing.Lead(dc, new Point(contact.X, contact.Y), new Point(contact.X, body.Top + 1));
            BoardDetailDrawing.Pad(dc, contact);
            dc.PushTransform(new TranslateTransform(contact.X + .8, contact.Y + (i2c ? 3.8 : 1.4)));
            dc.PushTransform(new RotateTransform(90));
            PhysicalDrawing.Text(dc, contact.ElectricalPinId, 0, 0, BoardDetailDrawing.PinLabelSize, Brushes.WhiteSmoke);
            dc.Pop(); dc.Pop();
        }
    }

    private static void DrawBackpack(DrawingContext dc, Rect body)
    {
        // The adapter is on the rear. A labelled cutaway shows its parts without changing the outline.
        var adapter = new Rect(body.Left + 22, body.Top + .8, 39, 6.4);
        dc.DrawRoundedRectangle(PhysicalDrawing.Ink("#1D2927"), new Pen(PhysicalDrawing.Ink("#94B68A"), .15), adapter, .3, .3);
        BoardDetailDrawing.Chip(dc, new Rect(adapter.Left + 2, adapter.Top + 1.2, 11, 3.6), "PCF8574");
        dc.DrawRectangle(PhysicalDrawing.Ink("#2575B9"), new Pen(Brushes.DimGray, .1), new Rect(adapter.Right - 6, adapter.Top + .8, 4.5, 4.5));
        dc.DrawEllipse(PhysicalDrawing.Metal, null, new Point(adapter.Right - 3.8, adapter.Top + 3), 1.1, 1.1);
        BoardDetailDrawing.TextCentered(dc, "REAR", adapter.Left + 21, adapter.Top + 2, 1.5, Brushes.LightGray);
    }

    private static void DrawCharacterCells(DrawingContext dc, Rect screen)
    {
        const double characterWidth = 2.95;
        const double characterHeight = 5.55;
        const double characterPitchX = 3.55;
        const double characterPitchY = 5.95;
        var startX = screen.Left + (screen.Width - ((Columns - 1) * characterPitchX + characterWidth)) / 2;
        var startY = screen.Top + (screen.Height - ((Rows - 1) * characterPitchY + characterHeight)) / 2;
        var dot = PhysicalDrawing.Ink("#334DAB");
        var matrix = new StreamGeometry { FillRule = FillRule.Nonzero };
        using (var path = matrix.Open())
        {
            for (var row = 0; row < Rows; row++)
                for (var column = 0; column < Columns; column++)
                    for (var y = 0; y < GlyphRows; y++)
                        for (var x = 0; x < GlyphColumns; x++)
                        {
                            var rect = new Rect(startX + column * characterPitchX + x * characterWidth / GlyphColumns,
                                startY + row * characterPitchY + y * characterHeight / GlyphRows,
                                characterWidth / GlyphColumns * .85, characterHeight / GlyphRows * .85);
                            path.BeginFigure(rect.TopLeft, true, true);
                            path.LineTo(rect.TopRight, true, false);
                            path.LineTo(rect.BottomRight, true, false);
                            path.LineTo(rect.BottomLeft, true, false);
                        }
        }
        matrix.Freeze();
        dc.DrawGeometry(dot, null, matrix);
    }
}

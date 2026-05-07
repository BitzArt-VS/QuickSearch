#!/usr/bin/env dotnet run
#:package SkiaSharp@2.88.8

// One-shot tool: generates the resize-cursor PNG assets shipped with the mod.
// Mirrors the bitmap layout vanilla uses at assets/game/textures/gui/cursors/*.png
// (24x24, premultiplied ARGB, hot-point at centre — declared in coords.json).
// Run with `dotnet run tools/generate-cursor-assets.cs` from the repo root when
// the cursor design needs to change; the produced PNGs are checked into
// resources/assets/bitzartuitweaks/textures/gui/cursors/.

using SkiaSharp;
using System.IO;

const int Size  = 24;
const float Half  = Size / 2f;
const float Arm   = Size * 0.40f;
const float HeadL = 4f;
const float HeadW = 4f;

string outDir = System.IO.Path.GetFullPath(System.IO.Path.Combine(
    Directory.GetCurrentDirectory(),
    "resources", "assets", "bitzartuitweaks", "textures", "gui", "cursors"));
Directory.CreateDirectory(outDir);

WriteCursor("horizontal", System.IO.Path.Combine(outDir, "resize-h.png"));
WriteCursor("vertical",   System.IO.Path.Combine(outDir, "resize-v.png"));
WriteCursor("nwse",       System.IO.Path.Combine(outDir, "resize-nwse.png"));

static void WriteCursor(string kind, string path)
{
    var info = new SKImageInfo(Size, Size, SKColorType.Bgra8888, SKAlphaType.Premul);
    using var surface = SKSurface.Create(info);
    var canvas = surface.Canvas;
    canvas.Clear(SKColors.Transparent);

    using var p = new SKPath();
    switch (kind)
    {
        case "horizontal":
            p.MoveTo(Half - Arm, Half); p.LineTo(Half + Arm, Half);
            p.MoveTo(Half - Arm + HeadL, Half - HeadW); p.LineTo(Half - Arm, Half); p.LineTo(Half - Arm + HeadL, Half + HeadW);
            p.MoveTo(Half + Arm - HeadL, Half - HeadW); p.LineTo(Half + Arm, Half); p.LineTo(Half + Arm - HeadL, Half + HeadW);
            break;
        case "vertical":
            p.MoveTo(Half, Half - Arm); p.LineTo(Half, Half + Arm);
            p.MoveTo(Half - HeadW, Half - Arm + HeadL); p.LineTo(Half, Half - Arm); p.LineTo(Half + HeadW, Half - Arm + HeadL);
            p.MoveTo(Half - HeadW, Half + Arm - HeadL); p.LineTo(Half, Half + Arm); p.LineTo(Half + HeadW, Half + Arm - HeadL);
            break;
        case "nwse":
            const float diag = 0.70710678f;
            float ax = Arm * diag;
            const float hl = HeadL + 2;
            p.MoveTo(Half - ax, Half - ax); p.LineTo(Half + ax, Half + ax);
            p.MoveTo(Half - ax + hl, Half - ax); p.LineTo(Half - ax, Half - ax); p.LineTo(Half - ax, Half - ax + hl);
            p.MoveTo(Half + ax - hl, Half + ax); p.LineTo(Half + ax, Half + ax); p.LineTo(Half + ax, Half + ax - hl);
            break;
    }

    using var outline = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke,
        StrokeCap = SKStrokeCap.Round, StrokeJoin = SKStrokeJoin.Round, StrokeWidth = 4f, Color = SKColors.Black };
    using var core = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke,
        StrokeCap = SKStrokeCap.Round, StrokeJoin = SKStrokeJoin.Round, StrokeWidth = 2f, Color = SKColors.White };

    canvas.DrawPath(p, outline);
    canvas.DrawPath(p, core);
    canvas.Flush();

    using var img  = surface.Snapshot();
    using var data = img.Encode(SKEncodedImageFormat.Png, 100);
    using var fs   = File.Create(path);
    data.SaveTo(fs);
    System.Console.WriteLine("wrote " + path);
}

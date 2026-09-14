// Rebuild the SVG-equivalent application artwork as a Windows multi-resolution ICO.
// Uses only .NET Framework System.Drawing; no external packages or network.
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

static class IconBuilder {
    static GraphicsPath Rounded(float x,float y,float w,float h,float r) {
        var p=new GraphicsPath(); float d=r*2;
        p.AddArc(x,y,d,d,180,90); p.AddArc(x+w-d,y,d,d,270,90);
        p.AddArc(x+w-d,y+h-d,d,d,0,90); p.AddArc(x,y+h-d,d,d,90,90); p.CloseFigure(); return p;
    }
    static PointF[] Pointer(float dy) { return new[]{new PointF(62,48+dy),new PointF(62,189+dy),new PointF(96,159+dy),new PointF(125,214+dy),new PointF(156,197+dy),new PointF(126,143+dy),new PointF(180,143+dy)}; }
    static Bitmap Draw(int size) {
        using(var high=new Bitmap(size*4,size*4,PixelFormat.Format32bppArgb)) {
            using(var g=Graphics.FromImage(high)) {
                g.SmoothingMode=SmoothingMode.AntiAlias; g.ScaleTransform(size*4/256f,size*4/256f);
                using(var p=Rounded(4,4,248,248,56)) using(var b=new LinearGradientBrush(new Point(4,4),new Point(252,252),Color.FromArgb(40,125,96),Color.FromArgb(16,73,54))) g.FillPath(b,p);
                using(var p=Rounded(10,10,236,236,50)) using(var pen=new Pen(Color.FromArgb(56,155,219,193),2)) g.DrawPath(pen,p);
                using(var shadow=new SolidBrush(Color.FromArgb(89,9,47,37))) g.FillPolygon(shadow,Pointer(7));
                using(var fill=new LinearGradientBrush(new Point(60,48),new Point(180,214),Color.FromArgb(255,253,242),Color.FromArgb(213,239,223))) using(var edge=new Pen(Color.FromArgb(249,255,246),4)) { edge.LineJoin=LineJoin.Round; g.FillPolygon(fill,Pointer(0)); g.DrawPolygon(edge,Pointer(0)); }
                if(size>=32) using(var line=new Pen(Color.FromArgb(100,188,152),5)) { line.StartCap=LineCap.Round; line.EndCap=LineCap.Round; g.DrawLine(line,78,82,78,151); }
                using(var gold=new SolidBrush(Color.FromArgb(232,207,142))) g.FillPolygon(gold,new[]{new PointF(188,43),new PointF(194,62),new PointF(213,68),new PointF(194,74),new PointF(188,93),new PointF(182,74),new PointF(163,68),new PointF(182,62)});
            }
            var result=new Bitmap(size,size,PixelFormat.Format32bppArgb);
            using(var g=Graphics.FromImage(result)) { g.CompositingMode=CompositingMode.SourceCopy; g.InterpolationMode=InterpolationMode.HighQualityBicubic; g.PixelOffsetMode=PixelOffsetMode.HighQuality; g.DrawImage(high,new Rectangle(0,0,size,size),0,0,high.Width,high.Height,GraphicsUnit.Pixel); }
            return result;
        }
    }
    static byte[] Frame(Bitmap b) {
        using(var stream=new MemoryStream()) using(var w=new BinaryWriter(stream)) {
            int n=b.Width, stride=((n+31)/32)*4;
            w.Write(40); w.Write(n); w.Write(n*2); w.Write((short)1); w.Write((short)32);
            w.Write(0); w.Write(n*n*4+stride*n); w.Write(0); w.Write(0); w.Write(0); w.Write(0);
            for(int y=n-1;y>=0;y--) for(int x=0;x<n;x++) { Color c=b.GetPixel(x,y); w.Write(c.B); w.Write(c.G); w.Write(c.R); w.Write(c.A); }
            for(int y=n-1;y>=0;y--) { byte[] mask=new byte[stride]; for(int x=0;x<n;x++) if(b.GetPixel(x,y).A==0) mask[x/8]|=(byte)(128>>(x%8)); w.Write(mask); }
            return stream.ToArray();
        }
    }
    static void Main(string[] args) {
        string dir=args.Length>0?args[0]:"assets"; Directory.CreateDirectory(dir);
        int[] sizes={16,20,24,32,40,48,64,128,256}; byte[][] frames=new byte[sizes.Length][];
        for(int i=0;i<sizes.Length;i++) using(var b=Draw(sizes[i])) frames[i]=Frame(b);
        using(var w=new BinaryWriter(File.Create(Path.Combine(dir,"mouse-studio.ico")))) {
            w.Write((short)0); w.Write((short)1); w.Write((short)sizes.Length); int offset=6+sizes.Length*16;
            for(int i=0;i<sizes.Length;i++) { w.Write((byte)(sizes[i]==256?0:sizes[i])); w.Write((byte)(sizes[i]==256?0:sizes[i])); w.Write((byte)0); w.Write((byte)0); w.Write((short)1); w.Write((short)32); w.Write(frames[i].Length); w.Write(offset); offset+=frames[i].Length; }
            foreach(byte[] f in frames) w.Write(f);
        }
        using(var b=Draw(512)) b.Save(Path.Combine(dir,"mouse-studio.png"),ImageFormat.Png);
    }
}

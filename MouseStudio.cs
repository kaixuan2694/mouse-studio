using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

static class Native {
    [StructLayout(LayoutKind.Sequential)] public struct IconInfo { public bool icon; public uint x, y; public IntPtr mask, color; }
    [DllImport("user32.dll", SetLastError=true)] public static extern bool SystemParametersInfo(uint action, uint param, IntPtr value, uint flags);
    [DllImport("user32.dll", EntryPoint="SystemParametersInfoW", SetLastError=true)] static extern bool GetParameter(uint action, uint param, ref int value, uint flags);
    [DllImport("user32.dll", SetLastError=true)] public static extern bool SetSystemCursor(IntPtr cursor, uint id);
    [DllImport("user32.dll", SetLastError=true)] public static extern IntPtr LoadCursor(IntPtr instance, IntPtr id);
    [DllImport("user32.dll", SetLastError=true)] public static extern IntPtr CopyImage(IntPtr image, uint type, int x, int y, uint flags);
    [DllImport("user32.dll", SetLastError=true)] public static extern IntPtr CreateIconIndirect(ref IconInfo info);
    [DllImport("user32.dll", SetLastError=true)] public static extern bool GetIconInfo(IntPtr icon, out IconInfo info);
    [DllImport("user32.dll")] public static extern IntPtr SetCursor(IntPtr cursor);
    [DllImport("user32.dll")] public static extern bool DestroyCursor(IntPtr cursor);
    [DllImport("user32.dll")] public static extern bool DestroyIcon(IntPtr icon);
    [DllImport("gdi32.dll")] public static extern bool DeleteObject(IntPtr obj);
    [DllImport("gdi32.dll")] public static extern IntPtr CreateBitmap(int w, int h, uint planes, uint bits, IntPtr data);
    public static readonly uint[] Roles = {32512,32513,32514,32515,32516,32642,32643,32644,32645,32646,32648,32649,32650,32651};
    public static void Check(bool ok) { if(!ok) throw new Win32Exception(Marshal.GetLastWin32Error()); }
    public static int Speed { get { int n=10; Check(GetParameter(0x70,0,ref n,0)); return n; } set { Check(SystemParametersInfo(0x71,0,new IntPtr(Math.Max(1,Math.Min(20,value))),0)); } }
    public static IntPtr Copy(IntPtr h) { IntPtr c=CopyImage(h,2,0,0,0); Check(c!=IntPtr.Zero); return c; }
    public static void ReloadCursors() {
        // Reload Windows-owned theme resources, including animated cursors. Do not
        // rebuild the theme from process-owned copies of possibly stale handles.
        Check(SystemParametersInfo(0x57,0,IntPtr.Zero,0));
        IntPtr arrow=LoadCursor(IntPtr.Zero,new IntPtr(32512)); Check(arrow!=IntPtr.Zero);
        // Refresh the current cursor immediately; the user should not need to move
        // into another window to stop displaying a superseded cursor handle.
        SetCursor(arrow);
    }
    public static IntPtr CursorFrom(int style, int size, uint role) {
        using(Bitmap b=Art.Render(style,size,role)) {
            IntPtr color=b.GetHbitmap(Color.FromArgb(0));
            IntPtr mask=CreateBitmap(size,size,1,1,IntPtr.Zero);
            try {
                Check(mask!=IntPtr.Zero);
                Point p=Art.Hotspot(size,role);
                IconInfo info=new IconInfo {icon=false,x=(uint)p.X,y=(uint)p.Y,color=color,mask=mask};
                IntPtr h=CreateIconIndirect(ref info); Check(h!=IntPtr.Zero); return h;
            } finally { DeleteObject(color); if(mask!=IntPtr.Zero) DeleteObject(mask); }
        }
    }
}

sealed class MouseSession : IDisposable {
    readonly int speed;
    bool dirty;
    public bool Active { get { return dirty; } }
    public static string RecoveryPath { get { return Path.Combine(AppStorage.DirectoryPath,"recovery.txt"); } }
    public MouseSession() { speed=Native.Speed; }
    void BeforeChange() {
        if(dirty) return;
        Directory.CreateDirectory(Path.GetDirectoryName(RecoveryPath));
        File.WriteAllText(RecoveryPath,speed.ToString());
        dirty=true;
    }
    public void Apply(int style,int size) {
        // Generate all handles first, so drawing failures cannot partially change the desktop.
        var cursors=new Dictionary<uint,IntPtr>();
        try {
            foreach(uint id in Native.Roles) cursors[id]=Native.CursorFrom(style,size,id);
            BeforeChange();
            foreach(uint id in Native.Roles) {
                IntPtr h=cursors[id];
                bool ok=Native.SetSystemCursor(h,id);
                cursors[id]=IntPtr.Zero; // SetSystemCursor consumes the input cursor.
                Native.Check(ok);
            }
        } catch { if(dirty) Restore(); throw; }
        finally { foreach(IntPtr h in cursors.Values) if(h!=IntPtr.Zero) Native.DestroyCursor(h); }
    }
    public void SetSpeed(int n) { BeforeChange(); Native.Speed=n; }
    public void Restore() {
        if(!dirty) return;
        Exception error=null;
        try { Native.ReloadCursors(); } catch(Exception e) { error=e; }
        try { Native.Speed=speed; } catch(Exception e) { error=e; }
        if(error!=null) throw error;
        dirty=false;
        if(File.Exists(RecoveryPath)) File.Delete(RecoveryPath);
    }
    public static void Recover() {
        Native.ReloadCursors();
        if(File.Exists(RecoveryPath)) {
            int n; if(int.TryParse(File.ReadAllText(RecoveryPath),out n) && n>=1 && n<=20) Native.Speed=n;
            File.Delete(RecoveryPath);
        }
    }
    // Also restore if the message loop exits without FormClosing (for example,
    // an exception). Keep the recovery marker when restoration fails.
    public void Dispose() { Restore(); }
}

static class Art {
    public static readonly string[] Names={"素笺","藏锋","流光","桃夭","听竹","方寸","逐日","渡海","鎏月","星游","游龙","折纸","飞羽","长剑","团扇","小鱼","猫步","玉簪","山岚","火箭"};
    public static readonly string[] Tags={"清晰 · 经典","沉稳 · 锐利","荧光 · 未来","柔和 · 甜美","轻盈 · 自然","像素 · 怀旧","暖意 · 活力","流线 · 冷静","金属 · 精致","星芒 · 幻想","蜿蜒 · 灵动","纸飞机 · 童心","羽翼 · 轻盈","剑意 · 侠气","扇影 · 风雅","游鱼 · 自在","猫耳 · 俏皮","簪花 · 清雅","山峰 · 空灵","升空 · 探索"};
    public static readonly Color[] Fills={Color.White,Color.FromArgb(32,37,44),Color.FromArgb(14,26,40),Color.FromArgb(255,178,207),Color.FromArgb(110,224,181),Color.FromArgb(255,249,220),Color.FromArgb(255,152,69),Color.FromArgb(47,143,232),Color.FromArgb(224,190,118),Color.FromArgb(183,145,244),Color.FromArgb(112,203,180),Color.FromArgb(194,218,243),Color.FromArgb(190,212,230),Color.FromArgb(209,221,226),Color.FromArgb(242,177,166),Color.FromArgb(244,181,79),Color.FromArgb(221,188,160),Color.FromArgb(163,216,192),Color.FromArgb(153,187,204),Color.FromArgb(233,162,132)};
    public static readonly Color[] Edges={Color.FromArgb(36,42,50),Color.FromArgb(220,227,237),Color.FromArgb(74,244,226),Color.FromArgb(136,50,91),Color.FromArgb(22,94,78),Color.FromArgb(62,52,55),Color.FromArgb(136,55,27),Color.FromArgb(15,50,108),Color.FromArgb(88,67,33),Color.FromArgb(71,43,123),Color.FromArgb(32,91,78),Color.FromArgb(48,77,117),Color.FromArgb(60,81,103),Color.FromArgb(53,67,82),Color.FromArgb(132,64,67),Color.FromArgb(118,75,30),Color.FromArgb(95,65,47),Color.FromArgb(48,102,82),Color.FromArgb(50,85,106),Color.FromArgb(119,61,43)};
    public static readonly Color?[] CustomColors=new Color?[20];
    public static Color Fill(int s) { return CustomColors[s] ?? Fills[s]; }
    public static Color Edge(int s) { Color c=Fill(s); return !CustomColors[s].HasValue ? Edges[s] : c.GetBrightness()<0.28f ? Color.FromArgb(231,237,233) : Color.FromArgb(c.R/3,c.G/3,c.B/3); }
    public static Color Detail(int s,Color original) { Color c=Fill(s); return !CustomColors[s].HasValue ? original : Color.FromArgb((c.R+255)/2,(c.G+255)/2,(c.B+255)/2); }
    public static string ColorsPath { get { return Path.Combine(AppStorage.DirectoryPath,"colors.txt"); } }
    public static void LoadColors() { if(!File.Exists(ColorsPath)) return; try { string[] lines=File.ReadAllLines(ColorsPath); for(int i=0;i<Math.Min(lines.Length,CustomColors.Length);i++) { int n; if(int.TryParse(lines[i],out n)) CustomColors[i]=Color.FromArgb(255,Color.FromArgb(n)); } } catch(IOException) { } catch(UnauthorizedAccessException) { } }
    public static void SaveColors() { Directory.CreateDirectory(Path.GetDirectoryName(ColorsPath)); string[] lines=new string[CustomColors.Length]; for(int i=0;i<lines.Length;i++) lines[i]=CustomColors[i].HasValue ? CustomColors[i].Value.ToArgb().ToString() : "default"; File.WriteAllLines(ColorsPath+".tmp",lines); if(File.Exists(ColorsPath)) File.Replace(ColorsPath+".tmp",ColorsPath,null); else File.Move(ColorsPath+".tmp",ColorsPath); }
    static PointF[] Points(params float[] a) { PointF[] p=new PointF[a.Length/2]; for(int i=0;i<p.Length;i++) p[i]=new PointF(a[i*2],a[i*2+1]); return p; }
    static void Poly(Graphics g,Brush b,Pen p,params float[] a) { var pts=Points(a); g.FillPolygon(b,pts); g.DrawPolygon(p,pts); }
    public static Point Hotspot(int size,uint role) {
        if(role==32512 || role==32650 || role==32651) return new Point((int)Math.Round(size*5.0/64),(int)Math.Round(size*4.0/64));
        if(role==32649) return new Point(size*26/64,size*7/64);
        if(role==32516) return new Point(size/2,size*6/64);
        return new Point(size/2,size/2);
    }
    public static Bitmap Render(int s,int size,uint role) {
        Bitmap b=new Bitmap(size,size,PixelFormat.Format32bppArgb);
        using(Graphics g=Graphics.FromImage(b)) {
            g.Clear(Color.Transparent); g.SmoothingMode=s==5 ? SmoothingMode.None:SmoothingMode.AntiAlias;
            g.ScaleTransform(size/64f,size/64f);
            using(var fill=new SolidBrush(Fill(s))) using(var pen=new Pen(Edge(s),s==5?2:2.6f)) {
                pen.LineJoin=LineJoin.Round; pen.StartCap=LineCap.Round; pen.EndCap=LineCap.Round;
                if(role==32512 || role==32650 || role==32651) {
                    switch(s) {
                        case 0: Poly(g,fill,pen,5,4,5,48,17,37,27,58,36,54,26,34,43,34); break;
                        case 1: Poly(g,fill,pen,5,4,12,52,23,37,43,31); break;
                        case 2:
                            using(var glow=new Pen(Color.FromArgb(75,Edge(s)),7)) g.DrawPolygon(glow,Points(5,4,8,49,20,35,31,55,37,51,26,31,45,30));
                            Poly(g,fill,pen,5,4,8,49,20,35,31,55,37,51,26,31,45,30);
                            using(var line=new Pen(Detail(s,Color.FromArgb(239,86,228)),2)) g.DrawLine(line,13,17,17,32); break;
                        case 3:
                            Poly(g,fill,pen,5,4,7,48,18,37,28,56,36,51,26,33,44,32);
                            using(var pink=new SolidBrush(Edge(s))) { g.FillEllipse(pink,21,20,8,8); g.FillEllipse(pink,27,20,8,8); g.FillPolygon(pink,Points(21,24,35,24,28,33)); } break;
                        case 4:
                            using(var path=new GraphicsPath()) { path.AddBezier(5,4,46,9,53,39,24,36); path.AddLine(24,36,33,55); path.AddLine(33,55,25,58); path.AddLine(25,58,16,36); path.AddBezier(16,36,3,35,6,17,5,4); g.FillPath(fill,path); g.DrawPath(pen,path); }
                            g.DrawLine(pen,11,14,25,38); break;
                        case 5:
                            // A contiguous 16 x 16 bitmap silhouette, never a self-crossing polygon.
                            string[] grid={"................",".#..............",".##.............",".#o#............",".#oo#...........",".#ooo#..........",".#oooo#.........",".#ooooo#........",".#oooooo#.......",".#oooo####......",".#oo#oo#........",".#o#.#o#........",".##..#oo#.......",".#....#o#.......","......###.......","................"};
                            using(var edge=new SolidBrush(Edge(s))) for(int y=0;y<16;y++) for(int x=0;x<16;x++) if(grid[y][x]!='.') g.FillRectangle(grid[y][x]=='#'?edge:fill,x*4,y*4,4,4);
                            break;
                        case 6: Poly(g,fill,pen,5,4,44,25,28,30,35,48,27,53,19,34,6,46);
                            using(var hi=new Pen(Detail(s,Color.FromArgb(255,224,144)),3)) g.DrawLine(hi,12,14,28,24); break;
                        case 7: Poly(g,fill,pen,5,4,49,34,27,33,22,55);
                            using(var hi=new Pen(Detail(s,Color.FromArgb(142,227,255)),2)) g.DrawLine(hi,12,13,26,30); break;
                        case 8:
                            using(var gold=new LinearGradientBrush(new Point(5,4),new Point(40,52),Detail(s,Color.FromArgb(255,244,197)),Fill(s))) Poly(g,gold,pen,5,4,8,49,19,37,29,56,36,51,26,32,44,32);
                            using(var inner=new Pen(Color.FromArgb(255,250,221),1.3f)) g.DrawLines(inner,Points(10,17,12,36,19,29,29,32)); break;
                        case 9: Poly(g,fill,pen,5,4,39,23,27,29,36,49,27,54,19,34,8,45);
                            using(var star=new SolidBrush(Color.FromArgb(255,237,164))) Poly(g,star,pen,47,8,49,15,56,17,49,19,47,26,45,19,38,17,45,15); break;
                        case 10:
                            using(var path=new GraphicsPath()) { path.AddLines(Points(5,4,29,12,23,20)); path.AddBezier(23,20,55,14,54,47,35,49); path.AddBezier(35,49,24,50,36,61,52,54); path.AddBezier(52,54,29,69,16,47,30,40); path.AddBezier(30,40,44,36,39,24,20,28); path.AddLines(Points(20,28,16,38,5,4)); g.FillPath(fill,path); g.DrawPath(pen,path); } using(var ink=new SolidBrush(Edge(s))) g.FillEllipse(ink,17,14,3,3); break;
                        case 11: Poly(g,fill,pen,5,4,57,24,34,33,25,56); g.DrawLines(pen,Points(5,4,34,33,25,56)); g.DrawLine(pen,5,4,47,26); break;
                        case 12:
                            using(var path=new GraphicsPath()) { path.AddBezier(5,4,48,3,54,30,37,40); path.AddLines(Points(37,40,28,38,29,46,21,42,20,51,12,43,5,4)); g.FillPath(fill,path); g.DrawPath(pen,path); } g.DrawLine(pen,9,10,43,55); g.DrawLine(pen,19,21,33,18); g.DrawLine(pen,27,31,39,27); break;
                        case 13: Poly(g,fill,pen,5,4,29,16,43,36,36,43,16,29); g.DrawLine(pen,5,4,39,39); Poly(g,fill,pen,30,41,41,30,46,34,34,46); Poly(g,fill,pen,39,43,43,39,56,52,52,56); break;
                        case 14:
                            Poly(g,fill,pen,5,4,22,10,13,21);
                            g.FillEllipse(fill,11,10,38,38); g.DrawEllipse(pen,11,10,38,38); g.DrawLine(pen,39,43,54,59); g.DrawLines(pen,Points(16,18,35,41,23,14)); g.DrawLine(pen,35,41,44,23); break;
                        case 15:
                            using(var path=new GraphicsPath()) { path.AddBezier(5,4,31,5,44,20,39,37); path.AddLines(Points(39,37,57,36,48,48,37,56,36,39)); path.AddBezier(36,39,16,43,7,22,5,4); g.FillPath(fill,path); g.DrawPath(pen,path); } g.DrawArc(pen,9,9,22,22,-50,145); using(var ink=new SolidBrush(Edge(s))) g.FillEllipse(ink,15,14,4,4); break;
                        case 16:
                            Poly(g,fill,pen,5,4,26,14,43,9,46,29,51,39,45,51,31,57,16,50,11,35); g.DrawLine(pen,12,16,20,20); g.DrawLine(pen,35,20,40,16); g.DrawEllipse(pen,22,30,2,3); g.DrawEllipse(pen,36,30,2,3); g.DrawLines(pen,Points(27,38,30,40,33,38)); g.DrawLine(pen,10,37,20,39); g.DrawLine(pen,40,39,54,35); break;
                        case 17:
                            Poly(g,fill,pen,5,4,36,29,30,36);
                            for(int n=0;n<5;n++) { double angle=n*Math.PI*2/5; float x=40+(float)Math.Cos(angle)*9, y=40+(float)Math.Sin(angle)*9; g.FillEllipse(fill,x-7,y-7,14,14); g.DrawEllipse(pen,x-7,y-7,14,14); } using(var ink=new SolidBrush(Edge(s))) g.FillEllipse(ink,36,36,8,8); break;
                        case 18: Poly(g,fill,pen,5,4,54,40,36,38,43,56,25,42,9,48); using(var snow=new SolidBrush(Detail(s,Color.White))) Poly(g,snow,pen,5,4,25,19,19,20,20,27,12,23); g.DrawLine(pen,25,42,30,32); break;
                        case 19:
                            Poly(g,fill,pen,5,4,29,9,46,29,30,45,10,28); g.DrawLine(pen,29,9,10,28); Poly(g,fill,pen,13,32,10,49,25,41); Poly(g,fill,pen,34,13,50,11,43,26); g.FillEllipse(fill,23,22,12,12); g.DrawEllipse(pen,23,22,12,12); using(var fire=new SolidBrush(Detail(s,Color.FromArgb(255,216,112)))) Poly(g,fire,pen,37,40,54,57,44,53,40,55,35,44); break;
                    }
                    if(role==32650) { g.FillEllipse(fill,38,39,22,22); g.DrawArc(pen,41,42,16,16,-80,270); }
                    if(role==32651) { using(var f=new Font("Segoe UI",21,FontStyle.Bold,GraphicsUnit.Pixel)) g.DrawString("?",f,fill,37,30); }
                } else if(role==32513) {
                    using(var outer=new Pen(Edge(s),7)) using(var inner=new Pen(Fill(s),3)) { foreach(Pen p in new[]{outer,inner}) { g.DrawLine(p,32,9,32,55); g.DrawLine(p,22,9,42,9); g.DrawLine(p,22,55,42,55); } }
                } else if(role==32514) {
                    using(var p=new Pen(Edge(s),8)) g.DrawEllipse(p,12,12,40,40);
                    using(var p=new Pen(Fill(s),5)) g.DrawArc(p,12,12,40,40,-90,285);
                } else if(role==32649) {
                    Poly(g,fill,pen,21,34,21,11,24,7,28,7,31,11,31,27,36,25,41,29,46,28,51,33,56,33,58,38,55,50,48,58,30,58,22,49,12,37,13,32,18,31);
                    g.DrawLine(pen,31,28,31,38); g.DrawLine(pen,41,30,41,40); g.DrawLine(pen,50,34,50,42);
                } else if(role==32648) {
                    g.FillEllipse(fill,9,9,46,46); g.DrawEllipse(pen,9,9,46,46); using(var p=new Pen(Edge(s),5)) g.DrawLine(p,16,16,48,48);
                } else if(role==32515) {
                    using(var outP=new Pen(Edge(s),6)) using(var inP=new Pen(Fill(s),2)) foreach(var p in new[]{outP,inP}) { g.DrawLine(p,32,6,32,58); g.DrawLine(p,6,32,58,32); }
                } else if(role==32516) {
                    Poly(g,fill,pen,32,6,12,29,24,29,24,57,40,57,40,29,52,29);
                } else {
                    GraphicsState state=g.Save(); g.TranslateTransform(32,32);
                    if(role==32645) g.RotateTransform(90);
                    if(role==32642) g.RotateTransform(45);
                    if(role==32643) g.RotateTransform(-45);
                    Poly(g,fill,pen,-27,0,-14,-12,-14,-5,14,-5,14,-12,27,0,14,12,14,5,-14,5,-14,12);
                    if(role==32646) { g.RotateTransform(90); Poly(g,fill,pen,-27,0,-14,-12,-14,-5,14,-5,14,-12,27,0,14,12,14,5,-14,5,-14,12); }
                    g.Restore(state);
                }
            }
        }
        return b;
    }
}

sealed class StyleCard : Control {
    public int Index; public bool Selected; bool hover;
    public readonly Button ColorButton=new Button();
    public event EventHandler ColorRequested;
    readonly Font titleFont=new Font("Microsoft YaHei UI",16,FontStyle.Bold,GraphicsUnit.Pixel);
    public StyleCard(int index) {
        Index=index; DoubleBuffered=true; TabStop=true; AccessibleName=Art.Names[index]; AccessibleRole=AccessibleRole.PushButton; Size=new Size(192,142); Cursor=Cursors.Hand;
        Font=new Font("Microsoft YaHei UI",13,FontStyle.Regular,GraphicsUnit.Pixel);
        ColorButton.SetBounds(153,36,27,27); ColorButton.Text=""; ColorButton.FlatStyle=FlatStyle.Flat; ColorButton.FlatAppearance.BorderColor=Color.FromArgb(155,165,156); ColorButton.AccessibleName="为"+Art.Names[index]+"选择颜色";
        ColorButton.Click+=delegate { if(ColorRequested!=null) ColorRequested(this,EventArgs.Empty); }; Controls.Add(ColorButton); RefreshColor();
    }
    public void RefreshColor() { ColorButton.BackColor=Art.Fill(Index); Invalidate(); }
    protected override void OnResize(EventArgs e) { base.OnResize(e); if(ColorButton!=null) ColorButton.SetBounds(Math.Max(0,Width-40),Math.Max(12,(Height-64)/2-14),28,28); Invalidate(); }
    protected override void OnMouseEnter(EventArgs e) { hover=true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { hover=false; Invalidate(); base.OnMouseLeave(e); }
    protected override void OnGotFocus(EventArgs e) { Invalidate(); base.OnGotFocus(e); }
    protected override void OnLostFocus(EventArgs e) { Invalidate(); base.OnLostFocus(e); }
    protected override void OnKeyDown(KeyEventArgs e) { if(e.KeyCode==Keys.Enter || e.KeyCode==Keys.Space) { OnClick(EventArgs.Empty); e.Handled=true; } base.OnKeyDown(e); }
    protected override void OnPaint(PaintEventArgs e) {
        Graphics g=e.Graphics; g.SmoothingMode=SmoothingMode.AntiAlias;
        using(var bg=new SolidBrush(Selected?Color.FromArgb(235,246,241):hover?Color.FromArgb(246,248,246):Color.White)) g.FillRectangle(bg,0,0,Width,Height);
        using(var p=new Pen(Selected?MainForm.Green:Color.FromArgb(225,229,225),Selected?2:1)) g.DrawRectangle(p,1,1,Width-3,Height-3);
        int artworkSize=Math.Max(32,Math.Min(96,Math.Min(Width-76,Height-76)));
        using(var b=Art.Render(Index,artworkSize,32512)) g.DrawImageUnscaled(b,(Width-30-artworkSize)/2,Math.Max(8,(Height-64-artworkSize)/2));
        TextRenderer.DrawText(g,Art.Names[Index],titleFont,new Rectangle(0,Height-60,Width,26),MainForm.Ink,TextFormatFlags.HorizontalCenter);
        TextRenderer.DrawText(g,Art.Tags[Index],Font,new Rectangle(0,Height-32,Width,24),MainForm.Muted,TextFormatFlags.HorizontalCenter);
        if(Selected) { using(var b=new SolidBrush(MainForm.Green)) g.FillEllipse(b,10,10,9,9); }
        if(Focused) ControlPaint.DrawFocusRectangle(g,new Rectangle(5,5,Width-10,Height-10));
    }
    protected override void Dispose(bool disposing) { if(disposing) titleFont.Dispose(); base.Dispose(disposing); }
}

sealed class Preview : Control {
    public int Style, CursorSize=40;
    public Preview() { DoubleBuffered=true; BackColor=Color.FromArgb(239,243,238); }
    protected override void OnPaint(PaintEventArgs e) {
        for(int x=12;x<Width;x+=18) for(int y=12;y<Height;y+=18) e.Graphics.FillRectangle(Brushes.LightGray,x,y,1,1);
        using(Bitmap b=Art.Render(Style,CursorSize,32512)) e.Graphics.DrawImageUnscaled(b,Width/2-CursorSize/2,Height/2-CursorSize/2);
    }
}

sealed partial class MainForm : Form {
    public static readonly Color Ink=Color.FromArgb(32,49,43), Muted=Color.FromArgb(113,126,117), Green=Color.FromArgb(30,111,79);
    readonly MouseSession session;
    readonly List<StyleCard> cards=new List<StyleCard>();
    readonly TrackBar sizeBar=new TrackBar(), speedBar=new TrackBar();
    readonly Label sizeValue=new Label(), speedValue=new Label(), status=new Label();
    readonly Preview preview=new Preview();
    readonly System.Windows.Forms.Timer debounce=new System.Windows.Forms.Timer();
    readonly NotifyIcon tray=new NotifyIcon();
    readonly Button previousPage=new Button(), nextPage=new Button();
    readonly Label pageLabel=new Label();
    readonly ToolTip tips=new ToolTip();
    readonly ContextMenuStrip palette=new ContextMenuStrip();
    int page;
    int selected=-1; bool initializing=true, quitting;
    bool persistChanges,hideOnStartup;
    public MainForm(MouseSession s) {
        session=s; Text="指针工坊 · Mouse Studio 1.2.0"; ClientSize=new Size(1080,880); MinimumSize=new Size(800,600);
        Font=new Font("Microsoft YaHei UI",14,FontStyle.Regular,GraphicsUnit.Pixel); BackColor=Color.FromArgb(247,248,244); ForeColor=Ink; AutoScaleMode=AutoScaleMode.Dpi; StartPosition=FormStartPosition.CenterScreen;
        using(var stream=typeof(MainForm).Assembly.GetManifestResourceStream("MouseStudio.AppIcon")) using(var appIcon=new Icon(stream,32,32)) Icon=(Icon)appIcon.Clone();
        BuildResponsiveLayout();
        var menu=new ContextMenuStrip(); menu.Items.Add("打开指针工坊",null,delegate { ShowWindow(); }); menu.Items.Add("恢复原设置",null,delegate { Restore(); }); menu.Items.Add(new ToolStripSeparator()); menu.Items.Add("退出并恢复",null,delegate { Quit(); });
        tray.Icon=Icon; tray.Text="指针工坊 · 双击打开"; tray.ContextMenuStrip=menu; tray.DoubleClick+=delegate { ShowWindow(); };
        debounce.Interval=100; debounce.Tick+=delegate { debounce.Stop(); ApplyStyle(); };
        sizeBar.ValueChanged+=delegate { UpdateValues(); if(initializing) return; if(selected<0) selected=0; debounce.Stop(); debounce.Start(); };
        speedBar.ValueChanged+=delegate { UpdateValues(); if(initializing) return; try { session.SetSpeed(speedBar.Value); status.Text="已应用 · 系统鼠标速度 "+Native.Speed+" / 20"; SaveCurrentProfile(true); } catch(Exception ex) { Error(ex); } };
        FormClosing+=delegate(object sender,FormClosingEventArgs e) {
            if(!quitting && e.CloseReason==CloseReason.UserClosing) { e.Cancel=true; Hide(); tray.Visible=true; tray.ShowBalloonTip(2500,"指针工坊仍在运行","双击托盘图标重新打开；右键可退出并恢复原设置。",ToolTipIcon.Info); }
            else { if(debounce.Enabled) SaveCurrentProfile(true); debounce.Stop(); try { session.Restore(); } catch(Exception ex) { if(e.CloseReason==CloseReason.UserClosing) { e.Cancel=true; quitting=false; Error(ex); } } }
        };
        FormClosed+=delegate { tray.Dispose(); debounce.Dispose(); tips.Dispose(); ClearPalette(); palette.Dispose(); foreach(var card in cards) card.Dispose(); };
        UpdateValues(); initializing=false;
    }
    static Label TextLabel(string text,int x,int y,int w,int h,float size,FontStyle weight) { return new Label {Text=text,Location=new Point(x,y),Size=new Size(w,h),Font=new Font("Microsoft YaHei UI",size*1.5f,weight,GraphicsUnit.Pixel),ForeColor=weight==FontStyle.Bold?Ink:Muted}; }
    public void SetPage(int value) {
        page=Math.Max(0,Math.Min(1,value)); cardGrid.SuspendLayout();
        try { cardGrid.Controls.Clear(); foreach(var c in cards) { c.Visible=false; if(c.Index/10==page) { cardGrid.Controls.Add(c,c.Index%5,(c.Index%10)/5); c.Visible=true; } } }
        finally { cardGrid.ResumeLayout(true); }
        pageLabel.Text="第 "+(page+1)+" / 2 页"; previousPage.Enabled=page>0; nextPage.Enabled=page<1;
    }
    void ClearPalette() { while(palette.Items.Count>0) { var item=palette.Items[0]; palette.Items.RemoveAt(0); if(item.Image!=null) item.Image.Dispose(); item.Dispose(); } }
    void OpenPalette(StyleCard card) {
        ClearPalette();
        Color[] colors={Color.White,Color.FromArgb(40,45,51),Color.FromArgb(233,108,128),Color.FromArgb(243,166,81),Color.FromArgb(226,202,123),Color.FromArgb(104,191,143),Color.FromArgb(83,182,188),Color.FromArgb(91,147,222),Color.FromArgb(175,138,214),Color.FromArgb(158,139,121)};
        string[] labels={"月白","墨色","胭脂","杏黄","流金","竹青","碧水","晴空","藤花","茶褐"};
        for(int i=0;i<colors.Length;i++) { Color color=colors[i]; var swatch=new Bitmap(18,18); using(var g=Graphics.FromImage(swatch)) { g.Clear(color); g.DrawRectangle(Pens.Gray,0,0,17,17); } var item=new ToolStripMenuItem(labels[i],swatch); item.Click+=delegate { ChangeColor(card.Index,color,true); }; palette.Items.Add(item); }
        palette.Items.Add(new ToolStripSeparator());
        palette.Items.Add("自选颜色…",null,delegate { using(var dialog=new ColorDialog { Color=Art.Fill(card.Index),FullOpen=true }) if(dialog.ShowDialog(this)==DialogResult.OK) ChangeColor(card.Index,dialog.Color,true); });
        palette.Items.Add("恢复这款默认配色",null,delegate { ChangeColor(card.Index,null,true); });
        palette.Show(card.ColorButton,new Point(0,card.ColorButton.Height));
    }
    void ChangeColor(int index,Color? color,bool persist) {
        Color? previous=Art.CustomColors[index];
        try { Art.CustomColors[index]=color; if(persist) Art.SaveColors(); cards[index].RefreshColor(); if(selected==index) ApplyStyle(); else status.Text="已保存「"+Art.Names[index]+"」配色 · 点击卡片应用"; preview.Invalidate(); }
        catch(Exception ex) { Art.CustomColors[index]=previous; cards[index].RefreshColor(); Error(ex); }
    }
    static void ConfigureBar(TrackBar b,int min,int max,int value,int x,int y,int width) { b.Minimum=min; b.Maximum=max; b.Value=value; b.SetBounds(x,y,width,42); b.TickStyle=TickStyle.None; b.BackColor=Color.White; }
    void UpdateValues() { sizeValue.Text=sizeBar.Value+" px"; speedValue.Text=speedBar.Value+" / 20"; preview.CursorSize=sizeBar.Value; preview.Style=Math.Max(0,selected); preview.Invalidate(); }
    void ApplyStyle() {
        if(selected<0) return;
        try { session.Apply(selected,sizeBar.Value); foreach(var c in cards) { c.Selected=c.Index==selected; c.Invalidate(); } UpdateValues(); status.Text="已全局应用 · "+Art.Names[selected]+" / "+sizeBar.Value+" px"; SaveCurrentProfile(true); }
        catch(Exception ex) { selected=-1; foreach(var c in cards) { c.Selected=false; c.Invalidate(); } Error(ex); }
    }
    void Error(Exception ex) { status.Text="未能完成操作 · "+ex.Message; MessageBox.Show(this,ex.Message,"操作未完成",MessageBoxButtons.OK,MessageBoxIcon.Warning); }
    void Restore() {
        debounce.Stop(); try { if(session.Active) session.Restore(); else Native.ReloadCursors(); selected=-1; foreach(var c in cards) { c.Selected=false; c.Invalidate(); } initializing=true; speedBar.Value=Native.Speed; initializing=false; UpdateValues(); status.Text="已恢复 · Windows 已保存的指针主题与原鼠标速度"; SaveCurrentProfile(false); } catch(Exception ex) { Error(ex); }
    }
    void ShowWindow() { hideOnStartup=false; Show(); WindowState=FormWindowState.Normal; Activate(); tray.Visible=false; }
    public void StartMinimized() { hideOnStartup=true; tray.Visible=true; }
    protected override void SetVisibleCore(bool value) { if(hideOnStartup && value) { if(!IsHandleCreated) CreateHandle(); base.SetVisibleCore(false); return; } base.SetVisibleCore(value); }
    public bool StartUserSession(bool updateStartupPath) {
        persistChanges=true; initializing=true;
        try {
            startupCheck.Checked=!string.IsNullOrEmpty(StartupRegistration.Read(StartupRegistration.ValueName));
            if(startupCheck.Checked && updateStartupPath) StartupRegistration.Set(true,Application.ExecutablePath,StartupRegistration.ValueName);
            var saved=SavedProfile.Load();
            if(saved.Enabled) {
                selected=saved.Style; sizeBar.Value=saved.Size; speedBar.Value=saved.Speed;
                if(selected>=0) session.Apply(selected,sizeBar.Value);
                session.SetSpeed(speedBar.Value);
                foreach(var card in cards) { card.Selected=card.Index==selected; card.RefreshColor(); }
                if(selected>=0) SetPage(selected/10);
                status.Text="已恢复上次配置 · "+(selected>=0?Art.Names[selected]:"系统指针")+" / "+sizeBar.Value+" px / 速度 "+speedBar.Value;
            }
            UpdateValues(); return true;
        } catch(Exception ex) {
            try { session.Restore(); } catch { }
            selected=-1; foreach(var card in cards) { card.Selected=false; card.Invalidate(); }
            speedBar.Value=Native.Speed; UpdateValues(); status.Text="未应用上次配置 · "+ex.Message; return false;
        } finally { initializing=false; }
    }
    void SaveCurrentProfile(bool enabled) {
        if(!persistChanges) return;
        try { new SavedProfile {Enabled=enabled,Style=selected,Size=sizeBar.Value,Speed=speedBar.Value}.Save(); }
        catch(Exception ex) { status.Text="系统设置已更新，但配置未保存 · "+ex.Message; }
    }
    void OnStartupChanged() {
        if(initializing || !persistChanges) return;
        try { StartupRegistration.Set(startupCheck.Checked,Application.ExecutablePath,StartupRegistration.ValueName); status.Text=startupCheck.Checked?"已开启开机自启 · 下次登录自动应用已保存配置":"已关闭开机自启 · 当前配置仍会自动保存"; }
        catch(Exception ex) { initializing=true; startupCheck.Checked=!startupCheck.Checked; initializing=false; Error(ex); }
    }
    void Quit() { quitting=true; Close(); }
    protected override bool ProcessCmdKey(ref Message msg,Keys keyData) { if(keyData==(Keys.Control|Keys.Q)) { Quit(); return true; } return base.ProcessCmdKey(ref msg,keyData); }
    public void SavePreview(string path) { using(var b=new Bitmap(Width,Height)) { DrawToBitmap(b,new Rectangle(0,0,Width,Height)); b.Save(path,ImageFormat.Png); } }
    public void ClosePreview() { quitting=true; Close(); }
    public void SaveLayoutPreviews() {
        Size original=ClientSize;
        foreach(Size size in new[]{new Size(1600,960),new Size(800,640)}) {
            ClientSize=size; SetPage(0); Application.DoEvents();
            SavePreview(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"preview-"+size.Width+"x"+size.Height+".png"));
        }
        ClientSize=original;
    }
    public void VerifyControls() {
        Show(); Application.DoEvents();
        if(cards.FindAll(delegate(StyleCard c){return c.Visible;}).Count!=10) throw new Exception("First page must have 10 styles");
        InvokeOnClick(nextPage,EventArgs.Empty);
        if(!cards[19].Visible || cards[0].Visible || nextPage.Enabled) throw new Exception("Second page navigation failed");
        InvokeOnClick(cards[19],EventArgs.Empty);
        if(!cards[19].Selected) throw new Exception("New style click failed");
        Color? originalColor=Art.CustomColors[19]; Color other=Art.Fill(0);
        string beforeColor=Program.Fingerprint(32512);
        ChangeColor(19,Color.Crimson,false);
        if(Art.Fill(19)!=Color.Crimson || Art.Fill(0)!=other) throw new Exception("Independent color selection failed");
        if(Program.Fingerprint(32512)==beforeColor) throw new Exception("Color did not reach actual system cursor");
        ChangeColor(19,originalColor,false);
        InvokeOnClick(previousPage,EventArgs.Empty);
        if(!cards[0].Visible || cards[19].Visible || !cards[19].Selected) throw new Exception("Paging changed selection");
        InvokeOnClick(cards[6],EventArgs.Empty);
        if(!cards[6].Selected || !session.Active) throw new Exception("Style card click did not apply");
        sizeBar.Value=72;
        DateTime deadline=DateTime.UtcNow.AddMilliseconds(300);
        while(DateTime.UtcNow<deadline) { Application.DoEvents(); Thread.Sleep(10); }
        Native.IconInfo info; Native.Check(Native.GetIconInfo(Native.LoadCursor(IntPtr.Zero,new IntPtr(32512)),out info));
        try { using(var b=Image.FromHbitmap(info.color)) if(b.Width!=72) throw new Exception("Size slider did not update system cursor"); }
        finally { Native.DeleteObject(info.color); Native.DeleteObject(info.mask); }
        speedBar.Value=13; if(Native.Speed!=13) throw new Exception("Speed slider did not update Windows");
        Restore(); if(session.Active || selected!=-1) throw new Exception("Restore button action failed");
        Close(); if(Visible || !tray.Visible) throw new Exception("Close-to-tray failed");
        ShowWindow(); if(!Visible) throw new Exception("Tray reopen failed");
        Quit();
    }
}

static class Program {
    [STAThread] static int Main(string[] args) {
        // Rendering is read-only and may run while the user's existing app is in the tray.
        if(args.Length>0 && args[0]=="--preview") {
            Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
            using(var session=new MouseSession()) using(var form=new MainForm(session)) {
                form.Show(); Application.DoEvents(); form.SavePreview(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"preview.png"));
                form.SetPage(1); Application.DoEvents(); form.SavePreview(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"preview-page2.png")); form.SaveLayoutPreviews(); form.ClosePreview();
            }
            return 0;
        }
        bool created;
        using(var mutex=new Mutex(true,"Local\\MouseStudio.Desktop.Session",out created)) {
            if(!created) { if(Array.IndexOf(args,"--startup")<0) MessageBox.Show("指针工坊已在运行，请双击系统托盘中的绿色指针图标。","指针工坊"); return 0; }
            try {
                Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
                if(args.Length>0 && args[0]=="--restore") { MouseSession.Recover(); return 0; }
                if(args.Length>0 && args[0]=="--self-test") { SelfTest(); return 0; }
                Art.LoadColors();
                if(File.Exists(MouseSession.RecoveryPath)) MouseSession.Recover();
                using(var session=new MouseSession()) using(var form=new MainForm(session)) {
                    bool loaded=form.StartUserSession(true);
                    if(Array.IndexOf(args,"--startup")>=0 && loaded) form.StartMinimized();
                    Application.Run(form);
                }
                return 0;
            } catch(Exception ex) {
                File.WriteAllText(Path.Combine(Path.GetTempPath(),"MouseStudio-error.txt"),ex.ToString());
                if(args.Length==0) MessageBox.Show(ex.Message,"指针工坊启动失败",MessageBoxButtons.OK,MessageBoxIcon.Error);
                return 1;
            }
        }
    }
    static void SelfTest() {
        var lines=new List<string>(); int original=Native.Speed;
        var fingerprints=new Dictionary<uint,string>(); foreach(uint id in Native.Roles) fingerprints[id]=Fingerprint(id);
        if(File.Exists(MouseSession.RecoveryPath)) throw new Exception("Pending recovery must be resolved before tests.");
        using(var s=new MouseSession()) {
            try {
                for(int style=0;style<20;style++) foreach(int size in new[]{24,40,96}) {
                    s.Apply(style,size);
                    foreach(uint role in Native.Roles) {
                        Native.IconInfo info; Native.Check(Native.GetIconInfo(Native.LoadCursor(IntPtr.Zero,new IntPtr(role)),out info));
                        try {
                            using(var bitmap=Image.FromHbitmap(info.color)) if(bitmap.Width!=size || bitmap.Height!=size) throw new Exception("System cursor size mismatch: "+role+" "+bitmap.Width);
                            var hot=Art.Hotspot(size,role); if(info.x!=hot.X || info.y!=hot.Y) throw new Exception("Hotspot mismatch");
                        } finally { if(info.color!=IntPtr.Zero) Native.DeleteObject(info.color); if(info.mask!=IntPtr.Zero) Native.DeleteObject(info.mask); }
                    }
                    lines.Add("PASS style="+Art.Names[style]+" size="+size+"; 14 actual system cursor handles and hotspots verified");
                }
                foreach(int speed in new[]{1,10,20}) { s.SetSpeed(speed); if(Native.Speed!=speed) throw new Exception("Speed readback mismatch"); lines.Add("PASS system mouse speed readback="+speed); }
            } finally { s.Restore(); }
        }
        if(Native.Speed!=original) throw new Exception("Original speed not restored");
        foreach(uint id in Native.Roles) if(fingerprints[id]!=Fingerprint(id)) throw new Exception("Original cursor not restored: "+id);
        if(File.Exists(MouseSession.RecoveryPath)) throw new Exception("Recovery marker not cleared");
        lines.Add("PASS all 14 original cursor bitmaps/hotspots restored; original speed="+original);
        using(var s=new MouseSession()) {
            try { using(var f=new MainForm(s)) f.VerifyControls(); }
            finally { s.Restore(); }
        }
        foreach(uint id in Native.Roles) if(fingerprints[id]!=Fingerprint(id)) throw new Exception("UI test did not restore cursor: "+id);
        if(Native.Speed!=original) throw new Exception("UI test did not restore speed");
        lines.Add("PASS UI 2 pages / 20 styles, page selection retention, per-style color isolation, size slider, speed slider, restore, tray and exit");
        BehaviorTests.Run();
        lines.Add("PASS saved profiles across reopen, logon-style tray launch, registry enable/disable, responsive 800/1080/1600 layouts, corrupt configuration recovery");
        File.WriteAllLines(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"test-results.txt"),lines);
    }
    public static string Fingerprint(uint role) {
        Native.IconInfo info; Native.Check(Native.GetIconInfo(Native.LoadCursor(IntPtr.Zero,new IntPtr(role)),out info));
        try {
            using(var stream=new MemoryStream()) {
                if(info.color!=IntPtr.Zero) using(var b=Image.FromHbitmap(info.color)) b.Save(stream,ImageFormat.Bmp);
                if(info.mask!=IntPtr.Zero) using(var b=Image.FromHbitmap(info.mask)) b.Save(stream,ImageFormat.Bmp);
                using(var sha=System.Security.Cryptography.SHA256.Create()) return info.x+":"+info.y+":"+Convert.ToBase64String(sha.ComputeHash(stream.ToArray()));
            }
        } finally { if(info.color!=IntPtr.Zero) Native.DeleteObject(info.color); if(info.mask!=IntPtr.Zero) Native.DeleteObject(info.mask); }
    }
}

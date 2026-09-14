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
    [DllImport("user32.dll")] public static extern bool DestroyCursor(IntPtr cursor);
    [DllImport("user32.dll")] public static extern bool DestroyIcon(IntPtr icon);
    [DllImport("gdi32.dll")] public static extern bool DeleteObject(IntPtr obj);
    [DllImport("gdi32.dll")] public static extern IntPtr CreateBitmap(int w, int h, uint planes, uint bits, IntPtr data);
    public static readonly uint[] Roles = {32512,32513,32514,32515,32516,32642,32643,32644,32645,32646,32648,32649,32650,32651};
    public static void Check(bool ok) { if(!ok) throw new Win32Exception(Marshal.GetLastWin32Error()); }
    public static int Speed { get { int n=10; Check(GetParameter(0x70,0,ref n,0)); return n; } set { Check(SystemParametersInfo(0x71,0,new IntPtr(Math.Max(1,Math.Min(20,value))),0)); } }
    public static IntPtr Copy(IntPtr h) { IntPtr c=CopyImage(h,2,0,0,0); Check(c!=IntPtr.Zero); return c; }
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
    readonly Dictionary<uint,IntPtr> originals=new Dictionary<uint,IntPtr>();
    readonly int speed;
    bool dirty;
    public bool Active { get { return dirty; } }
    public static string RecoveryPath { get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"MouseStudio","recovery.txt"); } }
    public MouseSession() {
        speed=Native.Speed;
        try { foreach(uint id in Native.Roles) originals[id]=Native.Copy(Native.LoadCursor(IntPtr.Zero,new IntPtr(id))); }
        catch { Dispose(); throw; }
    }
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
        foreach(var kv in originals) {
            try { Native.Check(Native.SetSystemCursor(Native.Copy(kv.Value),kv.Key)); }
            catch(Exception e) { error=e; }
        }
        try { Native.Speed=speed; } catch(Exception e) { error=e; }
        if(error!=null) throw error;
        dirty=false;
        if(File.Exists(RecoveryPath)) File.Delete(RecoveryPath);
    }
    public static void Recover() {
        Native.Check(Native.SystemParametersInfo(0x57,0,IntPtr.Zero,0));
        if(File.Exists(RecoveryPath)) {
            int n; if(int.TryParse(File.ReadAllText(RecoveryPath),out n) && n>=1 && n<=20) Native.Speed=n;
            File.Delete(RecoveryPath);
        }
    }
    public void Dispose() { foreach(IntPtr h in originals.Values) Native.DestroyCursor(h); originals.Clear(); }
}

static class Art {
    public static readonly string[] Names={"极简白","曜石黑","赛博霓虹","樱花粉","薄荷绿","像素复古","日落橙","深海蓝","香槟金","星际紫"};
    public static readonly string[] Tags={"清晰 · 经典","沉稳 · 锐利","荧光 · 未来","柔和 · 甜美","轻盈 · 自然","方格 · 怀旧","暖色 · 活力","流线 · 冷静","金属 · 精致","星芒 · 幻想"};
    public static readonly Color[] Fills={Color.White,Color.FromArgb(32,37,44),Color.FromArgb(14,26,40),Color.FromArgb(255,178,207),Color.FromArgb(110,224,181),Color.FromArgb(255,249,220),Color.FromArgb(255,152,69),Color.FromArgb(47,143,232),Color.FromArgb(224,190,118),Color.FromArgb(183,145,244)};
    public static readonly Color[] Edges={Color.FromArgb(36,42,50),Color.FromArgb(220,227,237),Color.FromArgb(74,244,226),Color.FromArgb(136,50,91),Color.FromArgb(22,94,78),Color.FromArgb(62,52,55),Color.FromArgb(136,55,27),Color.FromArgb(15,50,108),Color.FromArgb(88,67,33),Color.FromArgb(71,43,123)};
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
            using(var fill=new SolidBrush(Fills[s])) using(var pen=new Pen(Edges[s],s==5?3:2.6f)) {
                pen.LineJoin=LineJoin.Round; pen.StartCap=LineCap.Round; pen.EndCap=LineCap.Round;
                if(role==32512 || role==32650 || role==32651) {
                    switch(s) {
                        case 0: Poly(g,fill,pen,5,4,5,48,17,37,27,58,36,54,26,34,43,34); break;
                        case 1: Poly(g,fill,pen,5,4,12,52,23,37,43,31); break;
                        case 2:
                            using(var glow=new Pen(Color.FromArgb(75,74,244,226),7)) g.DrawPolygon(glow,Points(5,4,8,49,20,35,31,55,37,51,26,31,45,30));
                            Poly(g,fill,pen,5,4,8,49,20,35,31,55,37,51,26,31,45,30);
                            using(var line=new Pen(Color.FromArgb(239,86,228),2)) g.DrawLine(line,13,17,17,32); break;
                        case 3:
                            Poly(g,fill,pen,5,4,7,48,18,37,28,56,36,51,26,33,44,32);
                            using(var pink=new SolidBrush(Color.FromArgb(217,67,128))) { g.FillEllipse(pink,21,20,8,8); g.FillEllipse(pink,27,20,8,8); g.FillPolygon(pink,Points(21,24,35,24,28,33)); } break;
                        case 4:
                            using(var path=new GraphicsPath()) { path.AddBezier(5,4,46,9,53,39,24,36); path.AddLine(24,36,33,55); path.AddLine(33,55,25,58); path.AddLine(25,58,16,36); path.AddBezier(16,36,3,35,6,17,5,4); g.FillPath(fill,path); g.DrawPath(pen,path); }
                            g.DrawLine(pen,11,14,25,38); break;
                        case 5: Poly(g,fill,pen,5,4,11,4,11,10,17,10,17,16,23,16,23,22,29,22,29,28,41,28,41,34,29,34,29,40,35,40,35,52,29,52,29,46,23,46,23,34,17,34,17,40,11,40,11,46,5,46); break;
                        case 6: Poly(g,fill,pen,5,4,44,25,28,30,35,48,27,53,19,34,6,46);
                            using(var hi=new Pen(Color.FromArgb(255,224,144),3)) g.DrawLine(hi,12,14,28,24); break;
                        case 7: Poly(g,fill,pen,5,4,49,34,27,33,22,55);
                            using(var hi=new Pen(Color.FromArgb(142,227,255),2)) g.DrawLine(hi,12,13,26,30); break;
                        case 8:
                            using(var gold=new LinearGradientBrush(new Point(5,4),new Point(40,52),Color.FromArgb(255,244,197),Fills[s])) Poly(g,gold,pen,5,4,8,49,19,37,29,56,36,51,26,32,44,32);
                            using(var inner=new Pen(Color.FromArgb(255,250,221),1.3f)) g.DrawLines(inner,Points(10,17,12,36,19,29,29,32)); break;
                        default: Poly(g,fill,pen,5,4,39,23,27,29,36,49,27,54,19,34,8,45);
                            using(var star=new SolidBrush(Color.FromArgb(255,237,164))) Poly(g,star,pen,47,8,49,15,56,17,49,19,47,26,45,19,38,17,45,15); break;
                    }
                    if(role==32650) { g.FillEllipse(fill,38,39,22,22); g.DrawArc(pen,41,42,16,16,-80,270); }
                    if(role==32651) { using(var f=new Font("Segoe UI",21,FontStyle.Bold,GraphicsUnit.Pixel)) g.DrawString("?",f,fill,37,30); }
                } else if(role==32513) {
                    using(var outer=new Pen(Edges[s],7)) using(var inner=new Pen(Fills[s],3)) { foreach(Pen p in new[]{outer,inner}) { g.DrawLine(p,32,9,32,55); g.DrawLine(p,22,9,42,9); g.DrawLine(p,22,55,42,55); } }
                } else if(role==32514) {
                    using(var p=new Pen(Edges[s],8)) g.DrawEllipse(p,12,12,40,40);
                    using(var p=new Pen(Fills[s],5)) g.DrawArc(p,12,12,40,40,-90,285);
                } else if(role==32649) {
                    Poly(g,fill,pen,21,34,21,11,24,7,28,7,31,11,31,27,36,25,41,29,46,28,51,33,56,33,58,38,55,50,48,58,30,58,22,49,12,37,13,32,18,31);
                    g.DrawLine(pen,31,28,31,38); g.DrawLine(pen,41,30,41,40); g.DrawLine(pen,50,34,50,42);
                } else if(role==32648) {
                    g.FillEllipse(fill,9,9,46,46); g.DrawEllipse(pen,9,9,46,46); using(var p=new Pen(Edges[s],5)) g.DrawLine(p,16,16,48,48);
                } else if(role==32515) {
                    using(var outP=new Pen(Edges[s],6)) using(var inP=new Pen(Fills[s],2)) foreach(var p in new[]{outP,inP}) { g.DrawLine(p,32,6,32,58); g.DrawLine(p,6,32,58,32); }
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
    readonly Font titleFont=new Font("Microsoft YaHei UI",10,FontStyle.Bold);
    public StyleCard(int index) { Index=index; DoubleBuffered=true; TabStop=true; AccessibleName=Art.Names[index]; AccessibleRole=AccessibleRole.PushButton; Size=new Size(180,142); Cursor=Cursors.Hand; }
    protected override void OnMouseEnter(EventArgs e) { hover=true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { hover=false; Invalidate(); base.OnMouseLeave(e); }
    protected override void OnGotFocus(EventArgs e) { Invalidate(); base.OnGotFocus(e); }
    protected override void OnLostFocus(EventArgs e) { Invalidate(); base.OnLostFocus(e); }
    protected override void OnKeyDown(KeyEventArgs e) { if(e.KeyCode==Keys.Enter || e.KeyCode==Keys.Space) { OnClick(EventArgs.Empty); e.Handled=true; } base.OnKeyDown(e); }
    protected override void OnPaint(PaintEventArgs e) {
        Graphics g=e.Graphics; g.SmoothingMode=SmoothingMode.AntiAlias;
        using(var bg=new SolidBrush(Selected?Color.FromArgb(235,246,241):hover?Color.FromArgb(246,248,246):Color.White)) g.FillRectangle(bg,0,0,Width,Height);
        using(var p=new Pen(Selected?MainForm.Green:Color.FromArgb(225,229,225),Selected?2:1)) g.DrawRectangle(p,1,1,Width-3,Height-3);
        using(var b=Art.Render(Index,56,32512)) g.DrawImageUnscaled(b,Width/2-24,12);
        TextRenderer.DrawText(g,Art.Names[Index],titleFont,new Rectangle(0,80,Width,23),MainForm.Ink,TextFormatFlags.HorizontalCenter);
        TextRenderer.DrawText(g,Art.Tags[Index],Font,new Rectangle(0,107,Width,23),MainForm.Muted,TextFormatFlags.HorizontalCenter);
        if(Selected) { using(var b=new SolidBrush(MainForm.Green)) g.FillEllipse(b,Width-23,9,13,13); }
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

sealed class MainForm : Form {
    public static readonly Color Ink=Color.FromArgb(32,49,43), Muted=Color.FromArgb(113,126,117), Green=Color.FromArgb(30,111,79);
    readonly MouseSession session;
    readonly List<StyleCard> cards=new List<StyleCard>();
    readonly TrackBar sizeBar=new TrackBar(), speedBar=new TrackBar();
    readonly Label sizeValue=new Label(), speedValue=new Label(), status=new Label();
    readonly Preview preview=new Preview();
    readonly System.Windows.Forms.Timer debounce=new System.Windows.Forms.Timer();
    readonly NotifyIcon tray=new NotifyIcon();
    int selected=-1; bool initializing=true, quitting;
    public MainForm(MouseSession s) {
        session=s; Text="指针工坊 · Mouse Studio"; ClientSize=new Size(1060,790); MinimumSize=new Size(1090,835);
        Font=new Font("Microsoft YaHei UI",9); BackColor=Color.FromArgb(247,248,244); ForeColor=Ink; AutoScaleMode=AutoScaleMode.Dpi; StartPosition=FormStartPosition.CenterScreen;
        using(var b=Art.Render(4,64,32512)) { IntPtr h=b.GetHicon(); try { Icon=(Icon)Icon.FromHandle(h).Clone(); } finally { Native.DestroyIcon(h); } }
        var root=new Panel { Dock=DockStyle.Fill,AutoScroll=true,Padding=new Padding(32) }; Controls.Add(root);
        Label brand=TextLabel("MOUSE STUDIO   /   指针工坊",32,22,650,25,10,FontStyle.Bold); brand.ForeColor=Green; root.Controls.Add(brand);
        root.Controls.Add(TextLabel("让每一次移动，都有你的风格。",30,51,940,59,19,FontStyle.Bold));
        root.Controls.Add(TextLabel("10 套原创指针  ·  系统全局生效  ·  随时恢复",33,110,720,28,10,FontStyle.Regular));
        root.Controls.Add(TextLabel("01   选择你的指针",32,159,500,28,12,FontStyle.Bold));
        root.Controls.Add(TextLabel("点击即应用",887,161,150,24,9,FontStyle.Regular));
        for(int i=0;i<10;i++) {
            var c=new StyleCard(i) { Location=new Point(32+(i%5)*201,202+(i/5)*154),Width=192 };
            c.Click+=delegate(object sender,EventArgs e) { var card=(StyleCard)sender; selected=card.Index; ApplyStyle(); };
            cards.Add(c); root.Controls.Add(c);
        }
        var controlsPanel=new Panel { Location=new Point(32,523),Size=new Size(996,181),BackColor=Color.White }; root.Controls.Add(controlsPanel);
        controlsPanel.Controls.Add(TextLabel("02   指针大小",20,17,220,26,12,FontStyle.Bold));
        sizeValue.SetBounds(236,18,120,24); sizeValue.TextAlign=ContentAlignment.MiddleRight; controlsPanel.Controls.Add(sizeValue);
        ConfigureBar(sizeBar,24,96,40,16,61,344); sizeBar.SmallChange=1; sizeBar.LargeChange=8; controlsPanel.Controls.Add(sizeBar);
        controlsPanel.Controls.Add(TextLabel("小",23,109,40,20,9,FontStyle.Regular)); controlsPanel.Controls.Add(TextLabel("大",322,109,40,20,9,FontStyle.Regular));
        controlsPanel.Controls.Add(TextLabel("选择风格后，拖动即可实时调整",23,143,350,22,9,FontStyle.Regular));
        preview.SetBounds(379,22,143,137); controlsPanel.Controls.Add(preview);
        controlsPanel.Controls.Add(TextLabel("03   鼠标灵敏度",559,17,250,26,12,FontStyle.Bold));
        speedValue.SetBounds(852,18,114,24); speedValue.TextAlign=ContentAlignment.MiddleRight; controlsPanel.Controls.Add(speedValue);
        ConfigureBar(speedBar,1,20,Native.Speed,553,61,418); controlsPanel.Controls.Add(speedBar);
        controlsPanel.Controls.Add(TextLabel("慢",559,109,40,20,9,FontStyle.Regular)); controlsPanel.Controls.Add(TextLabel("快",936,109,40,20,9,FontStyle.Regular));
        controlsPanel.Controls.Add(TextLabel("调整 Windows 指针速度，非鼠标硬件 DPI",559,143,420,22,9,FontStyle.Regular));
        status.SetBounds(33,719,690,28); status.ForeColor=Green; status.Text="准备就绪 · 当前使用原有系统设置"; root.Controls.Add(status);
        root.Controls.Add(TextLabel("关闭窗口后驻留托盘；从托盘退出会恢复原设置。",33,753,720,22,9,FontStyle.Regular));
        var restore=new Button {Text="恢复原设置",Location=new Point(851,724),Size=new Size(176,43),FlatStyle=FlatStyle.Flat,BackColor=Green,ForeColor=Color.White}; restore.FlatAppearance.BorderSize=0; restore.Click+=delegate { Restore(); }; root.Controls.Add(restore);
        var menu=new ContextMenuStrip(); menu.Items.Add("打开指针工坊",null,delegate { ShowWindow(); }); menu.Items.Add("恢复原设置",null,delegate { Restore(); }); menu.Items.Add(new ToolStripSeparator()); menu.Items.Add("退出并恢复",null,delegate { Quit(); });
        tray.Icon=Icon; tray.Text="指针工坊 · 双击打开"; tray.ContextMenuStrip=menu; tray.DoubleClick+=delegate { ShowWindow(); };
        debounce.Interval=100; debounce.Tick+=delegate { debounce.Stop(); ApplyStyle(); };
        sizeBar.ValueChanged+=delegate { UpdateValues(); if(initializing) return; if(selected<0) selected=0; debounce.Stop(); debounce.Start(); };
        speedBar.ValueChanged+=delegate { UpdateValues(); if(initializing) return; try { session.SetSpeed(speedBar.Value); status.Text="已应用 · 系统鼠标速度 "+Native.Speed+" / 20"; } catch(Exception ex) { Error(ex); } };
        FormClosing+=delegate(object sender,FormClosingEventArgs e) {
            if(!quitting && e.CloseReason==CloseReason.UserClosing) { e.Cancel=true; Hide(); tray.Visible=true; tray.ShowBalloonTip(2500,"指针工坊仍在运行","双击托盘图标重新打开；右键可退出并恢复原设置。",ToolTipIcon.Info); }
            else { debounce.Stop(); try { session.Restore(); } catch(Exception ex) { if(e.CloseReason==CloseReason.UserClosing) { e.Cancel=true; quitting=false; Error(ex); } } }
        };
        FormClosed+=delegate { tray.Dispose(); debounce.Dispose(); };
        UpdateValues(); initializing=false;
    }
    static Label TextLabel(string text,int x,int y,int w,int h,float size,FontStyle weight) { return new Label {Text=text,Location=new Point(x,y),Size=new Size(w,h),Font=new Font("Microsoft YaHei UI",size,weight),ForeColor=weight==FontStyle.Bold?Ink:Muted}; }
    static void ConfigureBar(TrackBar b,int min,int max,int value,int x,int y,int width) { b.Minimum=min; b.Maximum=max; b.Value=value; b.SetBounds(x,y,width,42); b.TickStyle=TickStyle.None; b.BackColor=Color.White; }
    void UpdateValues() { sizeValue.Text=sizeBar.Value+" px"; speedValue.Text=speedBar.Value+" / 20"; preview.CursorSize=sizeBar.Value; preview.Style=Math.Max(0,selected); preview.Invalidate(); }
    void ApplyStyle() {
        if(selected<0) return;
        try { session.Apply(selected,sizeBar.Value); foreach(var c in cards) { c.Selected=c.Index==selected; c.Invalidate(); } UpdateValues(); status.Text="已全局应用 · "+Art.Names[selected]+" / "+sizeBar.Value+" px"; }
        catch(Exception ex) { selected=-1; foreach(var c in cards) { c.Selected=false; c.Invalidate(); } Error(ex); }
    }
    void Error(Exception ex) { status.Text="未能完成操作 · "+ex.Message; MessageBox.Show(this,ex.Message,"操作未完成",MessageBoxButtons.OK,MessageBoxIcon.Warning); }
    void Restore() {
        debounce.Stop(); try { session.Restore(); selected=-1; foreach(var c in cards) { c.Selected=false; c.Invalidate(); } initializing=true; speedBar.Value=Native.Speed; initializing=false; UpdateValues(); status.Text="已恢复 · 启动时的指针与鼠标速度"; } catch(Exception ex) { Error(ex); }
    }
    void ShowWindow() { Show(); WindowState=FormWindowState.Normal; Activate(); tray.Visible=false; }
    void Quit() { quitting=true; Close(); }
    public void SavePreview(string path) { using(var b=new Bitmap(Width,Height)) { DrawToBitmap(b,new Rectangle(0,0,Width,Height)); b.Save(path,ImageFormat.Png); } }
    public void ClosePreview() { quitting=true; Close(); }
    public void VerifyControls() {
        Show(); Application.DoEvents();
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
        bool created;
        using(var mutex=new Mutex(true,"Local\\MouseStudio.Desktop.Session",out created)) {
            if(!created) { MessageBox.Show("指针工坊已在运行，请双击系统托盘中的绿色指针图标。","指针工坊"); return 0; }
            try {
                Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
                if(args.Length>0 && args[0]=="--restore") { MouseSession.Recover(); return 0; }
                if(args.Length>0 && args[0]=="--self-test") { SelfTest(); return 0; }
                if(File.Exists(MouseSession.RecoveryPath)) MouseSession.Recover();
                using(var session=new MouseSession()) using(var form=new MainForm(session)) {
                    if(args.Length>0 && args[0]=="--preview") { form.Show(); Application.DoEvents(); form.SavePreview(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"preview.png")); form.ClosePreview(); }
                    else Application.Run(form);
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
                for(int style=0;style<10;style++) foreach(int size in new[]{24,40,96}) {
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
        lines.Add("PASS UI card click, debounced size slider, speed slider, restore, close-to-tray, reopen and exit");
        File.WriteAllLines(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"test-results.txt"),lines);
    }
    static string Fingerprint(uint role) {
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

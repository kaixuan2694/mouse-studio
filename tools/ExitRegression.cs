using System;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

// Runs the UI lifecycle in a separate process and checks cursors AFTER it exits.
static class ExitRegression {
    [DllImport("user32.dll")] static extern bool SystemParametersInfo(uint a,uint b,IntPtr c,uint d);
    [DllImport("user32.dll",EntryPoint="SystemParametersInfoW")] static extern bool GetSpeed(uint a,uint b,ref int c,uint d);
    [DllImport("user32.dll")] static extern IntPtr LoadCursor(IntPtr h,IntPtr id);
    [DllImport("user32.dll")] static extern IntPtr SetCursor(IntPtr h);
    [DllImport("user32.dll")] static extern IntPtr CreateCursor(IntPtr instance,int x,int y,int width,int height,byte[] andMask,byte[] xorMask);
    [DllImport("user32.dll")] static extern bool SetSystemCursor(IntPtr cursor,uint id);
    [StructLayout(LayoutKind.Sequential)] struct CursorInfo { public int size,flags; public IntPtr cursor; public Point position; }
    [DllImport("user32.dll")] static extern bool GetCursorInfo(ref CursorInfo info);
    [DllImport("user32.dll")] static extern bool DrawIconEx(IntPtr dc,int x,int y,IntPtr icon,int w,int h,uint step,IntPtr brush,uint flags);
    static readonly uint[] Roles={32512,32513,32514,32515,32516,32642,32643,32644,32645,32646,32648,32649,32650,32651};
    static void Invoke(object o,string method,params object[] args) { o.GetType().GetMethod(method,BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance).Invoke(o,args); }
    static string Fingerprint(Assembly a,uint role) { return (string)a.GetType("Program").GetMethod("Fingerprint").Invoke(null,new object[]{role}); }
    static int VisiblePixels(uint role) { return VisiblePixels(LoadCursor(IntPtr.Zero,new IntPtr(role))); }
    static int VisiblePixels(IntPtr cursor) {
        using(var b=new Bitmap(96,96)) using(var g=Graphics.FromImage(b)) {
            g.Clear(Color.Magenta); IntPtr dc=g.GetHdc();
            try { if(!DrawIconEx(dc,0,0,cursor,96,96,0,IntPtr.Zero,3)) throw new Exception("DrawIconEx failed"); }
            finally { g.ReleaseHdc(dc); }
            int count=0; for(int y=0;y<96;y++) for(int x=0;x<96;x++) if(b.GetPixel(x,y).ToArgb()!=Color.Magenta.ToArgb()) count++;
            return count;
        }
    }
    static void CheckDisplayedCursor() {
        CursorInfo info=new CursorInfo {size=Marshal.SizeOf(typeof(CursorInfo))};
        if(!GetCursorInfo(ref info)) throw new Exception("GetCursorInfo failed");
        if(info.flags==1 && (info.cursor==IntPtr.Zero || VisiblePixels(info.cursor)==0)) throw new Exception("Currently displayed cursor is invisible");
        Console.WriteLine("Displayed cursor flags="+info.flags+"; handle="+info.cursor);
    }
    [STAThread] static int Main(string[] args) {
        string target=System.IO.Path.GetFullPath(args[0]); Assembly assembly=Assembly.LoadFile(target);
        if(args.Length>1) { Child(assembly,args[1]); return 0; }
        bool created;
        using(var gate=new Mutex(true,"Local\\MouseStudio.Desktop.Session",out created)) {
        if(!created) { Console.Error.WriteLine("Exit the running Mouse Studio instance before testing."); return 1; }
        int originalSpeed=10; GetSpeed(0x70,0,ref originalSpeed,0);
        try {
            if(!SystemParametersInfo(0x57,0,IntPtr.Zero,0)) throw new Exception("Cannot reload system cursor theme");
            string[] before=new string[Roles.Length]; for(int i=0;i<Roles.Length;i++) before[i]=Fingerprint(assembly,Roles[i]);
            foreach(string mode in new[]{"blank-start","quit","restore-quit","tray-quit","pending-quit","dispose","restore-twice","startup"}) {
                var start=new ProcessStartInfo(Assembly.GetExecutingAssembly().Location,"\""+target+"\" "+mode) { UseShellExecute=false,CreateNoWindow=true,WindowStyle=ProcessWindowStyle.Hidden };
                using(var p=Process.Start(start)) { if(!p.WaitForExit(15000)) { p.Kill(); throw new Exception("Child timed out: "+mode); } if(p.ExitCode!=0) throw new Exception("Child failed: "+mode); }
                Thread.Sleep(100);
                CheckDisplayedCursor();
                int mismatches=0;
                for(int i=0;i<Roles.Length;i++) { if(Fingerprint(assembly,Roles[i])!=before[i]) mismatches++; if(VisiblePixels(Roles[i])==0) throw new Exception("INVISIBLE cursor after "+mode+": "+Roles[i]); }
                int speed=0; GetSpeed(0x70,0,ref speed,0);
                Console.WriteLine(mode+": changed cursor images="+mismatches+"; speed="+speed+"; all 14 cursors render visibly after child exit");
                if(mismatches!=0 || speed!=originalSpeed) throw new Exception("Restoration mismatch after "+mode);
            }
            return 0;
        } catch(Exception e) { Console.Error.WriteLine(e.ToString()); return 1; }
        finally { SystemParametersInfo(0x57,0,IntPtr.Zero,0); SystemParametersInfo(0x71,0,new IntPtr(originalSpeed),0); }
        }
    }
    static void Child(Assembly assembly,string mode) {
        Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
        string isolatedDirectory=null;
        if(mode=="startup") {
            isolatedDirectory=System.IO.Path.Combine(System.IO.Path.GetTempPath(),"MouseStudioStartup-"+Guid.NewGuid().ToString("N"));
            assembly.GetType("AppStorage").GetField("DirectoryPath").SetValue(null,isolatedDirectory);
            Type profileType=assembly.GetType("SavedProfile"); object profile=Activator.CreateInstance(profileType);
            profileType.GetField("Enabled").SetValue(profile,true); profileType.GetField("Style").SetValue(profile,19); profileType.GetField("Size").SetValue(profile,64); profileType.GetField("Speed").SetValue(profile,13); Invoke(profile,"Save");
        }
        if(mode=="blank-start") {
            // A previous interrupted/custom cursor session may leave an invisible
            // cursor. It must never become the app's permanent restore baseline.
            byte[] andMask=new byte[32], xorMask=new byte[32]; for(int i=0;i<andMask.Length;i++) andMask[i]=255;
            IntPtr blank=CreateCursor(IntPtr.Zero,0,0,16,16,andMask,xorMask);
            if(blank==IntPtr.Zero || !SetSystemCursor(blank,32512)) throw new Exception("Could not stage invisible initial cursor");
        }
        object session=Activator.CreateInstance(assembly.GetType("MouseSession"));
        if(mode=="dispose") { try { Invoke(session,"Apply",5,72); Invoke(session,"SetSpeed",13); } finally { ((IDisposable)session).Dispose(); } return; }
        using(var form=(Form)Activator.CreateInstance(assembly.GetType("MainForm"),new[]{session})) using(var timer=new System.Windows.Forms.Timer()) {
            int stage=0;
            if(mode=="startup") { Invoke(form,"StartUserSession",false); Invoke(form,"StartMinimized"); }
            timer.Interval=150;
            timer.Tick+=delegate {
                if(mode=="startup") {
                    timer.Stop(); int startupSpeed=0; GetSpeed(0x70,0,ref startupSpeed,0);
                    if(form.Visible || startupSpeed!=13) throw new Exception("Logon launch did not stay hidden with saved speed applied");
                    CheckDisplayedCursor(); Invoke(form,"Quit"); return;
                }
                if(stage++==0) {
                    form.GetType().GetField("selected",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(form,5);
                    Invoke(form,"ApplyStyle"); Invoke(session,"SetSpeed",13); SetCursor(LoadCursor(IntPtr.Zero,new IntPtr(32512)));
                    if(mode=="tray-quit") form.Close();
                    if(mode=="pending-quit") { var bar=(TrackBar)form.GetType().GetField("sizeBar",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(form); bar.Value=96; Invoke(form,"Quit"); }
                } else { timer.Stop(); if(mode=="restore-quit" || mode=="restore-twice") { Invoke(form,"Restore"); CheckDisplayedCursor(); if(mode=="restore-twice") { Invoke(form,"Restore"); CheckDisplayedCursor(); } } Invoke(form,"Quit"); }
            };
            if(mode=="startup") timer.Start(); else form.Shown+=delegate { timer.Start(); };
            try { Application.Run(form); } finally {
                ((IDisposable)session).Dispose();
                if(isolatedDirectory!=null && System.IO.Directory.Exists(isolatedDirectory)) { foreach(string file in System.IO.Directory.GetFiles(isolatedDirectory)) System.IO.File.Delete(file); System.IO.Directory.Delete(isolatedDirectory); }
            }
        }
    }
}

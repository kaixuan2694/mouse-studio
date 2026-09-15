using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

sealed partial class MainForm {
    public void VerifyResponsiveLayout() {
        ShowWindow(); Application.DoEvents();
        SetPage(0); ClientSize=new Size(1080,880); Application.DoEvents();
        int cardWidth=cards[0].Width, sliderWidth=sizeBar.Width, pagerLeft=nextPage.PointToScreen(Point.Empty).X-Left;
        ClientSize=new Size(1600,960); Application.DoEvents();
        if(cards[0].Width<=cardWidth || sizeBar.Width<=sliderWidth || nextPage.PointToScreen(Point.Empty).X-Left<=pagerLeft) throw new Exception("Controls did not resize with the window");
        if(cards[4].Right>cardGrid.ClientSize.Width || cards[9].Bottom>cardGrid.ClientSize.Height) throw new Exception("Card grid overflow");
        foreach(var card in cards) if(card.Parent!=null && (card.ColorButton.Right>card.Width || card.ColorButton.Bottom>card.Height-60)) throw new Exception("Color button escaped card");
        ClientSize=new Size(800,640); Application.DoEvents();
        if(viewport.HorizontalScroll.Visible || !viewport.VerticalScroll.Visible) throw new Exception("Small window should scroll vertically without horizontal overflow");
        viewport.ScrollControlIntoView(startupCheck); Application.DoEvents();
        if(startupCheck.Height<20 || startupCheck.Width<300) throw new Exception("Startup option clipped");
        viewport.ScrollControlIntoView(status); Application.DoEvents();
        if(status.Height<18) throw new Exception("Footer collapsed");
        if(status.Parent.Bottom>status.Parent.Parent.ClientSize.Height) throw new Exception("Footer text overflows its row");
        ClientSize=new Size(1080,880); viewport.AutoScrollPosition=Point.Empty; SetPage(1); Application.DoEvents();
    }
    public void VerifySavedStart(bool minimized) {
        if(!StartUserSession(false)) throw new Exception("Saved profile failed to load");
        if(selected!=17 || page!=1 || sizeBar.Value!=68 || Native.Speed!=13) throw new Exception("Saved style/size/speed was not applied");
        Native.IconInfo info; Native.Check(Native.GetIconInfo(Native.LoadCursor(IntPtr.Zero,new IntPtr(32512)),out info));
        try { using(var b=Image.FromHbitmap(info.color)) if(b.Width!=68) throw new Exception("Startup did not change actual cursor size"); }
        finally { Native.DeleteObject(info.color); Native.DeleteObject(info.mask); }
        if(minimized) { StartMinimized(); Show(); Application.DoEvents(); if(Visible || !tray.Visible) throw new Exception("Startup must stay in the tray"); }
    }
    public void ChangeSavedProfileForTest() { ChangeColor(17,Color.CornflowerBlue,true); sizeBar.Value=76; speedBar.Value=14; }
    public void QuitForTest() { Quit(); }
    public void RestoreForTest() { Restore(); }
}

static class BehaviorTests {
    public static void Run() {
        string originalDirectory=AppStorage.DirectoryPath;
        string testDirectory=Path.Combine(Path.GetTempPath(),"MouseStudioTests-"+Guid.NewGuid().ToString("N"));
        Color?[] originalColors=(Color?[])Art.CustomColors.Clone();
        string registration="MouseStudio.Regression."+Guid.NewGuid().ToString("N");
        int originalSpeed=Native.Speed;
        try {
            AppStorage.DirectoryPath=testDirectory;
            new SavedProfile {Enabled=true,Style=17,Size=68,Speed=13}.Save();
            Art.CustomColors[17]=Color.Crimson; Art.SaveColors(); Array.Clear(Art.CustomColors,0,Art.CustomColors.Length); Art.LoadColors();
            if(Art.Fill(17).ToArgb()!=Color.Crimson.ToArgb()) throw new Exception("Color preference did not survive reload");
            using(var session=new MouseSession()) using(var form=new MainForm(session)) {
                form.VerifySavedStart(true); form.VerifyResponsiveLayout(); form.ChangeSavedProfileForTest();
                // Quit before the size debounce fires: the user's final value must survive.
                form.QuitForTest();
            }
            var saved=SavedProfile.Load();
            if(!saved.Enabled || saved.Style!=17 || saved.Size!=76 || saved.Speed!=14 || Native.Speed!=originalSpeed) throw new Exception("Exit erased last profile or did not restore system speed");
            Array.Clear(Art.CustomColors,0,Art.CustomColors.Length); Art.LoadColors();
            if(Art.Fill(17).ToArgb()!=Color.CornflowerBlue.ToArgb()) throw new Exception("Changed color did not persist");
            using(var session=new MouseSession()) using(var form=new MainForm(session)) {
                if(!form.StartUserSession(false) || Native.Speed!=14) throw new Exception("Relaunch did not restore latest profile");
                form.Show(); Application.DoEvents(); form.RestoreForTest();
                if(SavedProfile.Load().Enabled) throw new Exception("Explicit restore must clear automatic style application");
                form.QuitForTest();
            }
            // The same registry writer is exercised using an isolated value name.
            string exe=Application.ExecutablePath;
            StartupRegistration.Set(true,exe,registration);
            if(StartupRegistration.Read(registration)!="\""+exe+"\" --startup") throw new Exception("Startup command missing quotes or startup flag");
            StartupRegistration.Set(false,exe,registration);
            if(StartupRegistration.Read(registration)!=null) throw new Exception("Disabling startup did not remove the entry");
            File.WriteAllText(SavedProfile.FilePath,"version=1\nenabled=1\nstyle=99\nsize=999\nspeed=30");
            bool rejected=false; try { SavedProfile.Load(); } catch(InvalidDataException) { rejected=true; }
            if(!rejected) throw new Exception("Malformed profile was accepted");
            Console.WriteLine("PASS persistence, startup/tray application, final slider value, explicit restore, registry opt-in/out, malformed profiles and responsive layouts");
        } finally {
            StartupRegistration.Set(false,Application.ExecutablePath,registration);
            Native.ReloadCursors(); Native.Speed=originalSpeed;
            AppStorage.DirectoryPath=originalDirectory; Array.Copy(originalColors,Art.CustomColors,originalColors.Length);
            if(Directory.Exists(testDirectory)) { foreach(string path in Directory.GetFiles(testDirectory)) File.Delete(path); Directory.Delete(testDirectory); }
        }
    }
}

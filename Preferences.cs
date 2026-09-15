using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Win32;

static class AppStorage {
    // Tests inject an isolated directory; normal launches always use LocalApplicationData.
    public static string DirectoryPath=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"MouseStudio");
    public static void WriteAtomic(string path,string[] lines) {
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllLines(path+".tmp",lines);
        if(File.Exists(path)) File.Replace(path+".tmp",path,null); else File.Move(path+".tmp",path);
    }
}

sealed class SavedProfile {
    public bool Enabled;
    public int Style=-1, Size=40, Speed=10;
    public static string FilePath { get { return Path.Combine(AppStorage.DirectoryPath,"profile.txt"); } }
    public static SavedProfile Load() {
        var result=new SavedProfile(); if(!File.Exists(FilePath)) return result;
        var values=new Dictionary<string,string>();
        foreach(string line in File.ReadAllLines(FilePath)) { int split=line.IndexOf('='); if(split>0) values[line.Substring(0,split)]=line.Substring(split+1); }
        int version,enabled,style,size,speed;
        if(!Read(values,"version",out version) || version!=1 || !Read(values,"enabled",out enabled) || (enabled!=0 && enabled!=1) ||
           !Read(values,"style",out style) || style < -1 || style>=Art.Names.Length ||
           !Read(values,"size",out size) || size<24 || size>96 || !Read(values,"speed",out speed) || speed<1 || speed>20)
            throw new InvalidDataException("已保存的配置无效，已保留系统鼠标设置；请重新选择指针。");
        result.Enabled=enabled==1; result.Style=style; result.Size=size; result.Speed=speed; return result;
    }
    static bool Read(Dictionary<string,string> values,string key,out int n) { string value; n=0; return values.TryGetValue(key,out value) && int.TryParse(value,out n); }
    public void Save() { AppStorage.WriteAtomic(FilePath,new[]{"version=1","enabled="+(Enabled?1:0),"style="+Style,"size="+Size,"speed="+Speed}); }
}

static class StartupRegistration {
    const string KeyPath=@"Software\Microsoft\Windows\CurrentVersion\Run";
    public const string ValueName="MouseStudio";
    public static string Read(string valueName) { using(var key=Registry.CurrentUser.OpenSubKey(KeyPath)) return key==null?null:key.GetValue(valueName) as string; }
    public static string Command(string executable) {
        if(string.IsNullOrWhiteSpace(executable) || executable.IndexOf('"')>=0 || !Path.IsPathRooted(executable)) throw new ArgumentException("开机启动需要有效的程序绝对路径。");
        string command="\""+executable+"\" --startup";
        if(command.Length>260) throw new ArgumentException("程序路径过长，请将程序放到较短的路径后再开启自启。");
        return command;
    }
    public static void Set(bool enabled,string executable,string valueName) {
        string command=enabled?Command(executable):null;
        using(var key=Registry.CurrentUser.CreateSubKey(KeyPath)) {
            if(enabled) key.SetValue(valueName,command,RegistryValueKind.String); else key.DeleteValue(valueName,false);
        }
    }
}

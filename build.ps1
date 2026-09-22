param([string]$OutputPath = 'MouseStudio.exe', [switch]$RebuildIcon)
$ErrorActionPreference = 'Stop'
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
Push-Location $PSScriptRoot
try {
    if ($RebuildIcon) {
        & $compiler /nologo /target:exe /out:tools\IconBuilder.exe /reference:System.Drawing.dll tools\IconBuilder.cs
        if ($LASTEXITCODE -ne 0) { throw 'Icon builder compilation failed' }
        & .\tools\IconBuilder.exe assets
        if ($LASTEXITCODE -ne 0) { throw 'Icon generation failed' }
    }
    & $compiler /nologo /target:winexe /platform:anycpu /optimize+ /win32manifest:app.manifest /win32icon:assets\mouse-studio.ico /resource:assets\mouse-studio.ico,MouseStudio.AppIcon "/out:$OutputPath" /reference:System.Drawing.dll /reference:System.Windows.Forms.dll MouseStudio.cs CuteArt.cs Preferences.cs ResponsiveLayout.cs BehaviorTests.cs
    if ($LASTEXITCODE -ne 0) { throw 'Build failed' }
} finally { Pop-Location }

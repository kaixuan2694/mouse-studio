param([string]$Target = (Join-Path $PSScriptRoot '..\MouseStudio.exe'))
$ErrorActionPreference = 'Stop'
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$runner = Join-Path $PSScriptRoot 'ExitRegression.exe'
& $compiler /nologo /target:exe "/out:$runner" /reference:System.Drawing.dll /reference:System.Windows.Forms.dll (Join-Path $PSScriptRoot 'ExitRegression.cs')
if ($LASTEXITCODE -ne 0) { throw 'Regression test compilation failed' }
& $runner $Target
if ($LASTEXITCODE -ne 0) { throw 'Post-exit cursor regression failed' }

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 2.0

$ProjectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$SourceRoot = Join-Path $ProjectRoot 'src\NextDeskBootstrapper'
$OutDir = Join-Path $ProjectRoot 'dist'
$OutFile = Join-Path $OutDir 'nextdesk-installer.exe'

$CscCandidates = @(
    (Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'),
    (Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe')
)

$Csc = $null
foreach ($Candidate in $CscCandidates) {
    if (Test-Path -Path $Candidate) {
        $Csc = $Candidate
        break
    }
}

if (-not $Csc) {
    throw 'Could not find .NET Framework csc.exe. Install .NET Framework Developer Pack or Visual Studio Build Tools.'
}

New-Item -ItemType Directory -Force -Path $OutDir | Out-Null

$Sources = @(
    (Join-Path $SourceRoot 'Program.cs'),
    (Join-Path $SourceRoot 'AuthenticodeVerifier.cs'),
    (Join-Path $SourceRoot 'Properties\AssemblyInfo.cs')
)

$IconPath = Join-Path $ProjectRoot 'assets\nextdesk.ico'
if (-not (Test-Path -Path $IconPath)) {
    throw "Icon not found: $IconPath"
}

$Args = @(
    '/nologo',
    '/target:winexe',
    '/platform:x86',
    '/optimize+',
    '/warn:4',
    ('/win32manifest:' + (Join-Path $SourceRoot 'app.manifest')),
    ('/win32icon:' + $IconPath),
    ('/out:' + $OutFile),
    '/reference:System.dll',
    '/reference:System.Core.dll',
    '/reference:System.Drawing.dll',
    '/reference:System.Windows.Forms.dll'
) + $Sources

& $Csc @Args
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

Write-Host "Built: $OutFile"
Get-Item -Path $OutFile | Format-List FullName,Length,LastWriteTime
Get-FileHash -Path $OutFile -Algorithm SHA256 | Format-List Algorithm,Hash,Path
Get-AuthenticodeSignature -FilePath $OutFile | Format-List Status,SignatureType,StatusMessage

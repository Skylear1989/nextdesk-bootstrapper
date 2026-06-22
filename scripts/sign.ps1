param(
    [string] $File,
    [string] $SignToolPath,
    [string] $PfxPath,
    [string] $PfxPassword,
    [string] $CertificateSubject,
    [string] $CertificateSha1,
    [string] $TimestampUrl = 'http://timestamp.sectigo.com'
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 2.0

$ProjectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
if (-not $File) {
    $File = Join-Path $ProjectRoot 'dist\nextdesk-installer.exe'
}

if (-not (Test-Path -Path $File)) {
    throw "File not found: $File"
}

if (-not $SignToolPath) {
    $KitRoot = 'C:\Program Files (x86)\Windows Kits\10\bin'
    if (Test-Path -Path $KitRoot) {
        $SignToolPath = Get-ChildItem -Path $KitRoot -Filter signtool.exe -Recurse -ErrorAction SilentlyContinue |
            Where-Object { $_.FullName -like '*\x64\signtool.exe' } |
            Select-Object -First 1 -ExpandProperty FullName
    }
}

if (-not $SignToolPath -or -not (Test-Path -Path $SignToolPath)) {
    throw 'signtool.exe was not found. Install Windows SDK or pass -SignToolPath.'
}

$SignArgs = @(
    'sign',
    '/fd', 'SHA256',
    '/tr', $TimestampUrl,
    '/td', 'SHA256',
    '/d', 'NextDesk Installer',
    '/du', 'https://next-desk.ru'
)

if ($PfxPath) {
    $SignArgs += @('/f', $PfxPath)
    if ($PfxPassword) {
        $SignArgs += @('/p', $PfxPassword)
    }
}
elseif ($CertificateSha1) {
    $SignArgs += @('/sha1', $CertificateSha1)
}
elseif ($CertificateSubject) {
    $SignArgs += @('/n', $CertificateSubject)
}
else {
    $SignArgs += '/a'
}

$SignArgs += $File

& $SignToolPath @SignArgs
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

& $SignToolPath verify /pa /v $File
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

Get-AuthenticodeSignature -FilePath $File | Format-List Status,SignatureType,SignerCertificate,StatusMessage

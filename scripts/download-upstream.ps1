$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 2.0
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

$ProjectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$DownloadDir = Join-Path $ProjectRoot 'downloads\latest'
New-Item -ItemType Directory -Force -Path $DownloadDir | Out-Null

$Items = @(
    @{
        Arch = 'x64'
        Url = 'https://next-desk.ru/releases/1.2/nextdesk-x64.exe'
        FileName = 'nextdesk-x64.exe'
    },
    @{
        Arch = 'x86'
        Url = 'https://next-desk.ru/releases/1.2/nextdesk-x86.exe'
        FileName = 'nextdesk-x86.exe'
    }
)

$Client = New-Object Net.WebClient
$Client.Headers.Add('User-Agent', 'NextDeskInstallerBuild/1.0 (+https://next-desk.ru)')

foreach ($Item in $Items) {
    $Path = Join-Path $DownloadDir $Item.FileName
    Write-Host "Downloading $($Item.Arch): $($Item.Url)"
    $Client.DownloadFile($Item.Url, $Path)

    $File = Get-Item -Path $Path
    $Hash = Get-FileHash -Path $Path -Algorithm SHA256
    $Signature = Get-AuthenticodeSignature -FilePath $Path

    [PSCustomObject]@{
        Arch = $Item.Arch
        Url = $Item.Url
        Path = $Path
        Length = $File.Length
        Sha256 = $Hash.Hash
        SignatureStatus = $Signature.Status
        SignerThumbprint = if ($Signature.SignerCertificate) { $Signature.SignerCertificate.Thumbprint } else { $null }
        SignerSubject = if ($Signature.SignerCertificate) { $Signature.SignerCertificate.Subject } else { $null }
    } | Format-List *
}

param(
    [string] $SignedInstaller,
    [string] $ExpectedPublisherSubject = 'CN=Bayside Computer Systems Inc, O=Bayside Computer Systems Inc, S=Texas, C=US'
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 2.0

$ProjectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$SourceRoot = Join-Path $ProjectRoot 'src\NextDeskBootstrapper'
$TestRoot = Join-Path $ProjectRoot 'tests'
$OutDir = Join-Path $ProjectRoot 'test-output'
$OutFile = Join-Path $OutDir 'AuthenticodeVerifierTests.exe'

if (-not $SignedInstaller) {
    $SignedInstaller = Join-Path $ProjectRoot 'downloads\latest\nextdesk-x64.exe'
}

if (-not (Test-Path -Path $SignedInstaller)) {
    throw "Signed NextDesk test fixture not found: $SignedInstaller"
}

$CscCandidates = @(
    (Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'),
    (Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe')
)

$Csc = $CscCandidates |
    Where-Object { Test-Path -Path $_ } |
    Select-Object -First 1

if (-not $Csc) {
    throw 'Could not find .NET Framework csc.exe.'
}

New-Item -ItemType Directory -Force -Path $OutDir | Out-Null

$Args = @(
    '/nologo',
    '/target:exe',
    '/platform:x86',
    '/optimize+',
    '/warn:4',
    ('/out:' + $OutFile),
    '/reference:System.dll',
    (Join-Path $SourceRoot 'AuthenticodeVerifier.cs'),
    (Join-Path $TestRoot 'AuthenticodeVerifierTests.cs')
)

& $Csc @Args
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

& $OutFile $SignedInstaller $ExpectedPublisherSubject
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

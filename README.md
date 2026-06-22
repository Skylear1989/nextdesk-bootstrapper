# NextDesk Bootstrapper

NextDesk Bootstrapper is a small open-source Windows launcher for installing
NextDesk from [next-desk.ru](https://next-desk.ru/).

It:

- detects whether Windows is 64-bit or 32-bit;
- downloads a vetted NextDesk 1.4.5 installer over HTTPS;
- verifies its pinned SHA-256 value;
- validates the executable with Windows Authenticode policy;
- requires the exact trusted publisher `Bayside Computer Systems Inc`;
- requests elevation only when launching the verified installer;
- starts NextDesk with `--silent-install`.

The bootstrapper is compiled as a 32-bit GUI executable so the same file runs
on both 32-bit and 64-bit Windows. It intentionally installs one immutable,
vetted release. Updates after initial installation are the responsibility of
the installed NextDesk application, allowing the bootstrapper to remain
byte-for-byte stable.

## Security model

The bootstrapper itself runs with the current user's permissions. It downloads
only from immutable HTTPS URLs under `next-desk.ru`, verifies pinned SHA-256
values, then calls Windows `WinVerifyTrust` and checks the signing-certificate
subject before requesting administrator elevation for the installer.

Changing the initial installer, download server, trusted publisher, or
bootstrapper behavior requires a source change and a newly signed bootstrapper
release.

The current Windows files on `next-desk.ru` are NextDesk 1.4.5 custom RustDesk
builds generated through the open-source
[RDGen project](https://github.com/bryangerlach/rdgen). They are independently
signed by `Bayside Computer Systems Inc`.

## Build

No .NET SDK is required. The build uses the Windows .NET Framework compiler:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\build.ps1
```

Output:

```text
dist\nextdesk-installer.exe
```

## Test

Download the current signed NextDesk files, then run the Authenticode tests:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\download-upstream.ps1
powershell -ExecutionPolicy Bypass -File .\scripts\test.ps1
```

The test accepts the expected publisher and rejects both a different publisher
and a modified executable.

## Code signing policy

See [CODE_SIGNING_POLICY.md](CODE_SIGNING_POLICY.md).

For releases accepted into the SignPath Foundation program: Free code signing
provided by [SignPath.io](https://about.signpath.io/), certificate by
[SignPath Foundation](https://signpath.org/).

Unsigned CI artifacts are build outputs only and must not be redistributed as
customer releases.

## Privacy

See [PRIVACY.md](PRIVACY.md).

## Uninstallation

The bootstrapper does not install itself. NextDesk can be removed through
Windows Settings under **Apps > Installed apps > NextDesk > Uninstall**.

## License

The bootstrapper source, build scripts, and included branding assets are
licensed under the [MIT License](LICENSE). The downloaded NextDesk application
is a separate artifact and is not included in this repository.

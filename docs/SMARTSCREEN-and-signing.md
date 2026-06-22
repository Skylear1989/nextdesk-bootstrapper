# SmartScreen and signing notes

## Findings

The supplied bootstrapper at
`forensics\original\nextdesk-installer.unsigned.exe` is unsigned and has no
useful version metadata:

- Authenticode status: `NotSigned`
- SHA-256:
  `D1C78EBF6B6BB6462D66AD6677E1CEDC0C64C399AB778DB9888C2D48F0901D39`
- PE architecture: 32-bit I386
- Toolchain traces: MinGW/GCC
- Embedded download URLs:
  - `https://next-desk.ru/releases/latest/nextdesk-x64.exe`
  - `https://next-desk.ru/releases/latest/nextdesk-x86.exe`
- Installer arguments found in strings: `--silent-install`

## Practical conclusion

There is no reliable technical switch inside an unsigned EXE that makes Windows
SmartScreen stop warning customers. SmartScreen checks whether a downloaded app
or installer is known unsafe, or whether it is well known and downloaded
frequently enough to have reputation. A newly built unsigned EXE usually has no
reputation.

The correct production path is:

1. Build a clean bootstrapper with stable metadata, a stable icon, and a stable
   file name.
2. Sign it through the SignPath Foundation open-source program or with an OV or
   EV code signing certificate.
3. Timestamp the signature.
4. Keep using the same publisher certificate for future versions where possible.
5. If Microsoft Defender or SmartScreen still blocks a clean signed file,
   submit it through Microsoft Security Intelligence as a software developer.

Replacing the bootstrapper with `.bat`, `.cmd`, `.ps1`, or an unsigned repacked
EXE can reduce engineering work, but it usually makes customer trust and browser
security prompts worse.

Self-signed Authenticode certificates are not a public-download workaround. They
can be useful for development or managed internal machines where the certificate
is deployed to trusted stores, but public Windows clients and browsers will not
treat them as a trusted publisher.

The bootstrapper installs one immutable NextDesk release, verifies its pinned
SHA-256 value, validates the separate Authenticode signature with Windows
`WinVerifyTrust`, and requires the expected `Bayside Computer Systems Inc`
publisher. Updates after initial installation belong in NextDesk itself. This
keeps the signed bootstrapper byte-for-byte stable.

The no-cost production fallback is to avoid distributing a new bootstrapper and
link customers directly to the already signed application files:

- `https://next-desk.ru/releases/latest/nextdesk-x64.exe`
- `https://next-desk.ru/releases/latest/nextdesk-x86.exe`

## Microsoft references

- Microsoft Defender SmartScreen overview:
  https://learn.microsoft.com/en-us/windows/security/operating-system-security/virus-and-threat-protection/microsoft-defender-smartscreen/
- Microsoft Security Intelligence file submission:
  https://www.microsoft.com/wdsi/filesubmission
- SignTool documentation:
  https://learn.microsoft.com/en-us/windows/win32/seccrypto/signtool

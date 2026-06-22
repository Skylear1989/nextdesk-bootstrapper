# Code signing policy

## Scope

Only `dist/nextdesk-installer.exe` built from this repository is eligible for
project code signing. The downloaded NextDesk application is a separate
artifact and is never signed with this project's certificate.

## Provider

For releases accepted into the SignPath Foundation program:

Free code signing provided by [SignPath.io](https://about.signpath.io/),
certificate by [SignPath Foundation](https://signpath.org/).

## Roles

- Committers and reviewers: [Skylear1989](https://github.com/Skylear1989)
- Approvers: [Skylear1989](https://github.com/Skylear1989)

Changes from other contributors must be submitted through pull requests and
reviewed before merge. Every signing request requires manual approval by an
approver.

## Release process

1. GitHub Actions builds the executable from a tagged source commit.
2. The Authenticode verifier test checks a valid installer, a publisher
   mismatch, and a tampered installer.
3. An approver reviews the source commit and build result.
4. The unsigned artifact is submitted to SignPath for signing and timestamping.
5. The signed artifact and its SHA-256 value are published as the release.

See [PRIVACY.md](PRIVACY.md) for the bootstrapper privacy policy.

## Downloaded application

The bootstrapper downloads a fixed, vetted NextDesk 1.4.5 Windows build from
`next-desk.ru`, verifies its SHA-256 value and Authenticode publisher, and then
starts installation. Those builds are generated from the open-source
[RDGen](https://github.com/bryangerlach/rdgen) and RustDesk projects and are
independently Authenticode-signed by `Bayside Computer Systems Inc`.

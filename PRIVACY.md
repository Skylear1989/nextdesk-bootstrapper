# Privacy policy

The NextDesk Bootstrapper does not collect telemetry, identifiers, account
data, or usage analytics.

When the user starts the bootstrapper, it makes one HTTPS request to
`next-desk.ru` to download a vetted Windows installer for the detected
operating-system architecture. Standard network metadata, including the
client IP address and the `NextDeskInstaller` user-agent string, may be visible
to the web server as part of that request.

The downloaded installer is verified using a pinned SHA-256 value, Windows
Authenticode policy, and an expected publisher identity before it is allowed to
run. The bootstrapper does not send the downloaded file, its signature, or
local system data to any other service.

The installed NextDesk application is a separate program. Its remote-access
features and network behavior are outside the scope of this bootstrapper.

The bootstrapper stores no personal data. Temporary installer files are placed
under the current user's Windows temporary directory.

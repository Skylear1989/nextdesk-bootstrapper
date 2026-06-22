using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;

namespace NextDeskBootstrapper
{
    internal static class AuthenticodeVerifier
    {
        private static readonly Guid GenericVerifyV2 =
            new Guid("00AAC56B-CD44-11D0-8CC2-00C04FC295EE");

        private const uint WtdUiNone = 2;
        private const uint WtdRevokeWholeChain = 1;
        private const uint WtdChoiceFile = 1;
        private const uint WtdStateActionIgnore = 0;
        private const uint WtdRevocationCheckChainExcludeRoot = 0x00000080;

        public static void VerifyTrustedPublisher(string filePath, string expectedPublisherSubject)
        {
            int trustResult;
            using (WinTrustFileInfo fileInfo = new WinTrustFileInfo(filePath))
            using (WinTrustData trustData = new WinTrustData(fileInfo))
            {
                trustResult = WinVerifyTrust(new IntPtr(-1), GenericVerifyV2, trustData);
            }

            if (trustResult != 0)
            {
                throw new ApplicationException(
                    "The downloaded installer does not have a valid trusted Authenticode signature " +
                    "(WinVerifyTrust 0x" + ((uint)trustResult).ToString("X8") + ").");
            }

            X509Certificate2 signer;
            try
            {
                using (X509Certificate certificate = X509Certificate.CreateFromSignedFile(filePath))
                {
                    signer = new X509Certificate2(certificate);
                }
            }
            catch (Exception ex)
            {
                throw new ApplicationException(
                    "Could not read the downloaded installer's signing certificate.",
                    ex);
            }

            using (signer)
            {
                if (!String.Equals(
                    signer.Subject,
                    expectedPublisherSubject,
                    StringComparison.OrdinalIgnoreCase))
                {
                    throw new ApplicationException(
                        "Downloaded installer publisher mismatch. Expected '" +
                        expectedPublisherSubject +
                        "', got '" +
                        signer.Subject +
                        "'.");
                }
            }
        }

        [DllImport("wintrust.dll", ExactSpelling = true, PreserveSig = true, SetLastError = true)]
        private static extern int WinVerifyTrust(
            IntPtr windowHandle,
            [MarshalAs(UnmanagedType.LPStruct)] Guid actionId,
            WinTrustData trustData);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private sealed class WinTrustFileInfo : IDisposable
        {
            public uint StructSize;
            public IntPtr FilePath;
            public IntPtr FileHandle;
            public IntPtr KnownSubject;

            public WinTrustFileInfo(string filePath)
            {
                StructSize = (uint)Marshal.SizeOf(typeof(WinTrustFileInfo));
                FilePath = Marshal.StringToCoTaskMemUni(filePath);
                FileHandle = IntPtr.Zero;
                KnownSubject = IntPtr.Zero;
            }

            public void Dispose()
            {
                if (FilePath != IntPtr.Zero)
                {
                    Marshal.FreeCoTaskMem(FilePath);
                    FilePath = IntPtr.Zero;
                }
            }
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private sealed class WinTrustData : IDisposable
        {
            public uint StructSize;
            public IntPtr PolicyCallbackData;
            public IntPtr SipClientData;
            public uint UiChoice;
            public uint RevocationChecks;
            public uint UnionChoice;
            public IntPtr FileInfo;
            public uint StateAction;
            public IntPtr StateData;
            public IntPtr UrlReference;
            public uint ProviderFlags;
            public uint UiContext;

            public WinTrustData(WinTrustFileInfo fileInfo)
            {
                StructSize = (uint)Marshal.SizeOf(typeof(WinTrustData));
                PolicyCallbackData = IntPtr.Zero;
                SipClientData = IntPtr.Zero;
                UiChoice = WtdUiNone;
                RevocationChecks = WtdRevokeWholeChain;
                UnionChoice = WtdChoiceFile;
                FileInfo = Marshal.AllocCoTaskMem(Marshal.SizeOf(typeof(WinTrustFileInfo)));
                Marshal.StructureToPtr(fileInfo, FileInfo, false);
                StateAction = WtdStateActionIgnore;
                StateData = IntPtr.Zero;
                UrlReference = IntPtr.Zero;
                ProviderFlags = WtdRevocationCheckChainExcludeRoot;
                UiContext = 0;
            }

            public void Dispose()
            {
                if (FileInfo != IntPtr.Zero)
                {
                    Marshal.FreeCoTaskMem(FileInfo);
                    FileInfo = IntPtr.Zero;
                }
            }
        }
    }
}

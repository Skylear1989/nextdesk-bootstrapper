using System;
using System.IO;

namespace NextDeskBootstrapper
{
    internal static class AuthenticodeVerifierTests
    {
        private static int Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.Error.WriteLine(
                    "Usage: AuthenticodeVerifierTests.exe <signed-exe> <expected-publisher-subject>");
                return 2;
            }

            string signedFile = Path.GetFullPath(args[0]);
            string expectedPublisherSubject = args[1];
            AuthenticodeVerifier.VerifyTrustedPublisher(
                signedFile,
                expectedPublisherSubject);

            ExpectFailure(
                delegate
                {
                    AuthenticodeVerifier.VerifyTrustedPublisher(
                        signedFile,
                        "CN=Unexpected Publisher");
                },
                "publisher mismatch");

            string tamperedFile = Path.Combine(
                Path.GetTempPath(),
                "NextDeskBootstrapperTests-" + Guid.NewGuid().ToString("N") + ".exe");

            try
            {
                File.Copy(signedFile, tamperedFile, true);
                TamperWithFile(tamperedFile);

                ExpectFailure(
                    delegate
                    {
                        AuthenticodeVerifier.VerifyTrustedPublisher(
                            tamperedFile,
                            expectedPublisherSubject);
                    },
                    "invalid Authenticode signature");
            }
            finally
            {
                if (File.Exists(tamperedFile))
                {
                    File.Delete(tamperedFile);
                }
            }

            Console.WriteLine("Authenticode verifier tests passed.");
            return 0;
        }

        private static void TamperWithFile(string path)
        {
            using (FileStream stream = new FileStream(
                path,
                FileMode.Open,
                FileAccess.ReadWrite,
                FileShare.None))
            {
                if (stream.Length <= 1024)
                {
                    throw new InvalidOperationException("Test fixture is unexpectedly small.");
                }

                stream.Position = 1024;
                int original = stream.ReadByte();
                stream.Position = 1024;
                stream.WriteByte((byte)(original ^ 0x01));
            }
        }

        private static void ExpectFailure(Action action, string scenario)
        {
            try
            {
                action();
            }
            catch (ApplicationException)
            {
                return;
            }

            throw new InvalidOperationException(
                "Expected verification failure for " + scenario + ".");
        }
    }
}

using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Windows.Forms;

namespace NextDeskBootstrapper
{
    internal static class Program
    {
        private const string ProductName = "NextDesk";
        private const string InstallArguments = "--silent-install";
        private const string ExpectedPublisherSubject =
            "CN=Bayside Computer Systems Inc, O=Bayside Computer Systems Inc, S=Texas, C=US";

        private const string X64Url = "https://next-desk.ru/releases/1.2/nextdesk-x64.exe";
        private const string X64FileName = "nextdesk-x64.exe";
        private const string X64Sha256 = "8EDDD9EB083126506EE1864EA15FAF96D40471E01B8D4496640A7E61EFA37EAB";

        private const string X86Url = "https://next-desk.ru/releases/1.2/nextdesk-x86.exe";
        private const string X86FileName = "nextdesk-x86.exe";
        private const string X86Sha256 = "4D9784B614039FE61C96617F1FD8F3E7D9282A3548B150622AF410087B36E41B";

        [STAThread]
        private static int Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            try
            {
                ReleaseInfo release = SelectRelease();
                string installerPath = Path.Combine(
                    Path.GetTempPath(),
                    "NextDesk",
                    release.FileName);

                using (InstallForm form = new InstallForm())
                {
                    form.Show();
                    Application.DoEvents();

                    Directory.CreateDirectory(Path.GetDirectoryName(installerPath));

                    form.SetStatus("Downloading " + ProductName + "...");
                    DownloadFile(release.Url, installerPath, form);

                    form.SetStatus("Verifying download...");
                    VerifySha256(installerPath, release.Sha256);

                    form.SetStatus("Verifying publisher...");
                    AuthenticodeVerifier.VerifyTrustedPublisher(
                        installerPath,
                        ExpectedPublisherSubject);

                    form.SetStatus("Starting installer...");
                    int exitCode = RunInstaller(installerPath);
                    if (exitCode != 0)
                    {
                        throw new ApplicationException(
                            ProductName + " installer exited with code " + exitCode + ".");
                    }

                    form.SetProgress(100);
                    form.SetStatus("Installation completed.");
                    System.Threading.Thread.Sleep(700);
                }

                return 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    ProductName + " installer",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return 1;
            }
        }

        private static ReleaseInfo SelectRelease()
        {
            if (Environment.Is64BitOperatingSystem)
            {
                return new ReleaseInfo(X64Url, X64FileName, X64Sha256);
            }

            return new ReleaseInfo(X86Url, X86FileName, X86Sha256);
        }

        private static void DownloadFile(string url, string destinationPath, InstallForm form)
        {
            Uri uri = new Uri(url);
            if (!String.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            {
                throw new ApplicationException("Download URL must use HTTPS.");
            }

            string tempPath = destinationPath + ".download";
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
            request.UserAgent = "NextDeskInstaller/1.0 (+https://next-desk.ru)";
            request.AllowAutoRedirect = true;
            request.Timeout = 30000;
            request.ReadWriteTimeout = 30000;

            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            {
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    throw new ApplicationException("Download failed: HTTP " + (int)response.StatusCode + ".");
                }

                long totalBytes = response.ContentLength;
                long downloadedBytes = 0;
                byte[] buffer = new byte[64 * 1024];

                using (Stream input = response.GetResponseStream())
                using (FileStream output = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    int read;
                    while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        output.Write(buffer, 0, read);
                        downloadedBytes += read;

                        if (totalBytes > 0)
                        {
                            int percent = (int)Math.Min(100, (downloadedBytes * 100L) / totalBytes);
                            form.SetProgress(percent);
                        }

                        Application.DoEvents();
                    }
                }
            }

            if (File.Exists(destinationPath))
            {
                File.Delete(destinationPath);
            }

            File.Move(tempPath, destinationPath);
        }

        private static void VerifySha256(string path, string expectedSha256)
        {
            string actualSha256;
            using (FileStream input = File.OpenRead(path))
            using (SHA256 sha256 = SHA256.Create())
            {
                actualSha256 = BitConverter.ToString(sha256.ComputeHash(input)).Replace("-", "");
            }

            if (!String.Equals(actualSha256, expectedSha256, StringComparison.OrdinalIgnoreCase))
            {
                throw new ApplicationException(
                    "Downloaded installer hash mismatch. Expected " +
                    expectedSha256 +
                    ", got " +
                    actualSha256 +
                    ".");
            }
        }

        private static int RunInstaller(string installerPath)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = installerPath;
            startInfo.Arguments = InstallArguments;
            startInfo.WorkingDirectory = Path.GetDirectoryName(installerPath);
            startInfo.UseShellExecute = true;
            startInfo.Verb = "runas";

            using (Process process = Process.Start(startInfo))
            {
                if (process == null)
                {
                    throw new ApplicationException("Failed to start " + ProductName + " installer.");
                }

                process.WaitForExit();
                return process.ExitCode;
            }
        }

        private sealed class ReleaseInfo
        {
            public readonly string Url;
            public readonly string FileName;
            public readonly string Sha256;

            public ReleaseInfo(string url, string fileName, string sha256)
            {
                Url = url;
                FileName = fileName;
                Sha256 = sha256;
            }
        }

        private sealed class InstallForm : Form
        {
            private readonly Label statusLabel;
            private readonly ProgressBar progressBar;

            public InstallForm()
            {
                Text = ProductName + " installer";
                StartPosition = FormStartPosition.CenterScreen;
                FormBorderStyle = FormBorderStyle.FixedDialog;
                MaximizeBox = false;
                MinimizeBox = false;
                ControlBox = false;
                ClientSize = new Size(420, 110);
                Font = new Font("Segoe UI", 9F);

                statusLabel = new Label();
                statusLabel.AutoSize = false;
                statusLabel.Location = new Point(16, 16);
                statusLabel.Size = new Size(388, 24);
                statusLabel.Text = "Preparing...";

                progressBar = new ProgressBar();
                progressBar.Location = new Point(16, 54);
                progressBar.Size = new Size(388, 22);
                progressBar.Minimum = 0;
                progressBar.Maximum = 100;
                progressBar.Style = ProgressBarStyle.Continuous;

                Controls.Add(statusLabel);
                Controls.Add(progressBar);
            }

            public void SetStatus(string text)
            {
                statusLabel.Text = text;
                statusLabel.Refresh();
            }

            public void SetProgress(int value)
            {
                if (value < progressBar.Minimum)
                {
                    value = progressBar.Minimum;
                }

                if (value > progressBar.Maximum)
                {
                    value = progressBar.Maximum;
                }

                progressBar.Value = value;
                progressBar.Refresh();
            }
        }
    }
}

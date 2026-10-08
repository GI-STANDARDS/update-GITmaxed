using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace GITmaxedUpdate
{
    internal static class Program
    {
        internal const int WinW = 732;
        internal const int WinH = 335;

        [STAThread]
        private static int Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string iniFile = Path.Combine(baseDir, "update.ini");

            string currentTitle = "GITmaxed Patch V3.6.5";
            string apiUrl = "https://api.github.com/repos/GI-STANDARDS/GITmaxed/releases/latest";
            string releasesUrl = "https://github.com/GI-STANDARDS/GITmaxed/releases/latest";
            string gitmaxedExe = Path.Combine(baseDir, "GITmaxed.exe");
            string userAgent = "GITmaxed-Version-Checker";

            var ini = Ini.Read(iniFile);
            if (ini != null)
            {
                string s;
                if (ini.TryGetValue("CURRENT_TITLE", out s) && !string.IsNullOrWhiteSpace(s)) currentTitle = s.Trim();
                if (ini.TryGetValue("API_URL", out s) && !string.IsNullOrWhiteSpace(s)) apiUrl = s.Trim();
                if (ini.TryGetValue("RELEASES_URL", out s) && !string.IsNullOrWhiteSpace(s)) releasesUrl = s.Trim();
                if (ini.TryGetValue("GITMAXED_EXE", out s) && !string.IsNullOrWhiteSpace(s)) gitmaxedExe = s.Trim();
                if (ini.TryGetValue("USER_AGENT", out s) && !string.IsNullOrWhiteSpace(s)) userAgent = s.Trim();
            }

            try
            {
                string json = FetchJson(apiUrl, userAgent);
                var ser = new JavaScriptSerializer();
                var rel = ser.Deserialize<Dictionary<string, object>>(json);

                string releaseName = GetStr(rel, "name");
                string tagName = GetStr(rel, "tag_name");
                string htmlUrl = GetStr(rel, "html_url");
                string publishedAt = GetStr(rel, "published_at");

                DateTime pub;
                string dateText = publishedAt;
                if (DateTime.TryParse(publishedAt, CultureInfo.InvariantCulture, DateTimeStyles.AllowInnerWhite | DateTimeStyles.AdjustToUniversal, out pub))
                    dateText = pub.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");

                bool upToDate = string.Equals(releaseName, currentTitle, StringComparison.Ordinal);
                if (upToDate)
                {
                    LaunchGitmaxedSilently(gitmaxedExe);
                    return 0;
                }

                Application.Run(new UpdateForm(currentTitle, releaseName, tagName, htmlUrl, dateText, releasesUrl));
                return 1;
            }
            catch (Exception ex)
            {
                Application.Run(new ErrorForm(ex.Message));
                return 2;
            }
        }

        private static void LaunchGitmaxedSilently(string exe)
        {
            if (string.IsNullOrWhiteSpace(exe) || !File.Exists(exe))
                throw new FileNotFoundException("GITMAXED_EXE not found: " + exe, exe);
            Process.Start(exe);
        }

        private static string FetchJson(string url, string userAgent)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            var req = (HttpWebRequest)WebRequest.Create(url);
            req.UserAgent = userAgent;
            req.Accept = "application/vnd.github+json";
            req.Timeout = 15000;
            req.ReadWriteTimeout = 15000;
            using (var resp = (HttpWebResponse)req.GetResponse())
            using (var stream = resp.GetResponseStream())
            using (var reader = new StreamReader(stream, Encoding.UTF8))
                return reader.ReadToEnd();
        }

        private static string GetStr(Dictionary<string, object> d, string key)
        {
            object v;
            return (d != null && d.TryGetValue(key, out v) && v != null) ? Convert.ToString(v, CultureInfo.InvariantCulture) : string.Empty;
        }

        internal static class Ini
        {
            public static Dictionary<string, string> Read(string path)
            {
                var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                if (!File.Exists(path))
                    return dict;
                foreach (string raw in File.ReadAllLines(path))
                {
                    string line = raw.Trim();
                    if (line.Length == 0 || line[0] == ';' || line[0] == '#')
                        continue;
                    int idx = line.IndexOf('=');
                    if (idx <= 0)
                        continue;
                    string key = line.Substring(0, idx).Trim();
                    string val = line.Substring(idx + 1).Trim();
                    if (key.Length > 0)
                        dict[key] = val;
                }
                return dict;
            }
        }
    }

    internal sealed class UpdateForm : Form
    {
        public UpdateForm(string current, string releaseName, string tagName, string htmlUrl, string dateText, string releasesUrl)
        {
            Text = "GITmaxed - Update Available";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(Program.WinW, Program.WinH);
            Font = new Font("Segoe UI", 9f);
            BackColor = Color.White;

            var header = new Label
            {
                Text = "GITmaxed - Update Available",
                Left = 24, Top = 18, Width = Program.WinW - 48, Height = 32,
                Font = new Font("Segoe UI", 14f, FontStyle.Bold),
                ForeColor = Color.FromArgb(20, 90, 50)
            };

            var lblCurrent = Row(66, "Current version : " + current);
            var lblName = Row(96, "Latest release : " + releaseName);
            var lblTag = Row(126, "Release tag    : " + tagName);
            var lblDate = Row(156, "Release date   : " + dateText);
            var lblUrl = Row(186, "Release URL    : " + htmlUrl);

            var btnUpdate = new Button
            {
                Text = "Update Available",
                Left = 24, Top = 240, Width = 300, Height = 48,
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 0 },
                BackColor = Color.FromArgb(34, 160, 70),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnUpdate.FlatAppearance.MouseOverBackColor = Color.FromArgb(28, 140, 60);
            btnUpdate.FlatAppearance.MouseDownBackColor = Color.FromArgb(22, 120, 50);
            btnUpdate.Click += delegate
            {
                try { Process.Start(releasesUrl); }
                catch { /* ignore */ }
            };

            Controls.Add(header);
            Controls.Add(lblCurrent);
            Controls.Add(lblName);
            Controls.Add(lblTag);
            Controls.Add(lblDate);
            Controls.Add(lblUrl);
            Controls.Add(btnUpdate);
        }

        private static Label Row(int top, string text)
        {
            return new Label
            {
                Text = text,
                Left = 24, Top = top, Width = Program.WinW - 48, Height = 24,
                AutoEllipsis = true
            };
        }
    }

    internal sealed class ErrorForm : Form
    {
        public ErrorForm(string message)
        {
            Text = "GITmaxed - Version Check Error";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(Program.WinW, Program.WinH);
            Font = new Font("Segoe UI", 9f);
            BackColor = Color.White;

            Controls.Add(new Label
            {
                Text = "STATUS: ERROR",
                Left = 24, Top = 20, Width = Program.WinW - 48, Height = 28,
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Color.DarkRed
            });

            Controls.Add(new Label
            {
                Text = "Unable to contact GitHub.\r\n" + message,
                Left = 24, Top = 62, Width = Program.WinW - 48, Height = 190
            });
        }
    }
}
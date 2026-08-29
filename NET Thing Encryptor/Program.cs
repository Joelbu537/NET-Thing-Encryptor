using System.Diagnostics;
using System.Reflection;
namespace NET_Thing_Encryptor
{
    public static class Program
    {
        private static ThingRoot? Root = ThingData.Root;
        public static Version Version { get; } = GetApplicationVersion();
        public static Color DarkColor = Color.FromArgb(unchecked((int)0xFF1E1E1E));
        public static Color ButtonBorder = Color.FromArgb(unchecked((int)0xFF646464));
        public static object Objective = new string("\x4D\x61\x64\x65\x20\x62\x79\x20\x4A\x6F\x65\x6C\x62\x75");

        private static Version GetApplicationVersion()
        {
            string? informationalVersion = typeof(Program).Assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                .InformationalVersion
                .Split('+', 2)[0];

            return Version.TryParse(informationalVersion, out Version? version)
                ? version
                : typeof(Program).Assembly.GetName().Version ?? new Version(0, 0, 0);
        }
            

        [STAThread]
        private static void Main()
        {
#if DEBUG
            Debug.WriteLine("Running in DEBUG mode.");
#else
            using Mutex mutex = new Mutex(true, "NET Thing Encryptor", out bool isAlone);
            if (!isAlone)
            {
                MessageBox.Show("There already is another instance of NET Thing Encryptor running!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                Process.GetCurrentProcess().Kill();
                return;
            }
#endif
            ApplicationConfiguration.Initialize();
            Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
            using WinFormsVaultNotificationAdapter notificationAdapter =
                WinFormsVaultNotificationAdapter.Subscribe();
            try
            {
                if (ThingData.LoadMainData().Result)
                {
                    using var pw = new PasswordForm();
                    if (pw.ShowDialog() == DialogResult.OK)
                    {
                        ThingData.SaveRootAsync().Wait();
                        Application.Run(new FormMain());
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"The Program was forced to stop." +
                    "\n\n" +
                    $"Exception: {ex.GetType().FullName} : {ex.Message}\n" +
                    $"StackTrace: {ex.StackTrace}",
                    "Critical Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Application.Exit();
            }
        }
    }
}
/*          /// TO DO \\\
 *      ListView multiselect amchen, um mehrere Items gleichzeitig exportieren und löschen zu können
 *      Dateien verschiebbar machen
 *      Autopsperre nach 5 Minuten inaktivität (Oder Variabler Zeit in Settings)
 *      Dateien automatisch in AppData speichern und nicht im Programmverzeichniss. (Auf C:\ führt das zu Access Violation Exception)
 *      Gifs abspielen lassen
 */

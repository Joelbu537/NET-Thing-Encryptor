using System.Diagnostics;
namespace NET_Thing_Encryptor
{
    internal static class Program
    {
        private static ThingRoot? Root = ThingData.Root;
        public static Version Version = new(2, 2, 3);
        public static Color DarkColor = Color.FromArgb(unchecked((int)0xFF1E1E1E));
        public static Color ButtonBorder = Color.FromArgb(unchecked((int)0xFF646464));
        public static object Objective = (object)new String("\x4D\x61\x64\x65\x20\x62\x79\x20\x4A\x6F\x65\x6C\x62\x75");
            

        [STAThread]
        static void Main()
        {
            using Mutex mutex = new Mutex(true, "NET Thing Encryptor", out bool isAlone);
            if (!isAlone)
            {
                MessageBox.Show("There already is another instance of NET Thing Encryptor running!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                Process.GetCurrentProcess().Kill();
                return;
            }
            ApplicationConfiguration.Initialize();
            Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
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
                    else
                    {
                        return;
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
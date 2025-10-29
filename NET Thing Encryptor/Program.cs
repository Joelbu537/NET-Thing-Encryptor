using System.Diagnostics;
namespace NET_Thing_Encryptor
{
    internal static class Program
    {
        private static ThingRoot? Root = ThingData.Root;

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
                MessageBox.Show($"The Program was forced to stop.\n" +
                    $"Try restarting the program, your pc, deleting Application Data and reinstalling the application.\n\n" +
                    $"Exception: {ex.GetType().FullName} : {ex.Message}\n" +
                    $"StackTrace: {ex.StackTrace}",
                    "Critical Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Application.Exit();
            }

        }
    }
}
/*          /// TO DO \\\
 *      ListView multiselect amchen, um mehrere Items gleichzeitig exportieren und l�schen zu k�nnen
 *      Dateien verschiebbar machen
 *      Autopsperre nach 5 Minuten inaktivit�t (Oder Variabler Zeit in Settings)
 *      Dateien automatisch in AppData speichern und nicht im Programmverzeichniss. (Auf C:\ f�hrt das zu Access Violation Exception)
 *      Gifs abspielen lassen
 */
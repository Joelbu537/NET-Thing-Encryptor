using System.Diagnostics;
namespace NET_Thing_Encryptor
{
    internal static class Program
    {
        private static ThingRoot? Root = ThingData.Root;
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
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
namespace NET_Thing_Encryptor
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static async Task Main()
        {
            ApplicationConfiguration.Initialize();
            try
            {
                if (await ThingData.LoadMainData())
                {
                    using (var pw = new PasswordForm())
                    {
                        if (pw.ShowDialog() == DialogResult.OK)
                        {

                            foreach(ulong ID in ThingData.Root.Content)
                            {
                                
                            }

                            MessageBox.Show("Test completed successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            Application.Run(new FormMain());
                        }
                        else
                        {
                            return;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}\n\n The Program was forced to stop.\n" +
                    $"Try restarting the program, your pc, deleting Application Data and reinstalling the application.\n\n" +
                    $"Exception Type: {ex.GetType().FullName}",
                    "Critical Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Application.Exit();
            }

        }
    }
}
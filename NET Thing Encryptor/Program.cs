namespace NET_Thing_Encryptor
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();
            using (var pw = new PasswordForm())
            {
                if (pw.ShowDialog() == DialogResult.OK)
                {
                    Application.Run(new FormMain());
                }
                else
                {
                    return;
                }
            }
        }
    }
}
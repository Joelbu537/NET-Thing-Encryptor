using System.Text;

namespace JoelbuInstaller
{
    public static class Program
    {
        public static Version Version = new(0, 0, 1);

        [STAThread]
        private static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                CheckPrivilege(args[0]);
                return;
            }

            RunInstaller();
        }

        private static void RunInstaller()
        {
            ApplicationConfiguration.Initialize();
            Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
            Application.Run(new FormMain());
        }

        private static void CheckPrivilege(string path)
        {
            bool isWorking = true;
            try
            {
                using FileStream fs = File.Create(path);
                fs.Write("Hello, World!"u8);
                fs.Flush();
                File.Delete(path);
            }
            catch
            {
                isWorking = false;
            }
            finally
            {

            }
        }
    }
}
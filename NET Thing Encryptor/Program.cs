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
                            ThingFolder? foldertest = new ThingFolder("TestFolder").AddToRoot();
                            await ThingData.SaveFileAsync(foldertest);
                            ThingFile? file = new ThingFile("TestFile", new byte[] { 0, 1, 2, 3, 4 });
                            await ThingData.SaveFileAsync(file);
                            await ThingData.MoveFileToFolderAsync(file, foldertest.ID);
                            for(int i = 0; i < Root.Content.Count; i++)
                            {
                                ThingObject? obj = await ThingData.LoadFileAsync(Root.Content[i].ID);
                                if(obj is ThingFile)
                                {
                                    Debug.WriteLine($"File: {file.Name}, MD5: {file.MD5Hash}, Size: {file.Content.Length} bytes");
                                }
                                else if(obj is ThingFolder folder)
                                {
                                    Debug.WriteLine($"Folder: {folder.Name}, Contains: {folder.Content.Count} items");
                                    foreach(ThingObjectLink item in folder.Content)
                                    {
                                        Debug.WriteLine("Item:" + item.Name);
                                    }
                                }
                            }

                            await ThingData.SaveRootAsync();
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
                    $"Exception Type: {ex.GetType().FullName}\n\n" +
                    $"Stack trace: {ex.StackTrace}",
                    "Critical Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Application.Exit();
            }

        }
    }
}
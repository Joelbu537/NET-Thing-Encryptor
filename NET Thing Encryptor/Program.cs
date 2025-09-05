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
                            /*ThingFolder? foldertest = new ThingFolder("TestFolder").AddToRoot();
                            await ThingData.SaveFileAsync(foldertest);
                            ThingFile? file = new ThingFile("TestFile", new byte[] { 0, 1, 2, 3, 4 });
                            await ThingData.MoveFileToFolderAsync(file, foldertest.ID);
                            await ThingData.SaveFileAsync(file);
                            ThingFolder? subfolder = new ThingFolder("SubFolder").AddToRoot();
                            await ThingData.SaveFileAsync(subfolder);
                            await ThingData.MoveFolderToFolderAsync(subfolder.ID, foldertest.ID);
                            ThingFile? subfile = new ThingFile("SubFile", new byte[] { 5, 6, 7, 8, 9 });
                            await ThingData.MoveFileToFolderAsync(subfile, subfolder.ID);
                            await ThingData.SaveFileAsync(subfile);
                            for (int i = 0; i < Root.Content.Count; i++)
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
                                        ThingObject? subobj = await ThingData.LoadFileAsync(Root.Content[i].ID);
                                        if (subobj is ThingFile subfile2)
                                        {
                                            Debug.WriteLine($"File: {subfile2.Name}, MD5: {subfile2.MD5Hash}, Size: {subfile2.Content.Length} bytes");
                                        }
                                    }
                                }
                            }

                            await ThingData.SaveRootAsync();
                            MessageBox.Show("Test completed successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);*/
                            await ThingData.SaveRootAsync();
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
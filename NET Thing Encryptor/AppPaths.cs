namespace NET_Thing_Encryptor;

public static class AppPaths
{
    private const string AppDirectoryName = "NET Thing Encryptor";

    public static string UserDataDirectory
    {
        get
        {
            string baseDirectory = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (string.IsNullOrWhiteSpace(baseDirectory))
                baseDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            if (string.IsNullOrWhiteSpace(baseDirectory))
                baseDirectory = Path.GetTempPath();

            return Path.Combine(baseDirectory, AppDirectoryName);
        }
    }

    public static string DataDirectory => Path.Combine(UserDataDirectory, "Data");

    public static string RootFilePath => Path.Combine(DataDirectory, "0.nte");

    public static IReadOnlyList<string> LegacyDataDirectories
    {
        get
        {
            string[] candidates =
            [
                Path.Combine(AppContext.BaseDirectory, "Data"),
                Path.Combine(Environment.CurrentDirectory, "Data")
            ];

            return candidates
                .Select(Path.GetFullPath)
                .Where(path => !PathEquals(path, DataDirectory))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
    }

    public static bool PathEquals(string left, string right)
    {
        return string.Equals(
            Path.GetFullPath(left).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
            Path.GetFullPath(right).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
            StringComparison.OrdinalIgnoreCase);
    }
}

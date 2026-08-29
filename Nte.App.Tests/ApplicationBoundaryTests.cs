using NET_Thing_Encryptor;
using Nte.App.Services;
using Nte.App.ViewModels;

namespace Nte.App.Tests;

public sealed class ApplicationBoundaryTests
{
    [Fact]
    public void SharedApplicationAssembly_HasNoDesktopBackendOrWindowsUiReference()
    {
        string[] references = typeof(AppShellViewModel).Assembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name ?? string.Empty)
            .ToArray();

        Assert.DoesNotContain("Avalonia.Desktop", references);
        Assert.DoesNotContain("System.Windows.Forms", references);
        Assert.DoesNotContain("System.Drawing.Common", references);
        Assert.DoesNotContain(references, name => name.StartsWith("Microsoft.Windows", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ThingDataAdapter_CompletesDocumentAndVaultArchiveRoundTrip()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string sourceDirectory = CreateTemporaryDirectory();
        string destinationDirectory = CreateTemporaryDirectory();
        try
        {
            byte[] archiveBytes;
            await using (var sourceServiceScope = new AsyncServiceScope(
                new ThingDataVaultService(new FileSystemVaultStorage(sourceDirectory))))
            {
                ThingDataVaultService sourceService = sourceServiceScope.Service;
                Assert.True(await sourceService.InitializeAsync(cancellationToken));
                Assert.False(sourceService.HasPersistedVault);
                Assert.True(await sourceService.UnlockAsync("M3 test password", cancellationToken));

                await sourceService.CreateFolderAsync("Documents", 0, cancellationToken);
                VaultItem folder = Assert.Single(await sourceService.GetFolderItemsAsync(0, cancellationToken));
                using var document = new MemoryStream("portable content"u8.ToArray());
                await sourceService.ImportFileAsync(
                    document,
                    "note.txt",
                    folder.Id,
                    "note",
                    cancellationToken);
                VaultItem file = Assert.Single(await sourceService.GetFolderItemsAsync(folder.Id, cancellationToken));

                using var exportedDocument = new MemoryStream();
                await sourceService.ExportFileAsync(file.Id, exportedDocument, cancellationToken);
                Assert.Equal("portable content"u8.ToArray(), exportedDocument.ToArray());

                using var archive = new MemoryStream();
                Assert.Equal(2, await sourceService.ExportVaultAsync(archive, cancellationToken));
                archiveBytes = archive.ToArray();
            }

            await using (var destinationServiceScope = new AsyncServiceScope(
                new ThingDataVaultService(new FileSystemVaultStorage(destinationDirectory))))
            {
                ThingDataVaultService destinationService = destinationServiceScope.Service;
                Assert.True(await destinationService.InitializeAsync(cancellationToken));
                using var archive = new MemoryStream(archiveBytes, writable: false);
                Assert.Equal(2, await destinationService.ImportVaultAsync(archive, cancellationToken));
                Assert.True(await destinationService.UnlockAsync("M3 test password", cancellationToken));

                VaultItem folder = Assert.Single(await destinationService.GetFolderItemsAsync(0, cancellationToken));
                VaultItem file = Assert.Single(await destinationService.GetFolderItemsAsync(folder.Id, cancellationToken));
                using var document = new MemoryStream();
                await destinationService.ExportFileAsync(file.Id, document, cancellationToken);
                Assert.Equal("portable content"u8.ToArray(), document.ToArray());
            }
        }
        finally
        {
            TryDelete(sourceDirectory);
            TryDelete(destinationDirectory);
        }
    }

    private static string CreateTemporaryDirectory()
    {
        string directory = Path.Combine(
            Path.GetTempPath(),
            "NETThingEncryptor.App.Tests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }

    private static void TryDelete(string directory)
    {
        try
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, recursive: true);
        }
        catch (IOException)
        {
            // A failed assertion remains the useful test result if Windows releases a file late.
        }
    }

    private sealed class AsyncServiceScope(ThingDataVaultService service) : IAsyncDisposable
    {
        public ThingDataVaultService Service { get; } = service;

        public ValueTask DisposeAsync()
        {
            Service.Dispose();
            return ValueTask.CompletedTask;
        }
    }
}

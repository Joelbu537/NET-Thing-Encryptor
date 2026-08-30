using NET_Thing_Encryptor;
using Nte.App.Services;
using Nte.App.ViewModels;

namespace Nte.App.Tests;

public sealed class VaultDocumentViewModelTests
{
    [Fact]
    public async Task TextDocument_StartsReadOnlyAndSavesWithOriginalBom()
    {
        byte[]? saved = null;
        string status = string.Empty;
        var file = new VaultFileContent(
            42,
            "note",
            FileType.text,
            "txt",
            [0xEF, 0xBB, 0xBF, .. "before"u8.ToArray()]);
        using var viewModel = new VaultDocumentViewModel(
            file,
            (content, _) =>
            {
                saved = content.ToArray();
                return Task.CompletedTask;
            },
            () => { },
            value => status = value);

        Assert.True(viewModel.IsReadOnly);
        await viewModel.ToggleEditingCommand.ExecuteAsync();
        viewModel.Text = "after";
        await viewModel.SaveCommand.ExecuteAsync();

        Assert.False(viewModel.IsDirty);
        Assert.NotNull(saved);
        Assert.True(saved.AsSpan().StartsWith(new byte[] { 0xEF, 0xBB, 0xBF }));
        Assert.Equal("after", TextDocumentCodec.Decode(saved).Text);
        Assert.Contains("gespeichert", status);
    }

    [Fact]
    public async Task ClosingDirtyText_RequiresExplicitDiscard()
    {
        int closes = 0;
        var file = new VaultFileContent(42, "note", FileType.text, "txt", "before"u8.ToArray());
        var viewModel = new VaultDocumentViewModel(
            file,
            (_, _) => Task.CompletedTask,
            () => closes++,
            _ => { });
        viewModel.Text = "changed";

        await viewModel.CloseCommand.ExecuteAsync();
        Assert.True(viewModel.ShowDiscardConfirmation);
        Assert.Equal(0, closes);

        await viewModel.DiscardAndCloseCommand.ExecuteAsync();
        Assert.Equal(1, closes);
    }

    [Fact]
    public void SearchCountsCaseInsensitiveMatches()
    {
        var file = new VaultFileContent(42, "note", FileType.text, "txt", "One one ONE"u8.ToArray());
        using var viewModel = new VaultDocumentViewModel(
            file,
            (_, _) => Task.CompletedTask,
            () => { },
            _ => { });

        viewModel.SearchText = "one";

        Assert.Equal("3 Treffer", viewModel.SearchResultText);
    }
}

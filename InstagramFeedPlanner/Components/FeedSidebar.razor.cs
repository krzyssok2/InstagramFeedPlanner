using InstagramFeedPlanner.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace InstagramFeedPlanner.Components;

public partial class FeedSidebar
{
    [Parameter] public List<Feed> Feeds { get; set; } = new();
    [Parameter] public Guid? SelectedFeedId { get; set; }
    [Parameter] public EventCallback<Guid> OnFeedSelected { get; set; }
    [Parameter] public EventCallback<Guid> OnDeleteFeed { get; set; }
    [Parameter] public EventCallback OnAddNewFeed { get; set; }
    [Parameter] public EventCallback<(Guid feedId, string newName)> OnRenameFeed { get; set; }

    private Guid? RenamingFeedId { get; set; }
    private string RenameText { get; set; } = "";

    private ElementReference? renameInputRef;
    private bool shouldFocusRenameInput;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (shouldFocusRenameInput && renameInputRef.HasValue)
        {
            shouldFocusRenameInput = false;
            await renameInputRef.Value.FocusAsync();
        }
    }

    private void StartRename(Guid id, string currentName)
    {
        RenamingFeedId = id;
        RenameText = currentName;
        shouldFocusRenameInput = true;
    }

    private async void ConfirmRename(Guid id)
    {
        if (!string.IsNullOrWhiteSpace(RenameText))
        {
            await OnRenameFeed.InvokeAsync((id, RenameText));
        }

        ResetRename();

        StateHasChanged();
    }

    private async Task OnRenameKeyDown(KeyboardEventArgs e, Guid id)
    {
        if (e.Key == "Enter")
        {
            await Task.Yield();
            ConfirmRename(id);
        }
        else if (e.Key == "Escape")
        {
            ResetRename();
        }
    }

    private void ResetRename()
    {
        RenamingFeedId = null;
        RenameText = "";
    }
}
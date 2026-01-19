using GongSolutions.Wpf.DragDrop;
using System;
using System.Windows;
using IDropTarget = GongSolutions.Wpf.DragDrop.IDropTarget;

namespace PromptMasterv5.ViewModels;

public sealed class PinnedPromptDropHandler : IDropTarget
{
    private readonly MainViewModel _vm;

    public PinnedPromptDropHandler(MainViewModel vm)
    {
        _vm = vm;
    }

    public void DragOver(IDropInfo dropInfo)
    {
        if (dropInfo == null) return;
        if (dropInfo.Data == null) return;
        if (dropInfo.TargetCollection == null) return;

        dropInfo.Effects = System.Windows.DragDropEffects.Move;
        dropInfo.DropTargetAdorner = DropTargetAdorners.Insert;
    }

    public void Drop(IDropInfo dropInfo)
    {
        if (dropInfo == null) return;
        if (dropInfo.DragInfo == null) return;

        var oldIndex = dropInfo.DragInfo.SourceIndex;
        var newIndex = dropInfo.InsertIndex;

        if (oldIndex < 0) return;
        if (newIndex < 0) return;

        if (newIndex > oldIndex) newIndex--;

        _vm.ReorderMiniPinnedPrompts(oldIndex, newIndex);
    }
}

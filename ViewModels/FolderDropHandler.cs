using GongSolutions.Wpf.DragDrop;
using PromptMasterv5.Models;
using System.Windows;

namespace PromptMasterv5.ViewModels
{
    public class FolderDropHandler : IDropTarget
    {
        private readonly MainViewModel _viewModel;

        public FolderDropHandler(MainViewModel viewModel)
        {
            _viewModel = viewModel;
        }

        // 1. 拖拽经过时：判断是否允许放下
        public void DragOver(IDropInfo dropInfo)
        {
            // 如果拖动的是文件(PromptItem)，且目标是文件夹(FolderItem)
            if (dropInfo.Data is PromptItem && dropInfo.TargetItem is FolderItem)
            {
                dropInfo.DropTargetAdorner = DropTargetAdorners.Highlight;
                dropInfo.Effects = DragDropEffects.Move;
            }
        }

        // 2. 放下时：执行移动逻辑
        public void Drop(IDropInfo dropInfo)
        {
            var file = dropInfo.Data as PromptItem;
            var targetFolder = dropInfo.TargetItem as FolderItem;

            if (file != null && targetFolder != null)
            {
                // 调用 ViewModel 的方法来处理实际的数据移动
                _viewModel.MoveFileToFolder(file, targetFolder);
            }
        }
    }
}
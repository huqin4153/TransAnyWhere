using Avalonia.Controls;
using Avalonia.Input;
using System.Linq;
using TransAnyWhereApp.ViewModels;

namespace TransAnyWhereApp.Views;

public partial class TransferManagerView : UserControl
{
    public TransferManagerView()
    {
        InitializeComponent();
        // 注册拖拽事件
        AddHandler(DragDrop.DragOverEvent, DragOver);
        AddHandler(DragDrop.DropEvent, Drop);

        AddHandler(DragDrop.DragLeaveEvent, (s, e) => {
            DropZone.Background = Avalonia.Media.Brush.Parse("Transparent");
        });
    }
     
    private void DragOver(object? sender, DragEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        topLevel?.Focus();

        // 使用你发现的 TryGetFiles() 扩展方法
        var files = e.DataTransfer.TryGetFiles();

        // 只要有文件，就允许“复制”操作
        if (files != null && files.Any())
        {
            e.DragEffects = DragDropEffects.Copy;
            // 视觉反馈：变色
            DropZone.Background = Avalonia.Media.Brush.Parse("#26007AFF");
        }
        else
        {
            e.DragEffects = DragDropEffects.None;
        }
    }

    private void Drop(object? sender, DragEventArgs e)
    {
        // 恢复背景色
        DropZone.Background = Avalonia.Media.Brush.Parse("Transparent");

        // 获取存储项集合
        var files = e.DataTransfer.TryGetFiles();

        if (files != null && DataContext is MainViewModel vm)
        {
            int count = 0;
            foreach (var file in files)
            {
                // file 是 IStorageItem，通过 Path.LocalPath 获取真实路径
                var path = file.Path.LocalPath;
                if (!string.IsNullOrEmpty(path))
                {
                    vm.AddFile(path);
                    count++;
                }
            }

            if (count > 0)
                vm.ShowMessage($"成功接收 {count} 个文件 📥");
        }
    }
}
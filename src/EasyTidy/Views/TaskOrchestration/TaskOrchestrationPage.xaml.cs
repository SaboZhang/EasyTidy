﻿// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

using EasyTidy.Model;
using Microsoft.UI.Xaml.Controls.Primitives;
using Windows.ApplicationModel.DataTransfer;

namespace EasyTidy.Views;
/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class TaskOrchestrationPage : Page
{
    public TaskOrchestrationViewModel ViewModel { get; set; }

    private const string DraggedTask = "DraggedTask";
    private const string DraggedIndex = "DraggedIndex";

    public TaskOrchestrationPage()
    {
        ViewModel = App.GetService<TaskOrchestrationViewModel>();
        this.InitializeComponent();
        XamlRoot = App.MainWindow.Content.XamlRoot;
    }

    private void EditButton_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.UpdateTaskCommand.Execute((sender as Button).DataContext);
    }

    private void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.DeleteTaskCommand.Execute((sender as Button).DataContext);
    }

    private void RunButton_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.ExecuteTaskCommand.Execute((sender as Button).DataContext);
    }

    private void IsEnableButton_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.IsEnableTaskCommand.Execute((sender as Button).DataContext);
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        ViewModel.Initialize(NotificationQueue);
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        ViewModel.Uninitialize();
    }

    private void ListView_DragItemsStarting(object sender, DragItemsStartingEventArgs e)
    {
        var draggedObject = e.Items.FirstOrDefault();
        var draggedViewModel = draggedObject as TaskOrchestrationTable;
        e.Data.Properties.Add(DraggedTask, draggedViewModel);
        e.Data.Properties.Add(DraggedIndex, ViewModel.TaskListACV.IndexOf(draggedViewModel));
    }

    private async void ListView_DropAsync(object sender, DragEventArgs e)
    {
        if (e.Data == null)
        {
            return;
        }

        var result = e.Data.Properties.TryGetValue(DraggedIndex, out var draggedIndexObject);

        if (!result || draggedIndexObject == null)
        {
            return;
        }

        var draggedIndex = (int)draggedIndexObject;
        var grid = sender as Grid;
        var droppedIndex = TaskListView.Items.IndexOf(grid.DataContext);

        if (draggedIndex == droppedIndex)
        {
            return;
        }

        result = e.Data.Properties.TryGetValue(DraggedTask, out var draggedObject);
        if (!result || draggedObject == null)
        {
            return;
        }

        Logger.Debug($"Move task from {draggedIndex} to {droppedIndex}");
        var draggedTask = draggedObject as TaskOrchestrationTable;
        await ViewModel.OnTaskListCollectionChangedAsync(draggedTask, draggedIndex, droppedIndex);
    }

    private void ListView_DragOver(object sender, DragEventArgs e)
    {
        if (e.Data != null)
        {
            e.AcceptedOperation = DataPackageOperation.Move;
        }
        else
        {
            // 若拖拽的项目不含数据，则不允许其被放置。
            e.AcceptedOperation = DataPackageOperation.None;
        }

    }

    private void TaskListView_ContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
    {
        // 获取当前项的索引
        var index = TaskListView.Items.IndexOf(args.Item);

        // 获取对应的 TextBlock 并设置序号
        if (args.ItemContainer != null)
        {
            var grid = args.ItemContainer.ContentTemplateRoot as Grid;
            var textBlock = grid?.FindName("IndexTextBlock") as TextBlock;

            if (textBlock != null)
            {
                textBlock.Text = (index + 1).ToString();
            }
        }
    }

    private void Order_Click(object sender, RoutedEventArgs e)
    {
        var btn = sender as ToggleButton;
        if (btn == null || !btn.IsChecked.HasValue)
            return;

        bool isIdOrder = btn.IsChecked.Value;

        UpdateTaskOrder(isIdOrder);
        UpdateSettings(isIdOrder);
    }

    private void UpdateTaskOrder(bool isIdOrder)
    {
        foreach (var task in ViewModel.TaskList)
        {
            task.TagOrder = isIdOrder;
        }

        ViewModel.TaskListACV.Refresh();
    }

    private void UpdateSettings(bool isIdOrder)
    {
        Settings.IdOrder = isIdOrder;
        Settings.Save();
    }

}

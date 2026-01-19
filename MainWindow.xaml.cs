using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using FolderSentinel.Services;
using FolderSentinel.ViewModels;
using WinForms = System.Windows.Forms;

namespace FolderSentinel
{
    public partial class MainWindow : Window
    {
        private AppViewModel Vm => (AppViewModel)DataContext;

        public MainWindow()
        {
            InitializeComponent();

            var service = new FolderMonitorService();
            DataContext = new AppViewModel(service);

            Vm.LoadWatchRoots();

            if (Vm.WatchRoots.Any())
            {
                Vm.StartMonitoring();
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            Vm.StopMonitoring();
            Vm.MonitorService.Dispose();
            Vm.SaveWatchRoots();
        }

        private void BrowseFolder_Click(object sender, RoutedEventArgs e)
        {
            using var dialog = new WinForms.FolderBrowserDialog
            {
                SelectedPath = FolderPathTextBox.Text
            };

            if (dialog.ShowDialog() == WinForms.DialogResult.OK)
            {
                FolderPathTextBox.Text = dialog.SelectedPath;
            }
        }

        private void AddFolder_Click(object sender, RoutedEventArgs e)
        {
            var path = FolderPathTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
            {
                System.Windows.MessageBox.Show("请选择有效的文件夹路径");
                return;
            }

            if (Vm.WatchRoots.Any(w => w.Path == path))
                return;

            Vm.WatchRoots.Add(new WatchRootViewModel(path));

            if (Vm.MonitorState == MonitorState.Monitoring)
            {
                Vm.MonitorService.AddWatchFolder(path);
            }
        }

        private void RemoveWatchRoot_Click(object sender, RoutedEventArgs e)
        {
            if (WatchRootsListBox.SelectedItem is WatchRootViewModel selectedRoot)
            {
                var result = System.Windows.MessageBox.Show(
                    $"确认取消监控目录：{selectedRoot.Path}？",
                    "确认取消",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    Vm.RemoveWatchRoot(selectedRoot);
                }
            }
            else
            {
                System.Windows.MessageBox.Show("请先选择一个监控目录");
            }
        }

        private void StartStopMonitoring_Click(object sender, RoutedEventArgs e)
        {
            if (Vm.MonitorState == MonitorState.Monitoring)
                Vm.StopMonitoring();
            else
                Vm.StartMonitoring();
        }

        private void MoveToRecycleBin_Click(object sender, RoutedEventArgs e)
        {
            Vm.MoveFoldersToRecycleBin();
        }

        private void DeleteDirect_Click(object sender, RoutedEventArgs e)
        {
            var result = System.Windows.MessageBox.Show(
                "确认直接删除所有监控到的文件夹吗？",
                "确认删除",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                Vm.DeleteFoldersDirectly();
            }
        }
    }
}

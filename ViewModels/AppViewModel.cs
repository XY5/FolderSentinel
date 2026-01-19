using FolderSentinel.Services;
using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Windows;

namespace FolderSentinel.ViewModels
{
    public enum MonitorState { Stopped, Monitoring }

    public class AppViewModel : ViewModelBase
    {
        private readonly IFolderMonitorService _monitorService;

        private string WatchRootsFile =>
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "FolderSentinel",
                "watchroots.json");

        public ObservableCollection<WatchRootViewModel> WatchRoots { get; } = new();
        public ObservableCollection<NewFolderViewModel> NewFolders { get; } = new();
        public ObservableCollection<LogEntryViewModel> Logs { get; } = new();

        private MonitorState _monitorState;
        public MonitorState MonitorState
        {
            get => _monitorState;
            private set
            {
                _monitorState = value;
                OnPropertyChanged(nameof(MonitorState));
                OnPropertyChanged(nameof(StartStopButtonText));
            }
        }

        public string StartStopButtonText =>
            MonitorState == MonitorState.Monitoring ? "停止监控" : "开始监控";

        public IFolderMonitorService MonitorService => _monitorService;

        public AppViewModel(IFolderMonitorService monitorService)
        {
            _monitorService = monitorService;
            _monitorService.FolderCreated += OnFolderCreated;
            _monitorService.FolderDeleted += OnFolderDeleted;
            _monitorService.MonitorError += OnMonitorError;
        }

        public void LoadWatchRoots()
        {
            if (!File.Exists(WatchRootsFile))
            {
                AddLogSafe(new LogEntryViewModel(
                    LogLevel.Info,
                    "未找到 watchroots.json，使用空配置"
                ));
                return;
            }

            try
            {
                var json = File.ReadAllText(WatchRootsFile);
                var paths = JsonSerializer.Deserialize<List<string>>(json);

                if (paths == null) return;

                foreach (var path in paths)
                {
                    if (!Directory.Exists(path))
                    {
                        AddLogSafe(new LogEntryViewModel(
                            LogLevel.Warning,
                            $"监控目录不存在，已跳过：{path}"
                        ));
                        continue;
                    }

                    if (WatchRoots.Any(w => w.Path == path))
                        continue;

                    WatchRoots.Add(new WatchRootViewModel(path));
                }

                AddLogSafe(new LogEntryViewModel(
                    LogLevel.Info,
                    "监控目录配置已加载"
                ));
            }
            catch (Exception ex)
            {
                AddLogSafe(new LogEntryViewModel(
                    LogLevel.Error,
                    $"加载监控目录失败：{ex.Message}"
                ));
            }
        }

        public void SaveWatchRoots()
        {
            try
            {
                var directory = Path.GetDirectoryName(WatchRootsFile);
                if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var paths = WatchRoots.Select(w => w.Path).ToList();

                var json = JsonSerializer.Serialize(
                    paths,
                    new JsonSerializerOptions { WriteIndented = true }
                );

                File.WriteAllText(WatchRootsFile, json);

                AddLogSafe(new LogEntryViewModel(
                    LogLevel.Info,
                    "监控目录配置已保存"
                ));
            }
            catch (Exception ex)
            {
                AddLogSafe(new LogEntryViewModel(
                    LogLevel.Error,
                    $"保存监控目录失败：{ex.Message}"
                ));
            }
        }

        public void StartMonitoring()
        {
            if (MonitorState == MonitorState.Monitoring)
                return;

            if (!WatchRoots.Any())
            {
                AddLogSafe(new LogEntryViewModel(LogLevel.Warning, "未选择监控目录，无法启动监控"));
                return;
            }

            foreach (var root in WatchRoots)
            {
                _monitorService.AddWatchFolder(root.Path);
            }

            MonitorState = MonitorState.Monitoring;
            AddLogSafe(new LogEntryViewModel(LogLevel.Info, "开始监控"));
        }

        public void StopMonitoring()
        {
            if (MonitorState == MonitorState.Stopped)
                return;

            foreach (var root in WatchRoots)
            {
                _monitorService.RemoveWatchFolder(root.Path);
            }

            MonitorState = MonitorState.Stopped;
            AddLogSafe(new LogEntryViewModel(LogLevel.Info, "停止监控"));
        }

        public void RemoveWatchRoot(WatchRootViewModel root)
        {
            if (root == null) return;

            _monitorService.RemoveWatchFolder(root.Path);
            WatchRoots.Remove(root);

            AddLogSafe(new LogEntryViewModel(LogLevel.Info, $"取消监控目录：{root.Path}"));
        }

        public void MoveFoldersToRecycleBin()
        {
            if (!NewFolders.Any())
            {
                AddLogSafe(new LogEntryViewModel(LogLevel.Warning, "新增文件夹列表为空，无法执行移动至回收站操作"));
                return;
            }

            foreach (var folder in NewFolders.ToList())
            {
                var success = TryDeleteFolder(folder.FullPath, toRecycleBin: true);
                if (!success) break;
            }
        }

        public void DeleteFoldersDirectly()
        {
            if (!NewFolders.Any())
            {
                AddLogSafe(new LogEntryViewModel(LogLevel.Warning, "新增文件夹列表为空，无法执行移动至回收站操作"));
                return;
            }

            foreach (var folder in NewFolders.ToList())
            {
                var success = TryDeleteFolder(folder.FullPath, toRecycleBin: false);
                if (!success) break;
            }
        }

        public void ClearNewFolders()
        {
            if (!NewFolders.Any())
            {
                AddLogSafe(new LogEntryViewModel(LogLevel.Info, "新增文件夹列表已为空"));
                return;
            }

            NewFolders.Clear();
            AddLogSafe(new LogEntryViewModel(LogLevel.Info, "已清空新增文件夹列表"));
        }

        private void OnFolderCreated(object? sender, FolderCreatedEventArgs e)
        {
            if (NewFolders.Any(f => f.FullPath == e.FullPath)) return;

            var folderVM = new NewFolderViewModel(e.FullPath, e.RootPath);
            AddNewFolderSafe(folderVM);
            AddLogSafe(new LogEntryViewModel(LogLevel.Info, $"新增文件夹：{e.FullPath}"));
        }

        private void OnFolderDeleted(object? sender, FolderDeletedEventArgs e)
        {
            var folder = NewFolders.FirstOrDefault(f => f.FullPath == e.FullPath);
            if (folder == null) return;

            RemoveNewFolderSafe(folder);
            AddLogSafe(new LogEntryViewModel(LogLevel.Info, $"文件夹被移除：{e.FullPath}"));
        }

        private void OnMonitorError(object? sender, MonitorErrorEventArgs e)
        {
            AddLogSafe(new LogEntryViewModel(LogLevel.Error, $"监控异常：{e.RootPath} - {e.Exception.Message}"));
        }

        private bool TryDeleteFolder(string path, bool toRecycleBin)
        {
            while (true)
            {
                try
                {
                    if (toRecycleBin)
                    {
                        FileSystem.DeleteDirectory(path, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
                        AddLogSafe(new LogEntryViewModel(LogLevel.Info, $"已移至回收站：{path}"));
                    }
                    else
                    {
                        FileSystem.DeleteDirectory(path, UIOption.OnlyErrorDialogs, RecycleOption.DeletePermanently);
                        AddLogSafe(new LogEntryViewModel(LogLevel.Info, $"已删除：{path}"));
                    }

                    var folderVM = NewFolders.FirstOrDefault(f => f.FullPath == path);
                    if (folderVM != null)
                        NewFolders.Remove(folderVM);

                    return true;
                }
                catch (Exception ex)
                {
                    var result = System.Windows.MessageBox.Show(
                        $"操作失败：{ex.Message}\n重试或取消？",
                        "操作失败",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning);

                    if (result == MessageBoxResult.No)
                        return false;
                }
            }
        }

        private void AddNewFolderSafe(NewFolderViewModel folder)
        {
            if (DispatcherCheckRequired())
            {
                System.Windows.Application.Current.Dispatcher.BeginInvoke(
                    new Action(() => NewFolders.Add(folder))
                );
                return;
            }

            NewFolders.Add(folder);
        }

        private void RemoveNewFolderSafe(NewFolderViewModel folder)
        {
            if (DispatcherCheckRequired())
            {
                System.Windows.Application.Current.Dispatcher.BeginInvoke(
                    new Action(() => NewFolders.Remove(folder))
                );
                return;
            }

            NewFolders.Remove(folder);
        }

        private void AddLogSafe(LogEntryViewModel log)
        {
            if (DispatcherCheckRequired())
            {
                System.Windows.Application.Current.Dispatcher.BeginInvoke(
                    new Action(() => Logs.Add(log))
                );
                return;
            }

            Logs.Add(log);
        }

        private bool DispatcherCheckRequired()
        {
            return System.Windows.Application.Current.Dispatcher.Thread != System.Threading.Thread.CurrentThread;
        }
    }
}

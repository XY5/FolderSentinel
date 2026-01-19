using System;
using System.Collections.Generic;
using System.IO;

namespace FolderSentinel.Services
{
    public class FolderMonitorService : IFolderMonitorService, IDisposable
    {
        public event EventHandler<FolderCreatedEventArgs>? FolderCreated;
        public event EventHandler<FolderDeletedEventArgs>? FolderDeleted;
        public event EventHandler<MonitorErrorEventArgs>? MonitorError;

        private readonly Dictionary<string, FileSystemWatcher> _watchers = new();
        private bool _isMonitoring;

        public void Start(IEnumerable<string> rootPaths)
        {
            if (_isMonitoring) return;

            var added = false;
            foreach (var path in rootPaths)
            {
                added |= TryAddWatcher(path);
            }

            _isMonitoring = added;
        }

        public void Stop()
        {
            if (_watchers.Count == 0)
            {
                _isMonitoring = false;
                return;
            }

            foreach (var watcher in _watchers.Values)
            {
                watcher.EnableRaisingEvents = false;
                watcher.Dispose();
            }

            _watchers.Clear();
            _isMonitoring = false;
        }

        public void AddWatchFolder(string path)
        {
            if (TryAddWatcher(path))
            {
                _isMonitoring = true;
            }
        }

        public void RemoveWatchFolder(string path)
        {
            if (_watchers.TryGetValue(path, out var watcher))
            {
                watcher.EnableRaisingEvents = false;
                watcher.Dispose();
                _watchers.Remove(path);
                _isMonitoring = _watchers.Count > 0;
            }
        }

        public void Dispose() => Stop();

        private bool TryAddWatcher(string path)
        {
            if (_watchers.ContainsKey(path) || !Directory.Exists(path))
                return false;

            try
            {
                var watcher = CreateWatcher(path);
                watcher.EnableRaisingEvents = true;
                _watchers[path] = watcher;
                return true;
            }
            catch (Exception ex)
            {
                MonitorError?.Invoke(this, new MonitorErrorEventArgs(path, ex));
                return false;
            }
        }

        private FileSystemWatcher CreateWatcher(string path)
        {
            var watcher = new FileSystemWatcher(path)
            {
                IncludeSubdirectories = false,
                NotifyFilter = NotifyFilters.DirectoryName
            };

            watcher.Created += (_, e) =>
            {
                if (Directory.Exists(e.FullPath))
                    FolderCreated?.Invoke(this, new FolderCreatedEventArgs(e.FullPath, path));
            };

            watcher.Deleted += (_, e) =>
            {
                FolderDeleted?.Invoke(this, new FolderDeletedEventArgs(e.FullPath, path));
            };

            watcher.Error += (_, e) =>
            {
                MonitorError?.Invoke(this, new MonitorErrorEventArgs(path, e.GetException()));
            };

            return watcher;
        }
    }

    // 事件参数类
    public class FolderCreatedEventArgs : EventArgs
    {
        public string FullPath { get; }
        public string RootPath { get; }
        public FolderCreatedEventArgs(string fullPath, string rootPath)
        {
            FullPath = fullPath;
            RootPath = rootPath;
        }
    }

    public class FolderDeletedEventArgs : EventArgs
    {
        public string FullPath { get; }
        public string RootPath { get; }
        public FolderDeletedEventArgs(string fullPath, string rootPath)
        {
            FullPath = fullPath;
            RootPath = rootPath;
        }
    }

    public class MonitorErrorEventArgs : EventArgs
    {
        public string RootPath { get; }
        public Exception Exception { get; }
        public MonitorErrorEventArgs(string rootPath, Exception ex)
        {
            RootPath = rootPath;
            Exception = ex;
        }
    }
}

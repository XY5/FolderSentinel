using System;
using System.Collections.Generic;

namespace FolderSentinel.Services
{
    public interface IFolderMonitorService : IDisposable
    {
        event EventHandler<FolderCreatedEventArgs>? FolderCreated;
        event EventHandler<FolderDeletedEventArgs>? FolderDeleted;
        event EventHandler<MonitorErrorEventArgs>? MonitorError;

        void Start(IEnumerable<string> rootPaths);
        void Stop();
        void AddWatchFolder(string path);
        void RemoveWatchFolder(string path);
    }
}

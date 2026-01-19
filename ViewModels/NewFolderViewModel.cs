using System;
using System.IO;

namespace FolderSentinel.ViewModels
{
    public class NewFolderViewModel : ViewModelBase
    {
        public string FullPath { get; }
        public string RootPath { get; }
        public DateTime DetectedAt { get; }

        public string Name => Path.GetFileName(FullPath);

        public NewFolderViewModel(string fullPath, string rootPath)
        {
            FullPath = fullPath;
            RootPath = rootPath;
            DetectedAt = DateTime.Now;
        }

        public override string ToString() => FullPath;
    }
}

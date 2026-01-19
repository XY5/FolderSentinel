namespace FolderSentinel.ViewModels
{
    public class WatchRootViewModel : ViewModelBase
    {
        public string Path { get; }

        public WatchRootViewModel(string path)
        {
            Path = path;
        }

        public override string ToString() => Path;
    }
}

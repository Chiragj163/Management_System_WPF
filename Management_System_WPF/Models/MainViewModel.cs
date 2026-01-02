using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;

namespace Management_System_WPF.ViewModels // <--- CHECK THIS NAMESPACE
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private Page _currentPage;
        private bool _isFullScreen;

        // 1. The Page currently displayed in the MainFrame
        public Page CurrentPage
        {
            get => _currentPage;
            set
            {
                _currentPage = value;
                OnPropertyChanged();
            }
        }

        // 2. Visibility Controls for Layout
        public Visibility TopBarVisibility => _isFullScreen ? Visibility.Collapsed : Visibility.Visible;
        public Visibility SideMenuVisibility => _isFullScreen ? Visibility.Collapsed : Visibility.Visible;
        public GridLength SideMenuWidth => _isFullScreen ? new GridLength(0) : new GridLength(300);
        public Thickness FrameMargin => _isFullScreen ? new Thickness(0) : new Thickness(0, 70, 0, 0);
        public int FrameColumn => _isFullScreen ? 0 : 1;
        public int FrameColumnSpan => _isFullScreen ? 2 : 1;

        // 3. Method to toggle layout modes
        public void SetLayoutMode(bool isFullScreen)
        {
            _isFullScreen = isFullScreen;

            // Notify UI to update binding
            OnPropertyChanged(nameof(TopBarVisibility));
            OnPropertyChanged(nameof(SideMenuVisibility));
            OnPropertyChanged(nameof(SideMenuWidth));
            OnPropertyChanged(nameof(FrameMargin));
            OnPropertyChanged(nameof(FrameColumn));
            OnPropertyChanged(nameof(FrameColumnSpan));

            // Adjust Window ResizeMode (Taskbar visibility)
            if (Application.Current.MainWindow != null)
            {
                Application.Current.MainWindow.ResizeMode = isFullScreen
                    ? ResizeMode.NoResize
                    : ResizeMode.CanResize;

                // Force refresh
                var state = Application.Current.MainWindow.WindowState;
                if (state == WindowState.Maximized)
                {
                    Application.Current.MainWindow.WindowState = WindowState.Normal;
                    Application.Current.MainWindow.WindowState = WindowState.Maximized;
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
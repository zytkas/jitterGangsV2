using System.Windows;
using System.Windows.Input;
using Wpf.Ui.Controls;

namespace jitterGangs
{
    public partial class LoginWindow : FluentWindow
    {
        private readonly LoginViewModel _viewModel;

        public LoginWindow()
        {
            try
            {
                InitializeComponent();

                // Get the ViewModel from the dependency container
                _viewModel = DependencyContainer.Resolve<LoginViewModel>();
                DataContext = _viewModel;

                this.WindowState = WindowState.Minimized;
                Loaded += LoginWindow_Loaded;
            }
            catch (Exception ex)
            {

                Application.Current.Shutdown();
            }
        }

        private async void LoginWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Initialize ViewModel and check license
                bool isLicenseValid = await _viewModel.InitializeAsync();

                // If license isn't valid, show the window
                if (!isLicenseValid)
                {
                    this.WindowState = WindowState.Normal;
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error: {ex.Message}", "Error");
                this.WindowState = WindowState.Normal;
            }
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }
    }
}
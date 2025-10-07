using System.Windows;
using Wpf.Ui;
using Wpf.Ui.Controls;

namespace jitterGangs.Admin
{
    public partial class LicenseManagerWindow : FluentWindow
    {
        private readonly LicenseManagerViewModel _viewModel;
        private readonly ContentDialogService _contentDialogService;

        public LicenseManagerWindow()
        {
            InitializeComponent();

            // Get ViewModel from dependency container
            _viewModel = DependencyContainer.Resolve<LicenseManagerViewModel>();
            DataContext = _viewModel;

            _contentDialogService = new ContentDialogService();
            _contentDialogService.SetContentPresenter(RootContentDialogPresenter);

            // Subscribe to close request event
            _viewModel.CloseRequested += (sender, args) => this.Close();

            Loaded += LicenseManagerWindow_Loaded;
        }

        private async void LicenseManagerWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await _viewModel.InitializeAsync();
        }

        protected override void OnClosed(EventArgs e)
        {
            // Unsubscribe from events to prevent memory leaks
            _viewModel.CloseRequested -= (sender, args) => this.Close();
            base.OnClosed(e);
        }
    }
}
using JitterGang.ViewModels;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Wpf.Ui;
using Wpf.Ui.Controls;

namespace jitterGangs
{
    public partial class MainWindow : FluentWindow
    {
        private readonly MainViewModel _viewModel;
        private bool _isClosing;
        private readonly IContentDialogService _contentDialogService;

        public MainWindow()
        {
            InitializeComponent();

            _viewModel = DependencyContainer.Resolve<MainViewModel>();
            DataContext = _viewModel;

            _contentDialogService = new ContentDialogService();

            _contentDialogService.SetDialogHost(RootContentDialogPresenter);

            Loaded += MainWindow_Loaded;
            Closing += MainWindow_Closing;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                await _viewModel.InitializeAsync();
            }
            catch (Exception ex)
            {
                var messageBox = new Wpf.Ui.Controls.MessageBox
                {
                    Title = "Error",
                    Content = $"Failed to initialize: {ex.Message}"
                };

                await messageBox.ShowDialogAsync();
            }
        }

        private async void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            if (_isClosing) return;
            e.Cancel = true;
            _isClosing = true;

            try
            {
                await _viewModel.ShutdownCommand.ExecuteAsync(null);
                Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                var messageBox = new Wpf.Ui.Controls.MessageBox
                {
                    Title = "Error",
                    Content = $"Error during application shutdown: {ex.Message}"
                };

                await messageBox.ShowDialogAsync();
                _isClosing = false;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }


        private async void ProcessComboBox_DropDownOpened(object sender, EventArgs e)
        {
            try
            {
                await _viewModel.RefreshProcessListCommand.ExecuteAsync(null);
            }
            catch (Exception ex)
            {
                Logger.Log($"Error refreshing process list: {ex.Message}");
            }
        }


        private async void ToggleSwitch_Click_1(object sender, RoutedEventArgs e)
        {
            var toggle = sender as ToggleSwitch;
            if (toggle == null) return;

            try
            {
                await _viewModel.ToggleControllerCommand.ExecuteAsync(toggle.IsChecked ?? false);
            }
            catch (Exception ex)
            {
                toggle.IsChecked = false;

                var contentStack = new StackPanel();
                var icon = new SymbolIcon
                {
                    Symbol = SymbolRegular.ErrorCircle24,
                    FontSize = 36,
                    HorizontalAlignment = HorizontalAlignment.Center
                };

                var messageText = new Wpf.Ui.Controls.TextBlock
                {
                    Text = $"{ex.Message}",
                    TextWrapping = TextWrapping.Wrap,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    TextAlignment = TextAlignment.Center,
                    Margin = new Thickness(0, 8, 0, 0)
                };

                contentStack.Children.Add(icon);
                contentStack.Children.Add(messageText);

                var dialog = new ContentDialog
                {
                    Content = contentStack,
                    CloseButtonText = "OK",
                    DefaultButton = ContentDialogButton.Close,
                    Padding = new Thickness(5),
                };

                await _contentDialogService.ShowAsync(dialog, CancellationToken.None);
            }
        }
    }
}
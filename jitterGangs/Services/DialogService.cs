using System.Windows;
using System.Windows.Controls;
using Wpf.Ui;
using Wpf.Ui.Controls;
using Wpf.Ui.Extensions;
using TextBlock = Wpf.Ui.Controls.TextBlock;

namespace jitterGangs.Services
{
    public interface IDialogService
    {
        Task<bool> ShowConfirmationAsync(string message, string title, CancellationToken cancellationToken = default);
        Task ShowErrorAsync(string message, string title, CancellationToken cancellationToken = default);
        Task ShowInfoAsync(string message, string title, CancellationToken cancellationToken = default);
        Task<ContentDialogResult> ShowContentDialogAsync(object content, string title, string primaryButtonText, string closeButtonText, CancellationToken cancellationToken = default);
        void SetContentPresenter(ContentPresenter contentPresenter);
    }

    public class DialogService : IDialogService
    {
        private readonly ContentDialogService _contentDialogService;

        public DialogService()
        {
            _contentDialogService = new ContentDialogService();
        }

        public void SetContentPresenter(ContentPresenter contentPresenter)
        {
            _contentDialogService.SetContentPresenter(contentPresenter);
        }

        public async Task<bool> ShowConfirmationAsync(string message, string title, CancellationToken cancellationToken = default)
        {
            var result = await _contentDialogService.ShowSimpleDialogAsync(
                new SimpleContentDialogCreateOptions
                {
                    Title = title,
                    Content = message,
                    PrimaryButtonText = "Yes",
                    CloseButtonText = "No"
                },
                cancellationToken
            );

            return result == ContentDialogResult.Primary;
        }

        public async Task ShowErrorAsync(string message, string title, CancellationToken cancellationToken = default)
        {
            var contentStack = new StackPanel();

            var icon = new SymbolIcon
            {
                Symbol = SymbolRegular.ErrorCircle24,
                FontSize = 36,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            var messageText = new TextBlock
            {
                Text = message,
                TextWrapping = TextWrapping.Wrap,
                HorizontalAlignment = HorizontalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 8, 0, 0)
            };

            contentStack.Children.Add(icon);
            contentStack.Children.Add(messageText);

            var dialog = new ContentDialog
            {
                Title = title,
                Content = contentStack,
                CloseButtonText = "OK",
                DefaultButton = ContentDialogButton.Close,
                Padding = new Thickness(5),
            };

            await _contentDialogService.ShowAsync(dialog, cancellationToken);
        }

        public async Task ShowInfoAsync(string message, string title, CancellationToken cancellationToken = default)
        {
            var contentStack = new StackPanel();

            var icon = new SymbolIcon
            {
                Symbol = SymbolRegular.Info24,
                FontSize = 36,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            var messageText = new TextBlock
            {
                Text = message,
                TextWrapping = TextWrapping.Wrap,
                HorizontalAlignment = HorizontalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 8, 0, 0)
            };

            contentStack.Children.Add(icon);
            contentStack.Children.Add(messageText);

            var dialog = new ContentDialog
            {
                Title = title,
                Content = contentStack,
                CloseButtonText = "OK",
                DefaultButton = ContentDialogButton.Close,
                Padding = new Thickness(5),
            };

            await _contentDialogService.ShowAsync(dialog, cancellationToken);
        }

        public async Task<ContentDialogResult> ShowContentDialogAsync(object content, string title, string primaryButtonText, string closeButtonText, CancellationToken cancellationToken = default)
        {
            var dialog = new ContentDialog
            {
                Title = title,
                Content = content,
                PrimaryButtonText = primaryButtonText,
                CloseButtonText = closeButtonText,
                DefaultButton = ContentDialogButton.Primary,
                Padding = new Thickness(5),
            };

            return await _contentDialogService.ShowAsync(dialog, cancellationToken);
        }
    }
}
using CommunityToolkit.Mvvm.ComponentModel;
using JitterGang.Services;
using System.Windows.Media;

namespace jitterGangs.Admin
{
    public partial class LicenseViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _key;

        [ObservableProperty]
        private bool _isValid;

        [ObservableProperty]
        private string _hwid;

        [ObservableProperty]
        private long _createdAt;

        public string CreatedAtFormatted =>
            DateTimeOffset.FromUnixTimeSeconds(CreatedAt).LocalDateTime.ToString("g");

        public string Status => IsValid ? "Active" : "Revoked";

        public Brush StatusColor => IsValid ?
            new SolidColorBrush(Color.FromRgb(76, 175, 80)) :
            new SolidColorBrush(Color.FromRgb(244, 67, 54));

        public LicenseViewModel(LicenseInfo info)
        {
            Key = info.Key;
            IsValid = info.IsValid;
            Hwid = info.HWID ?? "Not activated";
            CreatedAt = info.CreatedAt;
        }
    }
}
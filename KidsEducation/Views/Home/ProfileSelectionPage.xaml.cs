using KidsEducation.ViewModels.Home;

namespace KidsEducation.Views.Home;

public partial class ProfileSelectionPage : ContentPage
{
    private readonly ProfileSelectionViewModel _viewModel;
    private Border? _selectedAvatarBorder;

    // x:Name → Border map
    private Dictionary<string, Border>? _avatarBorders;

    public ProfileSelectionPage(ProfileSelectionViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.LoadProfilesCommand.Execute(null);

        _avatarBorders ??= new Dictionary<string, Border>
        {
            ["🐰"] = Av1, ["🦊"] = Av2, ["🦉"] = Av3, ["🐻"] = Av4,
            ["🐸"] = Av5, ["🦁"] = Av6, ["🦋"] = Av7, ["🐳"] = Av8
        };

        // Varsayılan seçili avatarı işaretle
        SelectAvatarVisual("🐰");
    }

    private void OnAvatarTapped(object sender, TappedEventArgs e)
    {
        var emoji = e.Parameter as string;
        if (string.IsNullOrEmpty(emoji)) return;

        _viewModel.SelectedAvatarEmoji = emoji;
        SelectAvatarVisual(emoji);
    }

    private void SelectAvatarVisual(string emoji)
    {
        if (_avatarBorders is null) return;

        // Öncekini sıfırla
        if (_selectedAvatarBorder is not null)
        {
            _selectedAvatarBorder.BackgroundColor = Color.FromArgb("#F5F5FF");
            _selectedAvatarBorder.Stroke = Color.FromArgb("#E0E0F0");
            _selectedAvatarBorder.StrokeThickness = 1;
        }

        // Yeniyi seç
        if (_avatarBorders.TryGetValue(emoji, out var border))
        {
            border.BackgroundColor = Color.FromArgb("#F1EEFF");
            border.Stroke = Color.FromArgb("#7B61FF");
            border.StrokeThickness = 2.5;
            _selectedAvatarBorder = border;
        }
    }
}

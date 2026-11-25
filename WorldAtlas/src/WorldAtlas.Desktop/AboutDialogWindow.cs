using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace WorldAtlas.Desktop;

public sealed class AboutDialogWindow : Window
{
    public AboutDialogWindow(string message, int scalePercent = 100)
    {
        const double width = 520;
        const double height = 310;
        var scale = scalePercent / 100d;
        Title = "World Atlas · About";
        Width = MinWidth = MaxWidth = width;
        Height = MinHeight = MaxHeight = height;
        SizeToContent = SizeToContent.Manual;
        CanResize = false;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = new SolidColorBrush(Color.Parse("#18212D"));
        Content = new Grid
        {
            RowDefinitions = new RowDefinitions("*,Auto"),
            Margin = new Thickness(22),
            RowSpacing = 14,
            Children =
            {
                new ScrollViewer
                {
                    VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
                    Content = new TextBlock
                    {
                        Text = message,
                        TextWrapping = TextWrapping.Wrap,
                        FontSize = 14 * scale,
                        LineHeight = 21 * scale
                    }
                },
                CreateCloseButton(scale)
            }
        };
        Grid.SetRow(((Grid)Content).Children[1], 1);
        ((Button)((Grid)Content).Children[1]).Click += (_, _) => Close();
    }

    private static Button CreateCloseButton(double scale) => new()
    {
        Name = "AboutCloseButton",
        Content = "Close",
        Width = 96 * scale,
        Height = 42 * scale,
        Padding = new Thickness(14 * scale, 7 * scale),
        HorizontalAlignment = HorizontalAlignment.Right,
        FontSize = 14 * scale
    };
}

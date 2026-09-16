using System;
using System.Threading.Tasks;
using Windows.Graphics;
using Windows.System;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using CmdDock.Core.Models;
using CmdDock.Core.Services;

namespace CmdDock_App.Views;

public enum CommandActionMode
{
    ParameterInput,
    Confirmation,
    ParameterAndConfirm
}

public class CommandActionResult
{
    public bool IsConfirmed { get; set; }
    public string ParameterValue { get; set; } = string.Empty;
}

public sealed class CommandActionWindow : Window
{
    private readonly TaskCompletionSource<CommandActionResult> _tcs = new();
    private readonly TextBox? _paramTextBox;
    private bool _isCompleted = false;

    private CommandActionWindow(CommandItem command, CommandActionMode mode, Window? ownerWindow = null)
    {
        try
        {
            this.SystemBackdrop = new MicaBackdrop();
        }
        catch { }

        var i18n = I18nService.Instance;
        string windowTitle = mode switch
        {
            CommandActionMode.Confirmation => i18n["Dialog.RunConfirm.Title"],
            _ => i18n["MiniDock.ParamTitle"]
        };

        this.Title = $"{windowTitle} - {command.DisplayName}";

        // Configure Presenter
        var appWindow = this.AppWindow;
        try
        {
            appWindow.SetIcon("Assets/AppIcon.ico");
        }
        catch { }

        var presenter = appWindow.Presenter as OverlappedPresenter;
        if (presenter != null)
        {
            presenter.IsAlwaysOnTop = true;
            presenter.IsResizable = false;
            presenter.IsMaximizable = false;
            presenter.IsMinimizable = false;
        }

        // Window Dimensions
        int windowWidth = 500;
        int windowHeight = mode == CommandActionMode.ParameterAndConfirm ? 370 : 310;
        appWindow.Resize(new SizeInt32(windowWidth, windowHeight));

        // Center on the active monitor
        var targetWindowId = ownerWindow != null ? ownerWindow.AppWindow.Id : appWindow.Id;
        var displayArea = DisplayArea.GetFromWindowId(targetWindowId, DisplayAreaFallback.Primary);
        if (displayArea != null)
        {
            var workArea = displayArea.WorkArea;
            var x = workArea.X + (workArea.Width - windowWidth) / 2;
            var y = workArea.Y + (workArea.Height - windowHeight) / 2;
            appWindow.Move(new PointInt32(x, y));
        }

        // --- Build Visual Tree ---
        var rootGrid = new Grid
        {
            Padding = new Thickness(24, 20, 24, 20),
            RowSpacing = 14
        };

        rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Header
        rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Command preview
        rootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Dynamic Content
        rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Buttons

        // 1. Header
        var headerStack = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };
        var icon = new FontIcon
        {
            Glyph = mode == CommandActionMode.Confirmation ? "\uE7BA" : "\uE765",
            FontSize = 22,
            Foreground = mode == CommandActionMode.Confirmation
                ? new SolidColorBrush(ColorHelper.FromArgb(255, 235, 120, 20))
                : Application.Current.Resources["AccentTextFillColorPrimaryBrush"] as Brush
        };

        var titleStack = new StackPanel { Spacing = 2 };
        var titleText = new TextBlock
        {
            Text = windowTitle,
            FontSize = 16,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
        };
        var subtitleText = new TextBlock
        {
            Text = command.DisplayName,
            FontSize = 12,
            Foreground = Application.Current.Resources["TextFillColorSecondaryBrush"] as Brush
        };
        titleStack.Children.Add(titleText);
        titleStack.Children.Add(subtitleText);

        headerStack.Children.Add(icon);
        headerStack.Children.Add(titleStack);
        Grid.SetRow(headerStack, 0);
        rootGrid.Children.Add(headerStack);

        // 2. Command Script Preview Box
        var previewBorder = new Border
        {
            Background = Application.Current.Resources["CardBackgroundFillColorDefaultBrush"] as Brush,
            BorderBrush = Application.Current.Resources["CardStrokeColorDefaultBrush"] as Brush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(12, 8, 12, 8)
        };

        var cmdTextToDisplay = string.IsNullOrWhiteSpace(command.Arguments)
            ? command.CommandText
            : $"{command.CommandText} {command.Arguments}";

        var previewText = new TextBlock
        {
            Text = cmdTextToDisplay,
            FontFamily = new FontFamily("Consolas, Segoe UI Mono, monospace"),
            FontSize = 12,
            TextWrapping = TextWrapping.Wrap,
            MaxLines = 3,
            TextTrimming = TextTrimming.CharacterEllipsis,
            Foreground = Application.Current.Resources["TextFillColorSecondaryBrush"] as Brush
        };
        previewBorder.Child = previewText;
        Grid.SetRow(previewBorder, 1);
        rootGrid.Children.Add(previewBorder);

        // 3. Dynamic Middle Section (Parameter Input and/or Confirmation Prompt)
        var middleStack = new StackPanel { Spacing = 12 };

        if (mode is CommandActionMode.ParameterInput or CommandActionMode.ParameterAndConfirm)
        {
            var promptText = new TextBlock
            {
                Text = i18n["MiniDock.ParamPrompt"],
                FontSize = 13
            };
            _paramTextBox = new TextBox
            {
                PlaceholderText = "输入参数...",
                CornerRadius = new CornerRadius(6)
            };
            _paramTextBox.KeyDown += (s, e) =>
            {
                if (e.Key == VirtualKey.Enter)
                {
                    e.Handled = true;
                    ConfirmAndClose();
                }
            };

            middleStack.Children.Add(promptText);
            middleStack.Children.Add(_paramTextBox);
        }

        if (mode is CommandActionMode.Confirmation or CommandActionMode.ParameterAndConfirm)
        {
            var confirmWarningBorder = new Border
            {
                Background = Application.Current.Resources["SubtleFillColorSecondaryBrush"] as Brush,
                BorderBrush = Application.Current.Resources["CardStrokeColorDefaultBrush"] as Brush,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(10, 8, 10, 8)
            };

            var warningPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
            warningPanel.Children.Add(new FontIcon
            {
                Glyph = "\uE7BA",
                FontSize = 16,
                Foreground = new SolidColorBrush(ColorHelper.FromArgb(255, 235, 120, 20))
            });

            var warningMsg = string.Format(i18n.GetString("Dialog.RunConfirm.Content", "即将执行命令: \"{0}\"\n\n是否确认执行？"), command.DisplayName, cmdTextToDisplay);
            warningPanel.Children.Add(new TextBlock
            {
                Text = warningMsg,
                FontSize = 12,
                TextWrapping = TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Center
            });

            confirmWarningBorder.Child = warningPanel;
            middleStack.Children.Add(confirmWarningBorder);
        }

        Grid.SetRow(middleStack, 2);
        rootGrid.Children.Add(middleStack);

        // 4. Action Buttons Footer
        var buttonPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 10
        };

        var cancelBtn = new Button
        {
            Content = i18n["Commands.Cancel"],
            MinWidth = 84,
            CornerRadius = new CornerRadius(6)
        };
        cancelBtn.Click += (s, e) => CancelAndClose();

        var executeBtn = new Button
        {
            Content = mode == CommandActionMode.Confirmation ? i18n["Dialog.RunConfirm.Execute"] : i18n["MiniDock.ParamExecute"],
            MinWidth = 96,
            Style = Application.Current.Resources["AccentButtonStyle"] as Style,
            CornerRadius = new CornerRadius(6)
        };
        executeBtn.Click += (s, e) => ConfirmAndClose();

        buttonPanel.Children.Add(cancelBtn);
        buttonPanel.Children.Add(executeBtn);
        Grid.SetRow(buttonPanel, 3);
        rootGrid.Children.Add(buttonPanel);

        // Key handlers on Window content
        rootGrid.KeyDown += (s, e) =>
        {
            if (e.Key == VirtualKey.Escape)
            {
                CancelAndClose();
            }
            else if (e.Key == VirtualKey.Enter)
            {
                ConfirmAndClose();
            }
        };

        this.Content = rootGrid;

        // Auto-focus TextBox when loaded
        rootGrid.Loaded += (s, e) =>
        {
            _paramTextBox?.Focus(FocusState.Programmatic);
        };

        this.Closed += (s, e) =>
        {
            if (!_isCompleted)
            {
                _isCompleted = true;
                _tcs.TrySetResult(new CommandActionResult { IsConfirmed = false });
            }
        };
    }

    private void ConfirmAndClose()
    {
        if (_isCompleted) return;
        _isCompleted = true;
        var param = _paramTextBox?.Text.Trim() ?? string.Empty;
        _tcs.TrySetResult(new CommandActionResult
        {
            IsConfirmed = true,
            ParameterValue = param
        });
        this.Close();
    }

    private void CancelAndClose()
    {
        if (_isCompleted) return;
        _isCompleted = true;
        _tcs.TrySetResult(new CommandActionResult { IsConfirmed = false });
        this.Close();
    }

    public static async Task<CommandActionResult> ShowDialogAsync(CommandItem command, CommandActionMode mode, Window? ownerWindow = null)
    {
        var window = new CommandActionWindow(command, mode, ownerWindow);
        window.Activate();
        return await window._tcs.Task;
    }
}

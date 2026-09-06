using CmdDock.Core.Models;
using CmdDock.Core.Services;
using CmdDock.Widget;
using Microsoft.Windows.Widgets;
using Microsoft.Windows.Widgets.Providers;
using System.Diagnostics;

namespace CmdDock_App.Services;

public static class WidgetNotificationService
{
    public static void NotifyWidgets(IEnumerable<CommandItem>? commands = null)
    {
        try
        {
            var widgetManager = WidgetManager.GetDefault();
            var widgetInfos = widgetManager.GetWidgetInfos();
            if (widgetInfos == null || widgetInfos.Length == 0) return;

            var list = commands?.ToList() ?? new CommandService().GetWidgetCommandsAsync().GetAwaiter().GetResult().ToList();
            var widgetCommands = list.Where(c => c.ShowInWidget).OrderBy(c => c.Order).ToList();

            foreach (var info in widgetInfos)
            {
                var context = info.WidgetContext;
                var sizeStr = context.Size switch
                {
                    WidgetSize.Small => "small",
                    WidgetSize.Large => "large",
                    _ => "medium"
                };

                var currentMode = WidgetSettings.GetViewMode();
                var currentCategory = WidgetSettings.GetSelectedCategory();
                var layoutMode = WidgetSettings.GetLayoutMode();
                var paginationStyle = WidgetSettings.GetPaginationStyle();
                var cardJson = WidgetCardBuilder.BuildCard(sizeStr, widgetCommands, I18nService.Instance["Commands.StatusReady"], currentMode, currentCategory, layoutMode, paginationStyle, pageIndex: 0, categoryPageIndex: 0);

                var options = new WidgetUpdateRequestOptions(context.Id)
                {
                    Template = cardJson,
                    Data = "{}"
                };

                widgetManager.UpdateWidget(options);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[CmdDock.App] NotifyWidgets exception: {ex.Message}");
        }
    }
}

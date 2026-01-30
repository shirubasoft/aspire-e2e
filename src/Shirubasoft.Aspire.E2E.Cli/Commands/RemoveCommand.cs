using Shirubasoft.Aspire.E2E.Cli.GlobalConfig;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace Shirubasoft.Aspire.E2E.Cli.Commands;

public sealed class RemoveCommand : Command<RemoveCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandArgument(0, "<id>")]
        [Description("The resource ID to remove")]
        public required string Id { get; set; }
    }

    public override int Execute(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        var config = GlobalConfigFile.Load();

        if (!config.RemoveResource(settings.Id))
        {
            AnsiConsole.MarkupLine($"[red]Resource '{settings.Id}' not found.[/]");
            return 1;
        }

        config.Save();
        AnsiConsole.MarkupLine($"[green]Resource '{settings.Id}' removed.[/]");
        return 0;
    }
}

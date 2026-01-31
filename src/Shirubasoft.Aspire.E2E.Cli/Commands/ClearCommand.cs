using Shirubasoft.Aspire.E2E.Common;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shirubasoft.Aspire.E2E.Cli.Commands;

public sealed class ClearCommand : Command<ClearCommand.Settings>
{
    public sealed class Settings : CommandSettings;

    public override int Execute(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        var config = new GlobalConfigFile();
        config.Save();

        AnsiConsole.MarkupLine("[green]Global configuration cleared.[/]");
        return 0;
    }
}

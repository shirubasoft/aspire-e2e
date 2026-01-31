using Shirubasoft.Aspire.E2E.Cli.GlobalConfig;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace Shirubasoft.Aspire.E2E.Cli.Commands;

public sealed class ImportCommand : Command<ImportCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandArgument(0, "<filePath>")]
        [Description("Path to the e2e-resources.json file to import")]
        public required string FilePath { get; set; }
    }

    public override int Execute(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        if (!File.Exists(settings.FilePath))
        {
            AnsiConsole.MarkupLine($"[red]File '{settings.FilePath}' not found.[/]");
            return 1;
        }

        var source = GlobalConfigFile.LoadFile(settings.FilePath);

        if (source.Aspire.Resources.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No resources found in the provided file.[/]");
            return 0;
        }

        var config = GlobalConfigFile.Load();

        foreach (var (id, entry) in source.Aspire.Resources)
        {
            config.SetResource(id, entry);
            AnsiConsole.MarkupLine($"[green]Imported resource '{id}'.[/]");
        }

        config.Save();
        AnsiConsole.MarkupLine($"[green]Successfully imported {source.Aspire.Resources.Count} resource(s).[/]");
        return 0;
    }
}

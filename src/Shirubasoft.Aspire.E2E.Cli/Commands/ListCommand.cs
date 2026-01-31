using Shirubasoft.Aspire.E2E.Common;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shirubasoft.Aspire.E2E.Cli.Commands;

public sealed class ListCommand : Command
{
    public override int Execute(CommandContext context, CancellationToken cancellationToken)
    {
        var config = GlobalConfigFile.Load();

        if (config.Aspire.Resources.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No resources registered.[/]");
            return 0;
        }

        var table = new Table();
        table.AddColumn("Id");
        table.AddColumn("Name");
        table.AddColumn("Mode");
        table.AddColumn("Project Path");
        table.AddColumn("Container Image");
        table.AddColumn("Container Tag");

        foreach (var (id, entry) in config.Aspire.Resources)
        {
            table.AddRow(
                id,
                entry.Name ?? "",
                entry.Mode,
                entry.ProjectPath ?? "",
                entry.ContainerImage ?? "",
                entry.ContainerTag ?? "");
        }

        AnsiConsole.Write(table);
        return 0;
    }
}

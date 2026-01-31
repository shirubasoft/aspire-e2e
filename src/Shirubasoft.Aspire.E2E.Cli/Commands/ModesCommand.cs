using Shirubasoft.Aspire.E2E.Common;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shirubasoft.Aspire.E2E.Cli.Commands;

public sealed class ModesCommand : Command
{
    public override int Execute(CommandContext context, CancellationToken cancellationToken)
    {
        var config = GlobalConfigFile.Load();

        if (config.Aspire.Resources.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No resources registered.[/]");
            return 0;
        }

        var resources = config.Aspire.Resources.ToList();

        var selected = AnsiConsole.Prompt(
            new MultiSelectionPrompt<string>()
                .Title("Select resources to toggle mode:")
                .NotRequired()
                .InstructionsText("[grey](Press [blue]<space>[/] to toggle, [green]<enter>[/] to apply)[/]")
                .AddChoices(resources.Select(r => $"{r.Key} ({r.Value.Mode})")));

        if (selected.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No changes made.[/]");
            return 0;
        }

        foreach (var item in selected)
        {
            var id = item[..item.LastIndexOf(" (")];
            var entry = config.Aspire.Resources[id];
            entry.Mode = entry.Mode == "Project" ? "Container" : "Project";

            AnsiConsole.MarkupLine($"  [blue]{id}[/] → [green]{entry.Mode}[/]");
        }

        config.Save();
        AnsiConsole.MarkupLine("\n[green]Configuration saved.[/]");
        return 0;
    }
}

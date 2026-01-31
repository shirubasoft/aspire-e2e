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

        [CommandOption("--merge")]
        [Description("Merge imported values into existing resources instead of replacing them")]
        public bool Merge { get; set; }
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
            AnsiConsole.MarkupLine($"\n[blue]Importing resource '{id}'...[/]");

            var existing = config.GetResource(id);

            if (existing is not null && !settings.Merge
                && !AnsiConsole.Confirm($"Resource '{id}' already exists. Overwrite?", false))
            {
                continue;
            }

            if (settings.Merge && existing is not null)
            {
                MergeEntry(existing, entry);
                AnsiConsole.MarkupLine($"[green]Merged resource '{id}'.[/]");
                continue;
            }

            if (string.IsNullOrWhiteSpace(entry.Name))
            {
                entry.Name = AnsiConsole.Ask("Name:", ToPascalCase(id));
            }

            if (string.IsNullOrWhiteSpace(entry.ProjectPath))
            {
                entry.ProjectPath = AnsiConsole.Ask<string>("Project path:");
            }

            config.SetResource(id, entry);
            AnsiConsole.MarkupLine($"[green]Imported resource '{id}'.[/]");
        }

        config.Save();
        AnsiConsole.MarkupLine("\n[green]Configuration saved.[/]");
        return 0;
    }

    private static void MergeEntry(ResourceEntry target, ResourceEntry source)
    {
        if (!string.IsNullOrWhiteSpace(source.Name)) target.Name = source.Name;
        if (source.Mode != "Project") target.Mode = source.Mode;
        if (!string.IsNullOrWhiteSpace(source.ContainerImage)) target.ContainerImage = source.ContainerImage;
        if (!string.IsNullOrWhiteSpace(source.ContainerTag)) target.ContainerTag = source.ContainerTag;
        if (!string.IsNullOrWhiteSpace(source.ProjectPath)) target.ProjectPath = source.ProjectPath;
        if (source.BuildImage) target.BuildImage = source.BuildImage;
        if (!string.IsNullOrWhiteSpace(source.BuildImageCommand)) target.BuildImageCommand = source.BuildImageCommand;
        if (!string.IsNullOrWhiteSpace(source.ImageRegistry)) target.ImageRegistry = source.ImageRegistry;
    }

    private static string ToPascalCase(string input)
    {
        return string.Concat(
            input.Split('-', '_', '.')
                .Select(part => part.Length > 0
                    ? char.ToUpperInvariant(part[0]) + part[1..]
                    : ""));
    }
}

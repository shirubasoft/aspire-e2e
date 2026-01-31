using Shirubasoft.Aspire.E2E.Common;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using System.Xml.Linq;

namespace Shirubasoft.Aspire.E2E.Cli.Commands;

public sealed class SearchCommand : Command<SearchCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandArgument(0, "[path]")]
        [Description("Root path to search for projects")]
        public string? Path { get; set; }

        [CommandOption("--depth")]
        [Description("Maximum directory depth to search")]
        [DefaultValue(10)]
        public int Depth { get; set; } = 10;
    }

    public override int Execute(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        var searchPath = settings.Path ?? Directory.GetCurrentDirectory();
        searchPath = System.IO.Path.GetFullPath(searchPath);

        AnsiConsole.MarkupLine($"[blue]Searching for projects in {searchPath}...[/]");

        var csprojFiles = FindCsprojFiles(searchPath, settings.Depth);
        var matchingProjects = csprojFiles
            .Where(HasE2EReference)
            .ToList();

        if (matchingProjects.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No projects referencing Shirubasoft.Aspire.E2E found.[/]");
            return 0;
        }

        var config = GlobalConfigFile.Load();

        foreach (var project in matchingProjects)
        {
            var projectName = System.IO.Path.GetFileNameWithoutExtension(project);
            var id = projectName.ToLowerInvariant().Replace('.', '-');

            AnsiConsole.MarkupLine($"\n[green]Found:[/] {project}");

            id = AnsiConsole.Ask("Resource ID:", id);

            if (config.GetResource(id) is not null)
            {
                if (!AnsiConsole.Confirm($"Resource '{id}' already exists. Overwrite?", false))
                {
                    continue;
                }
            }

            var name = AnsiConsole.Ask("Name:", ToPascalCase(id));
            var mode = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Mode:")
                    .AddChoices("Project", "Container"));

            var entry = new ResourceEntry
            {
                Id = id,
                Name = name,
                Mode = mode,
                ProjectPath = project,
                ContainerImage = id,
                ContainerTag = "latest",
                BuildImage = mode == "Container",
                BuildImageCommand = "dotnet publish --os linux --arch x64 /t:PublishContainer"
            };

            if (mode == "Container")
            {
                entry.ContainerImage = AnsiConsole.Ask("Container image:", entry.ContainerImage);
                entry.ContainerTag = AnsiConsole.Ask("Container tag:", entry.ContainerTag);
            }

            config.SetResource(id, entry);
        }

        config.Save();
        AnsiConsole.MarkupLine("\n[green]Configuration saved.[/]");
        return 0;
    }

    private static List<string> FindCsprojFiles(string rootPath, int maxDepth)
    {
        var results = new List<string>();
        SearchDirectory(rootPath, 0, maxDepth, results);
        return results;
    }

    private static void SearchDirectory(string path, int currentDepth, int maxDepth, List<string> results)
    {
        if (currentDepth > maxDepth)
            return;

        try
        {
            results.AddRange(Directory.GetFiles(path, "*.csproj"));

            foreach (var dir in Directory.GetDirectories(path))
            {
                var dirName = System.IO.Path.GetFileName(dir);
                if (dirName.StartsWith('.') || dirName is "bin" or "obj" or "node_modules")
                    continue;

                SearchDirectory(dir, currentDepth + 1, maxDepth, results);
            }
        }
        catch (UnauthorizedAccessException)
        {
            // Skip directories we can't access
        }
    }

    private static bool HasE2EReference(string csprojPath)
    {
        const string markerPackage = "Shirubasoft.Aspire.E2E";
        const string markerProjectFile = "Shirubasoft.Aspire.E2E.csproj";

        XDocument doc;
        try
        {
            doc = XDocument.Load(csprojPath);
        }
        catch
        {
            return false;
        }

        var ns = doc.Root?.Name.Namespace ?? XNamespace.None;

        return doc.Descendants(ns + "PackageReference")
                   .Any(e => string.Equals(e.Attribute("Include")?.Value?.Trim(), markerPackage, StringComparison.OrdinalIgnoreCase))
               || doc.Descendants(ns + "ProjectReference")
                   .Any(e => e.Attribute("Include")?.Value is { } path
                              && Path.GetFileName(path).Equals(markerProjectFile, StringComparison.OrdinalIgnoreCase));
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

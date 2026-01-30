using Shirubasoft.Aspire.E2E.Cli.GlobalConfig;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace Shirubasoft.Aspire.E2E.Cli.Commands;

public sealed class UpdateCommand : Command<UpdateCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandArgument(0, "<id>")]
        [Description("The resource ID to update")]
        public required string Id { get; set; }

        [CommandOption("--name")]
        [Description("Display name for the resource")]
        public string? Name { get; set; }

        [CommandOption("--mode")]
        [Description("Resource mode (Project or Container)")]
        public string? Mode { get; set; }

        [CommandOption("--container-image")]
        [Description("Container image name")]
        public string? ContainerImage { get; set; }

        [CommandOption("--container-tag")]
        [Description("Container image tag")]
        public string? ContainerTag { get; set; }

        [CommandOption("--project-path")]
        [Description("Path to the project file")]
        public string? ProjectPath { get; set; }

        [CommandOption("--build-image")]
        [Description("Whether to build the container image")]
        public bool? BuildImage { get; set; }

        [CommandOption("--build-image-command")]
        [Description("Command to build the container image")]
        public string? BuildImageCommand { get; set; }
    }

    public override int Execute(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        var config = GlobalConfigFile.Load();
        var entry = config.GetResource(settings.Id);

        if (entry is null)
        {
            AnsiConsole.MarkupLine($"[red]Resource '{settings.Id}' not found.[/]");
            return 1;
        }

        var isInteractive = settings.Name is null
            && settings.Mode is null
            && settings.ContainerImage is null
            && settings.ContainerTag is null
            && settings.ProjectPath is null
            && settings.BuildImage is null
            && settings.BuildImageCommand is null;

        if (isInteractive)
        {
            entry.Name = AnsiConsole.Ask("Name:", entry.Name ?? "");
            entry.Mode = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Mode:")
                    .AddChoices("Project", "Container"));
            entry.ProjectPath = AnsiConsole.Ask("Project path:", entry.ProjectPath ?? "");
            entry.ContainerImage = AnsiConsole.Ask("Container image:", entry.ContainerImage ?? "");
            entry.ContainerTag = AnsiConsole.Ask("Container tag:", entry.ContainerTag ?? "");
            entry.BuildImage = AnsiConsole.Confirm("Build image?", entry.BuildImage);
            if (entry.BuildImage)
            {
                entry.BuildImageCommand = AnsiConsole.Ask("Build image command:", entry.BuildImageCommand ?? "dotnet publish --os linux --arch x64 /t:PublishContainer");
            }
        }
        else
        {
            if (settings.Name is not null) entry.Name = settings.Name;
            if (settings.Mode is not null) entry.Mode = settings.Mode;
            if (settings.ContainerImage is not null) entry.ContainerImage = settings.ContainerImage;
            if (settings.ContainerTag is not null) entry.ContainerTag = settings.ContainerTag;
            if (settings.ProjectPath is not null) entry.ProjectPath = settings.ProjectPath;
            if (settings.BuildImage is not null) entry.BuildImage = settings.BuildImage.Value;
            if (settings.BuildImageCommand is not null) entry.BuildImageCommand = settings.BuildImageCommand;
        }

        config.SetResource(settings.Id, entry);
        config.Save();
        AnsiConsole.MarkupLine($"[green]Resource '{settings.Id}' updated.[/]");
        return 0;
    }
}

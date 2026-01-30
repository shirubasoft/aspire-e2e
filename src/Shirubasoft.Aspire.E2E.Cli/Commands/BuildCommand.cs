using System.ComponentModel;

using CliWrap;
using CliWrap.Buffered;

using Shirubasoft.Aspire.E2E.Cli.GlobalConfig;

using Spectre.Console;
using Spectre.Console.Cli;

namespace Shirubasoft.Aspire.E2E.Cli.Commands;

public sealed class BuildCommand : AsyncCommand<BuildCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandArgument(0, "<id>")]
        [Description("The resource ID to build")]
        public required string Id { get; set; }

        [CommandOption("--force")]
        [Description("Force rebuild even if the image already exists")]
        public bool Force { get; set; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        var config = GlobalConfigFile.Load();
        var entry = config.GetResource(settings.Id);

        if (entry is null)
        {
            AnsiConsole.MarkupLine($"[red]Resource '{settings.Id}' not found.[/]");
            return 1;
        }

        if (string.IsNullOrEmpty(entry.ProjectPath))
        {
            AnsiConsole.MarkupLine($"[red]Resource '{settings.Id}' has no project path.[/]");
            return 1;
        }

        var projectDir = Path.GetDirectoryName(Path.GetFullPath(entry.ProjectPath))!;

        // Get git branch and commit
        var branchResult = await CliWrap.Cli.Wrap("git")
            .WithArguments("rev-parse --abbrev-ref HEAD")
            .WithWorkingDirectory(projectDir)
            .ExecuteBufferedAsync();
        var branch = branchResult.StandardOutput.Trim();

        var commitResult = await CliWrap.Cli.Wrap("git")
            .WithArguments("rev-parse --short HEAD")
            .WithWorkingDirectory(projectDir)
            .ExecuteBufferedAsync();
        var commit = commitResult.StandardOutput.Trim();

        var tag = branch;
        var commitTag = commit;

        var containerRuntime = await GetContainerRuntime();
        if (!settings.Force)
        {
            // Check if image already exists with this commit tag
            try
            {
                var inspectResult = await CliWrap.Cli.Wrap(containerRuntime)
                    .WithArguments($"image inspect {entry.ContainerImage}:{commitTag}")
                    .ExecuteBufferedAsync();

                AnsiConsole.MarkupLine($"[yellow]Image {entry.ContainerImage}:{commitTag} already exists. Use --force to rebuild.[/]");
                return 0;
            }
            catch
            {
                // Image doesn't exist, proceed with build
            }
        }

        AnsiConsole.MarkupLine($"[blue]Building {settings.Id} (branch: {branch}, commit: {commit})...[/]");

        var buildCommand = entry.BuildImageCommand ?? "dotnet publish --os linux --arch x64 /t:PublishContainer";
        var parts = buildCommand.Split(' ', 2);

        var result = await CliWrap.Cli.Wrap(parts[0])
            .WithArguments(parts.Length > 1 ? parts[1] : "")
            .WithWorkingDirectory(projectDir)
            .WithStandardOutputPipe(PipeTarget.ToDelegate(line => AnsiConsole.WriteLine(line)))
            .WithStandardErrorPipe(PipeTarget.ToDelegate(line => AnsiConsole.MarkupLine($"[red]{line.EscapeMarkup()}[/]")))
            .WithValidation(CommandResultValidation.None)
            .ExecuteBufferedAsync();

        if (result.ExitCode != 0)
        {
            AnsiConsole.MarkupLine("[red]Build failed.[/]");
            return 1;
        }

        // Tag with branch name and commit hash
        var image = entry.ContainerImage ?? settings.Id;

        await CliWrap.Cli.Wrap(containerRuntime)
            .WithArguments($"tag {image}:latest {image}:{tag}")
            .ExecuteBufferedAsync();

        await CliWrap.Cli.Wrap(containerRuntime)
            .WithArguments($"tag {image}:latest {image}:{commitTag}")
            .ExecuteBufferedAsync();

        // Update config with the current branch tag
        entry.ContainerTag = tag;
        config.SetResource(settings.Id, entry);
        config.Save();

        AnsiConsole.MarkupLine($"[green]Built and tagged: {image}:{tag}, {image}:{commitTag}[/]");
        return 0;
    }

    private static async Task<string> GetContainerRuntime()
    {
        // Prefer docker since dotnet SDK PublishContainer targets docker by default
        try
        {
            await CliWrap.Cli.Wrap("docker").WithArguments("--version").ExecuteBufferedAsync();
            return "docker";
        }
        catch
        {
            return "podman";
        }
    }
}

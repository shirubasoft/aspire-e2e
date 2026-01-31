using Shirubasoft.Aspire.E2E.Cli.GlobalConfig;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace Shirubasoft.Aspire.E2E.Cli.Commands;

public sealed class OverrideSetCommand : Command<OverrideSetCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandArgument(0, "<key>")]
        [Description("Override key (Mode or BuildImage)")]
        public required string Key { get; set; }

        [CommandArgument(1, "<value>")]
        [Description("Override value")]
        public required string Value { get; set; }
    }

    public override int Execute(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        var config = GlobalConfigFile.LoadFile(GlobalConfigFile.ResolveSavePath());
        config.Aspire.Overrides ??= new OverrideSettings();

        switch (settings.Key)
        {
            case "Mode":
                config.Aspire.Overrides.Mode = settings.Value;
                break;
            case "BuildImage":
                if (!bool.TryParse(settings.Value, out var buildImage))
                {
                    AnsiConsole.MarkupLine("[red]BuildImage must be 'true' or 'false'.[/]");
                    return 1;
                }
                config.Aspire.Overrides.BuildImage = buildImage;
                break;
            default:
                AnsiConsole.MarkupLine($"[red]Unknown override key '{settings.Key}'. Valid keys: Mode, BuildImage.[/]");
                return 1;
        }

        config.Save();
        AnsiConsole.MarkupLine($"[green]Override '{settings.Key}' set to '{settings.Value}'.[/]");
        return 0;
    }
}

public sealed class OverrideSetRegistryCommand : Command<OverrideSetRegistryCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandArgument(0, "<from>")]
        [Description("Source registry to rewrite")]
        public required string From { get; set; }

        [CommandArgument(1, "<to>")]
        [Description("Target registry")]
        public required string To { get; set; }
    }

    public override int Execute(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        var config = GlobalConfigFile.LoadFile(GlobalConfigFile.ResolveSavePath());
        config.Aspire.Overrides ??= new OverrideSettings();
        config.Aspire.Overrides.ImageRegistryRewrites ??= new Dictionary<string, string>();
        config.Aspire.Overrides.ImageRegistryRewrites[settings.From] = settings.To;

        config.Save();
        AnsiConsole.MarkupLine($"[green]Registry rewrite '{settings.From}' → '{settings.To}' added.[/]");
        return 0;
    }
}

public sealed class OverrideRemoveCommand : Command<OverrideRemoveCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandArgument(0, "<key>")]
        [Description("Override key to remove (Mode or BuildImage)")]
        public required string Key { get; set; }
    }

    public override int Execute(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        var config = GlobalConfigFile.LoadFile(GlobalConfigFile.ResolveSavePath());

        if (config.Aspire.Overrides is null)
        {
            AnsiConsole.MarkupLine($"[yellow]No overrides configured.[/]");
            return 0;
        }

        switch (settings.Key)
        {
            case "Mode":
                config.Aspire.Overrides.Mode = null;
                break;
            case "BuildImage":
                config.Aspire.Overrides.BuildImage = null;
                break;
            default:
                AnsiConsole.MarkupLine($"[red]Unknown override key '{settings.Key}'. Valid keys: Mode, BuildImage.[/]");
                return 1;
        }

        config.Save();
        AnsiConsole.MarkupLine($"[green]Override '{settings.Key}' removed.[/]");
        return 0;
    }
}

public sealed class OverrideRemoveRegistryCommand : Command<OverrideRemoveRegistryCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandArgument(0, "<from>")]
        [Description("Source registry to remove")]
        public required string From { get; set; }
    }

    public override int Execute(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        var config = GlobalConfigFile.LoadFile(GlobalConfigFile.ResolveSavePath());

        if (config.Aspire.Overrides?.ImageRegistryRewrites is null ||
            !config.Aspire.Overrides.ImageRegistryRewrites.Remove(settings.From))
        {
            AnsiConsole.MarkupLine($"[yellow]Registry rewrite '{settings.From}' not found.[/]");
            return 0;
        }

        config.Save();
        AnsiConsole.MarkupLine($"[green]Registry rewrite '{settings.From}' removed.[/]");
        return 0;
    }
}

public sealed class OverrideClearCommand : Command<OverrideClearCommand.Settings>
{
    public sealed class Settings : CommandSettings;

    public override int Execute(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        var config = GlobalConfigFile.LoadFile(GlobalConfigFile.ResolveSavePath());
        config.Aspire.Overrides = null;
        config.Save();
        AnsiConsole.MarkupLine("[green]All overrides cleared.[/]");
        return 0;
    }
}

public sealed class OverrideListCommand : Command<OverrideListCommand.Settings>
{
    public sealed class Settings : CommandSettings;

    public override int Execute(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        var config = GlobalConfigFile.LoadFile(GlobalConfigFile.ResolveSavePath());

        if (config.Aspire.Overrides is null)
        {
            AnsiConsole.MarkupLine("[yellow]No overrides configured.[/]");
            return 0;
        }

        var table = new Table();
        table.AddColumn("Key");
        table.AddColumn("Value");

        if (config.Aspire.Overrides.Mode is not null)
        {
            table.AddRow("Mode", config.Aspire.Overrides.Mode);
        }

        if (config.Aspire.Overrides.BuildImage is not null)
        {
            table.AddRow("BuildImage", config.Aspire.Overrides.BuildImage.Value.ToString());
        }

        if (config.Aspire.Overrides.ImageRegistryRewrites is not null)
        {
            foreach (var (from, to) in config.Aspire.Overrides.ImageRegistryRewrites)
            {
                table.AddRow($"ImageRegistryRewrite: {from}", to);
            }
        }

        AnsiConsole.Write(table);
        return 0;
    }
}

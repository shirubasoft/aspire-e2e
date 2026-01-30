using Shirubasoft.Aspire.E2E.Cli.GlobalConfig;
using Spectre.Console.Cli;

namespace Shirubasoft.Aspire.E2E.Cli.Commands;

public sealed class GetConfigCommand : Command<GetConfigCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandArgument(0, "<id>")]
        public required string Id { get; set; }

        [CommandArgument(1, "<key>")]
        public required string Key { get; set; }
    }

    public override int Execute(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        var config = GlobalConfigFile.Load();

        if (!config.Aspire.Resources.TryGetValue(settings.Id, out var entry))
            return 1;

        var value = settings.Key switch
        {
            "Mode" => entry.Mode,
            "Name" => entry.Name,
            "ContainerImage" => entry.ContainerImage,
            "ContainerTag" => entry.ContainerTag,
            "ProjectPath" => entry.ProjectPath,
            "BuildImage" => entry.BuildImage.ToString(),
            "BuildImageCommand" => entry.BuildImageCommand,
            "SkipImageBuild" => entry.SkipImageBuild.ToString(),
            "ImageRegistry" => entry.ImageRegistry,
            _ => null
        };

        if (value is null)
            return 1;

        Console.Write(value);
        return 0;
    }
}

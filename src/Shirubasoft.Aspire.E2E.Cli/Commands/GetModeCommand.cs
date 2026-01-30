using Shirubasoft.Aspire.E2E.Cli.GlobalConfig;
using Spectre.Console.Cli;

namespace Shirubasoft.Aspire.E2E.Cli.Commands;

public sealed class GetModeCommand : Command<GetModeCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandArgument(0, "<id>")]
        public required string Id { get; set; }
    }

    public override int Execute(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        var config = GlobalConfigFile.Load();

        if (!config.Aspire.Resources.TryGetValue(settings.Id, out var entry))
            return 1;

        Console.Write(entry.Mode);
        return 0;
    }
}

using Shirubasoft.Aspire.E2E.Cli.Commands;
using Spectre.Console.Cli;

var app = new CommandApp();

app.Configure(config =>
{
    config.SetApplicationName("aspire-e2e");

    config.AddCommand<SearchCommand>("search")
        .WithDescription("Search for projects referencing Shirubasoft.Aspire.E2E and register them");

    config.AddCommand<ListCommand>("list")
        .WithDescription("List all registered resources");

    config.AddCommand<RemoveCommand>("remove")
        .WithDescription("Remove a registered resource");

    config.AddCommand<UpdateCommand>("update")
        .WithDescription("Update a registered resource");

    config.AddCommand<BuildCommand>("build")
        .WithDescription("Build the container image for a registered resource");

    config.AddCommand<GetModeCommand>("get-mode")
        .WithDescription("Get the mode of a registered resource (machine-readable)");

    config.AddCommand<GetProjectPathCommand>("get-project-path")
        .WithDescription("Get the project path of a registered resource (machine-readable)");
});

return app.Run(args);

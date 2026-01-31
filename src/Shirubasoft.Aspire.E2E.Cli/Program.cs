using Shirubasoft.Aspire.E2E.Cli.Commands;
using Spectre.Console.Cli;

var app = new CommandApp();

app.Configure(config =>
{
    config.SetApplicationName("a2a");

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

    config.AddCommand<GetConfigCommand>("get-config")
        .WithDescription("Get a configuration property of a registered resource (machine-readable)");

    config.AddCommand<ImportCommand>("import")
        .WithDescription("Import resources from an e2e-resources.json file into the global configuration");

    config.AddCommand<ModesCommand>("modes")
        .WithDescription("View and toggle the mode (Project/Container) of registered resources");

    config.AddCommand<ClearCommand>("clear")
        .WithDescription("Delete all resources from the global configuration");

    config.AddBranch("override", branch =>
    {
        branch.SetDescription("Manage global overrides");

        branch.AddCommand<OverrideSetCommand>("set")
            .WithDescription("Set an override (Mode or BuildImage)");

        branch.AddCommand<OverrideSetRegistryCommand>("set-registry")
            .WithDescription("Add a registry rewrite rule");

        branch.AddCommand<OverrideRemoveCommand>("remove")
            .WithDescription("Remove an override");

        branch.AddCommand<OverrideRemoveRegistryCommand>("remove-registry")
            .WithDescription("Remove a registry rewrite rule");

        branch.AddCommand<OverrideSetImageCommand>("set-image")
            .WithDescription("Add an image rewrite rule");

        branch.AddCommand<OverrideRemoveImageCommand>("remove-image")
            .WithDescription("Remove an image rewrite rule");

        branch.AddCommand<OverrideListCommand>("list")
            .WithDescription("List current overrides");

        branch.AddCommand<OverrideClearCommand>("clear")
            .WithDescription("Remove all overrides");
    });
});

return app.Run(args);

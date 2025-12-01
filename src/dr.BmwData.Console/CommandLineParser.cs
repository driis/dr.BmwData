namespace dr.BmwData.Console;

public enum Command
{
    Help,
    List,
    Create,
    Get,
    Delete
}

public record CommandLineArgs(Command Command, string? ContainerId = null, string[]? TechnicalDescriptors = null)
{
    public static CommandLineArgs Parse(string[] args)
    {
        if (args.Length == 0)
            return new CommandLineArgs(Command.Help);

        var command = args[0].ToLowerInvariant();

        return command switch
        {
            "help" or "-h" or "--help" or "-?" => new CommandLineArgs(Command.Help),
            "list" => new CommandLineArgs(Command.List),
            "create" when args.Length > 1 => new CommandLineArgs(Command.Create, TechnicalDescriptors: args[1..]),
            "create" => throw new ArgumentException("Create command requires at least one technical descriptor."),
            "get" when args.Length > 1 => new CommandLineArgs(Command.Get, ContainerId: args[1]),
            "get" => throw new ArgumentException("Get command requires a container ID."),
            "delete" when args.Length > 1 => new CommandLineArgs(Command.Delete, ContainerId: args[1]),
            "delete" => throw new ArgumentException("Delete command requires a container ID."),
            _ => throw new ArgumentException($"Unknown command: {args[0]}")
        };
    }

    public static void PrintHelp()
    {
        System.Console.WriteLine("""
            BMW CarData Console - Container Management

            Usage: dr.BmwData.Console <command> [arguments]

            Commands:
              help                          Show this help message
              list                          List all containers
              create <descriptor> [...]     Create a new container with the specified technical descriptors
              get <containerId>             Get container details (outputs JSON)
              delete <containerId>          Delete a container

            Examples:
              dr.BmwData.Console list
              dr.BmwData.Console create FUEL_LEVEL MILEAGE CHARGING_STATUS
              dr.BmwData.Console get abc123-container-id
              dr.BmwData.Console delete abc123-container-id

            Technical Descriptors:
              Common descriptors include: FUEL_LEVEL, MILEAGE, CHARGING_STATUS, DOOR_LOCK_STATE,
              LOCATION, TIRE_PRESSURE, etc. Refer to BMW CarData API documentation for full list.

            Authentication:
              If not authenticated, the app will initiate an interactive device code flow.
              You can configure a refresh token in appsettings.json or via environment variable
              BmwData__RefreshToken to skip interactive login.
            """);
    }
}

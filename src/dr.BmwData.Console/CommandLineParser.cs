namespace dr.BmwData.Console;

public enum Command
{
    Help,
    List,
    Create,
    Get,
    Delete,
    Mappings,
    GetData
}

public record CommandLineArgs(
    Command Command,
    string? ContainerId = null,
    string[]? TechnicalDescriptors = null,
    string? Vin = null)
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
            "mappings" => new CommandLineArgs(Command.Mappings),
            "get-data" when args.Length > 2 => new CommandLineArgs(Command.GetData, ContainerId: args[2], Vin: args[1]),
            "get-data" => throw new ArgumentException("get-data command requires a VIN and container ID. Usage: get-data <vin> <containerId>"),
            _ => throw new ArgumentException($"Unknown command: {args[0]}")
        };
    }

    public static void PrintHelp()
    {
        System.Console.WriteLine("""
            BMW CarData Console

            Usage: dr.BmwData.Console <command> [arguments]

            Container Commands:
              list                          List all containers
              create <descriptor> [...]     Create a new container with the specified technical descriptors
              get <containerId>             Get container details (outputs JSON)
              delete <containerId>          Delete a container

            Telemetry Commands:
              mappings                      List all mapped vehicles
              get-data <vin> <containerId>  Get telematic data for a vehicle

            Other Commands:
              help                          Show this help message

            Examples:
              dr.BmwData.Console list
              dr.BmwData.Console create FUEL_LEVEL MILEAGE CHARGING_STATUS
              dr.BmwData.Console get abc123-container-id
              dr.BmwData.Console delete abc123-container-id
              dr.BmwData.Console mappings
              dr.BmwData.Console get-data WBA12345678901234 abc123-container-id

            Technical Descriptors:
              Common descriptors include: FUEL_LEVEL, MILEAGE, CHARGING_STATUS, DOOR_LOCK_STATE,
              LOCATION, TIRE_PRESSURE, etc. Refer to BMW CarData API documentation for full list.

            Authentication:
              If not authenticated, the app will initiate an interactive device code flow.
              The refresh token is saved automatically to ~/.bmwdata/refresh_token.
            """);
    }
}

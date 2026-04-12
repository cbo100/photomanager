namespace PhotoManager.Cli.Parsing;

/// <summary>
/// Lightweight argument parser. Recognises --flag, --key value, --key=value, and short -f flags.
/// Flag names (those that never consume a value) must be declared up-front so the parser
/// does not accidentally consume the next positional as a value.
/// </summary>
internal static class ArgParser
{
    public sealed class Result(
        List<string> positionals,
        Dictionary<string, string?> options,
        bool helpRequested)
    {
        public IReadOnlyList<string> Positionals { get; } = positionals;
        public bool HelpRequested { get; } = helpRequested;

        public bool HasFlag(string name) => options.ContainsKey(name);

        public string? GetOption(string name) =>
            options.TryGetValue(name, out var v) ? v : null;

        public string GetOptionOrDefault(string name, string defaultValue) =>
            GetOption(name) ?? defaultValue;
    }

    /// <summary>
    /// Parse command-line arguments into positionals and options. Recognises --flag, --key value, --key=value, and short -f flags.
    /// </summary>
    /// <param name="args">Arguments after the command name has been stripped.</param>
    /// <param name="flagNames">Names of boolean flags (short or long, without dashes) that never take a value.</param>
    public static Result Parse(string[] args, IReadOnlySet<string> flagNames)
    {
        var positionals = new List<string>();
        var options = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        bool help = false;

        int i = 0;
        while (i < args.Length)
        {
            var arg = args[i];

            if (arg is "--help" or "-h" or "-?")
            {
                help = true;
                i++;
                continue;
            }

            if (arg.StartsWith("--"))
            {
                var key = arg[2..];

                // --key=value inline form
                var eqIdx = key.IndexOf('=');
                if (eqIdx >= 0)
                {
                    options[key[..eqIdx]] = key[(eqIdx + 1)..];
                    i++;
                    continue;
                }

                if (flagNames.Contains(key))
                {
                    options[key] = null;
                    i++;
                }
                else if (i + 1 < args.Length && !args[i + 1].StartsWith('-'))
                {
                    options[key] = args[i + 1];
                    i += 2;
                }
                else
                {
                    options[key] = null; // treat as flag if no value follows
                    i++;
                }
            }
            else if (arg.Length == 2 && arg[0] == '-')
            {
                var key = arg[1..];

                if (flagNames.Contains(key))
                {
                    options[key] = null;
                    i++;
                }
                else if (i + 1 < args.Length && !args[i + 1].StartsWith('-'))
                {
                    options[key] = args[i + 1];
                    i += 2;
                }
                else
                {
                    options[key] = null;
                    i++;
                }
            }
            else
            {
                positionals.Add(arg);
                i++;
            }
        }

        return new Result(positionals, options, help);
    }
}

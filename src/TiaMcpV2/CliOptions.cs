namespace TiaMcpV2
{
    public class CliOptions
    {
        public int? TiaMajorVersion { get; set; }
        public int? Logging { get; set; }

        public static CliOptions ParseArgs(string[] args)
        {
            var options = new CliOptions();
            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i].ToLowerInvariant())
                {
                    case "-tia-major-version":
                    case "--tia-major-version":
                    case "--tia-version":
                        if (i + 1 < args.Length && int.TryParse(args[i + 1], out int v))
                        {
                            options.TiaMajorVersion = v;
                            i++;
                        }
                        break;

                    case "-logging":
                    case "--logging":
                    case "--log-level":
                        if (i + 1 < args.Length && int.TryParse(args[i + 1], out int l))
                        {
                            options.Logging = l;
                            i++;
                        }
                        break;
                }
            }
            return options;
        }
    }
}

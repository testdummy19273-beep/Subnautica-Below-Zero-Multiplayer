namespace Subnautica.Client.Core
{
    using System;

    /// <summary>
    /// A host/join request passed on the game's command line by the launcher:
    ///   -bzmp-host &lt;worldGuid&gt;       host an existing world
    ///   -bzmp-host new:&lt;gameMode&gt;    create a world (Survival, Freedom, Hardcore, Creative or the number) and host it
    ///   -bzmp-join &lt;ip[:port]&gt;       join a server
    /// It is consumed once, when the main menu has loaded.
    /// </summary>
    public class LaunchRequest
    {
        public const string HostArgument = "-bzmp-host";

        public const string JoinArgument = "-bzmp-join";

        public const string NewWorldPrefix = "new:";

        public bool IsHost { get; private set; }

        public bool IsJoin { get; private set; }

        public string Value { get; private set; }

        public bool IsNewWorld => this.IsHost && this.Value.StartsWith(NewWorldPrefix, StringComparison.OrdinalIgnoreCase);

        public string NewWorldGameMode => this.IsNewWorld ? this.Value.Substring(NewWorldPrefix.Length) : null;

        private static LaunchRequest Pending { get; set; }

        private static bool IsParsed { get; set; }

        /// <summary>
        /// Returns the request from the command line the first time it is called, null afterwards (or if there is none).
        /// </summary>
        public static LaunchRequest Consume()
        {
            if (!IsParsed)
            {
                IsParsed = true;
                Pending = Parse(Environment.GetCommandLineArgs());
            }

            var request = Pending;
            Pending = null;
            return request;
        }

        public static LaunchRequest Parse(string[] args)
        {
            if (args == null)
            {
                return null;
            }

            for (int i = 0; i < args.Length - 1; i++)
            {
                var value = args[i + 1].Trim().Trim('"');
                if (value.Length == 0)
                {
                    continue;
                }

                if (string.Equals(args[i], HostArgument, StringComparison.OrdinalIgnoreCase))
                {
                    return new LaunchRequest { IsHost = true, Value = value };
                }

                if (string.Equals(args[i], JoinArgument, StringComparison.OrdinalIgnoreCase))
                {
                    return new LaunchRequest { IsJoin = true, Value = value };
                }
            }

            return null;
        }
    }
}

using System;

namespace NavalBattles.Runtime.UnityIntegration.Bootstrap
{
    public static class GameProcessRoleResolver
    {
        private const string ServerArgument = "-server";
        private const string ClientArgument = "-client";
        public static bool TryResolve(
            bool isEditor,
            string[] arguments,
            string projectRoot,
            out GameProcessRole role,
            out string error)
        {
            role = default;
            error = null;
            bool isServer = ContainsArgument(arguments, ServerArgument);
            bool isClient = ContainsArgument(arguments, ClientArgument);

            if (isServer && isClient)
            {
                error = $"Process arguments cannot contain both {ServerArgument} and {ClientArgument}.";
                return false;
            }

            if (isServer)
            {
                role = GameProcessRole.Server;
                return true;
            }

            if (isClient)
            {
                role = GameProcessRole.Client;
                return true;
            }

            if (isEditor)
                return LocalInstanceRoleConfiguration.TryRead(projectRoot, out role, out error);

            role = GameProcessRole.Client;
            return true;
        }

        private static bool ContainsArgument(string[] arguments, string expected)
        {
            if (arguments == null)
                return false;

            foreach (string argument in arguments)
            {
                if (string.Equals(argument, expected, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }
    }
}

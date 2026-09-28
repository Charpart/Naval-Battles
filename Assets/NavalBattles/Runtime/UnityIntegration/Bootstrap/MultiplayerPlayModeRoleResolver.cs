using System;

namespace NavalBattles.Runtime.UnityIntegration.Bootstrap
{
    public static class MultiplayerPlayModeRoleResolver
    {
        private const string ServerArgument = "-server";
        private const string ClientArgument = "-client";
        private const string PlayerNameArgument = "-name";

        public static GameProcessRole Resolve(bool isEditor, string[] arguments)
        {
            bool isServer = ContainsArgument(arguments, ServerArgument);
            bool isClient = ContainsArgument(arguments, ClientArgument);

            if (isServer && isClient)
            {
                throw new ArgumentException(
                    $"Process arguments cannot contain both {ServerArgument} and {ClientArgument}.",
                    nameof(arguments));
            }

            if (isServer)
                return GameProcessRole.Server;

            if (isClient)
                return GameProcessRole.Client;

            return isEditor && string.IsNullOrEmpty(GetPlayerName(arguments))
                ? GameProcessRole.Server
                : GameProcessRole.Client;
        }

        public static string GetPlayerName(string[] arguments)
        {
            if (arguments == null)
                return null;

            for (int index = 0; index < arguments.Length - 1; index++)
            {
                if (string.Equals(
                    arguments[index],
                    PlayerNameArgument,
                    StringComparison.OrdinalIgnoreCase))
                {
                    string playerName = arguments[index + 1];

                    return string.IsNullOrWhiteSpace(playerName)
                        || playerName.StartsWith("-", StringComparison.Ordinal)
                            ? null
                            : playerName;
                }
            }

            return null;
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

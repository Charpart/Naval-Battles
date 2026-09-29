using System;
using System.Globalization;
using System.IO;
using NavalBattles.Runtime.Client.Identity;
using UnityEngine;

namespace NavalBattles.Runtime.UnityIntegration.Bootstrap
{
    public static class LocalInstanceRoleConfiguration
    {
        public const string relativePath = "UserSettings/NavalBattlesInstance.json";

        public static bool TryRead(
            string projectRoot,
            out GameProcessRole role,
            out string error)
        {
            role = default;
            string path;

            try
            {
                path = GetPath(projectRoot);
            }
            catch (Exception exception)
            {
                error = exception.Message;
                return false;
            }

            if (File.Exists(path) == false)
            {
                error = CreateError(path, "The configuration file does not exist.");
                return false;
            }

            try
            {
                string json = File.ReadAllText(path);
                var data = JsonUtility.FromJson<ConfigurationData>(json);

                if (data != null && TryParseRole(data.role, out role))
                {
                    error = null;
                    return true;
                }

                error = CreateError(path, "The role value is missing or unknown.");
                return false;
            }
            catch (Exception exception) when (
                exception is IOException ||
                exception is UnauthorizedAccessException ||
                exception is ArgumentException)
            {
                error = CreateError(path, exception.Message);
                return false;
            }
        }

        public static void Write(string projectRoot, GameProcessRole role)
        {
            if (IsSupported(role) == false)
                throw new ArgumentOutOfRangeException(nameof(role), role, "Role must be Server or Client.");

            string path = GetPath(projectRoot);
            ConfigurationData data = ReadOrCreateForRoleWrite(path);
            data.role = role.ToString();

            if (role == GameProcessRole.Client)
                EnsureClientIdentity(data);

            Write(path, data);
        }

        public static bool TryCreateClientIdentityStore(
            string projectRoot,
            out IClientIdentityStore identityStore,
            out string error)
        {
            identityStore = null;
            string path = null;

            try
            {
                path = GetPath(projectRoot);
                ConfigurationData data = ReadOrCreate(path);
                Guid clientId = EnsureClientIdentity(data);
                ulong nextCommandId = ParseNextCommandIdOrThrow(data.nextCommandId);
                Write(path, data);
                identityStore = new JsonClientIdentityStore(path, clientId, nextCommandId);
                error = null;
                return true;
            }
            catch (Exception exception) when (
                exception is IOException ||
                exception is UnauthorizedAccessException ||
                exception is ArgumentException ||
                exception is OverflowException)
            {
                string pathSuffix = string.IsNullOrWhiteSpace(path) ? string.Empty : $" File: {path}.";
                error = $"Cannot load the client identity. {exception.Message}{pathSuffix}";
                return false;
            }
        }

        public static string GetProjectRoot(string assetsPath)
        {
            if (string.IsNullOrWhiteSpace(assetsPath))
                throw new ArgumentException("Assets path is required.", nameof(assetsPath));

            string fullAssetsPath = Path.GetFullPath(assetsPath);
            DirectoryInfo parent = Directory.GetParent(fullAssetsPath);

            if (parent == null)
                throw new ArgumentException($"Cannot resolve a project root from '{assetsPath}'.", nameof(assetsPath));

            return parent.FullName;
        }

        public static string GetPath(string projectRoot)
        {
            if (string.IsNullOrWhiteSpace(projectRoot))
                throw new ArgumentException("Project root is required.", nameof(projectRoot));

            return Path.GetFullPath(Path.Combine(projectRoot, relativePath));
        }

        private static bool IsSupported(GameProcessRole role)
        {
            return role == GameProcessRole.Server || role == GameProcessRole.Client;
        }

        private static bool TryParseRole(string value, out GameProcessRole role)
        {
            if (string.Equals(value, nameof(GameProcessRole.Server), StringComparison.OrdinalIgnoreCase))
            {
                role = GameProcessRole.Server;
                return true;
            }

            if (string.Equals(value, nameof(GameProcessRole.Client), StringComparison.OrdinalIgnoreCase))
            {
                role = GameProcessRole.Client;
                return true;
            }

            role = default;
            return false;
        }

        private static string CreateError(string path, string reason)
        {
            string pathSuffix = string.IsNullOrWhiteSpace(path) ? string.Empty : $" File: {path}.";
            return $"{reason} Configure this Editor as Server or Client through " +
                $"Naval Battles > Instance Role.{pathSuffix} Supported values: Server, Client.";
        }

        private static ConfigurationData ReadOrCreate(string path)
        {
            if (File.Exists(path) == false)
                return new ConfigurationData();

            string json = File.ReadAllText(path);
            return JsonUtility.FromJson<ConfigurationData>(json) ?? new ConfigurationData();
        }

        private static ConfigurationData ReadOrCreateForRoleWrite(string path)
        {
            try
            {
                return ReadOrCreate(path);
            }
            catch (ArgumentException)
            {
                return new ConfigurationData();
            }
        }

        private static Guid EnsureClientIdentity(ConfigurationData data)
        {
            if (Guid.TryParse(data.clientId, out Guid clientId) == false || clientId == Guid.Empty)
            {
                clientId = Guid.NewGuid();
                data.clientId = clientId.ToString("D");
                data.nextCommandId = "1";
                return clientId;
            }

            ulong nextCommandId = ParseNextCommandIdOrThrow(data.nextCommandId);
            data.nextCommandId = nextCommandId.ToString(CultureInfo.InvariantCulture);
            return clientId;
        }

        private static ulong ParseNextCommandIdOrThrow(string value)
        {
            if (ulong.TryParse(
                value,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out ulong nextCommandId) && nextCommandId > 0)
            {
                return nextCommandId;
            }

            throw new InvalidDataException("The persisted nextCommandId must be a positive integer.");
        }

        private static void Write(string path, ConfigurationData data)
        {
            string directory = Path.GetDirectoryName(path);
            Directory.CreateDirectory(directory);
            string temporaryPath = path + ".tmp";
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(temporaryPath, json);

            try
            {
                if (File.Exists(path))
                    File.Replace(temporaryPath, path, null);
                else
                    File.Move(temporaryPath, path);
            }
            finally
            {
                if (File.Exists(temporaryPath))
                    File.Delete(temporaryPath);
            }
        }

        [Serializable]
        private sealed class ConfigurationData
        {
            public string role;
            public string clientId;
            public string nextCommandId;
        }

        private sealed class JsonClientIdentityStore : IClientIdentityStore
        {
            private readonly string _path;
            private readonly object _syncRoot = new object();
            private ulong _nextCommandId;

            public Guid clientId { get; }

            public JsonClientIdentityStore(string path, Guid clientId, ulong nextCommandId)
            {
                _path = path;
                this.clientId = clientId;
                _nextCommandId = nextCommandId;
            }

            public ulong ReserveCommandId()
            {
                lock (_syncRoot)
                {
                    ulong commandId = _nextCommandId;
                    ulong nextCommandId = checked(commandId + 1);
                    ConfigurationData data = ReadOrCreate(_path);
                    data.clientId = clientId.ToString("D");
                    data.nextCommandId = nextCommandId.ToString(CultureInfo.InvariantCulture);
                    Write(_path, data);
                    _nextCommandId = nextCommandId;
                    return commandId;
                }
            }
        }
    }
}

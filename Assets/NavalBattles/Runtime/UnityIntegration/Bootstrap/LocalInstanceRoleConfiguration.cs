using System;
using System.IO;
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
            string directory = Path.GetDirectoryName(path);
            Directory.CreateDirectory(directory);
            string json = JsonUtility.ToJson(new ConfigurationData { role = role.ToString() }, true);
            File.WriteAllText(path, json);
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
            return $"{reason} Configure this Editor as Server or Client through " +
                $"Naval Battles > Instance Role. File: {path}. Supported values: Server, Client.";
        }

        [Serializable]
        private sealed class ConfigurationData
        {
            public string role;
        }
    }
}

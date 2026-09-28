using NavalBattles.Runtime.UnityIntegration.Bootstrap;
using UnityEditor;
using UnityEngine;

namespace NavalBattles.Editor
{
    public static class LocalInstanceRoleMenu
    {
        private const string SERVER_MENU_PATH = "Naval Battles/Instance Role/Server";
        private const string CLIENT_MENU_PATH = "Naval Battles/Instance Role/Client";

        [MenuItem(SERVER_MENU_PATH)]
        private static void SelectServer()
        {
            Select(GameProcessRole.Server);
        }

        [MenuItem(CLIENT_MENU_PATH)]
        private static void SelectClient()
        {
            Select(GameProcessRole.Client);
        }

        [MenuItem(SERVER_MENU_PATH, true)]
        private static bool ValidateServer()
        {
            UpdateChecks();
            return true;
        }

        [MenuItem(CLIENT_MENU_PATH, true)]
        private static bool ValidateClient()
        {
            UpdateChecks();
            return true;
        }

        private static void Select(GameProcessRole role)
        {
            string projectRoot = LocalInstanceRoleConfiguration.GetProjectRoot(Application.dataPath);
            LocalInstanceRoleConfiguration.Write(projectRoot, role);
            UpdateChecks();
            Debug.Log($"Naval Battles instance role: {role}. " +
                $"Configuration: {LocalInstanceRoleConfiguration.GetPath(projectRoot)}");
        }

        private static void UpdateChecks()
        {
            string projectRoot = LocalInstanceRoleConfiguration.GetProjectRoot(Application.dataPath);
            bool hasRole = LocalInstanceRoleConfiguration.TryRead(
                projectRoot,
                out GameProcessRole role,
                out _);
            Menu.SetChecked(SERVER_MENU_PATH, hasRole && role == GameProcessRole.Server);
            Menu.SetChecked(CLIENT_MENU_PATH, hasRole && role == GameProcessRole.Client);
        }
    }
}

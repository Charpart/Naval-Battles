using System;
using NavalBattles.Runtime.UnityIntegration.Bootstrap;
using NUnit.Framework;

namespace NavalBattles.Tests.EditMode.Unity
{
    [TestFixture]
    public sealed class MultiplayerPlayModeRoleResolverTests
    {
        [Test]
        public void Resolve_InMainEditorWithoutArguments_ReturnsServer()
        {
            Assert.That(
                MultiplayerPlayModeRoleResolver.Resolve(true, null),
                Is.EqualTo(GameProcessRole.Server));
            Assert.That(
                MultiplayerPlayModeRoleResolver.Resolve(true, Array.Empty<string>()),
                Is.EqualTo(GameProcessRole.Server));
        }

        [TestCase("Player1")]
        [TestCase("Player2")]
        [TestCase("Player3")]
        public void Resolve_InEditorProcessWithPlayerName_ReturnsClient(string playerName)
        {
            string[] arguments = { "Unity.exe", "-name", playerName };

            GameProcessRole role = MultiplayerPlayModeRoleResolver.Resolve(true, arguments);

            Assert.That(role, Is.EqualTo(GameProcessRole.Client));
        }

        [Test]
        public void Resolve_OutsideEditorWithoutExplicitRole_ReturnsClient()
        {
            Assert.That(
                MultiplayerPlayModeRoleResolver.Resolve(false, null),
                Is.EqualTo(GameProcessRole.Client));
            Assert.That(
                MultiplayerPlayModeRoleResolver.Resolve(false, Array.Empty<string>()),
                Is.EqualTo(GameProcessRole.Client));
            Assert.That(
                MultiplayerPlayModeRoleResolver.Resolve(false, new[] { "NavalBattles.exe" }),
                Is.EqualTo(GameProcessRole.Client));
        }

        [TestCase("")]
        [TestCase(" ")]
        [TestCase("-batchmode")]
        public void Resolve_InEditorWithInvalidPlayerName_ReturnsServer(string playerName)
        {
            string[] arguments = { "Unity.exe", "-name", playerName };

            GameProcessRole role = MultiplayerPlayModeRoleResolver.Resolve(true, arguments);

            Assert.That(role, Is.EqualTo(GameProcessRole.Server));
        }

        [Test]
        public void Resolve_InEditorWithMissingPlayerName_ReturnsServer()
        {
            string[] arguments = { "Unity.exe", "-name" };

            GameProcessRole role = MultiplayerPlayModeRoleResolver.Resolve(true, arguments);

            Assert.That(role, Is.EqualTo(GameProcessRole.Server));
        }

        [TestCase(true, new[] { "Unity.exe", "-server", "-name", "Player2" }, GameProcessRole.Server)]
        [TestCase(false, new[] { "NavalBattles.exe", "-server" }, GameProcessRole.Server)]
        [TestCase(true, new[] { "Unity.exe", "-client" }, GameProcessRole.Client)]
        [TestCase(false, new[] { "NavalBattles.exe", "-client" }, GameProcessRole.Client)]
        [TestCase(true, new[] { "Unity.exe", "-SERVER" }, GameProcessRole.Server)]
        [TestCase(false, new[] { "NavalBattles.exe", "-CLIENT" }, GameProcessRole.Client)]
        public void Resolve_WithExplicitRole_ReturnsRequestedRole(
            bool isEditor,
            string[] arguments,
            GameProcessRole expectedRole)
        {
            GameProcessRole role = MultiplayerPlayModeRoleResolver.Resolve(isEditor, arguments);

            Assert.That(role, Is.EqualTo(expectedRole));
        }

        [TestCase("-server", "-client")]
        [TestCase("-client", "-server")]
        public void Resolve_WithConflictingExplicitRoles_ThrowsDescriptiveException(
            string firstRole,
            string secondRole)
        {
            string[] arguments = { "NavalBattles.exe", firstRole, secondRole };

            ArgumentException exception = Assert.Throws<ArgumentException>(() =>
                MultiplayerPlayModeRoleResolver.Resolve(false, arguments));

            Assert.That(exception.Message, Does.Contain("-server"));
            Assert.That(exception.Message, Does.Contain("-client"));
        }
    }
}

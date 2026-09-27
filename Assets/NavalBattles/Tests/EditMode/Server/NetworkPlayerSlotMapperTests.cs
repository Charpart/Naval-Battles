using System;
using NavalBattles.Runtime.Domain.Matches;
using NavalBattles.Runtime.Protocol.Messages;
using NavalBattles.Runtime.Server.Snapshots;
using NUnit.Framework;

namespace NavalBattles.Tests.EditMode.Server
{
    [TestFixture]
    public sealed class NetworkPlayerSlotMapperTests
    {
        [TestCase(PlayerSlot.None, NetworkPlayerSlot.None)]
        [TestCase(PlayerSlot.First, NetworkPlayerSlot.First)]
        [TestCase(PlayerSlot.Second, NetworkPlayerSlot.Second)]
        public void Convert_KnownPlayerSlot_ReturnsNetworkValue(
            PlayerSlot player,
            NetworkPlayerSlot expected)
        {
            NetworkPlayerSlot result = NetworkPlayerSlotMapper.Convert(player);

            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void Convert_UnknownPlayerSlot_Throws()
        {
            var unknown = (PlayerSlot)byte.MaxValue;

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                NetworkPlayerSlotMapper.Convert(unknown));
        }
    }
}

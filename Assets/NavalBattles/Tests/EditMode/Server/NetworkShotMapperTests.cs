using System;
using NavalBattles.Runtime.Domain.Boards;
using NavalBattles.Runtime.Protocol.Messages;
using NavalBattles.Runtime.Server.Snapshots;
using NUnit.Framework;

namespace NavalBattles.Tests.EditMode.Server
{
    [TestFixture]
    public sealed class NetworkShotMapperTests
    {
        [TestCase(CellShotState.None, NetworkShotState.None)]
        [TestCase(CellShotState.Miss, NetworkShotState.Miss)]
        [TestCase(CellShotState.Hit, NetworkShotState.Hit)]
        public void Convert_KnownCellShotState_ReturnsNetworkValue(
            CellShotState state,
            NetworkShotState expected)
        {
            NetworkShotState result = NetworkShotMapper.Convert(state);

            Assert.That(result, Is.EqualTo(expected));
        }

        [TestCase(ShotResult.Invalid, NetworkShotResult.Invalid)]
        [TestCase(ShotResult.Miss, NetworkShotResult.Miss)]
        [TestCase(ShotResult.Hit, NetworkShotResult.Hit)]
        [TestCase(ShotResult.Sunk, NetworkShotResult.Sunk)]
        public void Convert_KnownShotResult_ReturnsNetworkValue(
            ShotResult result,
            NetworkShotResult expected)
        {
            NetworkShotResult networkResult = NetworkShotMapper.Convert(result);

            Assert.That(networkResult, Is.EqualTo(expected));
        }

        [Test]
        public void Convert_UnknownCellShotState_Throws()
        {
            var unknown = (CellShotState)byte.MaxValue;

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                NetworkShotMapper.Convert(unknown));
        }

        [Test]
        public void Convert_UnknownShotResult_Throws()
        {
            var unknown = (ShotResult)byte.MaxValue;

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                NetworkShotMapper.Convert(unknown));
        }
    }
}

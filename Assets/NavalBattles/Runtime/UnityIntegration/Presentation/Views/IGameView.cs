using System;
using NavalBattles.Runtime.UnityIntegration.Presentation.Models;

namespace NavalBattles.Runtime.UnityIntegration.Presentation.Views
{
    public interface IGameView
    {
        event Action<int> targetCellSelected;

        void RenderWaiting(string firstPlayerLabel, string secondPlayerLabel);
        void Render(GamePresentationModel model);
    }
}

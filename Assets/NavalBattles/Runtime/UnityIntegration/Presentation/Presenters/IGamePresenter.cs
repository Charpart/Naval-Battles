using System;

namespace NavalBattles.Runtime.UnityIntegration.Presentation.Presenters
{
    public interface IGamePresenter : IDisposable
    {
        void Initialize();
        void Refresh(double currentTime);
    }
}

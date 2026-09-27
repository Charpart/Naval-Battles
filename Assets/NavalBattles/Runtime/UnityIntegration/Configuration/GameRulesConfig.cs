using System;
using NavalBattles.Runtime.Domain.Configuration;
using UnityEngine;

namespace NavalBattles.Runtime.UnityIntegration.Configuration
{
    [CreateAssetMenu(menuName = "Naval Battles/Game Rules", fileName = "GameRulesConfig")]
    public sealed class GameRulesConfig : ScriptableObject
    {
        [SerializeField, Min(1)] private int _width = 6;
        [SerializeField, Min(1)] private int _height = 6;
        [SerializeField] private int[] _shipLengths = { 3, 2, 2, 1 };
        [SerializeField, Min(0.1f)] private float _turnDurationSeconds = 15.0f;

        public GameRules CreateRuntimeRules()
        {
            bool wereRulesCreated = GameRules.TryCreate(
                _width,
                _height,
                _shipLengths,
                _turnDurationSeconds,
                out GameRules rules,
                out GameRulesValidationError error);

            if (wereRulesCreated == false)
            {
                throw new InvalidOperationException($"Invalid game rules: {error}.");
            }

            return rules;
        }
    }
}

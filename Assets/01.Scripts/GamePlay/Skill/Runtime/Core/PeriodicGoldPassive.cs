using System;

namespace Skill.Data
{
    public sealed class PeriodicGoldPassive : PassiveSkill
    {
        private readonly IGameGoldService _goldService;
        private readonly float _intervalTime;
        private readonly int _goldAmount;

        private float _elapsedTime;
        private bool _isApplied;

        public PeriodicGoldPassive(
            IGameGoldService goldService,
            float intervalTime,
            int goldAmount)
        {
            _goldService = goldService ?? throw new ArgumentNullException(nameof(goldService));

            if (float.IsNaN(intervalTime)
                || float.IsInfinity(intervalTime)
                || intervalTime <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(intervalTime),
                    intervalTime,
                    "Gold passive interval must be positive and finite.");
            }

            if (goldAmount <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(goldAmount),
                    goldAmount,
                    "Gold passive amount must be greater than zero.");
            }

            _intervalTime = intervalTime;
            _goldAmount = goldAmount;
        }

        public override void Apply()
        {
            if (_isApplied)
                return;

            _elapsedTime = 0f;
            _isApplied = true;
        }

        public override void Tick(float deltaTime)
        {
            if (!_isApplied)
                return;

            if (float.IsNaN(deltaTime)
                || float.IsInfinity(deltaTime)
                || deltaTime < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(deltaTime),
                    deltaTime,
                    "Gold passive delta time must be non-negative and finite.");
            }

            _elapsedTime += deltaTime;

            while (_elapsedTime >= _intervalTime)
            {
                _elapsedTime -= _intervalTime;
                _goldService.GainMoney(_goldAmount);
            }
        }

        public override void Release()
        {
            if (!_isApplied)
                return;

            _isApplied = false;
            _elapsedTime = 0f;
        }
    }
}

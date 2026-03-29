using UnityEngine;
using System.Collections.Generic;

namespace QBlockyFighter.Modes
{
    public enum GameModeType
    {
        Duel1v1,
        TeamBattle5v5,
        Training,
        Challenge,
        BattleRoyale
    }

    public abstract class GameMode : MonoBehaviour
    {
        public GameModeType ModeType { get; protected set; }
        public bool IsActive { get; protected set; }
        public float GameTime { get; protected set; }
        public float MaxGameTime = 1500f; // 25分钟

        protected List<PlayerController> players = new List<PlayerController>();

        public virtual void Initialize(List<PlayerController> modePlayers)
        {
            players = modePlayers;
            IsActive = true;
            GameTime = 0f;
        }

        public virtual void StartMode()
        {
            IsActive = true;
            OnModeStart();
        }

        public virtual void EndMode()
        {
            IsActive = false;
            OnModeEnd();
        }

        protected virtual void Update()
        {
            if (!IsActive) return;
            GameTime += Time.deltaTime;
            if (GameTime >= MaxGameTime)
            {
                OnTimeUp();
            }
        }

        protected abstract void OnModeStart();
        protected abstract void OnModeEnd();
        protected abstract void OnTimeUp();
        public abstract void OnPlayerDeath(PlayerController player, PlayerController killer);
        public abstract bool CheckGameOver(out string winner);
    }
}

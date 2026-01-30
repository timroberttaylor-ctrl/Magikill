using System;

namespace Magikill.Core
{
    /// <summary>
    /// Interface for all game services managed by the GameManager.
    /// Services are auto-registered and initialized based on their priority.
    /// </summary>
    public interface IGameService
    {
        /// <summary>
        /// Priority for initialization order. Lower values initialize first.
        /// Recommended ranges:
        /// 0-10: Core systems (NetworkManager, SaveSystem)
        /// 11-20: Game systems (Combat, Inventory)
        /// 21-30: UI and visual systems
        /// </summary>
        int Priority { get; }

        /// <summary>
        /// Called by GameManager after all services are registered.
        /// Initialize your service here.
        /// </summary>
        void Initialize();

        /// <summary>
        /// Called by GameManager during shutdown.
        /// Clean up resources here.
        /// </summary>
        void Shutdown();
    }
}

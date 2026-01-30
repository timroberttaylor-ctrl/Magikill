using Fusion;
using UnityEngine;

namespace Magikill.Character
{
    /// <summary>
    /// Input data structure sent from client to server each network tick.
    /// Contains all player inputs including movement, combat, and interactions.
    /// Used by Fusion's Server Mode to process authoritative player actions.
    /// </summary>
    public struct NetworkInputData : INetworkInput
    {
        #region Movement Input

        /// <summary>
        /// Virtual joystick input for player movement.
        /// Normalized Vector2 (-1 to 1 on X and Y axes).
        /// Magnitude determines walk vs run speed.
        /// </summary>
        public Vector2 movementInput;

        /// <summary>
        /// Direction the player is looking/facing.
        /// Used for skill targeting and character rotation.
        /// Normalized Vector2 representing forward direction.
        /// </summary>
        public Vector2 lookDirection;

        #endregion

        #region Combat Input

        /// <summary>
        /// Skill button 1 - Also serves as basic attack.
        /// True when button is pressed this frame.
        /// </summary>
        public NetworkBool skill1Button;

        /// <summary>
        /// Skill button 2.
        /// True when button is pressed this frame.
        /// </summary>
        public NetworkBool skill2Button;

        /// <summary>
        /// Skill button 3.
        /// True when button is pressed this frame.
        /// </summary>
        public NetworkBool skill3Button;

        /// <summary>
        /// Skill button 4.
        /// True when button is pressed this frame.
        /// </summary>
        public NetworkBool skill4Button;

        /// <summary>
        /// Skill button 5.
        /// True when button is pressed this frame.
        /// </summary>
        public NetworkBool skill5Button;

        /// <summary>
        /// Skill button 6.
        /// True when button is pressed this frame.
        /// </summary>
        public NetworkBool skill6Button;

        #endregion

        #region Interaction Input

        /// <summary>
        /// Interaction button for NPCs, chests, doors, etc.
        /// True when button is pressed this frame.
        /// </summary>
        public NetworkBool interactButton;

        #endregion
    }
}

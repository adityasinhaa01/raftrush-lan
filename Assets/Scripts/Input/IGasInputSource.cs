namespace FartTogether.PlayerInput
{
    /// <summary>
    /// Interface for querying gas input per raft seat.
    /// Decouples raft physics from specific input hardware (keyboard, gamepad, microphone, etc.).
    /// </summary>
    public interface IGasInputSource
    {
        /// <summary>
        /// Total number of available seat inputs.
        /// </summary>
        int SeatCount { get; }

        /// <summary>
        /// Returns true if gas is actively being held/fired for the given seat.
        /// </summary>
        bool IsGasHeld(int seatIndex);

        /// <summary>
        /// Normalized thrust fraction (0.0 to 1.0) taking into account hold ramp and depletion.
        /// </summary>
        float GetThrustFraction(int seatIndex);

        /// <summary>
        /// Current gas fuel available for the given seat.
        /// </summary>
        float GetGasFuel(int seatIndex);

        /// <summary>
        /// Maximum gas fuel capacity for the given seat.
        /// </summary>
        float GetMaxGasFuel(int seatIndex);

        /// <summary>
        /// Whether this seat's gas is currently depleted (locked out until reaching recovery threshold).
        /// </summary>
        bool IsGasDepleted(int seatIndex);

        /// <summary>
        /// Whether the physical input key/button for this seat is currently pressed (regardless of fuel/depletion).
        /// </summary>
        bool IsKeyPressed(int seatIndex);
    }
}

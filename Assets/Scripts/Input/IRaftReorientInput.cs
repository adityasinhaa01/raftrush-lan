namespace FartTogether.PlayerInput
{
    /// <summary>
    /// Interface for querying raft facing reset / reorientation input requests.
    /// Decouples reorientation logic from hardware (keyboard, gamepad, netcode, etc.).
    /// </summary>
    public interface IRaftReorientInput
    {
        /// <summary>
        /// Returns true on the frame that facing reset / reorientation is requested.
        /// </summary>
        bool IsReorientRequested { get; }
    }
}

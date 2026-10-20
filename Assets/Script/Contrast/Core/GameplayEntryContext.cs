namespace Contrast.Core
{
    /// <summary>
    /// Distinguishes how a gameplay session was initiated so that
    /// in-game RETURN navigates to the correct workflow destination.
    /// </summary>
    public enum GameplayEntryContext
    {
        /// <summary>
        /// Gameplay was launched via Main Menu -> PLAY GAME -> LEVEL SELECT.
        /// RETURN navigates back to MainMenuUI (top-level menu).
        /// </summary>
        MainMenuGameplay,

        /// <summary>
        /// Gameplay was launched via EDIT LEVEL -> TEST PLAY.
        /// RETURN navigates back to the active Level Editor session, restoring working copy.
        /// </summary>
        EditorTestPlay
    }
}

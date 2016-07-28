namespace Ge
{
    public abstract class GameSystem
    {
        ///
        /// <summary>Performs a one-tick update of the GameSystem.</summary>
        ///
        public abstract void Update(float deltaSeconds);
    }
}
namespace Engine
{
    public abstract class GameSystem
    {
        ///
        /// <summary>Performs a one-tick update of the GameSystem.</summary>
        ///
        public void Update(float deltaSeconds)
        {
            if (Enabled)
            {
                UpdateCore(deltaSeconds);
            }
        }

        protected abstract void UpdateCore(float deltaSeconds);

        public bool Enabled { get; set; } = true;
    }
}
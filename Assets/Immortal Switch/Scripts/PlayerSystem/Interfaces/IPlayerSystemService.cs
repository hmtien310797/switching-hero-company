namespace Immortal_Switch.Scripts.PlayerSystem.Interfaces
{
    public interface IPlayerSystemService
    {
        /// <summary>
        /// them exp
        /// </summary>
        /// <param name="quantity">sl exp</param>
        void AddExp(int quantity);

        /// <summary>
        /// tang level
        /// </summary>
        void LevelUp();

        /// <summary>
        /// cap nhat exp ngay lap tuc
        /// </summary>
        /// <param name="quantity">sl exp can update.</param>
        void UpdateExp(int quantity);
    }
}
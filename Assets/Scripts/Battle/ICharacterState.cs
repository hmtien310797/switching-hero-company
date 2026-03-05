namespace Scripts.Battle
{
    public interface ICharacterState<T> where T : class
    {
        public void StartState(T state);
        public void UpdateState(T state);
        public void EndState(T state);
    }
}

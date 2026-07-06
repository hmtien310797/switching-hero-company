using Cysharp.Threading.Tasks;

namespace Battle.Dungeon
{
    public interface IDungeonMode
    {
        DungeonModeType ModeType { get; }

        UniTask InitializeAsync(DungeonModeContext context);
        void Begin();
        void Tick(float deltaTime);
        void OnTimeExpired();
        void Dispose();
    }
}
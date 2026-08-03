namespace Immortal_Switch.Scripts.Pvp.Snapshot
{
    /// <summary>
    /// Adapter cho future power systems (DOCX §9 — "Future power systems implement an adapter
    /// producing HeroPowerSourceSnapshot; BattleController must not read every progression manager
    /// directly"). Phase-1 không có adapter registered (Equipment/Growth/Transmutation có adapter
    /// riêng tách; system mới đăng ký qua <see cref="HeroBattleSnapshotBuilder.RegisterAdditionalSource"/>).
    /// </summary>
    public interface IHeroPowerSourceAdapter
    {
        string SourceId { get; }
        HeroPowerSourceSnapshot Build(int heroId);
    }
}

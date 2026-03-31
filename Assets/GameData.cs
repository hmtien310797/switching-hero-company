using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Core;
using UnityEngine;

public class GameData : Singleton<GameData>
{
    [Header("Config")]
    [field: SerializeField]
    public int maxCreepsPerStage { get; private set; } = 80;
    
    [field: SerializeField] 
    public int creepBatchSize { get; private set; } = 40;

    public override UniTask InitializeAsync()
    {
        throw new System.NotImplementedException();
    }
}

using UnityEngine;

/// <summary>
/// Per-environment Nakama connection settings (scheme/host/port/keys), stored as an asset
/// instead of hardcoded in NakamaClient — swap environments by assigning a different asset,
/// no code change. Dev.asset is committed (Assets/Resources/ServerConfig); Staging/Prod assets
/// are gitignored and created locally via Tools/Server Config in the Unity Editor.
/// </summary>
[CreateAssetMenu(menuName = "SwitchingHero/Nakama Server Config", fileName = "NewServerConfig")]
public class NakamaServerConfig : ScriptableObject
{
    [SerializeField] private string scheme = "http";
    [SerializeField] private string host = "127.0.0.1";
    [SerializeField] private int port = 7350;
    [SerializeField] private string serverKey = "";
    [SerializeField] private string httpKey = "";

    public string Scheme => scheme;
    public string Host => host;
    public int Port => port;
    public string ServerKey => serverKey;
    public string HttpKey => httpKey;
}

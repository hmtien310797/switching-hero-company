using System;
using Newtonsoft.Json;

[Serializable]
public class RegisterRequest
{
    [JsonProperty("username")] public string Username;
    [JsonProperty("password")] public string Password;
}

[Serializable]
public class RegisterResponse
{
    [JsonProperty("user_id")]  public string UserId;
    [JsonProperty("username")] public string Username;
    [JsonProperty("created")]  public bool   Created;
}

using System;

[Serializable]
public class FcmTokenRequest
{
    public string token;
    public string platform;
}

[Serializable]
public class FcmTokenResponse
{
    public bool success;
}

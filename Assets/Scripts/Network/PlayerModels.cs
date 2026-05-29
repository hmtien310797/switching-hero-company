using System;

[Serializable]
public class PlayerMeResponse
{
    public string user_id;
    public string username;
    public string display_name;
    public string avatar_url;

    public int level;
    public int exp;
    public int gems;
    public int tickets;
    public int coins;
    public int rating;
    public int total_summons;
}

[Serializable]
public class PlayerUpdateRequest
{
    public string display_name;
    public string avatar_url;
    public string lang_tag;
    public string location;
    public string timezone;
}

[Serializable]
public class PlayerUpdateResponse
{
    public bool updated;
}

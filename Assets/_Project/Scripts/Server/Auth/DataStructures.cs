using System;
using System.Collections.Generic;

[Serializable]
public class AccountInfo
{
    public string ID;
    public string Email;
    public string Login;
    public string PasswordHash;
    public string Salt;
}

[Serializable]
public class AccountsDatabase
{
    public List<AccountInfo> accounts = new List<AccountInfo>();
}

[Serializable]
public class PlayerData
{
    public string ID;
    public string Nickname;
    public int Level = 1;
    public int Rooms = 0;
    public List<string>Items;
    public int GameCurrency = 0;
    public int DonationCurrency = 0;
    public Preferences Preferences = new();
}

[Serializable]
public class Preferences
{
    public float MusicVolume = 0.6f;
    public float SfxVolume = 1f;
    public float MouseSensitivity = 0.5f;
}
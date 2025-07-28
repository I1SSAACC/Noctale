using Mirror;

public struct AuthRequestMessage : NetworkMessage
{
    public bool isRegister;
    public string email;
    public string login;
    public string password;
}

public struct AuthResponseMessage : NetworkMessage
{
    public bool isRegister;
    public bool success;
    public string message;
}
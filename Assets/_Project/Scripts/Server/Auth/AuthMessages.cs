using Mirror;

public struct AuthRequestMessage : NetworkMessage
{
    public bool IsRegister;
    public string Email;
    public string Login;
    public string Password;
}

public struct AuthResponseMessage : NetworkMessage
{
    public bool IsRegister;
    public bool Success;
    public string Message;
}
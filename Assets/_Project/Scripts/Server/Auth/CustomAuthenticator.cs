using Mirror;
using UnityEngine;

public class CustomAuthenticator : NetworkAuthenticator
{
    public override void OnStartServer() =>
        NetworkServer.RegisterHandler<AuthRequestMessage>(OnAuthRequestMessage, false);

    public override void OnStartClient() =>
        NetworkClient.RegisterHandler<AuthResponseMessage>(OnAuthResponseMessage, false);

    public override void OnServerAuthenticate(NetworkConnectionToClient conn) { }

    private void OnAuthRequestMessage(NetworkConnectionToClient conn, AuthRequestMessage msg)
    {
        if (msg.IsRegister == false && AuthManager.Instance.IsAccountLoggedIn(msg.Login))
        {
            AuthResponseMessage responseMessage = new()
            {
                IsRegister = false,
                Success = false,
                Message = "Аккаунт уже активен в другой сессии."
            };

            conn.Send(responseMessage);

            return;
        }

        if (msg.IsRegister)
        {
            bool ok = AuthManager.Instance.Register(
                msg.Email,
                msg.Login,
                msg.Password,
                out string regMessage);

            AuthResponseMessage responseMessage = new()
            {
                IsRegister = true,
                Success = ok,
                Message = regMessage
            };

            conn.Send(responseMessage);

        }
        else
        {
            bool ok = AuthManager.Instance.Login(
                msg.Login, 
                msg.Password,
                out _, 
                out string loginMessage);

            AuthResponseMessage responseMessage = new()
            {
                IsRegister = false,
                Success = ok,
                Message = loginMessage
            };

            conn.Send(responseMessage);

            if (ok)
            {
                AuthManager.Instance.AssociateConnectionWithAccount(conn, msg.Login);
                ServerAccept(conn);
            }
        }
    }

    private void OnAuthResponseMessage(AuthResponseMessage msg)
    {
        Debug.Log($"[Auth] {msg.Message}");

        if (msg.IsRegister == false && msg.Success)
            ClientAccept();
    }
}
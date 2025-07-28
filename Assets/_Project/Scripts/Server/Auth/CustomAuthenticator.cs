using Mirror;
using UnityEngine;

public class CustomAuthenticator : NetworkAuthenticator
{
    public override void OnStartServer()
    {
        NetworkServer.RegisterHandler<AuthRequestMessage>(OnAuthRequestMessage, false);
    }

    public override void OnStartClient()
    {
        NetworkClient.RegisterHandler<AuthResponseMessage>(OnAuthResponseMessage, false);
    }

    public override void OnServerAuthenticate(NetworkConnectionToClient conn) { }

    private void OnAuthRequestMessage(NetworkConnectionToClient conn, AuthRequestMessage msg)
    {
        if (!msg.isRegister && AuthManager.Instance.IsAccountLoggedIn(msg.login))
        {
            conn.Send(new AuthResponseMessage
            {
                isRegister = false,
                success = false,
                message = "Аккаунт уже активен в другой сессии."
            });
            return;
        }

        if (msg.isRegister)
        {
            bool ok = AuthManager.Instance.Register(
                msg.email, msg.login, msg.password,
                out string regMessage
            );

            conn.Send(new AuthResponseMessage
            {
                isRegister = true,
                success = ok,
                message = regMessage
            });

        }
        else
        {
            bool ok = AuthManager.Instance.Login(
                msg.login, msg.password,
                out _, out string loginMessage
            );

            conn.Send(new AuthResponseMessage
            {
                isRegister = false,
                success = ok,
                message = loginMessage
            });

            if (ok)
            {
                AuthManager.Instance.AssociateConnectionWithAccount(conn, msg.login);
                ServerAccept(conn);
            }
        }
    }

    private void OnAuthResponseMessage(AuthResponseMessage msg)
    {
        Debug.Log($"[Auth] {msg.message}");

        if (!msg.isRegister && msg.success)
            ClientAccept();
    }
}
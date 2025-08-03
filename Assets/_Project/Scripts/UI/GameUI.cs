using UnityEngine;
using TMPro;

public class GameUI : MonoBehaviour
{
    [SerializeField] private TMP_Text _nicknameText;
    [SerializeField] private TMP_Text _levelText;

    private void Awake()
    {
        if (AuthManager.Instance == null)
        {
            Debug.LogError("GameUI: AuthManager instance is null");
            return;
        }
    }

    public void DisplayPlayerInfo(string login)
    {
        if (TryGetPlayerData(login, out PlayerData playerData) == false)
        {
            Debug.LogWarning($"GameUI: Failed to load player data for login {login}");
            return;
        }

        _nicknameText.text = playerData.Nickname;
        _levelText.text = playerData.Level.ToString();
    }

    private bool TryGetPlayerData(string login, out PlayerData playerData)
    {
        return AuthManager.Instance.TryGetPlayerDataByLogin(login, out playerData);
    }
}
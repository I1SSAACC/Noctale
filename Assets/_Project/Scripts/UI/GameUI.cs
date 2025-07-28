using UnityEngine;
using TMPro;

public class GameUI : MonoBehaviour
{
    [SerializeField] private TMP_Text _playerNickname;
    [SerializeField] private TMP_Text _playerLevel;

    public void SetupPlayerInfo(PlayerData data)
    {
        _playerNickname.text = data.Nickname;
        _playerLevel.text = data.Level.ToString();
    }
}
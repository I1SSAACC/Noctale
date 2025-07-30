using TMPro;
using UnityEngine;

public class ElevatorView : MonoBehaviour
{
    private const float DecimalTimerThreshold = 5f;

    [SerializeField] private TextMeshPro _timerText;
    [SerializeField] private TextMeshPro _playerCountText;

    public void SetElapsedTime(float timeLeft)
    {
        if (timeLeft <= DecimalTimerThreshold)
            _timerText.text = timeLeft.ToString("F1") + "s";
        else 
            _timerText.text = Mathf.CeilToInt(timeLeft).ToString() + "s";
    }

    public void SetPlayerCount(int currentCount, int maxCount) =>
        _playerCountText.text = $"{currentCount}/{maxCount}";
}
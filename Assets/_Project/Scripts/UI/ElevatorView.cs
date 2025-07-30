using TMPro;
using UnityEngine;

public class ElevatorView : MonoBehaviour
{
    [SerializeField] private TextMeshPro _timerText;
    [SerializeField] private TextMeshPro _playerCountText;

    public void SetElapsedTime(float timeLeft)
    {
        if (_timerText == null)
        {
            Debug.LogError("TimerText is not assigned in ElevatorView.");
            return;
        }

        if (timeLeft < 0f)
        {
            _timerText.text = "0s";
            return;
        }

        if (timeLeft <= ElevatorConstants.DecimalTimerThreshold)
            _timerText.text = timeLeft.ToString("F1") + "s";
        else
            _timerText.text = Mathf.CeilToInt(timeLeft).ToString() + "s";
    }

    public void SetPlayerCount(int currentCount, int maxCount) =>
        _playerCountText.text = $"{currentCount}/{maxCount}";
}
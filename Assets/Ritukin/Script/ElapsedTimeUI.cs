using UnityEngine;
using UnityEngine.UI;

public sealed class ElapsedTimeUI : MonoBehaviour
{
    [SerializeField] private Text timeText;

    private float elapsedTime;
    private bool isRunning = true;

    private void Awake()
    {
        timeText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        UpdateText();
    }

    private void Update()
    {
        if (!isRunning || timeText == null)
        {
            return;
        }

        elapsedTime += Time.deltaTime;
        UpdateText();
    }

    public void StopTimer()
    {
        isRunning = false;
    }

    public void ResetTimer()
    {
        elapsedTime = 0f;
        isRunning = true;
        UpdateText();
    }

    private void UpdateText()
    {
        var minutes = Mathf.FloorToInt(elapsedTime / 60f);
        var seconds = Mathf.FloorToInt(elapsedTime % 60f);
        timeText.text = $"TIME {minutes:00}:{seconds:00}";
    }
}

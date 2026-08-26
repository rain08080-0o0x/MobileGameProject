using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class ElapsedTimeUI : MonoBehaviour
{
    [SerializeField] private Graphic timeText;

    private float elapsedTime;
    private bool isRunning = true;

    private void Awake()
    {
        if (timeText is Text legacyText)
        {
            legacyText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

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
        var value = $"TIME {minutes:00}:{seconds:00}";

        switch (timeText)
        {
            case Text legacyText:
                legacyText.text = value;
                break;
            case TMP_Text tmpText:
                tmpText.text = value;
                break;
        }
    }
}

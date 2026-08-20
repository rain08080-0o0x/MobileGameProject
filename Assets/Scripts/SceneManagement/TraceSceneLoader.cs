using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class TraceSceneLoader : MonoBehaviour
{
    [SerializeField] private string drawingSceneName = "TraceSysTest";
    [SerializeField] private string battleSceneName = "BattleScene";

    private IEnumerator Start()
    {
        yield return SceneManager.LoadSceneAsync(drawingSceneName, LoadSceneMode.Additive);
        yield return SceneManager.LoadSceneAsync(battleSceneName, LoadSceneMode.Additive);

        var traceSystem = FindFirstObjectByType<TraceSystem>();
        var battleController = FindFirstObjectByType<TraceBattleController>();
        if (traceSystem == null || battleController == null)
        {
            throw new InvalidOperationException("Additive battle scenes are missing required controllers.");
        }

        battleController.Initialize(traceSystem);
    }
}

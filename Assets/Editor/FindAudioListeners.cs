using UnityEngine;
using UnityEditor;

public class FindAudioListeners : EditorWindow
{
    [MenuItem("Tools/Find All Audio Listeners")]
    static void FindListeners()
    {
        AudioListener[] listeners = Object.FindObjectsOfType<AudioListener>(true);
        Debug.Log($"Found {listeners.Length} Audio Listeners:");
        
        foreach (var listener in listeners)
        {
            Debug.Log($"Audio Listener on: {listener.gameObject.name} (Active: {listener.gameObject.activeInHierarchy}, Enabled: {listener.enabled})");
        }
    }
}
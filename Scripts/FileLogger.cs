using UnityEngine;
using System.IO;

public class FileLogger : MonoBehaviour
{
    string path;

    void Awake()
    {
        path = Application.persistentDataPath + "/log.txt";

        Debug.Log("LOG FILE PATH: " + path);

        // очищаем файл при запуске
        File.WriteAllText(path, "");

        Application.logMessageReceived += HandleLog;
    }

    void OnDestroy()
    {
        Application.logMessageReceived -= HandleLog;
    }

    void HandleLog(string logString, string stackTrace, LogType type)
    {
        string logMessage =
            System.DateTime.Now.ToString("HH:mm:ss") +
            " [" + type + "] " +
            logString + "\n";

        File.AppendAllText(path, logMessage);
    }
}
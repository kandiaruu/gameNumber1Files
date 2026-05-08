//
// This script intercepts Unity console logs and writes them to a text file
// located in the persistent data path, clearing the previous file on startup.
//

using UnityEngine;
using System.IO;

public class FileLogger : MonoBehaviour
{
    string path;

    // Sets up the file path, clears the log file, and subscribes to the log event
    void Awake()
    {
        path = Application.persistentDataPath + "/log.txt";

        Debug.Log("LOG FILE PATH: " + path);

        File.WriteAllText(path, "");

        Application.logMessageReceived += HandleLog;
    }

    // Unsubscribes from the log event to prevent memory leaks when the object is destroyed
    void OnDestroy()
    {
        Application.logMessageReceived -= HandleLog;
    }

    // Formats the incoming log message with a timestamp and appends it to the text file
    void HandleLog(string logString, string stackTrace, LogType type)
    {
        string logMessage =
            System.DateTime.Now.ToString("HH:mm:ss") +
            " [" + type + "] " +
            logString + "\n";

        File.AppendAllText(path, logMessage);
    }
}
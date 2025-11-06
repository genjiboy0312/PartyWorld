using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* 2번
 * Logger 클래스는 개발자가 게임을 실행할 때 콘솔에서 메시지를 확인하고 디버깅할 때 사용됩니다. 
 * 또한 릴리스 버전에서는 특정 로그 레벨만 허용하여 필요한 로그 메시지만 출력할 수 있습니다. 
 * 따라서 Logger 클래스는 Unity에서 게임을 개발하고 디버깅하는 데 필수적입니다.
 * 
 * 3번
 * 성능 : 많은 양의 로그를 출력하는 경우 성능 저하
 * 
 */
public class Logger : MonoBehaviour
{
    public string output = "";
    public string stack = "";

    //  로그 이벤트 클래스
    public class LogEvent
    {
        public string _message;
        public string _trace;
        public LogType _type;

        public LogEvent(string _message, string _trace, LogType _type)
        {
            this._message = _message;
            this._trace = _trace;
            this._type = _type;
        }
    }
    private void Awake()
    {
        Application.logMessageReceived += Application_logMessageReceived;
    }
    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.A))
        {
            Debug.Log("Input Key 'A' Log");
        }

        if (Input.GetKeyDown(KeyCode.S))
        {
            Debug.LogException(new System.Exception("Input Key 'S' Exception"));
        }

        if (Input.GetKeyDown(KeyCode.D))
        {
            Debug.LogError("Input Key 'D' Error");
        }

        if (Input.GetKeyDown(KeyCode.F))
        {
            Debug.Assert(false, "Input Key 'F' Assert");
        }
    }
    void OnEnable()
    {
        Application.logMessageReceivedThreaded += HandleLog;
    }

    void OnDisable()
    {
        Application.logMessageReceivedThreaded -= HandleLog;
    }

    void HandleLog(string logString, string stackTrace, LogType type)
    {
        output = logString;
        stack = stackTrace;
    }

    private void OnDestroy()
    {
        Application.logMessageReceived -= Application_logMessageReceived;
    }

    private void Application_logMessageReceived(string condition, string stackTrace, LogType type)
    {
        Debug.Log(type + "\ncondition => " + condition + "\nstackTrace => " + stackTrace);
    }
}

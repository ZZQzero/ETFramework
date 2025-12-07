#if ENABLE_CONSOLE || DEVELOPMENT_BUILD
using UnityEngine;

namespace ET
{
    /// <summary>
    /// Console测试辅助类
    /// </summary>
    public class ConsoleTestHelper : MonoBehaviour
    {
        [Header("Test Settings")]
        public KeyCode testLogKey = KeyCode.F2;
        public KeyCode testWarningKey = KeyCode.F3;
        public KeyCode testErrorKey = KeyCode.F4;
        public KeyCode testBatchLogsKey = KeyCode.F5;

        private int logCounter = 0;

        private void Update()
        {
            if (Input.GetKeyDown(testLogKey))
            {
                TestLog();
            }

            if (Input.GetKeyDown(testWarningKey))
            {
                TestWarning();
            }

            if (Input.GetKeyDown(testErrorKey))
            {
                TestError();
            }

            if (Input.GetKeyDown(testBatchLogsKey))
            {
                TestBatchLogs();
            }
        }

        /// <summary>
        /// 测试普通日志
        /// </summary>
        public void TestLog()
        {
            logCounter++;
            Debug.Log($"这是一条测试日志 #{logCounter}");
        }

        /// <summary>
        /// 测试警告日志
        /// </summary>
        public void TestWarning()
        {
            logCounter++;
            Debug.LogWarning($"这是一条测试警告 #{logCounter}");
        }

        /// <summary>
        /// 测试错误日志
        /// </summary>
        public void TestError()
        {
            logCounter++;
            Debug.LogError($"这是一条测试错误 #{logCounter}");
        }

        /// <summary>
        /// 测试批量日志
        /// </summary>
        public void TestBatchLogs()
        {
            Debug.Log("开始批量日志测试...");

            for (int i = 0; i < 10; i++)
            {
                Debug.Log($"批量日志 - Log {i}");
            }

            for (int i = 0; i < 5; i++)
            {
                Debug.LogWarning($"批量日志 - Warning {i}");
            }

            for (int i = 0; i < 3; i++)
            {
                Debug.LogError($"批量日志 - Error {i}");
            }

            Debug.Log("批量日志测试完成！");
        }

        /// <summary>
        /// 测试长日志
        /// </summary>
        public void TestLongLog()
        {
            string longMessage = "这是一条非常长的日志消息，用于测试日志显示的换行和截断功能。" +
                "它包含了很多文字内容，可以用来验证UI是否能正确处理长文本。" +
                "在实际使用中，可能会遇到各种各样的长日志，所以需要确保Console能够正确显示。";
            Debug.Log(longMessage);
        }

        /// <summary>
        /// 测试重复日志
        /// </summary>
        public void TestRepeatedLogs()
        {
            for (int i = 0; i < 5; i++)
            {
                Debug.Log("这是一条重复的日志");
            }
        }

        /// <summary>
        /// 测试带堆栈的日志
        /// </summary>
        public void TestLogWithStackTrace()
        {
            try
            {
                throw new System.Exception("这是一个测试异常");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"捕获到异常: {e.Message}");
                Debug.LogException(e);
            }
        }

        /// <summary>
        /// 测试多行日志
        /// </summary>
        public void TestMultilineLog()
        {
            string multilineMessage = "第一行\n第二行\n第三行\n第四行";
            Debug.Log(multilineMessage);
        }

        /// <summary>
        /// 测试特殊字符
        /// </summary>
        public void TestSpecialCharacters()
        {
            Debug.Log("特殊字符测试: !@#$%^&*()_+-=[]{}|;':\",./<>?");
            Debug.Log("中文测试：你好世界！");
            Debug.Log("日文测试：こんにちは世界！");
            Debug.Log("韩文测试：안녕하세요 세계！");
        }

        /// <summary>
        /// 压力测试
        /// </summary>
        public void StressTest()
        {
            Debug.Log("开始压力测试...");

            for (int i = 0; i < 100; i++)
            {
                Debug.Log($"压力测试 Log {i}");
                Debug.LogWarning($"压力测试 Warning {i}");
                Debug.LogError($"压力测试 Error {i}");
            }

            Debug.Log("压力测试完成！");
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 300, 400));
            GUILayout.Label("Console测试工具", GUI.skin.box);
            GUILayout.Space(10);

            if (GUILayout.Button($"测试Log (F2)"))
            {
                TestLog();
            }

            if (GUILayout.Button($"测试Warning (F3)"))
            {
                TestWarning();
            }

            if (GUILayout.Button($"测试Error (F4)"))
            {
                TestError();
            }

            if (GUILayout.Button($"批量测试 (F5)"))
            {
                TestBatchLogs();
            }

            GUILayout.Space(10);

            if (GUILayout.Button("测试长日志"))
            {
                TestLongLog();
            }

            if (GUILayout.Button("测试重复日志"))
            {
                TestRepeatedLogs();
            }

            if (GUILayout.Button("测试异常日志"))
            {
                TestLogWithStackTrace();
            }

            if (GUILayout.Button("测试多行日志"))
            {
                TestMultilineLog();
            }

            if (GUILayout.Button("测试特殊字符"))
            {
                TestSpecialCharacters();
            }

            GUILayout.Space(10);

            if (GUILayout.Button("压力测试 (300条日志)"))
            {
                StressTest();
            }

            GUILayout.Space(10);
            GUILayout.Label($"日志计数: {logCounter}");

            GUILayout.EndArea();
        }
    }
}
#endif


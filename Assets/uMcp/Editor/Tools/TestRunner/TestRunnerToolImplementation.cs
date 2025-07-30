using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using uMCP.Editor.Core.Attributes;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace uMCP.Editor.Tools
{
    /// <summary>テスト実行ツールの実装</summary>
    [McpServerToolType, Description("Unityテスト実行ツール")]
    internal sealed class TestRunnerToolImplementation
    {
        /// <summary>EditModeテストを実行</summary>
        [McpServerTool, Description("EditModeテストを実行して結果を取得")]
        public async ValueTask<object> RunEditModeTests(
            [Description("実行するテストのフィルター（空の場合は全て）")] string testFilter = "",
            [Description("タイムアウト秒数")] int timeoutSeconds = 300,
            [Description("アセンブリ名でフィルター")] string assemblyNames = "",
            [Description("カテゴリ名でフィルター")] string categoryNames = "")
        {
            await UniTask.SwitchToMainThread();
            return await RunTests(TestMode.EditMode, testFilter, timeoutSeconds, assemblyNames, categoryNames, false);
        }

        /// <summary>PlayModeテストを実行</summary>
        [McpServerTool, Description("PlayModeテストを実行して結果を取得")]
        public async ValueTask<object> RunPlayModeTests(
            [Description("実行するテストのフィルター（空の場合は全て）")] string testFilter = "",
            [Description("タイムアウト秒数")] int timeoutSeconds = 600,
            [Description("アセンブリ名でフィルター")] string assemblyNames = "",
            [Description("カテゴリ名でフィルター")] string categoryNames = "",
            [Description("ドメインリロードを無効化するか")] bool disableDomainReload = true)
        {
            await UniTask.SwitchToMainThread();
            
            // PlayModeテスト実行前のチェック
            if (EditorApplication.isCompiling)
            {
                return new TestRunResponse
                {
                    Success = false,
                    Error = "Cannot run PlayMode tests while compiling",
                    TestMode = TestMode.PlayMode.ToString()
                };
            }
            
            if (EditorApplication.isPlaying)
            {
                return new TestRunResponse
                {
                    Success = false,
                    Error = "Cannot run PlayMode tests while already in Play Mode",
                    TestMode = TestMode.PlayMode.ToString()
                };
            }
            
            Debug.Log($"[uMCP TestRunner] PlayMode test requested - EditorApplication.isPlaying: {EditorApplication.isPlaying}, isPlayingOrWillChangePlaymode: {EditorApplication.isPlayingOrWillChangePlaymode}");
            Debug.Log($"[uMCP TestRunner] disableDomainReload parameter: {disableDomainReload}");
            return await RunTests(TestMode.PlayMode, testFilter, timeoutSeconds, assemblyNames, categoryNames, disableDomainReload);
        }

        /// <summary>利用可能なテスト一覧を取得</summary>
        [McpServerTool, Description("プロジェクト内の利用可能なテスト一覧を取得")]
        public async ValueTask<object> GetAvailableTests(
            [Description("テストモード: EditMode, PlayMode, または All")] string mode = "All")
        {
            await UniTask.SwitchToMainThread();

            try
            {
                var testRunnerApi = ScriptableObject.CreateInstance<TestRunnerApi>();
                var testModeInfos = new List<TestModeInfo>();

                if (mode is "All" or "EditMode")
                {
                    var editModeCount = await GetTestCount(testRunnerApi, TestMode.EditMode);
                    testModeInfos.Add(new TestModeInfo
                    {
                        Mode = "EditMode",
                        Message = $"{editModeCount} tests available"
                    });
                }

                if (mode is "All" or "PlayMode")
                {
                    var playModeCount = await GetTestCount(testRunnerApi, TestMode.PlayMode);
                    testModeInfos.Add(new TestModeInfo
                    {
                        Mode = "PlayMode",
                        Message = $"{playModeCount} tests available"
                    });
                }

                ScriptableObject.DestroyImmediate(testRunnerApi);

                return new AvailableTestsResponse
                {
                    Success = true,
                    RequestedMode = mode,
                    Tests = testModeInfos,
                    Note = "Unity Test Framework configured and ready",
                    Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                };
            }
            catch (Exception ex)
            {
                return new ErrorResponse
                {
                    Success = false,
                    Error = $"Failed to get available tests: {ex.Message}"
                };
            }
        }

        /// <summary>指定したテストモードのテスト数を取得</summary>
        private async Task<int> GetTestCount(TestRunnerApi testRunnerApi, TestMode testMode)
        {
            var tcs = new TaskCompletionSource<int>();
            
            testRunnerApi.RetrieveTestList(testMode, (testRoot) =>
            {
                int count = CountTests(testRoot);
                tcs.SetResult(count);
            });

            return await tcs.Task;
        }

        /// <summary>テスト数を再帰的にカウント</summary>
        private int CountTests(ITestAdaptor test)
        {
            if (test == null) return 0;
            
            if (!test.HasChildren)
                return test.IsSuite ? 0 : 1;

            int count = 0;
            foreach (var child in test.Children)
            {
                count += CountTests(child);
            }
            return count;
        }

        /// <summary>テスト実行の共通処理</summary>
        private async Task<object> RunTests(TestMode testMode, string testFilter, int timeoutSeconds, string assemblyNames, string categoryNames, bool disableDomainReload = false)
        {
            Debug.Log($"[uMCP TestRunner] Starting {testMode} test execution");
            TestRunnerApi testRunnerApi = null;
            TestResultCollector collector = null;
            
            // PlayModeテスト用のドメインリロード設定保存
            bool originalEnterPlayModeOptionsEnabled = false;
            EnterPlayModeOptions originalEnterPlayModeOptions = EnterPlayModeOptions.None;

            try
            {
                // PlayModeテスト用の特別な処理
                if (testMode == TestMode.PlayMode)
                {
                    Debug.Log($"[uMCP TestRunner] PlayMode test preparation - checking editor state");
                    
                    // ドメインリロード設定の保存と変更
                    if (disableDomainReload)
                    {
                        originalEnterPlayModeOptionsEnabled = EditorSettings.enterPlayModeOptionsEnabled;
                        originalEnterPlayModeOptions = EditorSettings.enterPlayModeOptions;
                        
                        EditorSettings.enterPlayModeOptionsEnabled = true;
                        EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload | EnterPlayModeOptions.DisableSceneReload;
                        
                        Debug.Log($"[uMCP TestRunner] Domain reload disabled for PlayMode test execution");
                    }
                    
                    // コンパイルが完了するまで待機
                    if (EditorApplication.isCompiling)
                    {
                        Debug.Log($"[uMCP TestRunner] Waiting for compilation to complete...");
                        var compilationWaitTime = 0;
                        while (EditorApplication.isCompiling && compilationWaitTime < 30)
                        {
                            await UniTask.Delay(1000);
                            compilationWaitTime++;
                        }
                        
                        if (EditorApplication.isCompiling)
                        {
                            // 設定を元に戻す
                            if (disableDomainReload)
                            {
                                EditorSettings.enterPlayModeOptionsEnabled = originalEnterPlayModeOptionsEnabled;
                                EditorSettings.enterPlayModeOptions = originalEnterPlayModeOptions;
                            }
                            
                            return new TestRunResponse
                            {
                                Success = false,
                                Error = "Compilation did not complete within 30 seconds",
                                TestMode = testMode.ToString()
                            };
                        }
                    }
                    
                    Debug.Log($"[uMCP TestRunner] PlayMode preparation complete");
                }

                testRunnerApi = ScriptableObject.CreateInstance<TestRunnerApi>();
                collector = new TestResultCollector();

                Debug.Log($"[uMCP TestRunner] Created TestRunnerApi and TestResultCollector for {testMode}");
                testRunnerApi.RegisterCallbacks(collector);
                Debug.Log($"[uMCP TestRunner] Registered callbacks for {testMode}");

                // フィルター設定
                var filter = new Filter
                {
                    testMode = testMode
                };

                if (!string.IsNullOrEmpty(testFilter))
                {
                    filter.testNames = testFilter.Split(',').Select(s => s.Trim()).ToArray();
                }

                if (!string.IsNullOrEmpty(assemblyNames))
                {
                    filter.assemblyNames = assemblyNames.Split(',').Select(s => s.Trim()).ToArray();
                }

                if (!string.IsNullOrEmpty(categoryNames))
                {
                    filter.categoryNames = categoryNames.Split(',').Select(s => s.Trim()).ToArray();
                }

                // テスト実行
                Debug.Log($"[uMCP TestRunner] Executing {testMode} tests with timeout {timeoutSeconds}s");
                Debug.Log($"[uMCP TestRunner] Filter settings - TestMode: {filter.testMode}, TestNames: {(filter.testNames?.Length ?? 0)} items");
                
                var executionSettings = new ExecutionSettings(filter);
                Debug.Log($"[uMCP TestRunner] Created ExecutionSettings for {testMode}");
                
                // PlayModeテストの場合は実行前に少し待機
                if (testMode == TestMode.PlayMode)
                {
                    Debug.Log($"[uMCP TestRunner] Preparing for PlayMode test execution...");
                    await UniTask.Delay(500); // 500ms待機
                }
                
                testRunnerApi.Execute(executionSettings);
                Debug.Log($"[uMCP TestRunner] Execute() called for {testMode}, waiting for callbacks...");

                // 結果待機（タイムアウト付き）
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
                try
                {
                    Debug.Log($"[uMCP TestRunner] Waiting for {testMode} test completion...");
                    await collector.WaitForRunFinished(cts.Token);
                    Debug.Log($"[uMCP TestRunner] {testMode} tests completed, preparing result");
                    var result = collector.GetResult();
                    Debug.Log($"[uMCP TestRunner] {testMode} result prepared, returning response");
                    return result;
                }
                catch (OperationCanceledException) when (!cts.Token.IsCancellationRequested)
                {
                    Debug.LogError($"[uMCP TestRunner] {testMode} test execution was cancelled unexpectedly");
                    return new TestRunResponse
                    {
                        Success = false,
                        Error = $"Test execution was cancelled unexpectedly",
                        TestMode = testMode.ToString()
                    };
                }
                catch (OperationCanceledException)
                {
                    Debug.LogError($"[uMCP TestRunner] {testMode} test execution timed out after {timeoutSeconds} seconds");
                    return new TestRunResponse
                    {
                        Success = false,
                        Error = $"Test execution timed out after {timeoutSeconds} seconds",
                        TestMode = testMode.ToString(),
                        TimeoutSeconds = timeoutSeconds
                    };
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[uMCP TestRunner] Exception during {testMode} test execution: {ex}");
                return new TestRunResponse
                {
                    Success = false,
                    Error = $"Failed to run {testMode} tests: {ex.Message}",
                    TestMode = testMode.ToString()
                };
            }
            finally
            {
                try
                {
                    // ドメインリロード設定を元に戻す
                    if (testMode == TestMode.PlayMode && disableDomainReload)
                    {
                        EditorSettings.enterPlayModeOptionsEnabled = originalEnterPlayModeOptionsEnabled;
                        EditorSettings.enterPlayModeOptions = originalEnterPlayModeOptions;
                        Debug.Log($"[uMCP TestRunner] Domain reload settings restored");
                    }
                    
                    if (testRunnerApi != null && collector != null)
                    {
                        testRunnerApi.UnregisterCallbacks(collector);
                        Debug.Log($"[uMCP TestRunner] Unregistered callbacks for {testMode}");
                    }
                    if (testRunnerApi != null)
                    {
                        ScriptableObject.DestroyImmediate(testRunnerApi);
                        Debug.Log($"[uMCP TestRunner] Destroyed TestRunnerApi for {testMode}");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[uMCP TestRunner] Error during cleanup: {ex.Message}");
                }
            }
        }
    }
}

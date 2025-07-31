using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace uMCP.Editor.Tools
{
    /// <summary>テスト結果収集クラス</summary>
    internal class TestResultCollector : ICallbacks
    {
        readonly UniTaskCompletionSource<TestRunResponse> completionSource = new();
        TestRunResponse currentResult = new();
        DateTime startTime;
        readonly int timeoutSeconds;

        public TestResultCollector(int timeoutSeconds = 0)
        {
            this.timeoutSeconds = timeoutSeconds;
        }

        public async UniTask<TestRunResponse> WaitForRunFinished(CancellationToken cancellationToken)
        {
            try
            {
                await using (cancellationToken.Register(() =>
                             {
                                 Debug.Log("[uMCP TestResultCollector] Cancellation requested");
                                 completionSource.TrySetCanceled();
                             }))
                {
                    Debug.Log("[uMCP TestResultCollector] Waiting for test completion...");
                    var result = await completionSource.Task;
                    Debug.Log("[uMCP TestResultCollector] Test completion received");
                    return result;
                }
            }
            catch (OperationCanceledException)
            {
                Debug.LogWarning("[uMCP TestResultCollector] Test execution was cancelled");
                throw;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[uMCP TestResultCollector] Unexpected error: {ex}");
                throw;
            }
        }

        public TestRunResponse GetResult()
        {
            return currentResult;
        }

        public void RunStarted(ITestAdaptor testsToRun)
        {
            Debug.Log($"[uMCP TestResultCollector] RunStarted called for {testsToRun.TestMode}");
            startTime = DateTime.Now;
            var totalTests = CountTests(testsToRun);

            currentResult = new TestRunResponse
            {
                Success = true,
                TestMode = testsToRun.TestMode.ToString(),
                TimeoutSeconds = timeoutSeconds,
                Summary = new TestSummary
                {
                    TotalTests = totalTests,
                    PassedTests = 0,
                    FailedTests = 0,
                    SkippedTests = 0
                },
                FailedTests = new List<FailedTestDetail>(),
                StartTime = startTime.ToString("yyyy-MM-dd HH:mm:ss")
            };

            Debug.Log($"[uMCP TestResultCollector] Starting {testsToRun.TestMode} test run with {totalTests} tests");
        }

        public void RunFinished(ITestResultAdaptor result)
        {
            Debug.Log($"[uMCP TestResultCollector] RunFinished called for {result.Test.TestMode}");
            var endTime = DateTime.Now;
            var duration = endTime - startTime;

            currentResult.EndTime = endTime.ToString("yyyy-MM-dd HH:mm:ss");
            currentResult.Summary.DurationSeconds = duration.TotalSeconds;
            currentResult.Summary.SuccessRate = currentResult.Summary.TotalTests > 0
                ? currentResult.Summary.PassedTests * 100.0 / currentResult.Summary.TotalTests
                : 0;
            currentResult.OverallResult = currentResult.Summary.FailedTests == 0 ? "PASSED" : "FAILED";

            Debug.Log($"[uMCP TestResultCollector] Test run finished: {currentResult.OverallResult} " +
                      $"({currentResult.Summary.PassedTests}/{currentResult.Summary.TotalTests} passed)");

            Debug.Log($"[uMCP TestResultCollector] Setting completion source result");
            completionSource.TrySetResult(currentResult);
        }

        public void TestStarted(ITestAdaptor test)
        {
            // テスト開始時の処理
        }

        public void TestFinished(ITestResultAdaptor result)
        {
            // TestSuiteではなく実際のテストケースのみカウント
            if (result.Test.IsSuite)
                return;

            switch (result.TestStatus)
            {
                case TestStatus.Passed:
                    currentResult.Summary.PassedTests++;
                    break;
                case TestStatus.Failed:
                    currentResult.Summary.FailedTests++;
                    currentResult.FailedTests.Add(new FailedTestDetail
                    {
                        Name = result.Test.Name,
                        FullName = result.Test.FullName,
                        Message = result.Message,
                        StackTrace = result.StackTrace,
                        Duration = result.Duration
                    });
                    break;
                case TestStatus.Skipped:
                    currentResult.Summary.SkippedTests++;
                    break;
            }
        }

        int CountTests(ITestAdaptor test)
        {
            if (!test.HasChildren)
                return test.IsSuite ? 0 : 1;

            int count = 0;
            foreach (var child in test.Children)
            {
                count += CountTests(child);
            }

            return count;
        }
    }
}

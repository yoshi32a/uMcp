using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace uMCP.Tests
{
    public class PlayModeTests
    {
        [Test]
        public void BasicMathTest()
        {
            // 基本的な計算テスト
            int result = 2 + 2;
            Assert.AreEqual(4, result, "2 + 2 should equal 4");
        }

        [Test]
        public void StringTest()
        {
            // 文字列テスト
            string hello = "Hello";
            string world = "World";
            string combined = hello + " " + world;
            Assert.AreEqual("Hello World", combined, "String concatenation should work");
        }

        [Test]
        public void UnityObjectTest()
        {
            // Unity Object テスト
            GameObject testObject = new GameObject("TestObject");
            Assert.IsNotNull(testObject, "GameObject should not be null");
            Assert.AreEqual("TestObject", testObject.name, "GameObject name should match");

            // クリーンアップ
            Object.DestroyImmediate(testObject);
        }

        [Test]
        [Category("MCP")]
        public void McpServerTest()
        {
            // MCP Server関連テスト
            Assert.IsTrue(Application.isEditor, "Should be running in editor");
            Assert.IsNotNull(Application.dataPath, "Data path should exist");
        }

        [UnityTest]
        public IEnumerator CoroutineTest()
        {
            // コルーチンテスト - 複数フレーム待機
            float startTime = Time.time;

            yield return new WaitForSeconds(0.1f);

            float elapsed = Time.time - startTime;
            Assert.GreaterOrEqual(elapsed, 0.09f, "Should wait at least 0.09 seconds");
        }

        [UnityTest]
        [Category("Slow")]
        public IEnumerator SlowTest()
        {
            // 時間のかかるテスト
            for (int i = 0; i < 2; i++)
            {
                Debug.Log($"[PlayModeTest] Frame {i + 1}/2");
                yield return null; // 1フレーム待機
            }

            Assert.IsTrue(true, "Slow test completed");
        }

        [Test]
        public void IntentionalFailureTest()
        {
            // 意図的に失敗するテスト（TestRunnerの失敗処理をテストするため）
            // このテストは通常はコメントアウトしておく
            // Assert.Fail("This test intentionally fails to test error handling");

            // 代わりに成功するテスト
            Assert.IsTrue(true, "This test passes instead of failing");
        }
    }
}

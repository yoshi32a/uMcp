using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using uMCP.Editor.Core.Attributes;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace uMCP.Editor.Tools
{
    /// <summary>Unity情報ツールの実装</summary>
    [McpServerToolType, Description("Unity情報を取得するツール")]
    internal sealed class UnityInfoToolImplementation
    {
        /// <summary>Unity エディターとプロジェクトの情報を取得</summary>
        [McpServerTool, Description("Unity エディターとプロジェクトの詳細情報を読みやすい形式で取得")]
        public async ValueTask<object> GetUnityInfo()
        {
            await UniTask.SwitchToMainThread();

            var info = new System.Text.StringBuilder();
            info.AppendLine("=== Unity プロジェクト情報 ===");
            info.AppendLine();

            // プロジェクト基本情報
            info.AppendLine("## プロジェクト詳細");
            info.AppendLine($"**プロジェクト名:** {Application.productName}");
            info.AppendLine($"**会社名:** {Application.companyName}");
            info.AppendLine($"**Unity バージョン:** {Application.unityVersion}");
            info.AppendLine($"**プラットフォーム:** {Application.platform}");
            info.AppendLine($"**システム言語:** {Application.systemLanguage}");
            info.AppendLine();

            // エディター状態
            info.AppendLine("## エディター状態");
            info.AppendLine($"**実行状態:** {(Application.isPlaying ? "Play Mode" : "Edit Mode")}");
            info.AppendLine($"**フォーカス状態:** {(Application.isFocused ? "フォーカス中" : "非フォーカス")}");
            info.AppendLine($"**バッチモード:** {(Application.isBatchMode ? "有効" : "無効")}");
            info.AppendLine();

            // パス情報
            info.AppendLine("## パス情報");
            info.AppendLine($"**アセットパス:** {Application.dataPath}");
            info.AppendLine($"**永続データパス:** {Application.persistentDataPath}");
            info.AppendLine($"**ストリーミングアセットパス:** {Application.streamingAssetsPath}");
            info.AppendLine();

            // クラウド情報
            if (!string.IsNullOrEmpty(Application.cloudProjectId))
            {
                info.AppendLine("## Unity Cloud");
                info.AppendLine($"**プロジェクトID:** {Application.cloudProjectId}");
                info.AppendLine();
            }

            // システム情報
            info.AppendLine("## システム情報");
            info.AppendLine($"**OS:** {SystemInfo.operatingSystem}");
            info.AppendLine($"**CPU:** {SystemInfo.processorType} ({SystemInfo.processorCount} cores)");
            info.AppendLine($"**メモリ:** {SystemInfo.systemMemorySize} MB");
            info.AppendLine($"**GPU:** {SystemInfo.graphicsDeviceName}");
            info.AppendLine($"**GPU メモリ:** {SystemInfo.graphicsMemorySize} MB");

            return new StandardResponse
            {
                Success = true,
                FormattedOutput = info.ToString()
            };
        }

        /// <summary>現在のシーン情報を取得</summary>
        [McpServerTool, Description("現在のシーン情報とGameObjectの一覧を読みやすい形式で取得")]
        public async ValueTask<object> GetSceneInfo()
        {
            await UniTask.SwitchToMainThread();

            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            var rootGameObjects = scene.GetRootGameObjects();

            var info = new System.Text.StringBuilder();
            info.AppendLine("=== シーン情報 ===");
            info.AppendLine($"**シーン名:** {scene.name}");
            info.AppendLine($"**パス:** {(string.IsNullOrEmpty(scene.path) ? "未保存" : scene.path)}");
            info.AppendLine($"**ビルドインデックス:** {scene.buildIndex}");
            info.AppendLine($"**状態:** {(scene.isLoaded ? "読み込み済み" : "未読み込み")}{(scene.isDirty ? " (変更あり)" : "")}");
            info.AppendLine();

            if (rootGameObjects.Length > 0)
            {
                // 統計情報の計算
                var allGameObjects = new List<GameObject>();
                int maxDepth = 0;
                int totalActiveObjects = 0;
                int totalInactiveObjects = 0;

                foreach (var root in rootGameObjects)
                {
                    allGameObjects.Add(root);
                    allGameObjects.AddRange(GetAllChildGameObjects(root));
                    
                    int rootDepth = CalculateMaxDepth(root);
                    maxDepth = Math.Max(maxDepth, rootDepth);
                    
                    if (root.activeInHierarchy) totalActiveObjects++;
                    else totalInactiveObjects++;
                    
                    // 子オブジェクトの状態もカウント
                    foreach (var child in GetAllChildGameObjects(root))
                    {
                        if (child.activeInHierarchy) totalActiveObjects++;
                        else totalInactiveObjects++;
                    }
                }

                // 統計情報表示
                info.AppendLine("## 📊 統計情報");
                info.AppendLine($"**ルートオブジェクト数:** {rootGameObjects.Length}件");
                info.AppendLine($"**総GameObject数:** {allGameObjects.Count}件");
                info.AppendLine($"**アクティブオブジェクト:** {totalActiveObjects}件");
                info.AppendLine($"**非アクティブオブジェクト:** {totalInactiveObjects}件");
                info.AppendLine($"**最大階層深度:** {maxDepth}階層");
                info.AppendLine();

                // 階層構造表示
                info.AppendLine("## 🌳 階層構造");
                
                foreach (var root in rootGameObjects)
                {
                    DisplayGameObjectHierarchy(root, info, "", true);
                }
            }
            else
            {
                info.AppendLine("**シーンは空です。**");
            }

            return new StandardResponse
            {
                Success = true,
                FormattedOutput = info.ToString()
            };
        }



        /// <summary>指定したGameObjectの詳細情報を取得</summary>
        [McpServerTool, Description("指定したGameObjectの詳細情報を読みやすい形式で取得")]
        public async ValueTask<object> GetGameObjectInfo(
            [Description("GameObjectの名前またはパス")] string gameObjectName)
        {
            await UniTask.SwitchToMainThread();

            if (string.IsNullOrEmpty(gameObjectName))
            {
                return new ErrorResponse
                {
                    Success = false,
                    Error = "GameObject name is required"
                };
            }

            // 名前でGameObjectを検索
            var gameObject = GameObject.Find(gameObjectName);
            if (gameObject == null)
            {
                // 名前が見つからない場合、すべてのGameObjectから部分一致で検索
                var allGameObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
                gameObject = allGameObjects.FirstOrDefault(go => go.name.Contains(gameObjectName));
            }

            if (gameObject == null)
            {
                return new ErrorResponse
                {
                    Success = false,
                    Error = $"GameObject '{gameObjectName}' not found"
                };
            }

            var components = gameObject.GetComponents<Component>();
            // Componentのnullチェック
            var validComponents = components.Where(c => c != null).ToArray();
            var transform = gameObject.transform;

            var info = new System.Text.StringBuilder();
            info.AppendLine($"=== GameObject詳細: {gameObject.name} ===");
            info.AppendLine($"**分析時刻:** {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            info.AppendLine();

            // 基本情報
            var icon = GetGameObjectIcon(gameObject);
            var type = GetGameObjectType(gameObject);
            info.AppendLine("## 📋 基本情報");
            info.AppendLine($"{icon} **{gameObject.name}** ({type})");
            info.AppendLine($"**アクティブ:** {(gameObject.activeSelf ? "✅" : "❌")} | **階層内:** {(gameObject.activeInHierarchy ? "✅" : "❌")}");
            info.AppendLine($"**Tag:** {gameObject.tag} | **Layer:** {LayerMask.LayerToName(gameObject.layer)}");
            info.AppendLine($"**子オブジェクト数:** {transform.childCount}件");
            info.AppendLine();

            // Transform情報
            info.AppendLine("## 🔄 Transform");
            info.AppendLine($"**ローカル位置:** ({transform.localPosition.x:F2}, {transform.localPosition.y:F2}, {transform.localPosition.z:F2})");
            info.AppendLine($"**ワールド位置:** ({transform.position.x:F2}, {transform.position.y:F2}, {transform.position.z:F2})");
            info.AppendLine($"**回転:** ({transform.localEulerAngles.x:F1}°, {transform.localEulerAngles.y:F1}°, {transform.localEulerAngles.z:F1}°)");
            info.AppendLine($"**スケール:** ({transform.localScale.x:F2}, {transform.localScale.y:F2}, {transform.localScale.z:F2})");
            if (transform.parent)
            {
                info.AppendLine($"**親オブジェクト:** {transform.parent.name} (順序: {transform.GetSiblingIndex()})");
            }
            info.AppendLine();

            // コンポーネント情報
            info.AppendLine($"## ⚙️ コンポーネント ({validComponents.Length}件)");
            foreach (var component in validComponents)
            {
                var componentName = component.GetType().Name;
                var componentIcon = componentName switch
                {
                    "Transform" => "🔄",
                    "Camera" => "📷",
                    "Light" => "💡",
                    "Renderer" or "MeshRenderer" or "SkinnedMeshRenderer" => "🎨",
                    "Collider" or "BoxCollider" or "SphereCollider" or "MeshCollider" => "🎯",
                    "Rigidbody" => "⚛️",
                    _ when componentName.Contains("UI") || componentName.Contains("Canvas") => "🖼️",
                    _ when componentName.Contains("Audio") => "🔊",
                    _ => "⚙️"
                };
                
                var enabled = component is Behaviour behaviour ? (behaviour.enabled ? "✅" : "❌") : "";
                info.AppendLine($"{componentIcon} **{componentName}** {enabled}");
            }

            return new StandardResponse
            {
                Success = true,
                FormattedOutput = info.ToString()
            };
        }

        /// <summary>指定したPrefabの詳細情報を取得</summary>
        [McpServerTool, Description("指定したPrefabの詳細情報を読みやすい形式で取得")]
        public async ValueTask<object> GetPrefabInfo(
            [Description("Prefabのアセットパス")] string prefabPath)
        {
            await UniTask.SwitchToMainThread();

            if (string.IsNullOrEmpty(prefabPath))
            {
                return new ErrorResponse
                {
                    Success = false,
                    Error = "Prefab path is required"
                };
            }

            var prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefabAsset == null)
            {
                return new ErrorResponse
                {
                    Success = false,
                    Error = $"Prefab not found at path: {prefabPath}"
                };
            }

            var components = prefabAsset.GetComponents<Component>();
            // Componentのnullチェック
            var validComponents = components.Where(c => c != null).ToArray();
            var transform = prefabAsset.transform;

            var info = new System.Text.StringBuilder();
            info.AppendLine($"=== Prefab詳細: {prefabAsset.name} ===");
            info.AppendLine($"**アセットパス:** {prefabPath}");
            info.AppendLine();

            // 基本情報
            var icon = GetGameObjectIcon(prefabAsset);
            var type = GetGameObjectType(prefabAsset);
            info.AppendLine("## 📋 基本情報");
            info.AppendLine($"{icon} **{prefabAsset.name}** ({type})");
            info.AppendLine($"**Tag:** {prefabAsset.tag} | **Layer:** {LayerMask.LayerToName(prefabAsset.layer)}");
            info.AppendLine($"**子オブジェクト数:** {transform.childCount}件");
            info.AppendLine();

            // Transform情報
            info.AppendLine("## 🔄 Transform");
            info.AppendLine($"**位置:** ({transform.position.x:F2}, {transform.position.y:F2}, {transform.position.z:F2})");
            info.AppendLine($"**回転:** ({transform.eulerAngles.x:F1}°, {transform.eulerAngles.y:F1}°, {transform.eulerAngles.z:F1}°)");
            info.AppendLine($"**スケール:** ({transform.localScale.x:F2}, {transform.localScale.y:F2}, {transform.localScale.z:F2})");
            info.AppendLine();

            // コンポーネント情報
            info.AppendLine($"## ⚙️ コンポーネント ({validComponents.Length}件)");
            foreach (var component in validComponents)
            {
                var componentName = component.GetType().Name;
                var componentIcon = componentName switch
                {
                    "Transform" => "🔄",
                    "Camera" => "📷",  
                    "Light" => "💡",
                    "Renderer" or "MeshRenderer" or "SkinnedMeshRenderer" => "🎨",
                    "Collider" or "BoxCollider" or "SphereCollider" or "MeshCollider" => "🎯",
                    "Rigidbody" => "⚛️",
                    _ when componentName.Contains("UI") || componentName.Contains("Canvas") => "🖼️",
                    _ when componentName.Contains("Audio") => "🔊",
                    _ => "⚙️"
                };
                
                var enabled = component is Behaviour behaviour ? (behaviour.enabled ? "✅" : "❌") : "";
                info.AppendLine($"{componentIcon} **{componentName}** {enabled}");
            }

            // 子オブジェクト
            if (transform.childCount > 0)
            {
                info.AppendLine();
                info.AppendLine($"## 👶 子オブジェクト ({transform.childCount}件)");
                for (int i = 0; i < Math.Min(transform.childCount, 10); i++)
                {
                    var child = transform.GetChild(i);
                    var childIcon = GetGameObjectIcon(child.gameObject);
                    info.AppendLine($"{childIcon} **{child.name}**");
                }
                if (transform.childCount > 10)
                {
                    info.AppendLine($"   ...他 {transform.childCount - 10}件");
                }
            }

            return new StandardResponse
            {
                Success = true,
                FormattedOutput = info.ToString()
            };
        }

        /// <summary>Missing Script（Nullコンポーネント）を持つGameObjectを検出</summary>
        [McpServerTool, Description("シーン内のMissing Script（Nullコンポーネント）を持つGameObjectを検出して詳細情報を取得")]
        public async ValueTask<object> DetectMissingScripts(
            [Description("検索対象（All/ActiveOnly/InactiveOnly）")] string searchScope = "All",
            [Description("詳細情報を含めるか")] bool includeDetails = true)
        {
            await UniTask.SwitchToMainThread();

            var info = new System.Text.StringBuilder();
            info.AppendLine("=== Missing Script 検出結果 ===");
            info.AppendLine($"**検索時刻:** {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            info.AppendLine($"**Unity バージョン:** {Application.unityVersion}");
            info.AppendLine($"**検索範囲:** {searchScope}");
            info.AppendLine();

            // 検索対象のGameObjectを取得
            var allGameObjects = new List<GameObject>();
            var loadedScenes = new List<UnityEngine.SceneManagement.Scene>();
            
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
            {
                var scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
                if (scene.isLoaded)
                {
                    loadedScenes.Add(scene);
                    var rootObjects = scene.GetRootGameObjects();
                    foreach (var root in rootObjects)
                    {
                        if (ShouldIncludeObject(root, searchScope))
                        {
                            allGameObjects.Add(root);
                            allGameObjects.AddRange(GetAllChildGameObjects(root));
                        }
                    }
                }
            }

            // Missing Scriptを持つGameObjectを検出
            var missingScriptObjects = new List<MissingScriptInfo>();
            int totalMissingCount = 0;

            foreach (var go in allGameObjects)
            {
                var components = go.GetComponents<Component>();
                var missingCount = components.Count(c => c == null);
                
                if (missingCount > 0)
                {
                    totalMissingCount += missingCount;
                    missingScriptObjects.Add(new MissingScriptInfo
                    {
                        GameObject = go,
                        MissingCount = missingCount,
                        TotalComponents = components.Length,
                        Path = GetGameObjectPath(go),
                        SceneName = go.scene.name
                    });
                }
            }

            // 統計情報
            info.AppendLine("## 📊 統計情報");
            info.AppendLine($"**検査GameObject数:** {allGameObjects.Count}件");
            info.AppendLine($"**問題のあるGameObject数:** {missingScriptObjects.Count}件");
            info.AppendLine($"**Missing Script総数:** {totalMissingCount}件");
            
            if (missingScriptObjects.Count > 0)
            {
                var avgMissing = (float)totalMissingCount / missingScriptObjects.Count;
                info.AppendLine($"**平均Missing数/GameObject:** {avgMissing:F1}件");
            }
            info.AppendLine();

            // 問題のあるGameObjectリスト
            if (missingScriptObjects.Count > 0)
            {
                info.AppendLine("## ⚠️ Missing Scriptを持つGameObject一覧");
                
                // シーンごとにグループ化
                var groupedByScene = missingScriptObjects.GroupBy(x => x.SceneName);
                
                foreach (var sceneGroup in groupedByScene)
                {
                    info.AppendLine($"\n### 📋 シーン: {sceneGroup.Key}");
                    
                    int displayCount = 0;
                    foreach (var item in sceneGroup.OrderByDescending(x => x.MissingCount).Take(50))
                    {
                        displayCount++;
                        var icon = item.GameObject.activeInHierarchy ? "🔴" : "⚫";
                        info.AppendLine($"{icon} **{item.Path}**");
                        info.AppendLine($"   Missing: {item.MissingCount}個 / 全{item.TotalComponents}個のコンポーネント");
                        
                        if (includeDetails)
                        {
                            // 有効なコンポーネントの詳細
                            var validComponents = item.GameObject.GetComponents<Component>()
                                .Where(c => c != null)
                                .Select(c => c.GetType().Name)
                                .ToArray();
                            
                            if (validComponents.Length > 0)
                            {
                                info.AppendLine($"   有効: {string.Join(", ", validComponents.Take(5))}");
                                if (validComponents.Length > 5)
                                {
                                    info.AppendLine($"   ...他 {validComponents.Length - 5}個");
                                }
                            }
                        }
                        info.AppendLine();
                    }
                    
                    if (sceneGroup.Count() > 50)
                    {
                        info.AppendLine($"   ...他 {sceneGroup.Count() - 50}個のGameObject");
                    }
                }
                
                // 推奨アクション
                info.AppendLine("\n## 💡 推奨アクション");
                info.AppendLine("1. **スクリプトの復元**: 削除したスクリプトが必要な場合は、バージョン管理から復元");
                info.AppendLine("2. **コンポーネントの削除**: 不要な場合は、Missing Scriptコンポーネントを手動で削除");
                info.AppendLine("3. **一括クリーンアップ**: EditorスクリプトでMissing Scriptを一括削除");
                info.AppendLine("4. **参照の更新**: スクリプトを移動/リネームした場合は、正しい参照に更新");
            }
            else
            {
                info.AppendLine("## ✅ 結果");
                info.AppendLine("**Missing Scriptは検出されませんでした！**");
                info.AppendLine("すべてのGameObjectのコンポーネントは正常です。");
            }

            return new StandardResponse
            {
                Success = true,
                FormattedOutput = info.ToString()
            };
        }

        IEnumerable<GameObject> GetAllChildGameObjects(GameObject parent)
        {
            foreach (Transform child in parent.transform)
            {
                yield return child.gameObject;
                foreach (var grandChild in GetAllChildGameObjects(child.gameObject))
                {
                    yield return grandChild;
                }
            }
        }

        void DisplayGameObjectHierarchy(GameObject gameObject, System.Text.StringBuilder info, string prefix, bool isLast)
        {
            // アイコンとオブジェクト情報
            var components = gameObject.GetComponents<Component>();
            var validComponents = components.Where(c => c != null).ToArray();
            var icon = GetGameObjectIcon(gameObject);
            var statusIcon = gameObject.activeInHierarchy ? "✅" : "❌";
            
            // 階層表示用の線
            var connector = isLast ? "└─ " : "├─ ";
            var childPrefix = isLast ? "    " : "│   ";

            info.AppendLine($"{prefix}{connector}{statusIcon} {icon} **{gameObject.name}** ({validComponents.Length}コンポーネント)");
            info.AppendLine($"{prefix}{childPrefix}   Tag: {gameObject.tag} | Layer: {LayerMask.LayerToName(gameObject.layer)}");
            
            // 有効なコンポーネント一覧（簡潔に）
            if (validComponents.Length > 0)
            {
                var componentNames = validComponents.Select(c => c.GetType().Name).Take(3);
                var componentList = string.Join(", ", componentNames);
                if (validComponents.Length > 3)
                {
                    componentList += $", ...他{validComponents.Length - 3}個";
                }
                info.AppendLine($"{prefix}{childPrefix}   Components: {componentList}");
            }
            
            // 子オブジェクトの処理
            var childCount = gameObject.transform.childCount;
            for (int i = 0; i < childCount; i++)
            {
                var child = gameObject.transform.GetChild(i).gameObject;
                var isLastChild = (i == childCount - 1);
                DisplayGameObjectHierarchy(child, info, prefix + childPrefix, isLastChild);
            }
        }

        bool ShouldIncludeObject(GameObject go, string searchScope)
        {
            return searchScope switch
            {
                "ActiveOnly" => go.activeInHierarchy,
                "InactiveOnly" => !go.activeInHierarchy,
                _ => true // "All"
            };
        }

        class MissingScriptInfo
        {
            public GameObject GameObject { get; set; }
            public int MissingCount { get; set; }
            public int TotalComponents { get; set; }
            public string Path { get; set; }
            public string SceneName { get; set; }
        }

        /// <summary>GameObjectの階層パスを取得</summary>
        string GetGameObjectPath(GameObject gameObject)
        {
            var path = gameObject.name;
            var parent = gameObject.transform.parent;
            
            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }
            
            return path;
        }

        /// <summary>子オブジェクトを再帰的に取得</summary>
        IEnumerable<string> GetChildrenRecursive(Transform parent)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                yield return child.name;
                
                foreach (var grandChild in GetChildrenRecursive(child))
                {
                    yield return child.name + "/" + grandChild;
                }
            }
        }

        /// <summary>階層構造をテキスト形式で構築</summary>
        void BuildHierarchyText(HierarchyNode node, System.Text.StringBuilder sb, string prefix, int depth)
        {
            if (depth > 10) // 深すぎる階層は制限
            {
                sb.AppendLine($"{prefix}... (階層が深すぎます)");
                return;
            }

            var icon = node.Type switch
            {
                "UI Canvas" => "🖼️",
                "Camera" => "📷",
                "Light" => "💡",
                "Audio" => "🔊",
                "Empty" => "📦",
                _ => "🎮"
            };

            var status = node.Active ? "" : " (非アクティブ)";
            var issues = node.Issues?.Length > 0 ? $" ⚠️({node.Issues.Length})" : "";
            
            sb.AppendLine($"{prefix}{icon} **{node.Name}**{status}{issues}");
            sb.AppendLine($"{prefix}   {node.ComponentCount}コンポーネント | Tag: {node.Tag} | Layer: {node.Layer}");

            if (node.Issues?.Length > 0)
            {
                foreach (var issue in node.Issues.Take(2)) // 最大2件まで表示
                {
                    sb.AppendLine($"{prefix}   ⚠️ {issue}");
                }
            }

            if (node.Children?.Length > 0)
            {
                var childPrefix = prefix + "  ";
                foreach (var child in node.Children.Take(20)) // 最大20件まで表示
                {
                    BuildHierarchyText(child, sb, childPrefix, depth + 1);
                }
                
                if (node.Children.Length > 20)
                {
                    sb.AppendLine($"{childPrefix}... 他 {node.Children.Length - 20}件の子オブジェクト");
                }
            }
        }

        /// <summary>GameObjectの種類を推定</summary>
        string GetGameObjectIcon(GameObject gameObject)
        {
            var type = GetGameObjectType(gameObject);
            return type switch
            {
                "UI Canvas" => "🖼️",
                "Camera" => "📷",
                "Light" => "💡",
                "Audio" => "🔊",
                "Empty" => "📦",
                _ => "🎮"
            };
        }

        string GetGameObjectType(GameObject gameObject)
        {
            var components = gameObject.GetComponents<Component>();
            // Componentのnullチェック
            var validComponents = components.Where(c => c != null).ToArray();
            
            if (validComponents.Any(c => c.GetType().Name == "Canvas"))
                return "UI Canvas";
            if (validComponents.Any(c => c.GetType().Name == "Camera"))
                return "Camera";
            if (validComponents.Any(c => c.GetType().Name == "Light"))
                return "Light";
            if (validComponents.Any(c => c.GetType().Name == "Renderer"))
                return "Rendered Object";
            if (validComponents.Any(c => c.GetType().Name.Contains("UI") || c.GetType().Namespace == "UnityEngine.UI"))
                return "UI Element";
            if (validComponents.Any(c => c.GetType().Name.Contains("Collider")))
                return "Physics Object";
            if (validComponents.Any(c => c.GetType().Name == "AudioSource"))
                return "Audio Source";
                
            return "GameObject";
        }

        /// <summary>コンポーネントのカテゴリを取得</summary>
        string GetComponentCategory(Type componentType)
        {
            var typeName = componentType.Name;
            var namespaceName = componentType.Namespace;
            
            if (typeName.Contains("Transform"))
                return "Transform";
            if (namespaceName == "UnityEngine.UI")
                return "UI";
            if (typeName.Contains("Renderer") || typeName.Contains("Material"))
                return "Rendering";
            if (typeName.Contains("Collider") || typeName.Contains("Rigidbody"))
                return "Physics";
            if (typeName.Contains("Audio"))
                return "Audio";
            if (typeName.Contains("Light"))
                return "Lighting";
            if (typeName.Contains("Camera"))
                return "Camera";
            if (typeName.Contains("Animation") || typeName.Contains("Animator"))
                return "Animation";
            if (namespaceName?.Contains("UnityEngine") == true)
                return "Unity Built-in";
            // カスタムUIコンポーネントの判定を追加
            if (typeName.Contains("UI") && namespaceName != "UnityEngine.UI")
                return "Custom UI";
                
            return "Custom";
        }

        /// <summary>コンポーネントの説明を取得</summary>
        string GetComponentDescription(Type componentType)
        {
            var typeName = componentType.Name;
            
            return typeName switch
            {
                "Transform" => "Position, rotation, and scale of the object",
                "RectTransform" => "UI transform component with anchoring and sizing",
                "Canvas" => "UI rendering component",
                "CanvasScaler" => "Controls UI scaling behavior",
                "GraphicRaycaster" => "Handles UI input events",
                "Camera" => "Renders the scene from a specific viewpoint",
                "Light" => "Provides lighting to the scene",
                "MeshRenderer" => "Renders 3D mesh geometry",
                "SpriteRenderer" => "Renders 2D sprites",
                "Rigidbody" => "Enables physics simulation",
                "BoxCollider" => "Box-shaped collision detection",
                "AudioSource" => "Plays audio clips",
                _ => $"{typeName} component"
            };
        }

        /// <summary>階層の深さを取得</summary>
        int GetHierarchyDepth(Transform transform)
        {
            int depth = 0;
            var parent = transform.parent;
            
            while (parent != null)
            {
                depth++;
                parent = parent.parent;
            }
            
            return depth;
        }

        /// <summary>コンポーネントのプロパティを取得</summary>
        Dictionary<string, object> GetComponentProperties(Component component)
        {
            if (component == null) return null;

            var componentType = component.GetType();
            var properties = new Dictionary<string, object>();

            // Unity組み込みコンポーネントは基本情報のみ
            if (componentType.Namespace?.StartsWith("UnityEngine") == true)
            {
                return GetUnityComponentProperties(component);
            }

            // カスタムコンポーネントは詳細解析
            try
            {
                return GetCustomComponentProperties(component);
            }
            catch (Exception ex)
            {
                return new Dictionary<string, object>
                {
                    ["error"] = $"Failed to analyze properties: {ex.Message}"
                };
            }
        }

        /// <summary>Unity組み込みコンポーネントの基本プロパティを取得</summary>
        Dictionary<string, object> GetUnityComponentProperties(Component component)
        {
            var properties = new Dictionary<string, object>();
            var componentType = component.GetType();

            switch (componentType.Name)
            {
                case "Transform":
                    var transform = component as Transform;
                    properties["childCount"] = transform.childCount;
                    properties["hasChanged"] = transform.hasChanged;
                    break;

                case "RectTransform":
                    var rectTransform = component as RectTransform;
                    properties["anchoredPosition"] = FormatVector2(rectTransform.anchoredPosition);
                    properties["sizeDelta"] = FormatVector2(rectTransform.sizeDelta);
                    properties["anchorMin"] = FormatVector2(rectTransform.anchorMin);
                    properties["anchorMax"] = FormatVector2(rectTransform.anchorMax);
                    break;

                case "Image":
                    var image = component as UnityEngine.UI.Image;
                    properties["sprite"] = GetObjectReference(image.sprite);
                    properties["color"] = FormatColor(image.color);
                    properties["type"] = image.type.ToString();
                    properties["fillAmount"] = image.fillAmount;
                    break;

                case "Button":
                    var button = component as UnityEngine.UI.Button;
                    properties["interactable"] = button.interactable;
                    properties["targetGraphic"] = GetObjectReference(button.targetGraphic);
                    break;

                case "Canvas":
                    var canvas = component as Canvas;
                    properties["renderMode"] = canvas.renderMode.ToString();
                    properties["sortingOrder"] = canvas.sortingOrder;
                    break;
            }

            return properties.Count > 0 ? properties : null;
        }

        /// <summary>カスタムコンポーネントの詳細プロパティを取得</summary>
        Dictionary<string, object> GetCustomComponentProperties(Component component)
        {
            var properties = new Dictionary<string, object>();
            var componentType = component.GetType();

            // Publicフィールドを取得
            var publicFields = componentType.GetFields(BindingFlags.Public | BindingFlags.Instance);
            foreach (var field in publicFields)
            {
                if (ShouldIncludeField(field))
                {
                    try
                    {
                        var value = field.GetValue(component);
                        properties[field.Name] = ConvertToJsonFriendly(value, field.FieldType);
                    }
                    catch (Exception ex)
                    {
                        properties[field.Name] = $"Error: {ex.Message}";
                    }
                }
            }

            // SerializeFieldを取得
            var serializedFields = componentType.GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(f => f.GetCustomAttribute<SerializeField>() != null);

            foreach (var field in serializedFields)
            {
                if (ShouldIncludeField(field))
                {
                    try
                    {
                        var value = field.GetValue(component);
                        properties[field.Name] = ConvertToJsonFriendly(value, field.FieldType);
                    }
                    catch (Exception ex)
                    {
                        properties[field.Name] = $"Error: {ex.Message}";
                    }
                }
            }

            // Publicプロパティを取得（setter付きのもの）
            var publicProperties = componentType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && p.CanWrite && p.GetIndexParameters().Length == 0);

            foreach (var property in publicProperties)
            {
                if (ShouldIncludeProperty(property))
                {
                    try
                    {
                        var value = property.GetValue(component);
                        properties[property.Name] = ConvertToJsonFriendly(value, property.PropertyType);
                    }
                    catch (Exception ex)
                    {
                        properties[property.Name] = $"Error: {ex.Message}";
                    }
                }
            }

            return properties.Count > 0 ? properties : null;
        }

        /// <summary>フィールドを含めるべきかチェック</summary>
        bool ShouldIncludeField(FieldInfo field)
        {
            // 除外するフィールド
            var excludeNames = new[] { "m_", "k_", "_" };
            if (excludeNames.Any(exclude => field.Name.StartsWith(exclude)))
                return false;

            // Unity内部フィールドを除外
            if (field.FieldType.Namespace?.StartsWith("UnityEngine") == true &&
                (field.FieldType.Name.Contains("Internal") || field.FieldType.Name.StartsWith("_")))
                return false;

            return true;
        }

        /// <summary>プロパティを含めるべきかチェック</summary>
        bool ShouldIncludeProperty(PropertyInfo property)
        {
            // 除外するプロパティ名
            var excludeNames = new[] { "hideFlags", "name", "tag", "transform", "gameObject" };
            if (excludeNames.Contains(property.Name))
                return false;

            return true;
        }

        /// <summary>値をJSON対応の形式に変換</summary>
        object ConvertToJsonFriendly(object value, Type valueType)
        {
            if (value == null) return null;

            // プリミティブ型
            if (valueType.IsPrimitive || valueType == typeof(string))
                return value;

            // Unity Vector types
            if (valueType == typeof(Vector2))
            {
                var v = (Vector2)value;
                return FormatVector2(v);
            }
            if (valueType == typeof(Vector3))
            {
                var v = (Vector3)value;
                return $"({v.x:F2}, {v.y:F2}, {v.z:F2})";
            }
            if (valueType == typeof(Vector4))
            {
                var v = (Vector4)value;
                return $"({v.x:F2}, {v.y:F2}, {v.z:F2}, {v.w:F2})";
            }

            // Color
            if (valueType == typeof(Color))
            {
                return FormatColor((Color)value);
            }

            // UnityEngine.Object参照
            if (typeof(Object).IsAssignableFrom(valueType))
            {
                return GetObjectReference(value as Object);
            }

            // Enum
            if (valueType.IsEnum)
            {
                return value.ToString();
            }

            // カスタムクラス（Serializable）
            if (valueType.GetCustomAttribute<SerializableAttribute>() != null)
            {
                return SerializeCustomObject(value);
            }

            // その他は文字列表現
            return value.ToString();
        }

        /// <summary>カスタムオブジェクトをシリアライズ</summary>
        object SerializeCustomObject(object obj)
        {
            if (obj == null) return null;

            var result = new Dictionary<string, object>();
            var objType = obj.GetType();

            var publicFields = objType.GetFields(BindingFlags.Public | BindingFlags.Instance);
            foreach (var field in publicFields)
            {
                try
                {
                    var value = field.GetValue(obj);
                    result[field.Name] = ConvertToJsonFriendly(value, field.FieldType);
                }
                catch
                {
                    result[field.Name] = "Error reading field";
                }
            }

            return result.Count > 0 ? result : obj.ToString();
        }

        /// <summary>Vector2を辞書形式に変換</summary>
        object FormatVector2(Vector2 vector)
        {
            return $"({vector.x:F2}, {vector.y:F2})";
        }

        /// <summary>Colorを辞書形式に変換</summary>
        object FormatColor(Color color)
        {
            return $"RGBA({color.r:F2}, {color.g:F2}, {color.b:F2}, {color.a:F2}) #{ColorUtility.ToHtmlStringRGBA(color)}";
        }

        /// <summary>UnityEngine.Object参照の状態を取得</summary>
        object GetObjectReference(Object obj)
        {
            if (obj == null) return "null";
            
            return $"{obj.name} ({obj.GetType().Name})";
        }

        /// <summary>階層構造を分析</summary>


        /// <summary>階層ノードを構築</summary>


        /// <summary>すべての子オブジェクトを収集</summary>


        /// <summary>最大深度を計算</summary>
        int CalculateMaxDepth(GameObject rootObject)
        {
            int maxDepth = 0;
            CalculateDepthRecursive(rootObject.transform, 0, ref maxDepth);
            return maxDepth;
        }

        void CalculateDepthRecursive(Transform transform, int currentDepth, ref int maxDepth)
        {
            maxDepth = Math.Max(maxDepth, currentDepth);
            for (int i = 0; i < transform.childCount; i++)
            {
                CalculateDepthRecursive(transform.GetChild(i), currentDepth + 1, ref maxDepth);
            }
        }

        /// <summary>UI要素を分析</summary>


        /// <summary>パフォーマンス問題を分析</summary>


        /// <summary>デザイン問題を分析</summary>


        /// <summary>参照の欠落を分析</summary>


        /// <summary>推奨事項を生成</summary>


        /// <summary>重要なコンポーネントかチェック</summary>


        /// <summary>オブジェクト固有の問題を分析</summary>


        /// <summary>参照の欠落をチェック</summary>


        /// <summary>UI構造のタイプを判定</summary>

    }






}

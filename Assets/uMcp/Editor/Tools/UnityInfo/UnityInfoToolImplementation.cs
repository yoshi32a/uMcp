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
        [McpServerTool, Description("Unity エディターとプロジェクトの詳細情報を取得")]
        public async ValueTask<object> GetUnityInfo()
        {
            await UniTask.SwitchToMainThread();

            return new UnityInfoResponse
            {
                UnityVersion = Application.unityVersion,
                Platform = Application.platform.ToString(),
                ProjectName = Application.productName,
                CompanyName = Application.companyName,
                DataPath = Application.dataPath,
                PersistentDataPath = Application.persistentDataPath,
                StreamingAssetsPath = Application.streamingAssetsPath,
                IsPlaying = Application.isPlaying,
                IsFocused = Application.isFocused,
                SystemLanguage = Application.systemLanguage.ToString(),
                EditorInfo = new EditorInfo
                {
                    IsBatchMode = Application.isBatchMode,
                    BuildGuid = Application.buildGUID,
                    CloudProjectId = Application.cloudProjectId
                }
            };
        }

        /// <summary>現在のシーン情報を取得</summary>
        [McpServerTool, Description("現在のシーン情報とGameObjectの一覧を取得")]
        public async ValueTask<object> GetSceneInfo()
        {
            await UniTask.SwitchToMainThread();

            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            var gameObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);

            return new SceneInfoResponse
            {
                SceneName = scene.name,
                ScenePath = scene.path,
                SceneBuildIndex = scene.buildIndex,
                IsLoaded = scene.isLoaded,
                IsDirty = scene.isDirty,
                GameObjectCount = gameObjects.Length,
                RootGameObjects = System.Array.ConvertAll(scene.GetRootGameObjects(), go => new GameObjectInfo
                {
                    Name = go.name,
                    Active = go.activeSelf,
                    Tag = go.tag,
                    Layer = LayerMask.LayerToName(go.layer),
                    ComponentCount = go.GetComponents<Component>().Length
                })
            };
        }

        /// <summary>指定したGameObjectの階層構造を分析</summary>
        [McpServerTool, Description("指定したGameObjectとその子階層の構造を詳細分析")]
        public async ValueTask<object> GetHierarchyAnalysis(
            [Description("分析対象のGameObjectの名前（デフォルト：Canvas）")] string gameObjectName = "Canvas")
        {
            await UniTask.SwitchToMainThread();

            if (string.IsNullOrEmpty(gameObjectName))
                gameObjectName = "Canvas";

            // GameObjectを探す
            var gameObject = GameObject.Find(gameObjectName);
            if (gameObject == null)
            {
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

            var analysis = AnalyzeHierarchy(gameObject);

            return new HierarchyAnalysisResponse
            {
                Success = true,
                ObjectType = "hierarchy_analysis",
                AnalysisTimestamp = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                UnityVersion = Application.unityVersion,
                RootObject = BuildHierarchyNode(gameObject),
                Analysis = analysis
            };
        }

        /// <summary>指定したGameObjectの詳細情報を取得</summary>
        [McpServerTool, Description("指定したGameObjectの詳細情報を取得")]
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
            var transform = gameObject.transform;

            return new GameObjectAnalysisResponse
            {
                Success = true,
                ObjectType = "gameobject",
                AnalysisTimestamp = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                UnityVersion = Application.unityVersion,
                Summary = new GameObjectSummary
                {
                    Name = gameObject.name,
                    Type = GetGameObjectType(gameObject),
                    ComponentCount = components.Length,
                    ChildCount = transform.childCount,
                    Active = gameObject.activeSelf,
                    ActiveInHierarchy = gameObject.activeInHierarchy,
                    Tag = gameObject.tag,
                    Layer = LayerMask.LayerToName(gameObject.layer)
                },
                Transform = new TransformInfo
                {
                    Position = new Vector3Info
                    {
                        X = transform.localPosition.x,
                        Y = transform.localPosition.y,
                        Z = transform.localPosition.z
                    },
                    Rotation = new Vector3Info
                    {
                        X = transform.localEulerAngles.x,
                        Y = transform.localEulerAngles.y,
                        Z = transform.localEulerAngles.z
                    },
                    Scale = new Vector3Info
                    {
                        X = transform.localScale.x,
                        Y = transform.localScale.y,
                        Z = transform.localScale.z
                    },
                    WorldPosition = new Vector3Info
                    {
                        X = transform.position.x,
                        Y = transform.position.y,
                        Z = transform.position.z
                    },
                    WorldRotation = new Vector3Info
                    {
                        X = transform.eulerAngles.x,
                        Y = transform.eulerAngles.y,
                        Z = transform.eulerAngles.z
                    },
                    WorldScale = new Vector3Info
                    {
                        X = transform.lossyScale.x,
                        Y = transform.lossyScale.y,
                        Z = transform.lossyScale.z
                    }
                },
                Components = components.Where(c => c != null).Select(c => new EnhancedComponentInfo
                {
                    Type = c.GetType().Name,
                    FullType = c.GetType().FullName,
                    Category = GetComponentCategory(c.GetType()),
                    Enabled = c is Behaviour behaviour ? behaviour.enabled : true,
                    Description = GetComponentDescription(c.GetType()),
                    Properties = GetComponentProperties(c)
                }).ToArray(),
                Hierarchy = new HierarchyInfo
                {
                    FullPath = GetGameObjectPath(gameObject),
                    Depth = GetHierarchyDepth(transform),
                    Parent = transform.parent?.name,
                    Children = System.Linq.Enumerable.Range(0, transform.childCount)
                        .Select(i => transform.GetChild(i).name).ToArray(),
                    SiblingIndex = transform.GetSiblingIndex()
                }
            };
        }

        /// <summary>指定したPrefabの詳細情報を取得</summary>
        [McpServerTool, Description("指定したPrefabの詳細情報を取得")]
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
            var transform = prefabAsset.transform;

            return new PrefabDetailResponse
            {
                Success = true,
                Name = prefabAsset.name,
                AssetPath = prefabPath,
                Tag = prefabAsset.tag,
                Layer = LayerMask.LayerToName(prefabAsset.layer),
                Position = new Vector3Info
                {
                    X = transform.position.x,
                    Y = transform.position.y,
                    Z = transform.position.z
                },
                Rotation = new Vector3Info
                {
                    X = transform.eulerAngles.x,
                    Y = transform.eulerAngles.y,
                    Z = transform.eulerAngles.z
                },
                Scale = new Vector3Info
                {
                    X = transform.localScale.x,
                    Y = transform.localScale.y,
                    Z = transform.localScale.z
                },
                Components = components.Where(c => c != null).Select(c => new ComponentInfo
                {
                    Type = c.GetType().Name,
                    FullType = c.GetType().FullName,
                    Enabled = c is Behaviour behaviour ? behaviour.enabled : true
                }).ToArray(),
                ChildCount = transform.childCount,
                Children = GetChildrenRecursive(transform).ToArray()
            };
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

        /// <summary>GameObjectの種類を推定</summary>
        string GetGameObjectType(GameObject gameObject)
        {
            var components = gameObject.GetComponents<Component>();
            
            if (components.Any(c => c.GetType().Name == "Canvas"))
                return "UI Canvas";
            if (components.Any(c => c.GetType().Name == "Camera"))
                return "Camera";
            if (components.Any(c => c.GetType().Name == "Light"))
                return "Light";
            if (components.Any(c => c.GetType().Name == "Renderer"))
                return "Rendered Object";
            if (components.Any(c => c.GetType().Name.Contains("UI") || c.GetType().Namespace == "UnityEngine.UI"))
                return "UI Element";
            if (components.Any(c => c.GetType().Name.Contains("Collider")))
                return "Physics Object";
            if (components.Any(c => c.GetType().Name == "AudioSource"))
                return "Audio Source";
                
            return "GameObject";
        }

        /// <summary>コンポーネントのカテゴリを取得</summary>
        string GetComponentCategory(System.Type componentType)
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
        string GetComponentDescription(System.Type componentType)
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
                return new { x = v.x, y = v.y, z = v.z };
            }
            if (valueType == typeof(Vector4))
            {
                var v = (Vector4)value;
                return new { x = v.x, y = v.y, z = v.z, w = v.w };
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
            if (valueType.GetCustomAttribute<System.SerializableAttribute>() != null)
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
            return new { x = vector.x, y = vector.y };
        }

        /// <summary>Colorを辞書形式に変換</summary>
        object FormatColor(Color color)
        {
            return new 
            { 
                r = color.r, 
                g = color.g, 
                b = color.b, 
                a = color.a,
                hex = ColorUtility.ToHtmlStringRGBA(color)
            };
        }

        /// <summary>UnityEngine.Object参照の状態を取得</summary>
        object GetObjectReference(Object obj)
        {
            if (obj == null) return "null";
            
            return new
            {
                name = obj.name,
                type = obj.GetType().Name,
                instanceId = obj.GetInstanceID(),
                status = "connected"
            };
        }

        /// <summary>階層構造を分析</summary>
        HierarchyAnalysis AnalyzeHierarchy(GameObject rootObject)
        {
            var allObjects = new List<GameObject>();
            var uiElements = new UIAnalysis();
            var performanceConcerns = new List<string>();
            var designIssues = new List<string>();
            var missingReferences = new List<string>();
            var recommendations = new List<string>();

            CollectAllChildren(rootObject, allObjects);
            
            int maxDepth = CalculateMaxDepth(rootObject);
            AnalyzeUIElements(allObjects, uiElements);
            AnalyzePerformance(allObjects, performanceConcerns);
            AnalyzeDesign(allObjects, designIssues);
            AnalyzeMissingReferences(allObjects, missingReferences);
            GenerateRecommendations(allObjects, uiElements, recommendations);

            return new HierarchyAnalysis
            {
                TotalObjects = allObjects.Count,
                MaxDepth = maxDepth,
                UIElements = uiElements,
                PerformanceConcerns = performanceConcerns.ToArray(),
                DesignIssues = designIssues.ToArray(),
                MissingReferences = missingReferences.ToArray(),
                Recommendations = recommendations.ToArray()
            };
        }

        /// <summary>階層ノードを構築</summary>
        HierarchyNode BuildHierarchyNode(GameObject gameObject)
        {
            var components = gameObject.GetComponents<Component>();
            var keyComponents = components.Where(c => c != null)
                .Select(c => c.GetType().Name)
                .Where(name => IsKeyComponent(name))
                .ToArray();

            var issues = new List<string>();
            AnalyzeObjectIssues(gameObject, issues);

            var children = new List<HierarchyNode>();
            for (int i = 0; i < gameObject.transform.childCount; i++)
            {
                var child = gameObject.transform.GetChild(i).gameObject;
                children.Add(BuildHierarchyNode(child));
            }

            return new HierarchyNode
            {
                Name = gameObject.name,
                Type = GetGameObjectType(gameObject),
                Active = gameObject.activeSelf,
                Tag = gameObject.tag,
                Layer = LayerMask.LayerToName(gameObject.layer),
                ComponentCount = components.Length,
                KeyComponents = keyComponents,
                Issues = issues.Count > 0 ? issues.ToArray() : null,
                Children = children.Count > 0 ? children.ToArray() : null
            };
        }

        /// <summary>すべての子オブジェクトを収集</summary>
        void CollectAllChildren(GameObject parent, List<GameObject> allObjects)
        {
            allObjects.Add(parent);
            for (int i = 0; i < parent.transform.childCount; i++)
            {
                CollectAllChildren(parent.transform.GetChild(i).gameObject, allObjects);
            }
        }

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
        void AnalyzeUIElements(List<GameObject> allObjects, UIAnalysis uiElements)
        {
            foreach (var obj in allObjects)
            {
                var components = obj.GetComponents<Component>();
                foreach (var component in components)
                {
                    if (component == null) continue;
                    
                    var typeName = component.GetType().Name;
                    switch (typeName)
                    {
                        case "Canvas":
                            uiElements.CanvasCount++;
                            break;
                        case "Button":
                            uiElements.ButtonCount++;
                            break;
                        case "Text":
                        case "TextMeshPro":
                        case "TextMeshProUGUI":
                            uiElements.TextCount++;
                            break;
                        case "Image":
                        case "RawImage":
                            uiElements.ImageCount++;
                            break;
                        case "InputField":
                        case "TMP_InputField":
                            uiElements.InputCount++;
                            break;
                        case "HorizontalLayoutGroup":
                        case "VerticalLayoutGroup":
                        case "GridLayoutGroup":
                            uiElements.LayoutGroups++;
                            break;
                        default:
                            if (component.GetType().Namespace != null && 
                                !component.GetType().Namespace.StartsWith("UnityEngine"))
                            {
                                uiElements.CustomUICount++;
                            }
                            break;
                    }
                }
            }

            uiElements.UIStructure = DetermineUIStructure(uiElements);
        }

        /// <summary>パフォーマンス問題を分析</summary>
        void AnalyzePerformance(List<GameObject> allObjects, List<string> concerns)
        {
            if (allObjects.Count > 100)
            {
                concerns.Add($"Large hierarchy: {allObjects.Count} objects may impact performance");
            }

            var canvasCount = allObjects.Count(obj => obj.GetComponent<Canvas>() != null);
            if (canvasCount > 3)
            {
                concerns.Add($"Multiple Canvas components ({canvasCount}) may cause overdraw");
            }

            var imageCount = allObjects.Count(obj => obj.GetComponent<UnityEngine.UI.Image>() != null);
            if (imageCount > 20)
            {
                concerns.Add($"Many Image components ({imageCount}) may impact fill rate");
            }
        }

        /// <summary>デザイン問題を分析</summary>
        void AnalyzeDesign(List<GameObject> allObjects, List<string> issues)
        {
            var inactiveObjects = allObjects.Where(obj => !obj.activeSelf).ToList();
            if (inactiveObjects.Count > allObjects.Count * 0.3f)
            {
                issues.Add($"Many inactive objects ({inactiveObjects.Count}/{allObjects.Count}) - consider cleanup");
            }

            var untaggedObjects = allObjects.Where(obj => obj.tag == "Untagged").ToList();
            if (untaggedObjects.Count > allObjects.Count * 0.8f)
            {
                issues.Add("Most objects are untagged - consider proper tagging for organization");
            }

            var layoutGroups = allObjects.Count(obj => 
                obj.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>() != null ||
                obj.GetComponent<UnityEngine.UI.VerticalLayoutGroup>() != null ||
                obj.GetComponent<UnityEngine.UI.GridLayoutGroup>() != null);
            
            var uiElements = allObjects.Count(obj => 
                obj.GetComponent<UnityEngine.UI.Image>() != null ||
                obj.GetComponent<UnityEngine.UI.Button>() != null);

            if (uiElements > 5 && layoutGroups == 0)
            {
                issues.Add("UI elements without layout management - consider using Layout Groups");
            }
        }

        /// <summary>参照の欠落を分析</summary>
        void AnalyzeMissingReferences(List<GameObject> allObjects, List<string> missing)
        {
            foreach (var obj in allObjects)
            {
                var components = obj.GetComponents<Component>();
                foreach (var component in components)
                {
                    if (component == null) continue;

                    // カスタムコンポーネントの参照チェック
                    if (component.GetType().Namespace != null && 
                        !component.GetType().Namespace.StartsWith("UnityEngine"))
                    {
                        CheckMissingReferences(component, missing);
                    }
                }
            }
        }

        /// <summary>推奨事項を生成</summary>
        void GenerateRecommendations(List<GameObject> allObjects, UIAnalysis uiElements, List<string> recommendations)
        {
            if (uiElements.LayoutGroups == 0 && (uiElements.ButtonCount + uiElements.ImageCount) > 3)
            {
                recommendations.Add("Add Layout Groups for better UI organization and responsive design");
            }

            if (uiElements.CustomUICount > 0)
            {
                recommendations.Add("Verify custom UI components have proper references and are functioning correctly");
            }

            if (allObjects.Count > 50)
            {
                recommendations.Add("Consider object pooling or LOD system for performance optimization");
            }

            var canvasObjects = allObjects.Where(obj => obj.GetComponent<Canvas>() != null).ToList();
            if (canvasObjects.Count > 1)
            {
                recommendations.Add("Multiple Canvas detected - ensure proper render order and consider merging if possible");
            }
        }

        /// <summary>重要なコンポーネントかチェック</summary>
        bool IsKeyComponent(string componentName)
        {
            var keyComponents = new[] 
            { 
                "Canvas", "Button", "Image", "Text", "TextMeshPro", "TextMeshProUGUI",
                "Slider", "InputField", "ScrollRect", "LayoutGroup", "ContentSizeFitter"
            };
            
            return keyComponents.Any(key => componentName.Contains(key)) || 
                   !componentName.StartsWith("UnityEngine");
        }

        /// <summary>オブジェクト固有の問題を分析</summary>
        void AnalyzeObjectIssues(GameObject gameObject, List<string> issues)
        {
            if (!gameObject.activeSelf)
            {
                issues.Add("Object is inactive");
            }

            var image = gameObject.GetComponent<UnityEngine.UI.Image>();
            if (image != null && image.sprite == null)
            {
                issues.Add("Image component without sprite");
            }

            var button = gameObject.GetComponent<UnityEngine.UI.Button>();
            if (button != null && !button.interactable)
            {
                issues.Add("Button is not interactable");
            }
        }

        /// <summary>参照の欠落をチェック</summary>
        void CheckMissingReferences(Component component, List<string> missing)
        {
            var componentType = component.GetType();
            var fields = componentType.GetFields(BindingFlags.Public | BindingFlags.Instance);
            
            foreach (var field in fields)
            {
                if (typeof(Object).IsAssignableFrom(field.FieldType))
                {
                    var value = field.GetValue(component);
                    if (value == null)
                    {
                        missing.Add($"{component.gameObject.name}.{componentType.Name}.{field.Name} is null");
                    }
                }
            }
        }

        /// <summary>UI構造のタイプを判定</summary>
        string DetermineUIStructure(UIAnalysis ui)
        {
            if (ui.CanvasCount == 0) return "Non-UI Structure";
            if (ui.LayoutGroups > 0) return "Managed Layout";
            if (ui.ButtonCount > 0 || ui.InputCount > 0) return "Interactive UI";
            if (ui.ImageCount > 0 || ui.TextCount > 0) return "Display UI";
            return "Basic Canvas";
        }
    }
}

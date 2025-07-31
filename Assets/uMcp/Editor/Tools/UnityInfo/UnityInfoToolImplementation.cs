using System;
using System.Collections.Generic;
using System.Linq;
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
                    Description = GetComponentDescription(c.GetType())
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
            if (namespaceName == "UnityEngine.UI" || typeName.Contains("UI"))
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
    }
}

using System;
using System.Collections.Generic;

namespace uMCP.Editor.Core.DependencyInjection
{
    /// <summary>簡易的なサービスコンテナ</summary>
    public class SimpleServiceContainer : IDisposable
    {
        readonly Dictionary<Type, object> services = new();
        readonly Dictionary<Type, Func<object>> factories = new();
        readonly List<IDisposable> disposables = new();

        /// <summary>登録されているサービスを取得</summary>
        public IReadOnlyDictionary<Type, object> Services => services;

        /// <summary>シングルトンサービスを登録</summary>
        public void AddSingleton<T>(T instance) where T : class
        {
            services[typeof(T)] = instance;
            if (instance is IDisposable disposable)
            {
                disposables.Add(disposable);
            }
        }

        /// <summary>シングルトンサービスをファクトリーで登録</summary>
        public void AddSingleton<T>(Func<T> factory) where T : class
        {
            factories[typeof(T)] = () => factory();
        }

        /// <summary>サービスを取得</summary>
        public T GetService<T>() where T : class
        {
            var type = typeof(T);

            if (services.TryGetValue(type, out var service))
            {
                return (T)service;
            }

            if (factories.TryGetValue(type, out var factory))
            {
                var instance = factory();
                services[type] = instance;
                if (instance is IDisposable disposable)
                {
                    disposables.Add(disposable);
                }

                return (T)instance;
            }

            return null;
        }

        /// <summary>サービスを取得（必須）</summary>
        public T GetRequiredService<T>() where T : class
        {
            var service = GetService<T>();
            if (service == null)
            {
                throw new InvalidOperationException($"Service of type {typeof(T)} not found");
            }

            return service;
        }

        /// <summary>リソースを解放</summary>
        public void Dispose()
        {
            foreach (var disposable in disposables)
            {
                try
                {
                    disposable?.Dispose();
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogError($"[uMCP] Error disposing service: {ex.Message}");
                }
            }

            disposables.Clear();
            services.Clear();
            factories.Clear();
        }
    }
}

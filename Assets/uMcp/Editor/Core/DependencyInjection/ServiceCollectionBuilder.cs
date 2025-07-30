using System;

namespace uMCP.Editor.Core.DependencyInjection
{
    /// <summary>サービスコレクションビルダー</summary>
    public class ServiceCollectionBuilder
    {
        readonly SimpleServiceContainer container = new();

        /// <summary>シングルトンサービスを追加</summary>
        public ServiceCollectionBuilder AddSingleton<T>(T instance) where T : class
        {
            container.AddSingleton(instance);
            return this;
        }

        /// <summary>シングルトンサービスをファクトリーで追加</summary>
        public ServiceCollectionBuilder AddSingleton<T>(Func<T> factory) where T : class
        {
            container.AddSingleton(factory);
            return this;
        }

        /// <summary>サービスコンテナを構築</summary>
        public SimpleServiceContainer Build()
        {
            return container;
        }
    }
}
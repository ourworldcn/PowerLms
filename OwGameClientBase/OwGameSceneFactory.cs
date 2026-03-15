using Microsoft.Extensions.DependencyInjection;

namespace OW.Game.Client;

/// <summary>
/// 游戏场景接口（所有场景的抽象）
/// 注意：由于客户端运行在多平台（WASM等）上，很多平台不支持多线程，
/// 因此所有方法都设计为同步模式。如果调用者需要异步，可自行封装。
/// </summary>
public interface IOwGameScene
{
    /// <summary>
    /// 场景名称
    /// </summary>
    string Name { get; }

    /// <summary>
    /// 渲染模式
    /// </summary>
    OwSceneRenderMode RenderMode { get; }

    /// <summary>
    /// 是否启用 ECS
    /// </summary>
    bool EnableECS { get; }

    /// <summary>
    /// 目标帧率
    /// </summary>
    int TargetFrameRate { get; }

    /// <summary>
    /// 场景加载（同步）
    /// </summary>
    void Load();

    /// <summary>
    /// 场景更新（逻辑）
    /// </summary>
    /// <param name="deltaTime">自上次更新以来的时间增量（秒）</param>
    void Update(double deltaTime);

    /// <summary>
    /// 场景渲染
    /// </summary>
    void Render();

    /// <summary>
    /// 场景卸载
    /// </summary>
    void Unload();
}

/// <summary>
/// 渲染模式枚举
/// </summary>
public enum OwSceneRenderMode
{
    /// <summary>
    /// Blazor DOM 渲染（适合静态 UI/菜单）
    /// </summary>
    BlazorDOM,

    /// <summary>
    /// Canvas 2D 渲染（适合中等规模战斗场景）
    /// </summary>
    Canvas2D,

    /// <summary>
    /// ECS + Canvas 渲染（适合大规模战斗场景）
    /// </summary>
    ECS_Canvas,

    /// <summary>
    /// 混合模式（Blazor UI + Canvas 游戏世界）
    /// </summary>
    Hybrid
}

/// <summary>
/// 场景配置
/// </summary>
public class OwSceneConfiguration
{
    /// <summary>
    /// 场景名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 场景类型（完全限定类名）
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// 渲染模式
    /// </summary>
    public string RenderMode { get; set; } = "BlazorDOM";

    /// <summary>
    /// 是否启用 ECS
    /// </summary>
    public bool EnableECS { get; set; } = false;

    /// <summary>
    /// 目标帧率
    /// </summary>
    public int TargetFrameRate { get; set; } = 60;

    /// <summary>
    /// 预加载资源
    /// </summary>
    public List<string> PreloadAssets { get; set; } = new();

    /// <summary>
    /// 扩展配置（场景特定的配置数据）
    /// </summary>
    public Dictionary<string, object> ExtendedSettings { get; set; } = new();
}

/// <summary>
/// 场景工厂接口（抽象工厂模式）
/// 职责：根据配置创建场景实例
/// 注意：所有方法都是同步的，避免多线程问题
/// </summary>
public interface IOwGameSceneFactory
{
    /// <summary>
    /// 创建场景实例
    /// </summary>
    /// <param name="sceneName">场景名称（来自配置文件）</param>
    /// <returns>场景实例</returns>
    /// <exception cref="ArgumentException">场景名称不存在</exception>
    /// <exception cref="InvalidOperationException">场景类型无法创建</exception>
    IOwGameScene CreateScene(string sceneName);

    /// <summary>
    /// 检查场景是否存在
    /// </summary>
    /// <param name="sceneName">场景名称</param>
    /// <returns>如果场景配置存在返回 true，否则返回 false</returns>
    bool SceneExists(string sceneName);

    /// <summary>
    /// 获取所有可用场景名称
    /// </summary>
    /// <returns>场景名称列表</returns>
    IEnumerable<string> GetAvailableScenes();

    /// <summary>
    /// 获取场景配置
    /// </summary>
    /// <param name="sceneName">场景名称</param>
    /// <returns>场景配置，如果不存在返回 null</returns>
    OwSceneConfiguration? GetSceneConfiguration(string sceneName);
}

/// <summary>
/// 场景工厂默认实现（基于配置和反射）
/// </summary>
public class OwGameSceneFactory : IOwGameSceneFactory
{
    private readonly Dictionary<string, OwSceneConfiguration> _sceneConfigurations;
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="sceneConfigurations">场景配置字典</param>
    /// <param name="serviceProvider">服务提供者（用于依赖注入）</param>
    public OwGameSceneFactory(
        Dictionary<string, OwSceneConfiguration> sceneConfigurations,
        IServiceProvider serviceProvider)
    {
        _sceneConfigurations = sceneConfigurations ?? throw new ArgumentNullException(nameof(sceneConfigurations));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    /// <inheritdoc/>
    public IOwGameScene CreateScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
            throw new ArgumentException("场景名称不能为空", nameof(sceneName));

        if (!_sceneConfigurations.TryGetValue(sceneName, out var config))
            throw new ArgumentException($"场景 '{sceneName}' 不存在", nameof(sceneName));

        // 通过反射创建场景实例
        var sceneType = Type.GetType(config.Type);
        if (sceneType == null)
            throw new InvalidOperationException($"场景类型 '{config.Type}' 未找到");

        if (!typeof(IOwGameScene).IsAssignableFrom(sceneType))
            throw new InvalidOperationException($"类型 '{config.Type}' 必须实现 IOwGameScene 接口");

        // 使用 ActivatorUtilities 支持构造函数注入
        var scene = (IOwGameScene)ActivatorUtilities.CreateInstance(_serviceProvider, sceneType);

        // 如果是基类，应用配置
        if (scene is OwGameSceneBase baseScene)
        {
            baseScene.ApplyConfiguration(config);
        }

        return scene;
    }

    /// <inheritdoc/>
    public bool SceneExists(string sceneName)
    {
        return _sceneConfigurations.ContainsKey(sceneName);
    }

    /// <inheritdoc/>
    public IEnumerable<string> GetAvailableScenes()
    {
        return _sceneConfigurations.Keys;
    }

    /// <inheritdoc/>
    public OwSceneConfiguration? GetSceneConfiguration(string sceneName)
    {
        _sceneConfigurations.TryGetValue(sceneName, out var config);
        return config;
    }
}

/// <summary>
/// 游戏场景基类（可选的基础实现）
/// 继承此类可自动获得配置注入功能
/// </summary>
public abstract class OwGameSceneBase : IOwGameScene
{
    /// <inheritdoc/>
    public abstract string Name { get; }

    /// <inheritdoc/>
    public virtual OwSceneRenderMode RenderMode { get; protected set; } = OwSceneRenderMode.BlazorDOM;

    /// <inheritdoc/>
    public virtual bool EnableECS { get; protected set; } = false;

    /// <inheritdoc/>
    public virtual int TargetFrameRate { get; protected set; } = 60;

    /// <summary>
    /// 场景配置（由工厂注入）
    /// </summary>
    protected OwSceneConfiguration? Configuration { get; set; }

    /// <inheritdoc/>
    public virtual void Load()
    {
        // 默认空实现，子类覆盖
    }

    /// <inheritdoc/>
    public virtual void Update(double deltaTime)
    {
        // 默认空实现，子类覆盖
    }

    /// <inheritdoc/>
    public virtual void Render()
    {
        // 默认空实现，子类覆盖
    }

    /// <inheritdoc/>
    public virtual void Unload()
    {
        // 默认空实现，子类覆盖
    }

    /// <summary>
    /// 应用配置到场景实例（由工厂调用）
    /// </summary>
    /// <param name="config">场景配置</param>
    internal virtual void ApplyConfiguration(OwSceneConfiguration config)
    {
        Configuration = config;

        // 应用配置到属性
        if (Enum.TryParse<OwSceneRenderMode>(config.RenderMode, out var renderMode))
        {
            RenderMode = renderMode;
        }

        EnableECS = config.EnableECS;
        TargetFrameRate = config.TargetFrameRate;
    }
}


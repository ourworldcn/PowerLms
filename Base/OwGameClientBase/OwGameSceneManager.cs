namespace OW.Game.Client;

/// <summary>
/// 场景管理器接口（管理场景切换和生命周期）
/// 职责：
/// - 管理当前场景
/// - 处理场景切换
/// - 协调游戏循环（Update/Render）
/// 注意：所有方法都是同步的，避免 WASM 等平台的多线程问题
/// </summary>
public interface IOwGameSceneManager
{
    /// <summary>
    /// 当前场景（只读）
    /// </summary>
    IOwGameScene? CurrentScene { get; }

    /// <summary>
    /// 当前场景名称（只读）
    /// </summary>
    string? CurrentSceneName { get; }

    /// <summary>
    /// 场景是否正在加载
    /// </summary>
    bool IsLoading { get; }

    /// <summary>
    /// 加载场景（同步）
    /// </summary>
    /// <param name="sceneName">场景名称</param>
    /// <exception cref="ArgumentException">场景名称不存在</exception>
    /// <exception cref="InvalidOperationException">场景正在加载中</exception>
    void LoadScene(string sceneName);

    /// <summary>
    /// 卸载当前场景
    /// </summary>
    void UnloadCurrentScene();

    /// <summary>
    /// 游戏循环更新（每帧调用）
    /// </summary>
    /// <param name="deltaTime">自上次更新以来的时间增量（秒）</param>
    void Update(double deltaTime);

    /// <summary>
    /// 渲染（每帧调用）
    /// </summary>
    void Render();

    /// <summary>
    /// 场景切换事件
    /// 参数：(旧场景名称, 新场景名称)
    /// </summary>
    event EventHandler<OwSceneChangedEventArgs>? SceneChanged;

    /// <summary>
    /// 场景加载开始事件
    /// </summary>
    event EventHandler<OwSceneLoadingEventArgs>? SceneLoadingStarted;

    /// <summary>
    /// 场景加载完成事件
    /// </summary>
    event EventHandler<OwSceneLoadingEventArgs>? SceneLoadingCompleted;
}

/// <summary>
/// 场景切换事件参数
/// </summary>
public class OwSceneChangedEventArgs : EventArgs
{
    /// <summary>
    /// 旧场景名称（可能为 null，如果是首次加载）
    /// </summary>
    public string? OldSceneName { get; }

    /// <summary>
    /// 新场景名称
    /// </summary>
    public string NewSceneName { get; }

    /// <summary>
    /// 场景切换时间戳
    /// </summary>
    public DateTime Timestamp { get; }

    public OwSceneChangedEventArgs(string? oldSceneName, string newSceneName)
    {
        OldSceneName = oldSceneName;
        NewSceneName = newSceneName;
        Timestamp = DateTime.UtcNow;
    }
}

/// <summary>
/// 场景加载事件参数
/// </summary>
public class OwSceneLoadingEventArgs : EventArgs
{
    /// <summary>
    /// 场景名称
    /// </summary>
    public string SceneName { get; }

    /// <summary>
    /// 加载开始时间
    /// </summary>
    public DateTime Timestamp { get; }

    /// <summary>
    /// 加载是否成功（仅在 LoadingCompleted 事件中有意义）
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// 错误信息（如果加载失败）
    /// </summary>
    public string? ErrorMessage { get; set; }

    public OwSceneLoadingEventArgs(string sceneName)
    {
        SceneName = sceneName;
        Timestamp = DateTime.UtcNow;
        Success = true;
    }
}

/// <summary>
/// 场景管理器配置选项
/// 用于配置场景管理器的行为，可通过 Options Pattern 从配置文件加载
/// </summary>
public class OwSceneManagerOptions
{
    /// <summary>
    /// 默认场景名称
    /// </summary>
    public string DefaultScene { get; set; } = "MainMenu";

    /// <summary>
    /// 是否在切换场景时自动卸载旧场景
    /// </summary>
    public bool AutoUnloadOldScene { get; set; } = true;

    /// <summary>
    /// 场景切换时的过渡时间（毫秒）
    /// </summary>
    public int TransitionDurationMs { get; set; } = 0;

    /// <summary>
    /// 是否启用场景切换日志
    /// </summary>
    public bool EnableLogging { get; set; } = true;
}

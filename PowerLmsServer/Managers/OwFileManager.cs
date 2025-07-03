using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerLmsServer.Managers
{
    /// <summary>
    /// 文件管理器。
    /// 提供文件系统操作的统一接口，包括文件路径管理、文件存储配置等功能。
    /// 作为单例服务注入，确保整个应用程序中文件路径配置的一致性。
    /// </summary>
    [OwAutoInjection(ServiceLifetime.Singleton)]
    public class OwFileManager
    {
        /// <summary>
        /// 构造函数，初始化文件管理器。
        /// </summary>
        /// <param name="options">
        /// 文件管理器配置选项，包含文件存储路径、文件大小限制、允许的文件扩展名等配置信息。
        /// 通过依赖注入容器提供配置，支持从配置文件、环境变量等多种方式获取配置。
        /// </param>
        public OwFileManager(IOptions<OwFileManagerOptions> options)
        {
            _Options = options.Value;
        }

        /// <summary>
        /// 文件管理器配置选项的私有字段。
        /// </summary>
        readonly OwFileManagerOptions _Options;

        /// <summary>
        /// 获取文件管理器的配置信息。
        /// 包含文件存储路径、文件大小限制、允许的文件扩展名等配置参数。
        /// </summary>
        /// <value>返回完整的文件管理器配置选项对象</value>
        public OwFileManagerOptions Options => _Options;

        /// <summary>
        /// 获取文件存储的根目录路径。
        /// 根据配置中的FilePath设置返回实际的文件存储目录，用于文件上传、下载等操作。
        /// </summary>
        /// <returns>
        /// 返回文件存储的根目录完整路径。
        /// 路径格式根据配置自动处理：相对路径会基于应用程序基础目录，绝对路径直接使用。
        /// </returns>
        public string GetDirectory()
        {
            return _Options.FilePath;
        }
    }

    /// <summary>
    /// <see cref="OwFileManager"/>类的配置选项类。
    /// 定义文件管理器的各种配置参数，包括存储路径、文件大小限制、允许的文件类型等。
    /// 实现IOptions模式，支持从配置系统中读取配置。
    /// </summary>
    public class OwFileManagerOptions : IOptions<OwFileManagerOptions>
    {
        /// <summary>
        /// 构造函数，初始化文件管理器配置选项。
        /// 设置默认的配置值，包括默认存储路径、文件大小限制等。
        /// </summary>
        public OwFileManagerOptions()
        {
        }

        /// <summary>
        /// 获取配置选项的值。
        /// 实现IOptions&lt;T&gt;接口要求，返回当前配置实例。
        /// </summary>
        /// <value>返回当前配置选项实例本身</value>
        public OwFileManagerOptions Value => this;

        /// <summary>
        /// 文件存储的根路径配置。
        /// 支持多种路径格式：
        /// <list type="bullet">
        /// <item><description>相对路径（如 "Files"）：基于应用程序运行目录</description></item>
        /// <item><description>根路径但无盘符（如 "\Files"）：使用当前驱动器</description></item>
        /// <item><description>绝对路径（如 "C:\Files"）：直接使用指定路径</description></item>
        /// </list>
        /// </summary>
        /// <value>
        /// 默认值为应用程序基础目录下的"Files"文件夹。
        /// 可通过配置文件或环境变量进行覆盖设置。
        /// </value>
        public string FilePath { get; set; } = Path.Combine(AppContext.BaseDirectory, "Files");

        /// <summary>
        /// 文件上传的最大大小限制，单位为MB。
        /// 用于控制单个文件的上传大小，防止过大文件消耗服务器资源。
        /// </summary>
        /// <value>
        /// 默认值为5MB。可根据业务需求和服务器性能进行调整。
        /// 在文件上传验证时使用此配置进行大小检查。
        /// </value>
        public int MaxFileSizeMB { get; set; } = 5;

        /// <summary>
        /// 允许上传的文件扩展名白名单。
        /// 定义系统允许的文件类型，用于文件上传时的安全验证。
        /// </summary>
        /// <value>
        /// 文件扩展名列表，格式示例：[".jpg", ".png", ".pdf", ".docx"]。
        /// 默认为空列表，表示不限制文件类型（需要根据安全要求进行配置）。
        /// 扩展名应包含点号前缀，不区分大小写。
        /// </value>
        /// <example>
        /// 配置示例：
        /// <code>
        /// AllowedFileExtensions = new List&lt;string&gt; { ".jpg", ".png", ".pdf", ".doc", ".docx" }
        /// </code>
        /// </example>
        public List<string> AllowedFileExtensions { get; set; } = new List<string>();
    }
}

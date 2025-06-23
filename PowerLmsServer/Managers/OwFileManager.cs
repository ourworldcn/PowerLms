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
    /// </summary>
    [OwAutoInjection(ServiceLifetime.Singleton)]
    public class OwFileManager
    {

        /// <summary>
        /// 构造函数。
        /// </summary>
        public OwFileManager(IOptions<OwFileManagerOptions> options)
        {
            _Options = options.Value;
        }

        readonly OwFileManagerOptions _Options;

        /// <summary>
        /// 配置信息。
        /// </summary>
        public OwFileManagerOptions Options => _Options;

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public string GetDirectory()
        {
            return _Options.FilePath;
        }

    }

    /// <summary>
    /// <see cref="OwFileManager"/>类的配置类。
    /// </summary>
    public class OwFileManagerOptions : IOptions<OwFileManagerOptions>
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public OwFileManagerOptions()
        {
            
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public OwFileManagerOptions Value => this;

        /// <summary>
        /// 存储文件的根路径。
        /// 没有根符号\(入 Files)，则使用基于运行时代码基础路径.有跟符号但没有盘符(如:\Files)则使用同一个驱动器作为盘符。可以指定全路径，如C:\Files
        /// </summary>
        public string FilePath { get; set; } = Path.Combine(AppContext.BaseDirectory, "Files");

        /// <summary>
        /// 文件上传的最大大小，单位MB。
        /// </summary>
        public int MaxFileSizeMB { get; set; } = 5;

        /// <summary>
        /// 允许上传的文件扩展名列表。
        /// 例如：.jpg, .png, .pdf
        /// </summary>
        public List<string> AllowedFileExtensions { get; set; } = new List<string>();
    }
}

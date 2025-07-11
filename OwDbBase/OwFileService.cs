using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OW.Data
{
    /// <summary>
    /// 文件表。
    /// </summary>
    [Comment("文件信息表")]
    [Index(nameof(ParentId), IsUnique = false)]
    public class PlFileInfo : GuidKeyObjectBase
    {
        /// <summary>
        /// 所属实体的Id。如属于客户资料的文件，就设置为客户Id。
        /// </summary>
        [Comment("所属实体的Id")]
        public Guid? ParentId { get; set; }

        /// <summary>
        /// 文件类型Id。关联字典FileType。可能是null，表示这是一个通用文件。
        /// </summary>
        [Comment("文件类型Id。关联字典FileType。可能是null，表示这是一个通用文件。")]
        public Guid? FileTypeId { get; set; }

        /// <summary>
        /// 文件的相对路径和全名。如 Customer\客户尽调资料.Pdf。
        /// </summary>
        [Comment("文件类型Id。文件的相对路径和全名。")]
        [MaxLength(1024)]
        public string FilePath { get; set; }

        /// <summary>
        /// 文件的显示名称。用于列表时的友好名称。
        /// </summary>
        [Comment("文件的显示名称")]
        [MaxLength(64)]
        public string DisplayName { get; set; }

        /// <summary>
        /// 上传时的文件名。
        /// </summary>
        [Comment("上传时的文件名")]
        [MaxLength(64)]
        public string FileName { get; set; }

        /// <summary>
        /// 备注。
        /// </summary>
        [Comment("备注")]
        public string Remark { get; set; }

        /// <summary>
        /// 上传人Id,可能是空。
        /// </summary>
        [Comment("操作员，可以更改相当于工作号的所有者")]
        public Guid? CreateBy { get; set; }

        /// <summary>
        /// 上传时间,系统默认。
        /// </summary>
        [Comment("新建时间,系统默认，不能更改。")]
        public DateTime CreateDateTime { get; set; }
    }

    /// <summary>
    /// 文件服务配置选项
    /// </summary>
    public class OwFileServiceOptions
    {
        /// <summary>
        /// 配置节名称
        /// </summary>
        public const string SectionName = "OwFileService";

        /// <summary>
        /// 文件存储的根路径配置
        /// </summary>
        public string FilePath { get; set; } = Path.Combine(AppContext.BaseDirectory, "Files");

        /// <summary>
        /// 文件上传的最大大小限制，单位为MB
        /// </summary>
        public int MaxFileSizeMB { get; set; } = 5;

        /// <summary>
        /// 允许上传的文件扩展名白名单
        /// </summary>
        public List<string> AllowedFileExtensions { get; set; } = new();
    }

    /// <summary>
    /// 文件管理器，提供文件系统操作的统一接口和配置管理
    /// 使用单例模式确保整个应用程序中文件路径配置的一致性
    /// </summary>
    /// <typeparam name="TDbContext">数据库上下文类型，必须包含 PlFileInfos 属性</typeparam>
    /// <remarks>
    /// 泛型类无法通过 OwAutoInjection 自动注册，需要在 Program.cs 中手动注册
    /// </remarks>
    public class OwFileService<TDbContext> where TDbContext : DbContext
    {
        #region 字段和属性

        private readonly OwFileServiceOptions _options; // 文件管理器配置选项
        private readonly IDbContextFactory<TDbContext> _dbContextFactory; // 数据库上下文工厂

        /// <summary>获取文件管理器的配置信息</summary>
        public OwFileServiceOptions Options => _options;

        #endregion

        #region 构造函数

        /// <summary>
        /// 初始化文件管理器实例
        /// </summary>
        /// <param name="options">文件管理器配置选项，包含文件存储路径、大小限制、允许的文件扩展名等</param>
        /// <param name="dbContextFactory">数据库上下文工厂</param>
        public OwFileService(IOptions<OwFileServiceOptions> options, IDbContextFactory<TDbContext> dbContextFactory)
        {
            _options = options.Value ?? throw new ArgumentNullException(nameof(options));
            _dbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));
            EnsureDirectoryExists(); // 确保配置的目录存在
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 获取文件存储的根目录路径
        /// </summary>
        /// <returns>返回文件存储的根目录完整路径</returns>
        public string GetDirectory()
        {
            return _options.FilePath;
        }

        /// <summary>
        /// 验证文件扩展名是否被允许
        /// </summary>
        /// <param name="fileName">文件名</param>
        /// <returns>如果文件扩展名被允许则返回true，否则返回false</returns>
        public bool IsFileExtensionAllowed(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName)) return false;
            if (!_options.AllowedFileExtensions.Any()) return true; // 如果没有配置限制，则允许所有类型

            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            return _options.AllowedFileExtensions.Any(ext => string.Equals(ext, extension, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// 验证文件大小是否在允许范围内
        /// </summary>
        /// <param name="fileSizeBytes">文件大小（字节）</param>
        /// <returns>如果文件大小在允许范围内则返回true，否则返回false</returns>
        public bool IsFileSizeAllowed(long fileSizeBytes)
        {
            var maxSizeBytes = _options.MaxFileSizeMB * 1024L * 1024L;
            return fileSizeBytes <= maxSizeBytes;
        }

        /// <summary>
        /// 构建相对于根目录的完整文件路径
        /// </summary>
        /// <param name="relativePath">相对路径</param>
        /// <returns>完整的文件路径</returns>
        public string GetFullPath(string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath)) throw new ArgumentException("相对路径不能为空", nameof(relativePath));
            return Path.Combine(_options.FilePath, relativePath);
        }

        /// <summary>
        /// 确保指定的子目录存在
        /// </summary>
        /// <param name="subDirectory">子目录路径</param>
        /// <returns>创建的目录完整路径</returns>
        public string EnsureSubDirectoryExists(string subDirectory)
        {
            var fullPath = GetFullPath(subDirectory);
            if (!Directory.Exists(fullPath)) Directory.CreateDirectory(fullPath);
            return fullPath;
        }

        /// <summary>
        /// 创建文件记录并保存文件到磁盘和数据库
        /// </summary>
        /// <param name="fileStream">要写入的流，从流的当前位置读取数据。</param>
        /// <param name="fileName">原始文件名</param>
        /// <param name="displayName">显示名称</param>
        /// <param name="parentId">所属实体ID</param>
        /// <param name="creatorId">创建者ID</param>
        /// <param name="fileTypeId">文件类型ID（可选）</param>
        /// <param name="remark">备注（可选）</param>
        /// <param name="subDirectory">子目录名称（可选，默认为"General"）</param>
        /// <param name="skipValidation">是否跳过验证（可选，默认为false）</param>
        /// <returns>创建的文件信息实体</returns>
        /// <exception cref="ArgumentNullException">当必要参数为null时抛出</exception>
        /// <exception cref="ArgumentException">当文件流为空或文件名无效时抛出</exception>
        /// <exception cref="InvalidOperationException">当文件验证失败时抛出</exception>
        public PlFileInfo CreateFile(Stream fileStream, string fileName, string displayName, Guid? parentId,
            Guid? creatorId, Guid? fileTypeId = null, string remark = null, string subDirectory = "General", bool skipValidation = false)
        {
            if (fileStream == null) throw new ArgumentNullException(nameof(fileStream));
            if (string.IsNullOrWhiteSpace(fileName)) throw new ArgumentException("文件名不能为空", nameof(fileName));
            if (string.IsNullOrWhiteSpace(displayName)) throw new ArgumentException("显示名称不能为空", nameof(displayName));

            if (!skipValidation)
            {
                // 验证文件大小
                if (!IsFileSizeAllowed(fileStream.Length))
                    throw new InvalidOperationException($"文件大小超过限制，最大允许 {_options.MaxFileSizeMB}MB");

                // 验证文件扩展名
                if (!IsFileExtensionAllowed(fileName))
                    throw new InvalidOperationException($"不支持的文件类型：{Path.GetExtension(fileName)}");
            }

            // 创建文件信息实体
            var fileInfo = new PlFileInfo
            {
                Id = Guid.NewGuid(),
                DisplayName = displayName,
                FileName = fileName,
                ParentId = parentId,
                FileTypeId = fileTypeId,
                Remark = remark,
                CreateBy = creatorId,
                CreateDateTime = DateTime.Now,
                FilePath = GenerateFilePath(subDirectory)
            };

            // 保存文件到磁盘
            SaveFileToDisk(fileStream, fileInfo.FilePath);

            // 保存到数据库
            using var dbContext = _dbContextFactory.CreateDbContext();
            var plFileInfosProperty = typeof(TDbContext).GetProperty("PlFileInfos");
            if (plFileInfosProperty == null)
                throw new InvalidOperationException($"数据库上下文 {typeof(TDbContext).Name} 不包含 PlFileInfos 属性");

            var plFileInfosDbSet = plFileInfosProperty.GetValue(dbContext) as DbSet<PlFileInfo>;
            plFileInfosDbSet.Add(fileInfo);
            dbContext.SaveChanges();

            return fileInfo;
        }

        /// <summary>
        /// 验证并创建文件（同步版本，用于任务中调用）
        /// </summary>
        /// <param name="fileContent">文件内容字节数组</param>
        /// <param name="fileName">文件名</param>
        /// <param name="displayName">显示名称</param>
        /// <param name="parentId">所属实体ID</param>
        /// <param name="creatorId">创建者ID</param>
        /// <param name="fileTypeId">文件类型ID（可选）</param>
        /// <param name="remark">备注（可选）</param>
        /// <param name="subDirectory">子目录名称（可选）</param>
        /// <returns>创建的文件信息实体</returns>
        public PlFileInfo CreateFileFromBytes(byte[] fileContent, string fileName, string displayName, Guid? parentId,
            Guid? creatorId, Guid? fileTypeId = null, string remark = null, string subDirectory = "Generated")
        {
            if (fileContent == null || fileContent.Length == 0) throw new ArgumentException("文件内容不能为空", nameof(fileContent));

            using var memoryStream = new MemoryStream(fileContent);
            return CreateFile(memoryStream, fileName, displayName, parentId, creatorId, fileTypeId, remark, subDirectory);
        }

        /// <summary>
        /// 删除文件（包括磁盘文件和数据库记录）
        /// </summary>
        /// <param name="fileId">文件ID</param>
        /// <returns>是否成功删除</returns>
        public bool DeleteFile(Guid fileId)
        {
            try
            {
                using var dbContext = _dbContextFactory.CreateDbContext();
                var plFileInfosProperty = typeof(TDbContext).GetProperty("PlFileInfos");
                if (plFileInfosProperty == null)
                    throw new InvalidOperationException($"数据库上下文 {typeof(TDbContext).Name} 不包含 PlFileInfos 属性");

                var plFileInfosDbSet = plFileInfosProperty.GetValue(dbContext) as DbSet<PlFileInfo>;
                var fileInfo = plFileInfosDbSet.Find(fileId);

                if (fileInfo == null) return false;

                // 删除磁盘文件
                var fullPath = GetFullPath(fileInfo.FilePath);
                if (File.Exists(fullPath)) File.Delete(fullPath);

                // 删除数据库记录
                plFileInfosDbSet.Remove(fileInfo);
                dbContext.SaveChanges();

                return true;
            }
            catch (Exception)
            {
                return false; // 删除失败
            }
        }

        /// <summary>
        /// 删除文件（仅删除磁盘文件，数据库记录需要调用方处理）
        /// </summary>
        /// <param name="filePath">文件相对路径</param>
        /// <returns>是否成功删除</returns>
        public bool DeleteFileOnly(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath)) return false;

            try
            {
                var fullPath = GetFullPath(filePath);
                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                    return true;
                }
                return false; // 文件不存在
            }
            catch (Exception)
            {
                return false; // 删除失败
            }
        }

        /// <summary>
        /// 检查文件是否存在
        /// </summary>
        /// <param name="filePath">文件相对路径</param>
        /// <returns>文件是否存在</returns>
        public bool FileExists(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath)) return false;
            var fullPath = GetFullPath(filePath);
            return File.Exists(fullPath);
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 确保配置的根目录存在
        /// </summary>
        private void EnsureDirectoryExists()
        {
            if (!Directory.Exists(_options.FilePath)) Directory.CreateDirectory(_options.FilePath);
        }

        /// <summary>
        /// 生成唯一的文件路径
        /// </summary>
        /// <param name="subDirectory">子目录名称</param>
        /// <returns>相对文件路径</returns>
        private string GenerateFilePath(string subDirectory)
        {
            var uniqueFileName = $"{Guid.NewGuid()}.bin";
            return Path.Combine(subDirectory, uniqueFileName);
        }

        /// <summary>
        /// 将文件流保存到磁盘
        /// </summary>
        /// <param name="fileStream">源文件流</param>
        /// <param name="relativePath">相对路径</param>
        private void SaveFileToDisk(Stream fileStream, string relativePath)
        {
            const int bufferSize = 1024 * 1024; // 缓冲区大小
            var fullPath = GetFullPath(relativePath);
            var directory = Path.GetDirectoryName(fullPath);

            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            using var fileStreamDest = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize, FileOptions.SequentialScan);
            fileStream.CopyTo(fileStreamDest, bufferSize);   // 将源文件流复制到目标文件流
        }

        #endregion
    }
}

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// OwFileService 依赖注入扩展方法
    /// </summary>
    public static class OwFileServiceExtensions
    {
        /// <summary>
        /// 添加文件服务到服务容器
        /// </summary>
        /// <typeparam name="TDbContext">数据库上下文类型</typeparam>
        /// <param name="services">服务集合</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddOwFileService<TDbContext>(this IServiceCollection services)
            where TDbContext : Microsoft.EntityFrameworkCore.DbContext
        {
            services.AddSingleton<OW.Data.OwFileService<TDbContext>>();
            return services;
        }
    }
}

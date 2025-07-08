using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Http;
using PowerLms.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerLmsServer.Managers
{
    /// <summary>
    /// 文件管理器，提供文件系统操作的统一接口和配置管理
    /// 使用单例模式确保整个应用程序中文件路径配置的一致性
    /// </summary>
    [OwAutoInjection(ServiceLifetime.Singleton)]
    public class OwFileManager
    {
        #region 字段和属性

        private readonly OwFileManagerOptions _options; // 文件管理器配置选项

        /// <summary>获取文件管理器的配置信息</summary>
        public OwFileManagerOptions Options => _options;

        #endregion

        #region 构造函数

        /// <summary>
        /// 初始化文件管理器实例
        /// </summary>
        /// <param name="options">文件管理器配置选项，包含文件存储路径、大小限制、允许的文件扩展名等</param>
        public OwFileManager(IOptions<OwFileManagerOptions> options)
        {
            _options = options.Value ?? throw new ArgumentNullException(nameof(options));
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
        /// 创建文件记录并保存文件到磁盘
        /// </summary>
        /// <param name="fileStream">文件流</param>
        /// <param name="fileName">原始文件名</param>
        /// <param name="displayName">显示名称</param>
        /// <param name="parentId">所属实体ID</param>
        /// <param name="creatorId">创建者ID</param>
        /// <param name="fileTypeId">文件类型ID（可选）</param>
        /// <param name="remark">备注（可选）</param>
        /// <param name="subDirectory">子目录名称（可选，默认为"General"）</param>
        /// <returns>创建的文件信息实体</returns>
        /// <exception cref="ArgumentNullException">当必要参数为null时抛出</exception>
        /// <exception cref="ArgumentException">当文件流为空或文件名无效时抛出</exception>
        /// <exception cref="InvalidOperationException">当文件验证失败时抛出</exception>
        public PowerLms.Data.PlFileInfo CreateFile(Stream fileStream, string fileName, string displayName, Guid? parentId, 
            Guid? creatorId, Guid? fileTypeId = null, string remark = null, string subDirectory = "General")
        {
            if (fileStream == null) throw new ArgumentNullException(nameof(fileStream));
            if (string.IsNullOrWhiteSpace(fileName)) throw new ArgumentException("文件名不能为空", nameof(fileName));
            if (string.IsNullOrWhiteSpace(displayName)) throw new ArgumentException("显示名称不能为空", nameof(displayName));
            
            // 验证文件大小
            if (!IsFileSizeAllowed(fileStream.Length))
                throw new InvalidOperationException($"文件大小超过限制，最大允许 {_options.MaxFileSizeMB}MB");
            
            // 验证文件扩展名
            if (!IsFileExtensionAllowed(fileName))
                throw new InvalidOperationException($"不支持的文件类型：{Path.GetExtension(fileName)}");
            
            // 创建文件信息实体
            var fileInfo = new PowerLms.Data.PlFileInfo
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
            
            return fileInfo;
        }

        /// <summary>
        /// 创建文件记录并保存文件到磁盘（IFormFile重载）
        /// </summary>
        /// <param name="formFile">表单文件</param>
        /// <param name="displayName">显示名称</param>
        /// <param name="parentId">所属实体ID</param>
        /// <param name="creatorId">创建者ID</param>
        /// <param name="fileTypeId">文件类型ID（可选）</param>
        /// <param name="remark">备注（可选）</param>
        /// <param name="subDirectory">子目录名称（可选，默认为"General"）</param>
        /// <returns>创建的文件信息实体</returns>
        public PowerLms.Data.PlFileInfo CreateFile(IFormFile formFile, string displayName, Guid? parentId, 
            Guid? creatorId, Guid? fileTypeId = null, string remark = null, string subDirectory = "General")
        {
            if (formFile == null) throw new ArgumentNullException(nameof(formFile));
            if (formFile.Length == 0) throw new ArgumentException("文件不能为空", nameof(formFile));
            
            using var fileStream = formFile.OpenReadStream();
            return CreateFile(fileStream, formFile.FileName, displayName, parentId, creatorId, fileTypeId, remark, subDirectory);
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
        public PowerLms.Data.PlFileInfo CreateFileFromBytes(byte[] fileContent, string fileName, string displayName, Guid? parentId,
            Guid? creatorId, Guid? fileTypeId = null, string remark = null, string subDirectory = "Generated")
        {
            if (fileContent == null || fileContent.Length == 0) throw new ArgumentException("文件内容不能为空", nameof(fileContent));
            
            using var memoryStream = new MemoryStream(fileContent);
            return CreateFile(memoryStream, fileName, displayName, parentId, creatorId, fileTypeId, remark, subDirectory);
        }

        /// <summary>
        /// 删除文件（包括磁盘文件和数据库记录需要调用方处理）
        /// </summary>
        /// <param name="filePath">文件相对路径</param>
        /// <returns>是否成功删除</returns>
        public bool DeleteFile(string filePath)
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
            var fullPath = GetFullPath(relativePath);
            var directory = Path.GetDirectoryName(fullPath);
            
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);
            
            using var fileStreamDest = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, FileOptions.SequentialScan);
            fileStream.CopyTo(fileStreamDest);
        }

        #endregion

        #region 使用示例

        // 以下是 OwFileManager 文件创建功能的使用示例（注释形式）
        
        /*
        // 示例1: 从 IFormFile 创建文件
        var fileInfo = _fileManager.CreateFile(
            formFile: uploadedFile,
            displayName: "用户上传的文档",
            parentId: customerId,
            creatorId: currentUserId,
            fileTypeId: documentTypeId,
            remark: "客户资料文件"
        );
        
        // 示例2: 从字节数组创建文件（适用于生成的文件）
        var generatedFileInfo = _fileManager.CreateFileFromBytes(
            fileContent: pdfBytes,
            fileName: "invoice_export.pdf",
            displayName: "发票导出文件",
            parentId: taskId,
            creatorId: systemUserId,
            subDirectory: "Generated"
        );
        
        // 示例3: 从文件流创建文件
        using var fileStream = File.OpenRead(localFilePath);
        var streamFileInfo = _fileManager.CreateFile(
            fileStream: fileStream,
            fileName: "imported_document.xlsx",
            displayName: "导入的Excel文档",
            parentId: jobId,
            creatorId: userId
        );
        
        // 示例4: 验证和删除文件
        if (_fileManager.FileExists(fileInfo.FilePath))
        {
            var deleted = _fileManager.DeleteFile(fileInfo.FilePath);
            // 注意：还需要从数据库中删除 PlFileInfo 记录
        }
        */

        #endregion
    }

    /// <summary>
    /// 文件管理器配置选项，定义文件存储路径、大小限制、允许的文件类型等参数
    /// </summary>
    public class OwFileManagerOptions : IOptions<OwFileManagerOptions>
    {
        #region 属性

        /// <summary>获取配置选项实例本身</summary>
        public OwFileManagerOptions Value => this;

        /// <summary>
        /// 文件存储的根路径配置
        /// 支持相对路径（基于应用程序运行目录）和绝对路径
        /// </summary>
        public string FilePath { get; set; } = Path.Combine(AppContext.BaseDirectory, "Files");

        /// <summary>
        /// 文件上传的最大大小限制，单位为MB
        /// 用于控制单个文件的上传大小，防止过大文件消耗服务器资源
        /// </summary>
        public int MaxFileSizeMB { get; set; } = 5;

        /// <summary>
        /// 允许上传的文件扩展名白名单
        /// 定义系统允许的文件类型，用于文件上传时的安全验证
        /// 扩展名应包含点号前缀，不区分大小写
        /// </summary>
        public List<string> AllowedFileExtensions { get; set; } = new();

        #endregion
    }
}

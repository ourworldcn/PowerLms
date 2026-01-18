/*
 * 1.	构造函数：初始化控制器所需的服务和管理器。
 * 2.	获取业务负责人的所属关系：通过查询条件获取文件列表。
 * 3.	下载客户资料：提供下载客户资料的接口（已标记为过时）。
 * 4.	上传客户资料：提供上传客户资料的接口（已标记为过时）。
 * 5.	通用文件管理接口：
    •	删除文件
    •	上传文件
    •	下载文件
    •	获取全部文件信息
 */
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.Web.CodeGeneration;
using NuGet.Common;
using NuGet.Protocol.Plugins;
using PowerLms.Data;
using PowerLmsServer;
using PowerLmsServer.EfData;
using PowerLmsServer.Managers;
using PowerLmsWebApi.Dto;
using SixLabors.ImageSharp.Metadata;
using System.ComponentModel.DataAnnotations;
using System.Drawing.Imaging;
using System.Net;
using System.Net.Mime;
using System.Text;
using System.Text.Unicode;
using SysIO = System.IO;
namespace PowerLmsWebApi.Controllers.System
{
    /// <summary>
    /// 文件相关操作的控制器。
    /// 在项目基目录下有Files文件夹，其下按业务分为不同文件夹存储文件。
    /// </summary>
    public class FileController : PlControllerBase
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public FileController(IServiceProvider serviceProvider, IMapper mapper, AccountManager accountManager, PowerLmsUserDbContext dbContext, EntityManager entityManager, AuthorizationManager authorizationManager, OwFileService<PowerLmsUserDbContext> fileService, ILogger<FileController> logger)
        {
            _Mapper = mapper;
            _AccountManager = accountManager;
            _DbContext = dbContext;
            _ServiceProvider = serviceProvider;
            _EntityManager = entityManager;
            _AuthorizationManager = authorizationManager;
            _FileService = fileService;
            _Logger = logger;
        }
        private readonly PowerLmsUserDbContext _DbContext;
        private readonly AccountManager _AccountManager;
        private readonly IServiceProvider _ServiceProvider;
        private readonly EntityManager _EntityManager;
        private readonly IMapper _Mapper;
        private readonly AuthorizationManager _AuthorizationManager;
        private readonly OwFileService<PowerLmsUserDbContext> _FileService;
        private readonly ILogger<FileController> _Logger;
        /// <summary>
        /// 存储文件的根目录。
        /// </summary>
        public static string RootPath = Path.Combine(AppContext.BaseDirectory, "Files");
        /// <summary>
        /// 获取业务负责人的所属关系。
        /// </summary>
        /// <param name="model"></param>
        /// <param name="conditional">已支持通用查询。</param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">指定类别Id无效。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpGet]
        public ActionResult<GetAllCustomerFileListReturnDto> GetAllCustomerFileList([FromQuery] PagingParamsDtoBase model,
            [FromQuery] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetAllCustomerFileListReturnDto();
            var dbSet = _DbContext.PlFileInfos;
            var coll = dbSet.OrderBy(model.OrderFieldName, model.IsDesc).AsNoTracking();
            if (conditional != null && conditional.Any())
            {
                coll = EfHelper.GenerateWhereAnd(coll, conditional);
            }
            var prb = _EntityManager.GetAll(coll, model.StartIndex, model.Count);
            _Mapper.Map(prb, result);
            return result;
        }
        /// <summary>
        /// 下载客户资料的特定接口。
        /// </summary>
        /// <param name="token">登录令牌</param>
        /// <param name="fileId">文件的Id。</param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="404">没有找到指定文件。</response>  
        /// <response code="410">接口已废弃，请使用新的通用接口。</response>
        [HttpGet]
        [Obsolete("已废弃接口，请使用 GetFile 替代。该接口将在未来版本中移除。")]
        public ActionResult DownloadCustomerFile(Guid token, Guid fileId)
        {
            _Logger.LogWarning("尝试使用已废弃的客户文件下载接口，令牌: {Token}, 文件ID: {FileId}",
                token, fileId);
            return StatusCode(StatusCodes.Status410Gone,
                "此接口已废弃，请使用新的通用文件下载接口 GetFile。新接口提供更好的安全性和权限控制。");
        }
        /// <summary>
        /// 上传客户资料的特定接口。
        /// 已废弃：强烈建议使用 AddFile 接口替代，该接口将在未来版本中移除。
        /// </summary>
        /// <param name="file"></param>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="410">接口已废弃，请使用新的通用接口。</response>
        [HttpPost]
        [Obsolete("已废弃接口，请使用 AddFile 替代。该接口将在未来版本中移除。")]
        public ActionResult<UploadCustomerFileReturnDto> UploadCustomerFile(IFormFile file, [FromForm] UploadCustomerFileParamsDto model)
        {
            // 🔧 紧急修复：禁用旧版接口，强制使用新版通用接口
            _Logger.LogWarning("尝试使用已废弃的客户文件上传接口，用户: {UserId}, 文件: {FileName}",
                model.Token, file?.FileName);
            var result = new UploadCustomerFileReturnDto();
            result.HasError = true;
            result.ErrorCode = 410; // Gone
            result.DebugMessage = "此接口已废弃，请使用新的通用文件上传接口 AddFile。新接口提供更好的安全性和文件类型验证。";
            return StatusCode(StatusCodes.Status410Gone, result);
        }
        #region 通用文件管理接口
        /// <summary>
        /// 删除存储的文件。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        /// <response code="404">指定Id的文件描述符不存在。</response>  
        /// <response code="410">指定Id的文件描述符已经无效，此时将删除描述符。</response>  
        /// <response code="500">其他错误，并发导致数据变化不能完成操作。</response>
        [HttpDelete]
        public ActionResult<RemoveFileReturnDto> RemoveFile(RemoveFileParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context)
                return Unauthorized();
            var result = new RemoveFileReturnDto();
            try
            {
                var item = _DbContext.PlFileInfos.Find(model.Id);
                if (item is null) return NotFound(model.Id);
                // 检查权限
                if (item.ParentId.HasValue)
                {
                    CheckJobPermissions(item.ParentId.Value, "8.4");
                }
                // 使用 OwFileService 删除文件（包括磁盘文件和数据库记录）
                var fileDeleted = _FileService.DeleteFile(model.Id);
                if (!fileDeleted)
                {
                    _Logger.LogWarning("文件删除失败，文件ID: {FileId}", model.Id);
                    return StatusCode((int)HttpStatusCode.Gone, $"指定文件不存在或删除失败");
                }
                _Logger.LogInformation("文件删除成功：{fileName}，ID：{fileId}", item.FileName, item.Id);
                return result;
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode((int)HttpStatusCode.Forbidden, ex.Message);
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "删除文件时发生错误，文件ID: {FileId}", model.Id);
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = $"删除文件时发生错误: {ex.Message}";
                return StatusCode((int)HttpStatusCode.InternalServerError, result);
            }
        }
        /// <summary>
        /// 上传(追加)通用的文件。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">文件上传成功。</response>  
        /// <response code="400">请求无效，未提供文件或文件为空。</response>
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>
        /// <response code="413">文件大小超过限制。</response>
        /// <response code="415">不支持的文件类型。</response>
        /// <remarks>
        /// 规则配置热更新，无需重启服务即可生效，文件上传配置在appsettings.json 或 appsettings.XXX.json中设置：
        /// {
        ///   "OwFileService": {
        ///     "MaxFileSizeMB": 5,
        ///     "AllowedFileExtensions": [
        ///       ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx",
        ///       ".xml", ".ofd", ".json",
        ///       ".jpg", ".jpeg", ".png", ".bmp", ".gif",
        ///       ".txt"
        ///     ]
        ///   }
        /// }
        /// 
        /// 配置项说明：
        /// - MaxFileSizeMB：允许上传的最大文件大小，单位MB
        /// - AllowedFileExtensions：允许上传的文件类型列表，注意包含前导点(.)
        /// </remarks>
        [HttpPost]
        public ActionResult<AddFileReturnDto> AddFile([FromForm] AddFileParamsDto model)
        {
            var result = new AddFileReturnDto();
            // 身份验证
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context)
                return Unauthorized(result);
            try
            {
                // 权限检查 - 可以提取为独立方法进一步简化
                CheckJobPermissions(model.ParentId, "8.1");
                // 使用 OwFileService 创建文件 - 文件会自动保存到磁盘和数据库
                PlFileInfo fileInfo;
                using (var fileStream = model.File.OpenReadStream())
                {
                    fileInfo = _FileService.CreateFile(
                        fileStream: fileStream,
                        fileName: model.File.FileName,
                        displayName: model.DisplayName,
                        parentId: model.ParentId,
                        creatorId: context.User?.Id,
                        fileTypeId: model.FileTypeId,
                        remark: model.Remark,
                        clientString: model.ClientString
                    );
                }
                result.Id = fileInfo.Id;
                _Logger.LogInformation("文件上传成功：{fileName}，大小：{fileSize}MB，ID：{fileId}",
                    model.File.FileName, Math.Round(model.File.Length / 1024.0 / 1024.0, 2), fileInfo.Id);
                return result;
            }
            catch (ArgumentNullException)
            {
                result.HasError = true;
                result.ErrorCode = 1001;
                result.DebugMessage = "未提供有效的文件";
                _Logger.LogWarning("文件上传失败：未提供有效的文件");
                return BadRequest(result);
            }
            catch (ArgumentException ex)
            {
                result.HasError = true;
                result.ErrorCode = 1001;
                result.DebugMessage = ex.Message;
                _Logger.LogWarning("文件上传失败：{message}", ex.Message);
                return BadRequest(result);
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("文件大小"))
            {
                result.HasError = true;
                result.ErrorCode = 1002;
                result.DebugMessage = ex.Message;
                _Logger.LogWarning("文件上传失败：{fileName}，{message}", model.File?.FileName, ex.Message);
                return StatusCode(StatusCodes.Status413PayloadTooLarge, result);
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("文件类型"))
            {
                result.HasError = true;
                result.ErrorCode = 1003;
                result.DebugMessage = ex.Message;
                _Logger.LogWarning("文件上传失败：{fileName}，{message}", model.File?.FileName, ex.Message);
                return StatusCode(StatusCodes.Status415UnsupportedMediaType, result);
            }
            catch (UnauthorizedAccessException ex)
            {
                _Logger.LogWarning("权限不足：{message}", ex.Message);
                return StatusCode(StatusCodes.Status403Forbidden, ex.Message);
            }
            catch (Exception ex)
            {
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = $"文件上传时发生未知错误：{ex.Message}";
                _Logger.LogError(ex, "文件上传时发生未知错误");
                return StatusCode(StatusCodes.Status500InternalServerError, result);
            }
        }
        /// <summary>
        /// 下载文件。一般应先调用GetAllFileInfo接口以获得文件Id。
        /// Token可以从以下位置获取（优先级从高到低）：
        /// 1. 查询参数 token
        /// 2. HTTP Header: Authorization: Bearer {token}
        /// 3. Cookie: token（不区分大小写）
        /// </summary>
        /// <param name="fileId">文件Id</param>
        /// <param name="token">查询参数中的Token（可选）</param>
        /// <param name="authToken">Header中的Token（可选）</param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="404">指定Id的文件不存在。</response>  
        /// <response code="403">权限不足。</response>  
        [HttpGet]
        public ActionResult GetFile(
            [FromQuery] Guid fileId,
            [FromQuery] Guid? token = null,
            [FromHeader(Name = "Authorization")] string authToken = null)
        {
            Guid? finalToken = null;
            if (token.HasValue)
            {
                finalToken = token.Value;
            }
            else if (!string.IsNullOrEmpty(authToken) && authToken.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                var tokenString = authToken[7..];
                if (Guid.TryParse(tokenString, out var headerToken))
                    finalToken = headerToken;
            }
            else
            {
                var tokenCookie = Request.Cookies.FirstOrDefault(c => c.Key.Equals("token", StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrEmpty(tokenCookie.Value) && Guid.TryParse(tokenCookie.Value, out var parsedCookieToken))
                    finalToken = parsedCookieToken;
            }
            if (!finalToken.HasValue)
            {
                _Logger.LogWarning("文件下载失败：未提供有效的Token，文件ID: {FileId}", fileId);
                return Unauthorized("未提供有效的Token，请通过查询参数、Header(Authorization: Bearer)或Cookie提供");
            }
            if (_AccountManager.GetOrLoadContextByToken(finalToken.Value, _ServiceProvider) is not OwContext context)
            {
                _Logger.LogWarning("文件下载失败：无效的Token，文件ID: {FileId}", fileId);
                return Unauthorized();
            }
            try
            {
                var info = _DbContext.PlFileInfos.Find(fileId);
                if (info == null) return NotFound();
                if (info.ParentId.HasValue)
                {
                    CheckJobPermissions(info.ParentId.Value, "8.2");
                }
                if (!_FileService.FileExists(info.FilePath))
                {
                    _Logger.LogWarning("请求的文件不存在：{FilePath}", info.FilePath);
                    return NotFound("文件不存在");
                }
                var fullPath = _FileService.GetFullPath(info.FilePath);
                var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read);
                _Logger.LogDebug("文件下载：{fileName}，ID：{fileId}", info.FileName, info.Id);
                return File(stream, "application/octet-stream", info.FileName);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode((int)HttpStatusCode.Forbidden, ex.Message);
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "下载文件时发生错误，文件ID: {FileId}", fileId);
                return StatusCode((int)HttpStatusCode.InternalServerError, "下载文件时发生错误");
            }
        }
        /// <summary>
        /// 获取全部通用文件信息。
        /// </summary>
        /// <param name="model"></param>
        /// <param name="conditional">查询的条件。支持 Id，ParentId(所属实体Id)。不区分大小写。</param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpGet]
        public ActionResult<GetAllFileInfoReturnDto> GetAllFileInfo([FromQuery] PagingParamsDtoBase model,
            [FromQuery][ModelBinder(typeof(DotKeyDictionaryModelBinder))] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetAllFileInfoReturnDto();
            var dbSet = _DbContext.PlFileInfos;
            var coll = dbSet.OrderBy(model.OrderFieldName, model.IsDesc).AsNoTracking();
            coll = EfHelper.GenerateWhereAnd(coll, conditional);
            var prb = _EntityManager.GetAll(coll, model.StartIndex, model.Count);
            _Mapper.Map(prb, result);
            return result;
        }
        #endregion 通用文件管理接口
        #region 私有辅助方法
        /// <summary>
        /// 检查作业相关的权限
        /// </summary>
        /// <param name="parentId">父实体ID</param>
        /// <param name="operationCode">操作代码（如"8.1"表示上传操作）</param>
        /// <exception cref="UnauthorizedAccessException">权限不足时抛出</exception>
        private void CheckJobPermissions(Guid parentId, string operationCode)
        {
            if (_DbContext.PlJobs.Find(parentId) is PlJob job)
            {
                var permissionCode = job.JobTypeId switch
                {
                    var id when id == ProjectContent.AeId => $"D0.{operationCode}",
                    var id when id == ProjectContent.AiId => $"D1.{operationCode}",
                    var id when id == ProjectContent.SeId => $"D2.{operationCode}",
                    var id when id == ProjectContent.SiId => $"D3.{operationCode}",
                    _ => null
                };
                if (permissionCode != null && !_AuthorizationManager.Demand(out var err, permissionCode))
                {
                    throw new UnauthorizedAccessException(err);
                }
            }
        }
        #endregion
    }
}

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
using System.IO;
using System.Net;
using System.Net.Mime;
using System.Text;
using System.Text.Unicode;

namespace PowerLmsWebApi.Controllers
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
        public FileController(IServiceProvider serviceProvider, IMapper mapper, AccountManager accountManager, PowerLmsUserDbContext dbContext, EntityManager entityManager, AuthorizationManager authorizationManager, OwFileManager fileManager)
        {
            _Mapper = mapper;
            _AccountManager = accountManager;
            _DbContext = dbContext;
            _ServiceProvider = serviceProvider;
            _EntityManager = entityManager;
            _AuthorizationManager = authorizationManager;
            _FileManager = fileManager;
        }

        readonly PowerLmsUserDbContext _DbContext;
        private readonly AccountManager _AccountManager;
        readonly IServiceProvider _ServiceProvider;
        EntityManager _EntityManager;
        IMapper _Mapper;
        readonly private AuthorizationManager _AuthorizationManager;
        readonly OwFileManager _FileManager;

        /// <summary>
        /// 存储文件的根目录。
        /// </summary>
        public static string RootPath = Path.Combine(AppContext.BaseDirectory, "Files");

        /// <summary>
        /// 获取业务负责人的所属关系。
        /// </summary>
        /// <param name="model"></param>
        /// <param name="conditional">查询的条件。支持 Id,DisplayName,FileName,ParentId。</param>
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
            foreach (var item in conditional)
                if (string.Equals(item.Key, "Id", StringComparison.OrdinalIgnoreCase))
                {
                    if (Guid.TryParse(item.Value, out var Id))
                        coll = coll.Where(c => c.Id == Id);
                }
                else if (string.Equals(item.Key, "ParentId", StringComparison.OrdinalIgnoreCase))
                {
                    if (Guid.TryParse(item.Value, out var Id))
                        coll = coll.Where(c => c.ParentId == Id);
                }
                else if (string.Equals(item.Key, "DisplayName", StringComparison.OrdinalIgnoreCase))
                {
                    coll = coll.Where(c => c.DisplayName.Contains(item.Value));
                }
                else if (string.Equals(item.Key, "OrderTypeId", StringComparison.OrdinalIgnoreCase))
                {
                    coll = coll.Where(c => c.FileName.Contains(item.Value));
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
        [HttpGet]
        [Obsolete("未来将删除此接口，请使用 GetFile 替代。")]
        public ActionResult DownloadCustomerFile(Guid token, Guid fileId)
        {
            if (_AccountManager.GetOrLoadContextByToken(token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var info = _DbContext.PlFileInfos.Find(fileId);
            if (info == null) return NotFound();
            var path = Path.Combine(AppContext.BaseDirectory, "Files", info.FilePath);
            if (!System.IO.File.Exists(path)) return NotFound();
            var stream = new FileStream(path, FileMode.Open);
            return File(stream, "application/octet-stream", info.FileName);
        }

        /// <summary>
        /// 上传客户资料的特定接口。
        /// </summary>
        /// <param name="file"></param>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpPost]
        [Obsolete("未来将删除此接口，请使用 AddFile替代。")]
        public ActionResult<UploadCustomerFileReturnDto> UploadCustomerFile(IFormFile file, [FromForm] UploadCustomerFileParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new UploadCustomerFileReturnDto();
            if (file is null)
            {
                throw new ArgumentNullException(nameof(file));
            }
            var info = new PlFileInfo
            {
                DisplayName = model.DisplayName,
                FileTypeId = model.FileTypeId,
                ParentId = model.ParentId,
                FileName = file.FileName,   //从 Content-Disposition 标头获取文件名。
            };

            info.FilePath = $"Customer\\{info.Id}.bin";
            _DbContext.Add(info);

            var stream = file.OpenReadStream();
            var path = Path.Combine(_FileManager.GetDirectory(), info.FilePath);
            var dir = Path.GetDirectoryName(path);
            Directory.CreateDirectory(dir);
            using var destStream = new FileStream(path, FileMode.Create);
            stream.CopyTo(destStream);
            _DbContext.SaveChanges();
            result.Result = info.Id;
            return result;
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
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();

            var result = new RemoveFileReturnDto();
            var item = _DbContext.PlFileInfos.Find(model.Id);
            if (item is null) return NotFound(model.Id);
            string err;
            if (item.ParentId.HasValue && _DbContext.PlJobs.Find(item.ParentId.Value) is PlJob job)
                if (job.JobTypeId == ProjectContent.AeId)
                {
                    if (!_AuthorizationManager.Demand(out err, "D0.8.4")) return StatusCode((int)HttpStatusCode.Forbidden, err);
                }
                else if (job.JobTypeId == ProjectContent.AiId)
                {
                    if (!_AuthorizationManager.Demand(out err, "D1.8.4")) return StatusCode((int)HttpStatusCode.Forbidden, err);
                }
                else if (job.JobTypeId == ProjectContent.SeId)
                {
                    if (!_AuthorizationManager.Demand(out err, "D2.8.4")) return StatusCode((int)HttpStatusCode.Forbidden, err);
                }
                else if (job.JobTypeId == ProjectContent.SiId)
                {
                    if (!_AuthorizationManager.Demand(out err, "D3.8.4")) return StatusCode((int)HttpStatusCode.Forbidden, err);
                }

            var path = Path.Combine(_FileManager.GetDirectory(), item.FilePath);
            _EntityManager.Remove(item);
            _DbContext.SaveChanges();
            if (!System.IO.File.Exists(path))   //若此文件已不存在
            {
                return StatusCode((int)HttpStatusCode.Gone, $"指定文件已经不存在,{path}");
            }
            Task.Run(() => System.IO.File.Delete(path));
            return result;
        }

        /// <summary>
        /// 上传(追加)通用的文件。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        [HttpPost]
        public ActionResult<AddFileReturnDto> AddFile([FromForm] AddFileParamsDto model)
        {
            var result = new AddFileReturnDto();
            if (_DbContext.PlJobs.Find(model.ParentId) is PlJob job)
                if (job.JobTypeId == ProjectContent.AeId)
                {
                    if (!_AuthorizationManager.Demand(out var err, "D0.8.1")) return StatusCode((int)HttpStatusCode.Forbidden, err);
                }
                else if (job.JobTypeId == ProjectContent.AiId)
                {
                    if (!_AuthorizationManager.Demand(out var err, "D1.8.1")) return StatusCode((int)HttpStatusCode.Forbidden, err);
                }
                else if (job.JobTypeId == ProjectContent.SeId)
                {
                    if (!_AuthorizationManager.Demand(out var err, "D2.8.1")) return StatusCode((int)HttpStatusCode.Forbidden, err);
                }
                else if (job.JobTypeId == ProjectContent.SiId)
                {
                    if (!_AuthorizationManager.Demand(out var err, "D3.8.1")) return StatusCode((int)HttpStatusCode.Forbidden, err);
                }

            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var fileInfo = new PlFileInfo
            {
                DisplayName = model.DisplayName,
                ParentId = model.ParentId,
                FileName = model.File.FileName,
                Remark = model.Remark,
                FilePath = Path.Combine("General", $"{Guid.NewGuid()}.bin"),
                FileTypeId = null,
            };
            if (fileInfo is ICreatorInfo creatorInfo)
            {
                creatorInfo.CreateBy = context?.User?.Id;
                creatorInfo.CreateDateTime = OwHelper.WorldNow;
            }
            result.Id = fileInfo.Id;
            _DbContext.PlFileInfos.Add(fileInfo);
            _DbContext.SaveChanges();
            var fullPath = Path.Combine(AppContext.BaseDirectory, "Files", fileInfo.FilePath);
            var dir = Path.GetDirectoryName(fullPath);
            Directory.CreateDirectory(dir);
            using var file = new FileStream(fullPath, FileMode.CreateNew);
            model.File.CopyTo(file);
            return result;
        }

        /// <summary>
        /// 下载文件。一般应先调用GetAllFileInfo接口以获得文件Id。
        /// </summary>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="404">指定Id的文件不存在。</response>  
        /// <response code="403">权限不足。</response>  
        [HttpGet]
        public ActionResult GetFile([FromQuery] GetFileParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var info = _DbContext.PlFileInfos.Find(model.FileId);
            if (info == null) return NotFound();
            string err;
            if (info.ParentId.HasValue && _DbContext.PlJobs.Find(info.ParentId.Value) is PlJob job)
                if (job.JobTypeId == ProjectContent.AeId)
                {
                    if (!_AuthorizationManager.Demand(out err, "D0.8.2")) return StatusCode((int)HttpStatusCode.Forbidden, err);
                }
                else if (job.JobTypeId == ProjectContent.AiId)
                {
                    if (!_AuthorizationManager.Demand(out err, "D1.8.2")) return StatusCode((int)HttpStatusCode.Forbidden, err);
                }
                else if (job.JobTypeId == ProjectContent.SeId)
                {
                    if (!_AuthorizationManager.Demand(out err, "D2.8.2")) return StatusCode((int)HttpStatusCode.Forbidden, err);
                }
                else if (job.JobTypeId == ProjectContent.SiId)
                {
                    if (!_AuthorizationManager.Demand(out err, "D3.8.2")) return StatusCode((int)HttpStatusCode.Forbidden, err);
                }

            var path = Path.Combine(AppContext.BaseDirectory, "Files", info.FilePath);
            if (!System.IO.File.Exists(path)) return NotFound();
            var stream = new FileStream(path, FileMode.Open);
            return File(stream, "application/octet-stream", info.FileName);
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
            [FromQuery] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetAllFileInfoReturnDto();

            var dbSet = _DbContext.PlFileInfos;
            var coll = dbSet.OrderBy(model.OrderFieldName, model.IsDesc).AsNoTracking();
            foreach (var item in conditional)
                if (string.Equals(item.Key, "Id", StringComparison.OrdinalIgnoreCase))
                {
                    if (Guid.TryParse(item.Value, out var id))
                        coll = coll.Where(c => c.Id == id);
                }
                else if (string.Equals(item.Key, "ParentId", StringComparison.OrdinalIgnoreCase))
                {
                    if (Guid.TryParse(item.Value, out var id))
                        coll = coll.Where(c => c.ParentId == id);
                }
            var prb = _EntityManager.GetAll(coll, model.StartIndex, model.Count);
            _Mapper.Map(prb, result);
            return result;
        }
        #endregion 通用文件管理接口
    }

}

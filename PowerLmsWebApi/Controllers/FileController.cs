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
using PowerLmsServer.EfData;
using PowerLmsServer.Managers;
using PowerLmsWebApi.Dto;
using SixLabors.ImageSharp.Metadata;
using System.ComponentModel.DataAnnotations;
using System.Drawing.Imaging;
using System.IO;
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
        public FileController(IServiceProvider serviceProvider, IMapper mapper, AccountManager accountManager, PowerLmsUserDbContext dbContext, EntityManager entityManager)
        {
            _Mapper = mapper;
            _AccountManager = accountManager;
            _DbContext = dbContext;
            _ServiceProvider = serviceProvider;
            _EntityManager = entityManager;
        }

        readonly PowerLmsUserDbContext _DbContext;
        private readonly AccountManager _AccountManager;
        readonly IServiceProvider _ServiceProvider;
        EntityManager _EntityManager;
        IMapper _Mapper;

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
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
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
        public ActionResult DownloadCustomerFile(Guid token, Guid fileId)
        {
            if (_AccountManager.GetAccountFromToken(token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var info = _DbContext.PlFileInfos.Find(fileId);
            if (info == null) return NotFound();
            var path = Path.Combine(AppContext.BaseDirectory, "Files", info.FilePath);
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
        public ActionResult<UploadCustomerFileReturnDto> UploadCustomerFile(IFormFile file, [FromForm] UploadCustomerFileParamsDto model)
        {
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
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
            var path = Path.Combine(AppContext.BaseDirectory, "Files", info.FilePath);
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
        /// <response code="404">指定Id的文件不存在。</response>  
        /// <response code="500">其他错误，并发导致数据变化不能完成操作。</response>
        [HttpDelete]
        public ActionResult<RemoveFileReturnDto> RemoveFile(RemoveFileParamsDto model)
        {
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new RemoveFileReturnDto();
            var item = _DbContext.PlFileInfos.Find(model.Id);
            if (item is null) return NotFound(model.Id);
            var path = Path.Combine(AppContext.BaseDirectory, "Files", item.FilePath);
            if (!System.IO.File.Exists(path)) return NotFound(path);
            Task.Run(() => System.IO.File.Delete(path));
            _EntityManager.Remove(item);
            _DbContext.SaveChanges();
            return result;
        }

        /// <summary>
        /// 上传(追加)通用的文件。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpPost]
        public ActionResult<AddFileReturnDto> AddFile([FromForm] AddFileParamsDto model)
        {
            var result = new AddFileReturnDto();
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
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
        [HttpGet]
        public ActionResult GetFile([FromQuery] GetFileParamsDto model)
        {
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var info = _DbContext.PlFileInfos.Find(model.FileId);
            if (info == null) return NotFound();
            var path = Path.Combine(AppContext.BaseDirectory, "Files", info.FilePath);
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
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
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

    #region 通用文件管理

    /// <summary>
    /// 获取全部通用文件信息返回值封装类。
    /// </summary>
    public class GetAllFileInfoReturnDto : PagingReturnDtoBase<PlFileInfo>
    {
    }

    /// <summary>
    /// 下载文件通用功能参数封装类。
    /// </summary>
    public class GetFileParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 文件Id。
        /// </summary>
        public Guid FileId { get; set; }
    }

    /// <summary>
    /// 上传文件通用接口的参数封装类。
    /// </summary>
    public class AddFileParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 文件。
        /// </summary>
        public IFormFile File { get; set; }

        /// <summary>
        /// 所附属实体的Id，如附属在Ea单上则是Ea单的Id,附属在货场出重条目上的则是那个条目的Id。
        /// </summary>
        public Guid ParentId { get; set; }

        /// <summary>
        /// 显示名。
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// 备注。
        /// </summary>
        public string Remark { get; set; }
    }

    /// <summary>
    /// 上传文件通用接口的返回值封装类。
    /// </summary>
    public class AddFileReturnDto : AddReturnDtoBase
    {
    }

    /// <summary>
    /// 删除存储的文件功能参数封装类。
    /// </summary>
    public class RemoveFileParamsDto : RemoveParamsDtoBase
    {
    }

    /// <summary>
    /// 删除存储的文件功能返回值封装类。
    /// </summary>
    public class RemoveFileReturnDto : RemoveReturnDtoBase
    {
    }

    #endregion 通用文件管理

    /// <summary>
    /// 获取文件列表功能的返回值封装类。
    /// </summary>
    public class GetAllCustomerFileListReturnDto : PagingReturnDtoBase<PlFileInfo>
    {
    }

    /// <summary>
    /// 上传文件的功能参数封装类。
    /// </summary>
    public class UploadCustomerFileParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 文件的显示名。这是个友好名称。任意设置。
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// 类型字典Id。
        /// </summary>
        public Guid FileTypeId { get; set; }

        /// <summary>
        /// 所属实体的Id。
        /// </summary>
        public Guid? ParentId { get; set; }

    }

    /// <summary>
    /// 上传文件的功能返回值封装类。
    /// </summary>
    public class UploadCustomerFileReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 文件的唯一Id。
        /// </summary>
        public Guid Result { get; set; }
    }
}

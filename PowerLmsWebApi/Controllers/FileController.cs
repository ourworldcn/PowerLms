using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.VisualStudio.Web.CodeGeneration;
using NuGet.Protocol.Plugins;
using PowerLms.Data;
using PowerLmsServer.EfData;
using PowerLmsServer.Managers;
using PowerLmsWebApi.Dto;
using SixLabors.ImageSharp.Metadata;
using System.ComponentModel.DataAnnotations;
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
        /// 
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
        /// 
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

        /// <summary>
        /// 删除存储的文件。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="404">指定的实体不存在。</response>  
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

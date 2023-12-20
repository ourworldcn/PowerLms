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
        public FileController(PowerLmsUserDbContext dbContext, AccountManager accountManager, ServiceProvider serviceProvider, EntityManager entityManager, IMapper mapper)
        {
            _DbContext = dbContext;
            _AccountManager = accountManager;
            _ServiceProvider = serviceProvider;
            _EntityManager = entityManager;
            _Mapper = mapper;
        }

        readonly PowerLmsUserDbContext _DbContext;
        private readonly AccountManager _AccountManager;
        readonly ServiceProvider _ServiceProvider;
        EntityManager _EntityManager;
        IMapper _Mapper;

        /// <summary>
        /// 获取业务负责人的所属关系。
        /// </summary>
        /// <param name="token">登录令牌。</param>
        /// <param name="startIndex">起始位置，从0开始。</param>
        /// <param name="count">最大返回数量。-1表示全返回。</param>
        /// <param name="conditional">查询的条件。支持 Id,DisplayName,FileName 。</param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">指定类别Id无效。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpGet]
        public ActionResult<GetAllCustomerFileListReturnDto> GetAllCustomerFileList(Guid token,
            [Range(0, int.MaxValue, ErrorMessage = "必须大于或等于0.")] int startIndex, [FromQuery][Range(-1, int.MaxValue)] int count = -1,
            [FromQuery] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetAccountFromToken(token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetAllCustomerFileListReturnDto();
            var coll = _DbContext.PlFileInfos.AsNoTracking();
            foreach (var item in conditional)
                if (string.Equals(item.Key, "Id", StringComparison.OrdinalIgnoreCase))
                {
                    if (Guid.TryParse(item.Value, out var Id))
                        coll = coll.Where(c => c.Id == Id);
                }
                else if (string.Equals(item.Key, "DisplayName", StringComparison.OrdinalIgnoreCase))
                {
                    coll = coll.Where(c => c.DisplayName.Contains(item.Value));
                }
                else if (string.Equals(item.Key, "OrderTypeId", StringComparison.OrdinalIgnoreCase))
                {
                    coll = coll.Where(c => c.FileName.Contains(item.Value));
                }
            var prb = _EntityManager.GetAll(coll, startIndex, count);
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
        public ActionResult<UploadCustomerFileReturnDto> UploadCustomerFile(IFormFile file, [FromQuery] UploadCustomerFileParamsDto model)
        {
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new UploadCustomerFileReturnDto();
            if (file is null)
            {
                throw new ArgumentNullException(nameof(file));
            }
            var ext = Path.GetExtension(file.FileName);
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
            using var destStream = new FileStream(path, FileMode.Create);
            stream.CopyTo(destStream);
            return result;
        }
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

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PowerLms.Data;
using PowerLmsServer.EfData;
using PowerLmsServer.Managers;
using PowerLmsWebApi.Dto;
using SysIO = System.IO;

namespace PowerLmsWebApi.Controllers.System
{
    /// <summary>
    /// 验证码相关功能的控制器。
    /// </summary>
    public class CaptchaController : PlControllerBase
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public CaptchaController(CaptchaManager captchaManager, PowerLmsUserDbContext userDbContext = null)
        {
            _CaptchaManager = captchaManager;
            _UserDbContext = userDbContext;
        }

        readonly CaptchaManager _CaptchaManager;
        readonly PowerLmsUserDbContext _UserDbContext;

        /// <summary>
        /// 获取一个新的验证码图片，下载的图片文件名（无扩展名）需要记住，在验证时，需要将答案和文件名一同上传。
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public ActionResult GetNewCaptcha([FromQuery]Guid id)
        {
            //var result = new GetNewCaptchaReturnDto();
            //if (_AccountManager.GetOrLoadAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();

            //var path = Path.Combine(AppContext.BaseDirectory, "Files", info.FilePath);
            //var stream = new FileStream(path, FileMode.Open);
            //return File(stream, "application/octet-stream", info.FileName);

            //var id = Guid.NewGuid();
            var fileName = id.ToString();
            var path = Path.GetTempPath();
            var fullPath = Path.Combine(path, fileName);
            fullPath = Path.ChangeExtension(fullPath, ".jpg");
            var ans = _CaptchaManager.GetNew(fullPath);

            var captchaInfo = new CaptchaInfo(id)
            {
                Answer = ans,
                CreateDateTime = DateTime.UtcNow,
                FullPath = fullPath,
                DownloadDateTime = DateTime.UtcNow,
            };
            _UserDbContext.CaptchaInfos.Add(captchaInfo);
            _UserDbContext.SaveChanges();
            var stream = SysIO.File.OpenRead(fullPath);
            return File(stream, "application/jpeg", Path.GetFileName(fullPath));
        }

    }

}

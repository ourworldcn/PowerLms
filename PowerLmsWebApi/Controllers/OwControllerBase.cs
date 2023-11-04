using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace PowerLmsWebApi.Controllers
{
    /// <summary>
    /// 控制器基类。
    /// </summary>
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class OwControllerBase : ControllerBase
    {
    }
}

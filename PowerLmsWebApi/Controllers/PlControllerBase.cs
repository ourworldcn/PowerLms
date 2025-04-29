using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace PowerLmsWebApi.Controllers
{
    /// <summary>
    /// 控制器基类。所有业务控制器继承自此类，提供通用的路由及API控制器设置。
    /// </summary>
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class PlControllerBase : ControllerBase
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public PlControllerBase()
        {
            // 基类构造函数，目前为空实现
            // 子类可通过依赖注入获取所需服务
        }
    }

    /// <summary>
    /// 专用于处理字典参数的模型绑定器。
    /// 解决ASP.NET Core默认模型绑定无法正确处理带点号(.)的查询参数键名问题。
    /// 例如：将"PlJob.jobNo=123"正确绑定为字典中键为"PlJob.jobNo"的条目，
    /// 而不是默认行为下被解析为"PlJob"对象的"jobNo"属性。
    /// </summary>
    public class DotKeyDictionaryModelBinder : IModelBinder
    {
        /// <summary>
        /// 执行模型绑定操作。
        /// </summary>
        /// <param name="bindingContext">绑定上下文，包含请求信息和目标模型元数据</param>
        /// <returns>表示异步操作的任务</returns>
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            // 创建一个不区分大小写的字典作为结果
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            // 获取当前HTTP请求的查询参数集合
            var query = bindingContext.HttpContext.Request.Query;

            // 遍历所有查询参数
            foreach (var key in query.Keys)
            {
                // 跳过已知的基本分页和认证参数，这些参数会被单独绑定到其他模型属性
                if (key == "Token" || key == "StartIndex" || key == "Count" ||
                    key == "OrderFieldName" || key == "IsDesc" || key == "WfState")
                    continue;

                // 获取参数值并转换为字符串
                var value = query[key].ToString();
                // 将完整键名（包含可能的点号）和值添加到结果字典中
                result[key] = value;
            }

            // 设置绑定结果为成功，并返回填充的字典
            bindingContext.Result = ModelBindingResult.Success(result);
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// 为Dictionary&lt;string, string&gt;类型提供自定义模型绑定器的提供程序。
    /// 当框架需要绑定字符串字典时，此提供程序将返回DotKeyDictionaryModelBinder实例。
    /// </summary>
    public class DotKeyDictionaryModelBinderProvider : IModelBinderProvider
    {
        /// <summary>
        /// 获取适用于当前模型类型的绑定器。
        /// </summary>
        /// <param name="context">包含模型类型和元数据的上下文</param>
        /// <returns>如果模型类型为Dictionary&lt;string, string&gt;则返回DotKeyDictionaryModelBinder实例，否则返回null</returns>
        public IModelBinder GetBinder(ModelBinderProviderContext context)
        {
            // 检查模型类型是否为字符串字典
            if (context.Metadata.ModelType == typeof(Dictionary<string, string>))
            {
                // 返回自定义的字典绑定器
                return new DotKeyDictionaryModelBinder();
            }

            // 对于其他类型，返回null以使用默认绑定器
            return null;
        }
    }

}

using Microsoft.EntityFrameworkCore;
using NPOI.SS.Formula.Functions;
using SixLabors.ImageSharp.Metadata;
using System.Data;
using System.Net;
using System.Text;

namespace PowerLmsWebApi.Middleware
{
    /// <summary>
    /// 自定义中间件。
    /// </summary>
    public class PlExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<PlExceptionMiddleware> _Logger;

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="next"></param>
        /// <param name="logger"></param>
        public PlExceptionMiddleware(RequestDelegate next, ILogger<PlExceptionMiddleware> logger)
        {
            _next = next;
            _Logger = logger;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="httpContext"></param>
        /// <returns></returns>
        public async Task InvokeAsync(HttpContext httpContext)
        {
            try
            {
                await _next(httpContext);
            }
            catch (DbUpdateConcurrencyException err)
            {
                httpContext.Response.StatusCode = (int)HttpStatusCode.InsufficientStorage;
                await httpContext.Response.WriteAsJsonAsync(err.Message, typeof(string));
            }
            catch (DBConcurrencyException err)
            {
                httpContext.Response.StatusCode = (int)HttpStatusCode.InsufficientStorage;
                await httpContext.Response.WriteAsJsonAsync(err.Message, typeof(string));
            }
            catch { throw; }
            finally
            {

            }
        }
    }
}

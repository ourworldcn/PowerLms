using Microsoft.Extensions.DependencyInjection;
using OW;
using PowerLms.Data;
using PowerLmsServer.EfData;

namespace PowerLmsServer.Managers
{
    /// <summary>
    /// 与Token生存期对应的上下文。
    /// </summary>
    [OwAutoInjection(ServiceLifetime.Scoped)]
    public class OwContext : OwDisposableBase
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="serviceProvider"></param>
        public OwContext(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;
        }

        /// <summary>
        /// 令牌。
        /// </summary>
        public Guid Token { get; set; }

        /// <summary>
        /// 账号对象。
        /// </summary>
        public Account User { get; set; }

        /// <summary>
        /// 这次工作上下文的创建时间。
        /// </summary>
        public DateTime CreateDateTime { get; set; } = OwHelper.WorldNow;

        /// <summary>
        /// 当前使用的范围服务容器。
        /// </summary>
        public IServiceProvider ServiceProvider { get; }

        #region 方法

        /// <summary>
        /// 标记当前进行了一次有效操作，这将导致延迟清理时间。
        /// </summary>
        public void Nop()
        {
            User.LastModifyDateTimeUtc = OwHelper.WorldNow;
        }

        /// <summary>
        /// 保存变化。
        /// </summary>
        /// <returns></returns>
        public int SaveChanges()
        {
            int result = ServiceProvider.GetRequiredService<PowerLmsUserDbContext>().SaveChanges();
            return result;
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
        #endregion 方法
    }
}

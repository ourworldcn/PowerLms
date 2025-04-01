using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PowerLms.Data;
using PowerLmsServer.EfData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PowerLmsServer.Managers
{
    /// <summary>
    /// 消息系统配置选项
    /// </summary>
    public class OwMessageOptions : IOptions<OwMessageOptions>
    {
        /// <summary>
        /// 配置节名称
        /// </summary>
        public const string SectionName = "OwMessage";

        /// <summary>
        /// 已读消息的保存天数，默认30天
        /// </summary>
        public int ReadMessageExpiryDays { get; set; } = 30;

        /// <summary>
        /// 未读消息的保存天数，默认90天
        /// </summary>
        public int UnreadMessageExpiryDays { get; set; } = 90;

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public OwMessageOptions Value => this;
    }

    /// <summary>
    /// 系统内消息管理器，用于处理消息的发送、查询、标记已读和删除等操作
    /// </summary>
    [OwAutoInjection(ServiceLifetime.Singleton)]
    public class OwMessageManager : IDisposable
    {
        private readonly IDbContextFactory<PowerLmsUserDbContext> _dbContextFactory;
        private readonly ILogger<OwMessageManager> _logger;
        private readonly IOptionsMonitor<OwMessageOptions> _optionsMonitor;
        private readonly Timer _cleanupTimer;
        private bool _isDisposed;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="dbContextFactory">数据库上下文工厂</param>
        /// <param name="logger">日志记录器</param>
        /// <param name="optionsMonitor">消息配置选项监视器，支持运行时更新配置</param>
        public OwMessageManager(
            IDbContextFactory<PowerLmsUserDbContext> dbContextFactory,
            ILogger<OwMessageManager> logger,
            IOptionsMonitor<OwMessageOptions> optionsMonitor)
        {
            _dbContextFactory = dbContextFactory;
            _logger = logger;
            _optionsMonitor = optionsMonitor;

            // 注册配置变更通知
            _optionsMonitor.OnChange(options =>
            {
                _logger.LogInformation("消息系统配置已更新，已读消息保存期限: {ReadDays} 天，未读消息保存期限: {UnreadDays} 天",
                    options.ReadMessageExpiryDays,
                    options.UnreadMessageExpiryDays);
            });

            // 创建定时器，每小时执行一次清理过期消息
            _cleanupTimer = new Timer(CleanupExpiredMessages, null, TimeSpan.FromMinutes(1), TimeSpan.FromHours(1));

            _logger.LogInformation("消息管理器初始化完成，已读消息保存期限: {ReadDays} 天，未读消息保存期限: {UnreadDays} 天",
                _optionsMonitor.CurrentValue.ReadMessageExpiryDays,
                _optionsMonitor.CurrentValue.UnreadMessageExpiryDays);
        }

        #region 发送消息

        /// <summary>
        /// 发送消息给指定用户
        /// </summary>
        /// <param name="senderId">发送者ID，如果是系统消息可以为null</param>
        /// <param name="receiverIds">接收者ID集合</param>
        /// <param name="title">消息标题</param>
        /// <param name="content">消息内容（HTML格式）</param>
        /// <param name="isSystemMessage">是否是系统消息</param>
        /// <returns>成功发送的消息数量</returns>
        public int SendMessage(
            Guid? senderId,
            IEnumerable<Guid> receiverIds,
            string title,
            string content,
            bool isSystemMessage = false)
        {
            try
            {
                if (string.IsNullOrEmpty(title))
                    throw new ArgumentException("消息标题不能为空", nameof(title));

                if (string.IsNullOrEmpty(content))
                    throw new ArgumentException("消息内容不能为空", nameof(content));

                if (title.Length > 64)
                    title = title.Substring(0, 64);

                var receiverList = receiverIds?.Distinct().ToList();
                if (receiverList == null || receiverList.Count == 0)
                    throw new ArgumentException("接收者列表不能为空", nameof(receiverIds));

                // 在后台发送消息
                Task.Run(() => SendMessageInternal(senderId, receiverList, title, content, isSystemMessage));

                return receiverList.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "发送消息失败: {Title}", title);
                throw;
            }
        }

        /// <summary>
        /// 内部方法：发送消息给指定用户
        /// </summary>
        private void SendMessageInternal(
            Guid? senderId,
            List<Guid> receiverIds,
            string title,
            string content,
            bool isSystemMessage)
        {
            try
            {
                using var dbContext = _dbContextFactory.CreateDbContext();

                var messages = new List<OwMessage>();
                var currentTime = DateTime.UtcNow;

                foreach (var receiverId in receiverIds)
                {
                    var message = new OwMessage
                    {
                        UserId = receiverId,
                        Title = title,
                        Content = content,
                        CreateBy = senderId,
                        CreateUtc = currentTime,
                        IsSystemMessage = isSystemMessage
                    };
                    messages.Add(message);
                }

                dbContext.AddRange(messages);
                dbContext.SaveChanges();

                _logger.LogInformation(
                    "已成功发送消息 \"{Title}\" 给 {ReceiverCount} 位用户",
                    title,
                    receiverIds.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "发送消息失败: {Title}", title);
                throw;
            }
        }

        /// <summary>
        /// 发送系统消息给指定用户
        /// </summary>
        /// <param name="receiverIds">接收者ID集合</param>
        /// <param name="title">消息标题</param>
        /// <param name="content">消息内容（HTML格式）</param>
        /// <returns>成功发送的消息数量</returns>
        public int SendSystemMessage(
            IEnumerable<Guid> receiverIds,
            string title,
            string content)
        {
            return SendMessage(null, receiverIds, title, content, true);
        }

        #endregion

        #region 读取消息

        /// <summary>
        /// 获取用户的消息列表
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="onlyUnread">是否只获取未读消息</param>
        /// <param name="startIndex">起始索引</param>
        /// <param name="count">获取数量，-1表示获取全部</param>
        /// <returns>消息列表</returns>
        public List<OwMessage> GetMessages(
            Guid userId,
            bool onlyUnread = false,
            int startIndex = 0,
            int count = -1)
        {
            try
            {
                using var dbContext = _dbContextFactory.CreateDbContext();

                var query = dbContext.OwMessages
                    .AsNoTracking()
                    .Where(m => m.UserId == userId);

                if (onlyUnread)
                {
                    query = query.Where(m => m.ReadUtc == null);
                }

                query = query.OrderByDescending(m => m.CreateUtc);

                if (startIndex > 0)
                {
                    query = query.Skip(startIndex);
                }

                if (count > 0)
                {
                    query = query.Take(count);
                }

                return query.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取用户 {UserId} 的消息列表失败", userId);
                throw;
            }
        }

        /// <summary>
        /// 获取用户未读消息数量
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <returns>未读消息数量</returns>
        public int GetUnreadMessageCount(Guid userId)
        {
            try
            {
                using var dbContext = _dbContextFactory.CreateDbContext();

                return dbContext.OwMessages
                    .Count(m => m.UserId == userId && m.ReadUtc == null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取用户 {UserId} 的未读消息数量失败", userId);
                throw;
            }
        }

        /// <summary>
        /// 获取指定ID的消息详情
        /// </summary>
        /// <param name="messageId">消息ID</param>
        /// <param name="userId">用户ID，用于验证消息归属</param>
        /// <param name="markAsRead">是否标记为已读</param>
        /// <returns>消息详情，如果消息不存在或不属于指定用户则返回null</returns>
        public OwMessage GetMessageDetail(Guid messageId, Guid userId, bool markAsRead = true)
        {
            try
            {
                using var dbContext = _dbContextFactory.CreateDbContext();

                var message = dbContext.OwMessages
                    .FirstOrDefault(m => m.Id == messageId && m.UserId == userId);

                if (message == null)
                    return null;

                // 如果消息未读且需要标记为已读，则更新已读时间
                if (markAsRead && message.ReadUtc == null)
                {
                    message.ReadUtc = DateTime.UtcNow;
                    dbContext.SaveChanges();
                }

                return message;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取消息详情失败，消息ID: {MessageId}", messageId);
                throw;
            }
        }

        #endregion

        #region 标记已读

        /// <summary>
        /// 标记消息为已读
        /// </summary>
        /// <param name="messageId">消息ID</param>
        /// <param name="userId">用户ID，用于验证消息归属</param>
        /// <returns>操作是否成功</returns>
        public bool MarkAsRead(Guid messageId, Guid userId)
        {
            try
            {
                using var dbContext = _dbContextFactory.CreateDbContext();

                var message = dbContext.OwMessages
                    .FirstOrDefault(m => m.Id == messageId && m.UserId == userId);

                if (message == null || message.ReadUtc != null)
                    return false;

                message.ReadUtc = DateTime.UtcNow;
                dbContext.SaveChanges();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "标记消息为已读失败，消息ID: {MessageId}", messageId);
                throw;
            }
        }

        /// <summary>
        /// 批量标记消息为已读
        /// </summary>
        /// <param name="messageIds">消息ID集合</param>
        /// <param name="userId">用户ID，用于验证消息归属</param>
        /// <returns>成功标记的消息数量</returns>
        public int MarkAsRead(IEnumerable<Guid> messageIds, Guid userId)
        {
            try
            {
                using var dbContext = _dbContextFactory.CreateDbContext();

                var idList = messageIds.ToList();
                var messages = dbContext.OwMessages
                    .Where(m => idList.Contains(m.Id) && m.UserId == userId && m.ReadUtc == null)
                    .ToList();

                if (messages.Count == 0)
                    return 0;

                var now = DateTime.UtcNow;
                foreach (var message in messages)
                {
                    message.ReadUtc = now;
                }

                dbContext.SaveChanges();
                return messages.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量标记消息为已读失败");
                throw;
            }
        }

        /// <summary>
        /// 标记所有消息为已读
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <returns>成功标记的消息数量</returns>
        public int MarkAllAsRead(Guid userId)
        {
            try
            {
                using var dbContext = _dbContextFactory.CreateDbContext();

                var now = DateTime.UtcNow;

                // 直接使用SQL更新语句提高性能
                var count = dbContext.Database.ExecuteSqlRaw(
                    "UPDATE OwMessages SET ReadUtc = {0} WHERE UserId = {1} AND ReadUtc IS NULL",
                    now, userId);

                return count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "标记所有消息为已读失败，用户ID: {UserId}", userId);
                throw;
            }
        }

        #endregion

        #region 删除消息

        /// <summary>
        /// 删除指定消息
        /// </summary>
        /// <param name="messageId">消息ID</param>
        /// <param name="userId">用户ID，用于验证消息归属</param>
        /// <returns>操作是否成功</returns>
        public bool DeleteMessage(Guid messageId, Guid userId)
        {
            try
            {
                using var dbContext = _dbContextFactory.CreateDbContext();

                var message = dbContext.OwMessages
                    .FirstOrDefault(m => m.Id == messageId && m.UserId == userId);

                if (message == null)
                    return false;

                dbContext.Remove(message);
                dbContext.SaveChanges();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除消息失败，消息ID: {MessageId}", messageId);
                throw;
            }
        }

        /// <summary>
        /// 批量删除消息
        /// </summary>
        /// <param name="messageIds">消息ID集合</param>
        /// <param name="userId">用户ID，用于验证消息归属</param>
        /// <returns>成功删除的消息数量</returns>
        public int DeleteMessages(IEnumerable<Guid> messageIds, Guid userId)
        {
            try
            {
                using var dbContext = _dbContextFactory.CreateDbContext();

                var idList = messageIds.ToList();
                var messages = dbContext.OwMessages
                    .Where(m => idList.Contains(m.Id) && m.UserId == userId)
                    .ToList();

                if (messages.Count == 0)
                    return 0;

                dbContext.RemoveRange(messages);
                dbContext.SaveChanges();
                return messages.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量删除消息失败");
                throw;
            }
        }

        /// <summary>
        /// 删除用户的所有消息
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <returns>成功删除的消息数量</returns>
        public int DeleteAllMessages(Guid userId)
        {
            try
            {
                using var dbContext = _dbContextFactory.CreateDbContext();

                // 直接使用SQL删除语句提高性能
                var count = dbContext.Database.ExecuteSqlRaw(
                    "DELETE FROM OwMessages WHERE UserId = {0}",
                    userId);

                return count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除用户所有消息失败，用户ID: {UserId}", userId);
                throw;
            }
        }

        #endregion

        #region 定时清理

        /// <summary>
        /// 清理过期消息的定时任务
        /// </summary>
        private void CleanupExpiredMessages(object state)
        {
            try
            {
                // 获取当前配置值
                var options = _optionsMonitor.CurrentValue;

                using var dbContext = _dbContextFactory.CreateDbContext();

                var now = DateTime.UtcNow;
                var readExpiry = now.AddDays(-options.ReadMessageExpiryDays);
                var unreadExpiry = now.AddDays(-options.UnreadMessageExpiryDays);

                // 删除已读的过期消息
                var readCount = dbContext.Database.ExecuteSqlRaw(
                    "DELETE FROM OwMessages WHERE ReadUtc IS NOT NULL AND ReadUtc < {0}",
                    readExpiry);

                // 删除未读的过期消息
                var unreadCount = dbContext.Database.ExecuteSqlRaw(
                    "DELETE FROM OwMessages WHERE ReadUtc IS NULL AND CreateUtc < {0}",
                    unreadExpiry);

                _logger.LogInformation(
                    "已清理过期消息: {ReadCount} 条已读消息, {UnreadCount} 条未读消息",
                    readCount,
                    unreadCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "清理过期消息失败");
            }
        }

        /// <summary>
        /// 手动触发清理过期消息
        /// </summary>
        /// <returns>清理的消息总数</returns>
        public int TriggerCleanup()
        {
            try
            {
                // 获取当前配置值
                var options = _optionsMonitor.CurrentValue;

                using var dbContext = _dbContextFactory.CreateDbContext();

                var now = DateTime.UtcNow;
                var readExpiry = now.AddDays(-options.ReadMessageExpiryDays);
                var unreadExpiry = now.AddDays(-options.UnreadMessageExpiryDays);

                // 删除已读的过期消息
                var readCount = dbContext.Database.ExecuteSqlRaw(
                    "DELETE FROM OwMessages WHERE ReadUtc IS NOT NULL AND ReadUtc < {0}",
                    readExpiry);

                // 删除未读的过期消息
                var unreadCount = dbContext.Database.ExecuteSqlRaw(
                    "DELETE FROM OwMessages WHERE ReadUtc IS NULL AND CreateUtc < {0}",
                    unreadExpiry);

                _logger.LogInformation(
                    "手动清理过期消息: {ReadCount} 条已读消息, {UnreadCount} 条未读消息",
                    readCount,
                    unreadCount);

                return readCount + unreadCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "手动清理过期消息失败");
                throw;
            }
        }

        #endregion

        #region IDisposable 实现

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        /// <param name="disposing">是否正在释放托管资源</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    _cleanupTimer?.Dispose();
                    _logger.LogInformation("消息管理器已释放资源");
                }

                _isDisposed = true;
            }
        }

        #endregion
    }

    /// <summary>
    /// 消息管理器扩展方法，用于配置服务
    /// </summary>
    public static class OwMessageServiceExtensions
    {
        /// <summary>
        /// 添加消息系统服务
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <param name="configureOptions">配置选项的委托</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddOwMessageServices(
            this IServiceCollection services,
            Action<OwMessageOptions> configureOptions)
        {
            // 注册数据库上下文工厂（如果尚未注册）
            if (!services.Any(s => s.ServiceType == typeof(IDbContextFactory<PowerLmsUserDbContext>)))
            {
                services.AddDbContextFactory<PowerLmsUserDbContext>();
            }
            // 配置选项
            services.Configure(configureOptions);

            // 注册消息管理器服务
            services.AddSingleton<OwMessageManager>();

            return services;
        }
    }
}

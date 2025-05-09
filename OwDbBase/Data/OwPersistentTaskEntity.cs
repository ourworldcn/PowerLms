using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using OW.Data;

namespace OW.Data
{
    /// <summary>
    /// 长时间运行任务的信息实体类。
    /// </summary>
    [Table("OwPersistentTaskEntities")]
    [Comment("长时间运行任务信息")]
    public class OwPersistentTaskEntity : GuidKeyObjectBase
    {
        /// <summary>
        /// 任务名称
        /// </summary>
        [Required]
        [StringLength(80)]
        [Comment("任务名称")]
        public string Name { get; set; }

        /// <summary>
        /// 任务描述
        /// </summary>
        [StringLength(255)]
        [Comment("任务描述")]
        public string Description { get; set; }

        /// <summary>
        /// 任务类型代码
        /// </summary>
        [StringLength(32)]
        [Comment("任务类型代码")]
        public string TaskType { get; set; }

        /// <summary>
        /// 任务状态位标识
        /// 0x01(1): 等待中
        /// 0x02(2): 运行中
        /// 0x04(4): 已完成
        /// 0x08(8): 失败
        /// 0x10(16): 已取消
        /// 0x20(32): 可被取消
        /// 0x40(64): 已中止
        /// 0x80(128): 已暂停
        /// </summary>
        [Comment("任务状态位标识")]
        public byte StatusFlags { get; set; }

        /// <summary>
        /// 任务属性位标识
        /// <list type="bullet">
        /// <item>0x01(1): 可被取消</item>
        /// <item>0x02(2): 可被暂停</item>
        /// <item>0x04(4): 重要任务</item>
        /// <item>0x08(8): 需要报告</item>
        /// <item>0x10(16): 系统任务</item>
        /// <item>0x20(32): 定期任务</item>
        /// <item>0x40(64): 保留</item>
        /// <item>0x80(128): 保留</item>
        /// </list>
        /// </summary>
        [Comment("任务属性位标识")]
        public byte PropertyFlags { get; set; }

        /// <summary>
        /// 任务创建时间（UTC），精确到毫秒
        /// </summary>
        [Comment("创建时间(UTC)")]
        [Precision(3)] // 保留到毫秒级别(3位小数)
        public DateTime CreateUtc { get; set; }

        /// <summary>
        /// 任务开始时间（UTC），精确到毫秒
        /// </summary>
        [Comment("开始时间(UTC)")]
        [Precision(3)] // 保留到毫秒级别(3位小数)
        public DateTime? StartUtc { get; set; }

        /// <summary>
        /// 任务结束时间（UTC），精确到毫秒
        /// </summary>
        [Comment("结束时间(UTC)")]
        [Precision(3)] // 保留到毫秒级别(3位小数)
        public DateTime? EndUtc { get; set; }

        /// <summary>
        /// 任务进度（0-100）
        /// </summary>
        [Comment("任务进度（0-100）")]
        public byte Progress { get; set; }

        /// <summary>
        /// 任务优先级（越小越高）：0-最高，1-高，2-中，3-低，4-最低
        /// </summary>
        [Comment("任务优先级：0-最高~4-最低")]
        public byte Priority { get; set; }

        /// <summary>
        /// 创建者ID
        /// </summary>
        [Comment("创建者ID")]
        public Guid CreatorId { get; set; }

        /// <summary>
        /// 执行任务的处理器类名
        /// </summary>
        [StringLength(100)]
        [Comment("处理器类名")]
        public string ProcessorClass { get; set; }

        /// <summary>
        /// 任务参数（JSON格式）
        /// </summary>
        [Comment("任务参数（JSON格式）")]
        public string Parameters { get; set; }

        /// <summary>
        /// 任务结果/错误信息（JSON格式）
        /// </summary>
        [Comment("任务结果/错误信息（JSON格式）")]
        public string ResultData { get; set; }

        /// <summary>
        /// 重试信息：低4位为已重试次数(0-15)，高4位为最大重试次数(0-15)
        /// </summary>
        [Comment("重试信息：低4位为已重试次数，高4位为最大重试次数")]
        public byte RetryInfo { get; set; }

        /// <summary>
        /// 下次重试时间（UTC），精确到毫秒
        /// </summary>
        [Comment("下次重试时间(UTC)")]
        [Precision(3)] // 保留到毫秒级别(3位小数)
        public DateTime? NextRetryUtc { get; set; }

        /// <summary>
        /// 最后更新时间（UTC），精确到毫秒
        /// </summary>
        [Comment("最后更新时间(UTC)")]
        [Precision(3)] // 保留到毫秒级别(3位小数)
        public DateTime LastUpdateUtc { get; set; }

        /// <summary>
        /// 父任务ID，如果这是子任务
        /// </summary>
        [Comment("父任务ID")]
        public Guid? ParentTaskId { get; set; }

        /// <summary>
        /// 机器/服务器标识码
        /// </summary>
        [StringLength(50)]
        [Comment("执行机器标识码")]
        public string MachineId { get; set; }

        /// <summary>
        /// 任务超时秒数
        /// </summary>
        [Comment("任务超时秒数")]
        public short? TimeoutSeconds { get; set; }

        /// <summary>
        /// 租户ID
        /// </summary>
        [Comment("租户ID")]
        public Guid? TenantId { get; set; }

        /// <summary>
        /// 扩展数据（JSON格式）
        /// </summary>
        [Comment("扩展数据（JSON格式）")]
        public string ExtData { get; set; }

        #region 状态标志位操作方法

        /// <summary>
        /// 设置任务为等待中状态
        /// </summary>
        public void SetWaiting()
        {
            StatusFlags = (byte)((StatusFlags & ~0x1E) | (byte)TaskStatusFlags.Waiting); // 显式转换为byte
        }

        /// <summary>
        /// 设置任务为运行中状态
        /// </summary>
        public void SetRunning()
        {
            StatusFlags = (byte)((StatusFlags & ~0x1F) | (byte)TaskStatusFlags.Running); // 显式转换为byte
        }

        /// <summary>
        /// 设置任务为已完成状态
        /// </summary>
        public void SetCompleted()
        {
            StatusFlags = (byte)((StatusFlags & ~0x1B) | (byte)TaskStatusFlags.Completed); // 显式转换为byte
            Progress = 100; // 进度设为100%
        }

        /// <summary>
        /// 设置任务为失败状态
        /// </summary>
        public void SetFailed()
        {
            StatusFlags = (byte)((StatusFlags & ~0x17) | (byte)TaskStatusFlags.Failed); // 显式转换为byte
        }

        /// <summary>
        /// 设置任务为已取消状态
        /// </summary>
        public void SetCancelled()
        {
            StatusFlags = (byte)((StatusFlags & ~0x0F) | (byte)TaskStatusFlags.Cancelled); // 显式转换为byte
        }

        /// <summary>
        /// 设置任务为已暂停状态
        /// </summary>
        public void SetPaused()
        {
            StatusFlags = (byte)((StatusFlags & ~0x3F) | (byte)TaskStatusFlags.Paused); // 显式转换为byte
        }

        /// <summary>
        /// 获取任务当前状态
        /// </summary>
        /// <returns>任务状态</returns>
        public TaskStatusFlags GetStatus()
        {
            if ((StatusFlags & (byte)TaskStatusFlags.Waiting) != 0) return TaskStatusFlags.Waiting;
            if ((StatusFlags & (byte)TaskStatusFlags.Running) != 0) return TaskStatusFlags.Running;
            if ((StatusFlags & (byte)TaskStatusFlags.Completed) != 0) return TaskStatusFlags.Completed;
            if ((StatusFlags & (byte)TaskStatusFlags.Failed) != 0) return TaskStatusFlags.Failed;
            if ((StatusFlags & (byte)TaskStatusFlags.Cancelled) != 0) return TaskStatusFlags.Cancelled;
            if ((StatusFlags & (byte)TaskStatusFlags.Paused) != 0) return TaskStatusFlags.Paused;
            return TaskStatusFlags.Waiting; // 默认为等待状态
        }

        /// <summary>
        /// 检查任务是否处于指定状态
        /// </summary>
        /// <param name="status">要检查的状态</param>
        /// <returns>如果处于指定状态则返回true，否则返回false</returns>
        public bool IsInStatus(TaskStatusFlags status)
        {
            return (StatusFlags & (byte)status) != 0; // 显式转换为byte
        }

        #endregion

        #region 属性标志位操作方法

        /// <summary>
        /// 设置任务属性标志
        /// </summary>
        /// <param name="flag">要设置的标志</param>
        /// <param name="value">标志值</param>
        public void SetPropertyFlag(TaskPropertyFlags flag, bool value)
        {
            if (value)
                PropertyFlags |= (byte)flag; // 显式转换为byte
            else
                PropertyFlags &= (byte)~(byte)flag; // 显式转换为byte
        }

        /// <summary>
        /// 检查任务是否有指定属性标志
        /// </summary>
        /// <param name="flag">要检查的标志</param>
        /// <returns>如果有指定标志则返回true，否则返回false</returns>
        public bool HasPropertyFlag(TaskPropertyFlags flag)
        {
            return (PropertyFlags & (byte)flag) != 0; // 显式转换为byte
        }

        /// <summary>
        /// 设置任务是否可被取消
        /// </summary>
        /// <param name="value">是否可被取消</param>
        public void SetCancellable(bool value)
        {
            SetPropertyFlag(TaskPropertyFlags.Cancellable, value);
        }

        #endregion

        #region 重试信息操作方法

        /// <summary>
        /// 获取当前重试次数
        /// </summary>
        /// <returns>当前重试次数</returns>
        public int GetRetryCount()
        {
            return RetryInfo & 0x0F; // 低4位
        }

        /// <summary>
        /// 获取最大重试次数
        /// </summary>
        /// <returns>最大重试次数</returns>
        public int GetMaxRetries()
        {
            return (RetryInfo >> 4) & 0x0F; // 高4位
        }

        /// <summary>
        /// 设置当前重试次数
        /// </summary>
        /// <param name="count">重试次数(0-15)</param>
        public void SetRetryCount(int count)
        {
            if (count < 0) count = 0;
            if (count > 15) count = 15;
            RetryInfo = (byte)((RetryInfo & 0xF0) | count);
        }

        /// <summary>
        /// 设置最大重试次数
        /// </summary>
        /// <param name="maxRetries">最大重试次数(0-15)</param>
        public void SetMaxRetries(int maxRetries)
        {
            if (maxRetries < 0) maxRetries = 0;
            if (maxRetries > 15) maxRetries = 15;
            RetryInfo = (byte)((RetryInfo & 0x0F) | (maxRetries << 4));
        }

        /// <summary>
        /// 增加重试次数
        /// </summary>
        /// <returns>增加后的重试次数</returns>
        public int IncrementRetryCount()
        {
            int currentRetries = GetRetryCount();
            if (currentRetries < 15) // 确保不超过最大值
                SetRetryCount(currentRetries + 1);
            return GetRetryCount();
        }

        #endregion
    }

    /// <summary>
    /// 任务状态标志位枚举
    /// </summary>
    [Flags]
    public enum TaskStatusFlags : byte
    {
        /// <summary>
        /// 等待中
        /// </summary>
        Waiting = 0x01,

        /// <summary>
        /// 运行中
        /// </summary>
        Running = 0x02,

        /// <summary>
        /// 已完成
        /// </summary>
        Completed = 0x04,

        /// <summary>
        /// 失败
        /// </summary>
        Failed = 0x08,

        /// <summary>
        /// 已取消
        /// </summary>
        Cancelled = 0x10,

        /// <summary>
        /// 已暂停
        /// </summary>
        Paused = 0x80
    }

    /// <summary>
    /// 任务属性标志位枚举
    /// </summary>
    [Flags]
    public enum TaskPropertyFlags : byte
    {
        /// <summary>
        /// 可被取消
        /// </summary>
        Cancellable = 0x01,

        /// <summary>
        /// 可被暂停
        /// </summary>
        Pausable = 0x02,

        /// <summary>
        /// 重要任务
        /// </summary>
        Important = 0x04,

        /// <summary>
        /// 需要报告
        /// </summary>
        RequireReport = 0x08,

        /// <summary>
        /// 系统任务
        /// </summary>
        SystemTask = 0x10,

        /// <summary>
        /// 定期任务
        /// </summary>
        Recurring = 0x20
    }
}

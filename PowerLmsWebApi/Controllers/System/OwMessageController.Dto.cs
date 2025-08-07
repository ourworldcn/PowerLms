using PowerLms.Data;
using PowerLmsWebApi.Dto;
using System.ComponentModel.DataAnnotations;

namespace PowerLmsWebApi.Controllers
{
    /// <summary>
    /// 获取未读消息数量的返回值封装类。
    /// </summary>
    public class GetUnreadMessageCountReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 未读消息数量。
        /// </summary>
        public int UnreadCount { get; set; }
    }

    /// <summary>
    /// 获取所有消息列表的返回值封装类。
    /// </summary>
    public class GetAllOwMessageReturnDto : PagingReturnDtoBase<OwMessage>
    {
    }

    /// <summary>
    /// 发送消息的参数封装类。
    /// </summary>
    public class SendOwMessageParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 接收者用户ID列表。
        /// </summary>
        [Required]
        public List<Guid> ReceiverIds { get; set; } = new List<Guid>();

        /// <summary>
        /// 消息标题。
        /// </summary>
        [Required, MaxLength(64)]
        public string Title { get; set; }

        /// <summary>
        /// 消息内容。HTML格式。
        /// </summary>
        [Required]
        public string Content { get; set; }
    }

    /// <summary>
    /// 发送消息的返回值封装类。
    /// </summary>
    public class SendOwMessageReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 发送成功的消息ID列表。
        /// </summary>
        public List<Guid> MessageIds { get; set; } = new List<Guid>();
    }

    /// <summary>
    /// 标记消息为已读的参数封装类。
    /// </summary>
    public class MarkMessagesAsReadParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 要标记为已读的消息ID列表。
        /// 当 MarkAll 为 true 时，此列表可为空。
        /// </summary>
        public List<Guid> MessageIds { get; set; } = new List<Guid>();

        /// <summary>
        /// 是否标记所有未读消息为已读。
        /// 当此值为 true 时，将忽略 MessageIds 列表，标记当前用户的所有未读消息为已读。
        /// </summary>
        public bool MarkAll { get; set; } = false;
    }

    /// <summary>
    /// 标记消息为已读的返回值封装类。
    /// </summary>
    public class MarkMessagesAsReadReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 成功标记为已读的消息数量。
        /// </summary>
        public int MarkedCount { get; set; }
    }

    /// <summary>
    /// 批量删除消息的参数封装类。
    /// </summary>
    public class RemoveAllOwMessageParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 要删除的消息ID列表。
        /// </summary>
        [Required]
        public List<Guid> Ids { get; set; } = new List<Guid>();
    }

    /// <summary>
    /// 批量删除消息的返回值封装类。
    /// </summary>
    public class RemoveAllOwMessageReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 成功删除的消息数量。
        /// </summary>
        public int RemovedCount { get; set; }
    }
}
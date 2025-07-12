using PowerLms.Data;
using PowerLmsWebApi.Dto;
using System.ComponentModel.DataAnnotations;

namespace PowerLmsWebApi.Controllers
{
    /// <summary>
    /// ��ȡδ����Ϣ�����ķ���ֵ��װ�ࡣ
    /// </summary>
    public class GetUnreadMessageCountReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// δ����Ϣ������
        /// </summary>
        public int UnreadCount { get; set; }
    }

    /// <summary>
    /// ��ȡ������Ϣ�б�ķ���ֵ��װ�ࡣ
    /// </summary>
    public class GetAllOwMessageReturnDto : PagingReturnDtoBase<OwMessage>
    {
    }

    /// <summary>
    /// ������Ϣ�Ĳ�����װ�ࡣ
    /// </summary>
    public class SendOwMessageParamsDto : TokenDtoBase
    {
        /// <summary>
        /// �������û�ID�б�
        /// </summary>
        [Required]
        public List<Guid> ReceiverIds { get; set; } = new List<Guid>();

        /// <summary>
        /// ��Ϣ���⡣
        /// </summary>
        [Required, MaxLength(64)]
        public string Title { get; set; }

        /// <summary>
        /// ��Ϣ���ݡ�HTML��ʽ��
        /// </summary>
        [Required]
        public string Content { get; set; }
    }

    /// <summary>
    /// ������Ϣ�ķ���ֵ��װ�ࡣ
    /// </summary>
    public class SendOwMessageReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// ���ͳɹ�����ϢID�б�
        /// </summary>
        public List<Guid> MessageIds { get; set; } = new List<Guid>();
    }

    /// <summary>
    /// �����ϢΪ�Ѷ��Ĳ�����װ�ࡣ
    /// </summary>
    public class MarkMessagesAsReadParamsDto : TokenDtoBase
    {
        /// <summary>
        /// Ҫ���Ϊ�Ѷ�����ϢID�б�
        /// �� MarkAll Ϊ true ʱ�����б��Ϊ�ա�
        /// </summary>
        public List<Guid> MessageIds { get; set; } = new List<Guid>();

        /// <summary>
        /// �Ƿ�������δ����ϢΪ�Ѷ���
        /// ����ֵΪ true ʱ�������� MessageIds �б���ǵ�ǰ�û�������δ����ϢΪ�Ѷ���
        /// </summary>
        public bool MarkAll { get; set; } = false;
    }

    /// <summary>
    /// �����ϢΪ�Ѷ��ķ���ֵ��װ�ࡣ
    /// </summary>
    public class MarkMessagesAsReadReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// �ɹ����Ϊ�Ѷ�����Ϣ������
        /// </summary>
        public int MarkedCount { get; set; }
    }

    /// <summary>
    /// ����ɾ����Ϣ�Ĳ�����װ�ࡣ
    /// </summary>
    public class RemoveAllOwMessageParamsDto : TokenDtoBase
    {
        /// <summary>
        /// Ҫɾ������ϢID�б�
        /// </summary>
        [Required]
        public List<Guid> Ids { get; set; } = new List<Guid>();
    }

    /// <summary>
    /// ����ɾ����Ϣ�ķ���ֵ��װ�ࡣ
    /// </summary>
    public class RemoveAllOwMessageReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// �ɹ�ɾ������Ϣ������
        /// </summary>
        public int RemovedCount { get; set; }
    }
}
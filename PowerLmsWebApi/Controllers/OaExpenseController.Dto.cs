using PowerLms.Data;
using PowerLms.Data.OA;
using PowerLmsWebApi.Dto;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PowerLmsWebApi.Dto
{
    #region OA�������뵥����

    /// <summary>
    /// ��ȡ����OA�������뵥���ܵĲ�����װ�ࡣ
    /// </summary>
    public class GetAllOaExpenseRequisitionParamsDto : PagingParamsDtoBase
    {
        /// <summary>
        /// �����ı�����������ؿͻ�����ע�ȡ�
        /// </summary>
        public string SearchText { get; set; }

        /// <summary>
        /// ��ʼ���ڹ��ˡ�
        /// </summary>
        public DateTime? StartDate { get; set; }

        /// <summary>
        /// �������ڹ��ˡ�
        /// </summary>
        public DateTime? EndDate { get; set; }
    }

    /// <summary>
    /// ��ȡ����OA�������뵥���ܵķ���ֵ��װ�ࡣ
    /// </summary>
    public class GetAllOaExpenseRequisitionReturnDto : PagingReturnDtoBase<OaExpenseRequisition>
    {
    }

    /// <summary>
    /// ������OA�������뵥���ܲ�����װ�ࡣ
    /// </summary>
    public class AddOaExpenseRequisitionParamsDto : TokenDtoBase
    {
        /// <summary>
        /// ��OA�������뵥��Ϣ������Id�������κ�ֵ������ʱ��ָ����ֵ��
        /// </summary>
        [Required]
        public OaExpenseRequisition OaExpenseRequisition { get; set; }

        /// <summary>
        /// �Ƿ��Ϊ�Ǽǡ�true��ʾ����������˵Ǽǣ�false��ʾ�û��������롣
        /// </summary>
        public bool IsRegisterForOthers { get; set; }
    }

    /// <summary>
    /// ������OA�������뵥���ܷ���ֵ��װ�ࡣ
    /// </summary>
    public class AddOaExpenseRequisitionReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// ����ɹ���ӣ����ﷵ����OA�������뵥��Id��
        /// </summary>
        public Guid Id { get; set; }
    }

    /// <summary>
    /// �޸�OA�������뵥��Ϣ���ܲ�����װ�ࡣ
    /// </summary>
    public class ModifyOaExpenseRequisitionParamsDto : TokenDtoBase
    {
        /// <summary>
        /// OA�������뵥���ݡ�
        /// </summary>
        [Required]
        public OaExpenseRequisition OaExpenseRequisition { get; set; }
    }

    /// <summary>
    /// �޸�OA�������뵥��Ϣ���ܷ���ֵ��װ�ࡣ
    /// </summary>
    public class ModifyOaExpenseRequisitionReturnDto : ReturnDtoBase
    {
    }

    /// <summary>
    /// ɾ��OA�������뵥���ܵĲ�����װ�ࡣ
    /// </summary>
    public class RemoveOaExpenseRequisitionParamsDto : RemoveItemsParamsDtoBase
    {
    }

    /// <summary>
    /// ɾ��OA�������뵥���ܵķ���ֵ��װ�ࡣ
    /// </summary>
    public class RemoveOaExpenseRequisitionReturnDto : RemoveItemsReturnDtoBase
    {
    }

    #endregion

    #region OA�������뵥��ϸ

    /// <summary>
    /// ��ȡ����OA�������뵥��ϸ���ܵĲ�����װ�ࡣ
    /// </summary>
    public class GetAllOaExpenseRequisitionItemParamsDto : PagingParamsDtoBase
    {
        /// <summary>
        /// ���뵥Id�����ڹ���ָ�����뵥����ϸ��
        /// </summary>
        public Guid? RequisitionId { get; set; }
    }

    /// <summary>
    /// ��ȡ����OA�������뵥��ϸ���ܵķ���ֵ��װ�ࡣ
    /// </summary>
    public class GetAllOaExpenseRequisitionItemReturnDto : PagingReturnDtoBase<OaExpenseRequisitionItem>
    {
    }

    /// <summary>
    /// ������OA�������뵥��ϸ���ܲ�����װ�ࡣ
    /// </summary>
    public class AddOaExpenseRequisitionItemParamsDto : TokenDtoBase
    {
        /// <summary>
        /// ��OA�������뵥��ϸ��Ϣ������Id�������κ�ֵ������ʱ��ָ����ֵ��
        /// </summary>
        [Required]
        public OaExpenseRequisitionItem OaExpenseRequisitionItem { get; set; }
    }

    /// <summary>
    /// ������OA�������뵥��ϸ���ܷ���ֵ��װ�ࡣ
    /// </summary>
    public class AddOaExpenseRequisitionItemReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// ����ɹ���ӣ����ﷵ����OA�������뵥��ϸ��Id��
        /// </summary>
        public Guid Id { get; set; }
    }

    /// <summary>
    /// �޸�OA�������뵥��ϸ��Ϣ���ܲ�����װ�ࡣ
    /// </summary>
    public class ModifyOaExpenseRequisitionItemParamsDto : TokenDtoBase
    {
        /// <summary>
        /// OA�������뵥��ϸ���ݡ�
        /// </summary>
        [Required]
        public OaExpenseRequisitionItem OaExpenseRequisitionItem { get; set; }
    }

    /// <summary>
    /// �޸�OA�������뵥��ϸ��Ϣ���ܷ���ֵ��װ�ࡣ
    /// </summary>
    public class ModifyOaExpenseRequisitionItemReturnDto : ReturnDtoBase
    {
    }

    /// <summary>
    /// ɾ��OA�������뵥��ϸ���ܵĲ�����װ�ࡣ
    /// </summary>
    public class RemoveOaExpenseRequisitionItemParamsDto : RemoveItemsParamsDtoBase
    {
    }

    /// <summary>
    /// ɾ��OA�������뵥��ϸ���ܵķ���ֵ��װ�ࡣ
    /// </summary>
    public class RemoveOaExpenseRequisitionItemReturnDto : RemoveItemsReturnDtoBase
    {
    }

    #endregion

    #region ������DTO

    /// <summary>
    /// ���OA�������뵥����DTO��
    /// </summary>
    public class AuditOaExpenseRequisitionParamsDto : TokenDtoBase
    {
        /// <summary>
        /// ���뵥Id��
        /// </summary>
        [Required]
        public Guid RequisitionId { get; set; }

        /// <summary>
        /// ��˱�־��true���ͨ����falseȡ����ˡ�
        /// </summary>
        public bool IsAudit { get; set; }

        /// <summary>
        /// ���㷽ʽ���ֽ������ת�ˣ�ֻ�������ʱָ����
        /// </summary>
        public SettlementMethodType? SettlementMethod { get; set; }

        /// <summary>
        /// �����˻�Id�������㷽ʽ������ʱ��ѡ�񱾹�˾��Ϣ�е������˻�id��ֻ�������ʱָ����
        /// </summary>
        public Guid? BankAccountId { get; set; }
    }

    /// <summary>
    /// ���OA�������뵥����DTO��
    /// </summary>
    public class AuditOaExpenseRequisitionReturnDto : ReturnDtoBase
    {
    }

    #endregion

    #region ��չDTO

    /// <summary>
    /// OA�������뵥��ϸ��ϢDTO��
    /// �������뵥����ϸ���������Ϣ��
    /// </summary>
    public class OaExpenseRequisitionDetailDto
    {
        /// <summary>
        /// ���뵥����Ϣ��
        /// </summary>
        public OaExpenseRequisition Requisition { get; set; }

        /// <summary>
        /// ������ϸ���б�
        /// </summary>
        public List<OaExpenseRequisitionItem> Items { get; set; } = new List<OaExpenseRequisitionItem>();

        /// <summary>
        /// ��������Ϣ��
        /// </summary>
        public Account Applicant { get; set; }

        /// <summary>
        /// �Ǽ�����Ϣ��
        /// </summary>
        public Account Registrar { get; set; }
    }

    #endregion
}
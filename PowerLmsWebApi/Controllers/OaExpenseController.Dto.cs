using PowerLms.Data;
using PowerLms.Data.OA;
using PowerLmsWebApi.Dto;
using OW.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PowerLmsWebApi.Controllers
{
    #region OA�������뵥����

    /// <summary>
    /// ��ȡ����OA�������뵥���ܵĲ�����װ�ࡣ
    /// </summary>
    public class GetAllOaExpenseRequisitionParamsDto : PagingParamsDtoBase
    {
        /// <summary>
        /// �����ı���ƥ����ؿͻ��ͱ�ע�ȡ�
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
    public class AddOaExpenseRequisitionParamsDto : AddParamsDtoBase<OaExpenseRequisition>
    {
        /// <summary>
        /// �Ƿ�Ϊ���ǡ�true��ʾ�����˵Ǽǣ�false��ʾ�û��Լ����롣
        /// </summary>
        public bool IsRegisterForOthers { get; set; }
    }

    /// <summary>
    /// ������OA�������뵥���ܷ���ֵ��װ�ࡣ
    /// </summary>
    public class AddOaExpenseRequisitionReturnDto : AddReturnDtoBase
    {
    }

    /// <summary>
    /// �޸�OA�������뵥��Ϣ���ܲ�����װ�ࡣ
    /// </summary>
    public class ModifyOaExpenseRequisitionParamsDto : ModifyParamsDtoBase<OaExpenseRequisition>
    {
    }

    /// <summary>
    /// �޸�OA�������뵥��Ϣ���ܷ���ֵ��װ�ࡣ
    /// </summary>
    public class ModifyOaExpenseRequisitionReturnDto : ModifyReturnDtoBase
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
    public class RemoveOaExpenseRequisitionReturnDto : RemoveReturnDtoBase
    {
    }

    /// <summary>
    /// ���OA�������뵥���ܵĲ�����װ�ࡣ
    /// </summary>
    public class AuditOaExpenseRequisitionParamsDto : TokenDtoBase
    {
        /// <summary>
        /// ���뵥ID��
        /// </summary>
        [Required]
        public Guid RequisitionId { get; set; }

        /// <summary>
        /// �Ƿ����ͨ����true��ʾ���ͨ����false��ʾȡ����ˡ�
        /// </summary>
        public bool IsAudit { get; set; }
    }

    /// <summary>
    /// ���OA�������뵥���ܵķ���ֵ��װ�ࡣ
    /// </summary>
    public class AuditOaExpenseRequisitionReturnDto : ReturnDtoBase
    {
    }

    #endregion

    #region OA�������뵥��ϸ

    /// <summary>
    /// ��ȡ����OA�������뵥��ϸ���ܵķ���ֵ��װ�ࡣ
    /// </summary>
    public class GetAllOaExpenseRequisitionItemReturnDto : PagingReturnDtoBase<OaExpenseRequisitionItem>
    {
    }

    /// <summary>
    /// ������OA�������뵥��ϸ���ܲ�����װ�ࡣ
    /// </summary>
    public class AddOaExpenseRequisitionItemParamsDto : AddParamsDtoBase<OaExpenseRequisitionItem>
    {
    }

    /// <summary>
    /// ������OA�������뵥��ϸ���ܷ���ֵ��װ�ࡣ
    /// </summary>
    public class AddOaExpenseRequisitionItemReturnDto : AddReturnDtoBase
    {
    }

    /// <summary>
    /// �޸�OA�������뵥��ϸ��Ϣ���ܲ�����װ�ࡣ
    /// </summary>
    public class ModifyOaExpenseRequisitionItemParamsDto : ModifyParamsDtoBase<OaExpenseRequisitionItem>
    {
    }

    /// <summary>
    /// �޸�OA�������뵥��ϸ��Ϣ���ܷ���ֵ��װ�ࡣ
    /// </summary>
    public class ModifyOaExpenseRequisitionItemReturnDto : ModifyReturnDtoBase
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
    public class RemoveOaExpenseRequisitionItemReturnDto : RemoveReturnDtoBase
    {
    }

    #endregion

    #region ��ϸ��ϢDTO

    /// <summary>
    /// OA�������뵥��ϸ��ϢDTO�ࡣ
    /// �������뵥��������ϸ�ľۺ���Ϣ��
    /// </summary>
    public class OaExpenseRequisitionDetailDto
    {
        /// <summary>
        /// ���뵥������Ϣ��
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

    #region �����������DTO

    /// <summary>
    /// ��ȡ��ǰ�û���ص�OA�������뵥��������״̬�Ĳ�����װ�ࡣ
    /// </summary>
    public class GetAllOaExpenseRequisitionWithWfParamsDto : PagingParamsDtoBase
    {
    }

    /// <summary>
    /// ��ȡ��ǰ�û���ص�OA�������뵥��������״̬�ķ���ֵ��װ�ࡣ
    /// </summary>
    public class GetAllOaExpenseRequisitionWithWfReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// ���캯����
        /// </summary>
        public GetAllOaExpenseRequisitionWithWfReturnDto()
        {
            Result = new List<GetAllOaExpenseRequisitionWithWfItemDto>();
        }

        /// <summary>
        /// ���ص����뵥�͹�������Ϣ���ϡ�
        /// </summary>
        public List<GetAllOaExpenseRequisitionWithWfItemDto> Result { get; set; }

        /// <summary>
        /// ��������
        /// </summary>
        public int Total { get; set; }
    }

    /// <summary>
    /// OA�������뵥�͹�������Ϣ������
    /// </summary>
    public class GetAllOaExpenseRequisitionWithWfItemDto
    {
        /// <summary>
        /// ���뵥��Ϣ��
        /// </summary>
        public OaExpenseRequisition Requisition { get; set; }

        /// <summary>
        /// �����Ĺ�������Ϣ��
        /// </summary>
        public OwWfDto Wf { get; set; }
    }

    #endregion

    #region ƾ֤���������DTO

    /// <summary>
    /// ����ƾ֤�Ź��ܵĲ�����װ�ࡣ
    /// </summary>
    public class GenerateVoucherNumberParamsDto : TokenDtoBase
    {
        /// <summary>
        /// ���뵥ID��
        /// </summary>
        [Required]
        public Guid RequisitionId { get; set; }

        /// <summary>
        /// �����˺�ID��
        /// </summary>
        [Required]
        public Guid SettlementAccountId { get; set; }

        /// <summary>
        /// ����ʱ�䡣
        /// </summary>
        [Required]
        public DateTime SettlementDateTime { get; set; }
    }

    /// <summary>
    /// ����ƾ֤�Ź��ܵķ���ֵ��װ�ࡣ
    /// </summary>
    public class GenerateVoucherNumberReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// ���ɵ�ƾ֤�š�
        /// </summary>
        public string VoucherNumber { get; set; }

        /// <summary>
        /// ƾ֤�֡�
        /// </summary>
        public string VoucherCharacter { get; set; }

        /// <summary>
        /// �ڼ䣨�·ݣ���
        /// </summary>
        public int Period { get; set; }

        /// <summary>
        /// ��š�
        /// </summary>
        public int SequenceNumber { get; set; }

        /// <summary>
        /// �Ƿ�����غž��档
        /// </summary>
        public bool HasDuplicateWarning { get; set; }

        /// <summary>
        /// �غž�����Ϣ��
        /// </summary>
        public string DuplicateWarningMessage { get; set; }
    }

    #endregion
}
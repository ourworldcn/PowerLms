/*
 * 项目：PowerLms财务系统
 * 模块：实际收付记录控制器 - DTO定义
 * 文件说明：
 * - 功能1：定义实际收付记录CRUD操作的请求和响应DTO
 * - 功能2：支持软删除和恢复功能的DTO
 * 技术要点：
 * - 基于标准CRUD模式设计
 * - 继承基础DTO类以保持一致性
 * 作者：zc
 * 创建：2025-01
 */

using PowerLms.Data;
using PowerLmsWebApi.Dto;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PowerLmsWebApi.Controllers.Financial
{
    #region 实际收付记录相关

    /// <summary>
    /// 获取全部实际收付记录的返回值封装类。
    /// </summary>
    public class GetAllActualFinancialTransactionReturnDto : PagingReturnDtoBase<ActualFinancialTransaction>
    {
    }

    /// <summary>
    /// 添加实际收付记录的功能参数封装类。
    /// </summary>
    public class AddActualFinancialTransactionParamsDto : AddParamsDtoBase<ActualFinancialTransaction>
    {
    }

    /// <summary>
    /// 添加实际收付记录的功能返回值封装类。
    /// </summary>
    public class AddActualFinancialTransactionReturnDto : AddReturnDtoBase
    {
    }

    /// <summary>
    /// 修改实际收付记录的功能参数封装类。
    /// </summary>
    public class ModifyActualFinancialTransactionParamsDto : ModifyParamsDtoBase<ActualFinancialTransaction>
    {
    }

    /// <summary>
    /// 修改实际收付记录的功能返回值封装类。
    /// </summary>
    public class ModifyActualFinancialTransactionReturnDto : ModifyReturnDtoBase
    {
    }

    /// <summary>
    /// 删除实际收付记录的功能参数封装类。
    /// </summary>
    public class RemoveActualFinancialTransactionParamsDto : RemoveParamsDtoBase
    {
    }

    /// <summary>
    /// 删除实际收付记录的功能返回值封装类。
    /// </summary>
    public class RemoveActualFinancialTransactionReturnDto : RemoveReturnDtoBase
    {
    }

    /// <summary>
    /// 恢复指定被删除实际收付记录的功能参数封装类。
    /// </summary>
    public class RestoreActualFinancialTransactionParamsDto : RestoreParamsDtoBase
    {
    }

    /// <summary>
    /// 恢复指定被删除实际收付记录的功能返回值封装类。
    /// </summary>
    public class RestoreActualFinancialTransactionReturnDto : RestoreReturnDtoBase
    {
    }

    #endregion 实际收付记录相关
}
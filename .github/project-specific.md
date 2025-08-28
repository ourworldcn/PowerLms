# PowerLms 项目特定配置

<!-- PowerLms特有信息，避免与通用规范重复 -->

## 🏗️ PowerLms项目架构

### 关键组件
- **数据库上下文**：`PowerLmsUserDbContext`
- **多租户字段**：`OrgId`（严格隔离）
- **权限系统**：`AuthorizationManager`分级权限
- **基础控制器**：`PlControllerBase`
- **依赖注入**：`[OwAutoInjection(ServiceLifetime.Scoped)]`

### 业务权限编码
```
D0 - 空运出口  D1 - 空运进口  D2 - 海运出口  D3 - 海运进口
F.2 - 财务功能  F.2.4 - 费用管理  F.2.8 - 审核  F.2.9 - 账期管理
```

## 🔧 PowerLms特定规范

### Controller特有规范
- 继承`PlControllerBase`
- Token验证：`_AccountManager.GetOrLoadContextByToken()`
- 权限验证：`_AuthorizationManager.Demand()`
- 多租户过滤：查询必须包含`c.OrgId == context.User.OrgId`

### Manager特有规范
- 事务管理：`_DbContext.Database.BeginTransaction()`
- 审计日志：`_SqlAppLogger`记录重要操作
- 错误处理：`OwHelper.SetLastErrorAndMessage()`

### API特有规范
- DTO命名：`{Action}{Entity}ParamsDto` / `{Action}{Entity}ReturnDto`
- 分页参数：继承`PagingParamsDtoBase`
- 条件查询：`Dictionary<string, string> conditional`参数
- 返回格式：继承`ReturnDtoBase`

## 📊 PowerLms业务域

### 核心实体关系
```
PlJob(工作号) 1:1 PlEaDoc/PlIaDoc/PlEsDoc/PlIsDoc(业务单据)
PlJob(工作号) 1:N DocFee(费用)
DocFee(费用) N:1 DocBill(账单)
DocFee(费用) N:M DocFeeRequisition(申请单明细)
```

### 特殊业务规则
- **财务日期**：进口=到港日期，出口=开航日期
- **账期关闭**：批量关闭同期工作号，自动递增账期
- **申请单回退**：清空审批流，状态回退，释放费用锁定

## 🚨 PowerLms已知技术债务

### 当前问题
- **空运接口重复**：`PlJobController.EaDoc.cs`与`PlAirborneController`功能重复
- **费用过滤Bug**：`GetDocFeeRequisitionItem`中`fee_id`过滤失效
- **OA申请单**：缺少`CustomerId`字段关联客户资料
- **客户资料**：缺少`IsActive`状态管理

### AI特定行为
- 所有新接口必须添加权限验证和多租户检查
- 重要操作自动记录审计日志
- 自动跟踪业务单据状态变更

---
**版本**：v1.0 | **更新**：2025-01-27
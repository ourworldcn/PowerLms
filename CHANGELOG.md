# 变更日志

## [未发布] - 2026-01-27

### 📋 空运出口实体命名规范统一

#### 功能变更总览
**实体字段命名规范化**：主单和分单实体全面修正违反命名规范的字段名，统一使用`Kind`和`Category`后缀，提升代码可维护性

#### 业务变更（面向项目经理）
无业务功能变更，仅实体字段命名优化，数据库迁移后前端需同步更新字段名

#### API变更（面向前端）

**主单和分单字段名变更**：
- `CnrLType` → `CnrLinkKind`（发货人联系人类型）
- `CneLType` → `CneLinkKind`（收货人联系人类型）
- `NtLType` → `NtLinkKind`（通知货人联系人类型）
- `RateClass` → `RateCategory`（运价等级）
- `PkgsType` → `PkgsKind`（包装方式）
- `FreightClass` → `FreightCategory`（服务等级）

**影响范围**：
- ✅ 主单实体（EaMawb）
- ✅ 分单实体（EaHawb）
- ⚠️ 前端需同步修改JSON字段名

#### 架构调整（面向开发团队）

**命名规范（编程规范3.2节）**：
- ❌ **禁止Type后缀**：字段名不得以`Type`结尾
- ❌ **禁止Class后缀**：字段名不得以`Class`结尾
- ✅ **推荐后缀**：使用`Kind`、`Category`、`Group`等语义化后缀

**修改清单**：

| 旧字段名 | 新字段名 | 实体 | 说明 |
|---------|---------|------|------|
| CnrLType | CnrLinkKind | EaMawb/EaHawb | 发货人联系人类型 |
| CneLType | CneLinkKind | EaMawb/EaHawb | 收货人联系人类型 |
| NtLType | NtLinkKind | EaMawb/EaHawb | 通知货人联系人类型 |
| RateClass | RateCategory | EaMawb/EaHawb | 运价等级 |
| PkgsType | PkgsKind | EaMawb/EaHawb | 包装方式 |
| FreightClass | FreightCategory | EaMawb/EaHawb | 服务等级 |

**编译验证**：✅ 100%通过（.NET 6）

---

### 📋 空运出口分单CRUD控制器开发

#### 功能变更总览
**分单制作模块完成**：参照主单控制器架构，完成空运出口分单（HAWB）及其子表的完整CRUD控制器和DTO开发

#### 业务变更（面向项目经理）
空运出口分单制作模块上线，支持分单的创建、查询、修改、删除操作

#### API变更（面向前端）

**新增接口（分单主表）**：
- `GET /api/EaHawb/GetAllPlEaHawb` - 查询分单列表（权限：D0.16.2）
- `POST /api/EaHawb/AddPlEaHawb` - 创建分单（权限：D0.16.1）
- `PUT /api/EaHawb/ModifyPlEaHawb` - 修改分单（权限：D0.16.3）
- `DELETE /api/EaHawb/RemovePlEaHawb` - 删除分单（权限：D0.16.4）

**新增接口（分单其他费用）**：
- `GET /api/EaHawb/GetAllEaHawbOtherCharge` - 查询分单其他费用列表（权限：D0.16.2）
- `POST /api/EaHawb/AddEaHawbOtherCharge` - 创建分单其他费用（权限：D0.16.1）
- `PUT /api/EaHawb/ModifyEaHawbOtherCharge` - 修改分单其他费用（权限：D0.16.3）
- `DELETE /api/EaHawb/RemoveEaHawbOtherCharge` - 删除分单其他费用（权限：D0.16.4）

**新增接口（分单委托明细）**：
- `GET /api/EaHawb/GetAllEaHawbCubage` - 查询分单委托明细列表（权限：D0.16.2）
- `POST /api/EaHawb/AddEaHawbCubage` - 创建分单委托明细（权限：D0.16.1）
- `PUT /api/EaHawb/ModifyEaHawbCubage` - 修改分单委托明细（权限：D0.16.3）
- `DELETE /api/EaHawb/RemoveEaHawbCubage` - 删除分单委托明细（权限：D0.16.4）

#### 架构调整（面向开发团队）

**控制器与DTO**：
- 主控制器：`PowerLmsWebApi/Controllers/Business/AirFreight/EaHawbController.cs`
- 其他费用子表：`PowerLmsWebApi/Controllers/Business/AirFreight/EaHawbController.OtherCharge.cs`
- 委托明细子表：`PowerLmsWebApi/Controllers/Business/AirFreight/EaHawbController.Cubage.cs`
- DTO定义：`PowerLmsWebApi/Controllers/Business/AirFreight/EaHawbController.Dto.cs`
- 路由前缀：`/api/EaHawb`

**DbContext扩展**：
- `DbSet<EaHawb> EaHawbs`（空运出口分单表）
- `DbSet<EaHawbOtherCharge> EaHawbOtherCharges`（分单其他费用表）
- `DbSet<EaHawbCubage> EaHawbCubages`（分单委托明细表）

**权限配置**：
- D0.16.1（新建分单）
- D0.16.2（查看分单）
- D0.16.3（编辑分单）
- D0.16.4（删除分单）

---

## [历史记录] - 2026-01-26

### 📋 空运出口主单命名规范重构

#### 功能变更总览
移除空运出口主单相关实体的`Pl`前缀，采用更简洁的`Ea`（Export Air）前缀

#### 架构调整

**实体类重命名**：
- `PlEaMawb` → `EaMawb`（空运出口主单）
- `PlEaMawbOtherCharge` → `EaMawbOtherCharge`（主单其他费用）
- `PlEaCubage` → `EaCubage`（主单委托明细）
- `PlEaGoodsDetail` → `EaGoodsDetail`（主单品名明细）
- `PlEaContainer` → `EaContainer`（主单集装器）

**控制器重命名**：
- `PlEaMawbController` → `EaMawbController`
- 文件夹：`PowerLmsWebApi/Controllers/Business/AirFreight/`

**DbContext更新**：
- `PlEaMawbs` → `EaMawbs`
- `PlEaMawbOtherCharges` → `EaMawbOtherCharges`
- `PlEaCubages` → `EaCubages`
- `PlEaGoodsDetails` → `EaGoodsDetails`
- `PlEaContainers` → `EaContainers`

**权限配置**：
- 所有控制器方法已按照权限.md配置正确权限（D0.15.1~D0.15.4）


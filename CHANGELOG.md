# 📝 变更日志

---

## [未发布] - 2026-02-08

### 📊 工作摘要（一句话总结）

- **国家字典增强**：国家字典支持备注字段，可记录国家相关补充信息
- **空运进口舱单上线**：新增空运进口舱单功能，用于海关报关数据管理，支持主单和分单混合存储
- **智能关联优化**：舱单明细自动关联空运出口主单/分单，找不到对应单据时拒绝保存，确保数据一致性

### ✨ 业务变更（面向项目经理）

#### 基础数据管理
- **国家字典功能增强**：国家字典已支持备注字段，方便记录国家相关的额外信息

#### 空运进口业务
- **空运舱单功能上线（Manifest - 行业标准术语）**：新增空运舱单数据管理功能，用于海关报关流程
  - 支持舱单主表和明细数据的录入、修改、删除
  - 支持主单和分单混合存储（通过分单号字段区分）
  - 支持直单（无分单）场景
  - 主单号格式：11位纯数字（海关仓单科格式）
  - **命名规范**：采用国际货代行业标准术语`Manifest`（舱单）

### 🔧 API变更（面向前端）

#### 基础数据API
- **国家字典接口** (`/api/Admin/PlCountry/*`)
  - ✅ 已支持Remark字段的读写
  - 所有CRUD接口（GetAllPlCountry、AddPlCountry、ModifyPlCountry）均可操作备注字段
  - 技术实现：PlCountry实体继承自NamedSpecialDataDicBase基类，Remark字段在基类中定义
  - 无需数据库迁移（字段已存在于基类中）

#### 进口仓单API
- **新增接口** (`/api/IaManifest/*` - 空运进口舱单控制器，采用行业标准术语)
  - **命名规范**：
    - 控制器：`IaManifestController`（与主表实体`IaManifest`对应）
    - 主表实体：`IaManifest`（空运进口舱单，Ia=Import Air，Manifest为国际标准）
    - 子表实体：`IaManifestDetail`（空运进口舱单明细）
    - .NET命名规范：缩写`Ia`只首字母大写，避免三个连续大写字母
  - **业务归属**：明确标注为空运进口业务模块
  - `GET /api/IaManifest` - 查询空运进口舱单列表（支持分页和条件过滤）
  - `GET /api/IaManifest/detail?id={id}` - 查询单个空运进口舱单详情（含主表和子表）
  - `POST /api/IaManifest` - 创建空运进口舱单（支持同时创建主表和子表）
  - `PUT /api/IaManifest` - 修改空运进口舱单主表信息
  - `DELETE /api/IaManifest` - 删除空运进口舱单（需先删除明细）
  - `GET /api/IaManifest/details?parentId={id}&mawbNo={no}` - 查询空运进口舱单明细列表
  - `POST /api/IaManifest/detail` - 创建空运进口舱单明细
  - `PUT /api/IaManifest/detail` - 修改空运进口舱单明细
  - `DELETE /api/IaManifest/detail` - 删除空运进口舱单明细

- **数据模型**
- 主表实体：`IaManifest`（空运进口舱单主表，Ia=Import Air，Manifest为国际货代行业标准术语）
- 子表实体：`IaManifestDetail`（空运进口舱单明细）
- 主分单识别：子表中`HBLNO`字段为空表示主单行，不为空表示分单行
- 关联方式：通过`ParentId`外键关联（可为空，保持灵活性）
- .NET命名规范：缩写`Ia`遵循.NET规范，只首字母大写
  - **智能关联（分单优先策略）**：
    - 子表新增`MawbId`字段（Guid?），采用**分单优先策略**关联
    - **有分单号**：`MawbId`指向分单ID（EaHawb.Id）
    - **无分单号**：`MawbId`指向主单ID（EaMawb.Id）
    - **设计优势**：
      - ✅ 避免重复查询：通过分单可直接获取主单信息（EaHawb.MawbNo）
      - ✅ 提高查询效率：需要分单信息时直接通过MawbId查询
      - ✅ 数据关联清晰：通过HBLNO是否为空判断关联的是主单还是分单
      - ✅ 扩展性好：分单包含主单号，可以追溯到主单
    - **验证机制**：
      - ❌ 找不到对应主单/分单时返回BadRequest，拒绝保存
      - ✅ 确保数据一致性：只有已存在的主单/分单才能被引用
    - 保存时自动标准化主单号/分单号（去空格，主单号保留横杠为999-12345678格式）
    - 主单号/分单号原样保存（海关格式），内部通过GUID关联

### 📋 功能变更总览

```
基础数据管理
  └─ 国家字典：新增备注字段支持

空运进口业务
└─ 空运进口舱单（IaManifest，Ia=Import Air）：新增完整CRUD功能
    ├─ 舱单主表管理（IaManifest）
    ├─ 舱单明细管理（IaManifestDetail）
    ├─ 主分单混合存储
    └─ 采用国际货代行业标准术语，遵循.NET命名规范
```

---

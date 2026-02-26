# 📝 变更日志

## [未发布]-2026-02-23

### 📋 一句话总结
新增海运出口提单管理功能，支持主提单(船东提单)和分提单(货代提单)的完整CRUD操作。

### 🎯 面向项目经理
本次更新新增了海运出口业务中最核心的提单管理模块。支持录入、查询、修改船公司签发的主提单(MBL)和货代签发的分提单(HBL)，一个工作号可以对应一个主提单和多个分提单，满足海运业务的实际需求。

### 💻 面向前端开发
**新增API接口（共8个）：**

**主提单(EsMbl)：**
- `GET /api/EsMbl/GetAllEsMbl` - 分页查询主提单列表
- `POST /api/EsMbl/AddEsMbl` - 新增主提单
- `PUT /api/EsMbl/ModifyEsMbl` - 修改主提单
- `DELETE /api/EsMbl/RemoveEsMbl` - 删除主提单

**分提单(EsHbl)：**
- `GET /api/EsHbl/GetAllEsHbl` - 分页查询分提单列表
- `POST /api/EsHbl/AddEsHbl` - 新增分提单
- `PUT /api/EsHbl/ModifyEsHbl` - 修改分提单
- `DELETE /api/EsHbl/RemoveEsHbl` - 删除分提单

**注意事项：**
- 修改操作的请求参数从单个实体改为`Items`数组（支持批量修改，但当前建议每次只传一个）
- 新增操作的请求参数使用`Item`属性传递实体数据
- 权限码统一使用：D2.1.1.2(新增)、D2.1.1.3(修改)、D2.1.1.4(删除)

---

### 业务变更（面向项目经理）

#### 新增功能
- **海运出口主提单管理(船东提单)**：支持海运业务中船公司签发的主提单管理，包含完整的提单信息字段
- **海运出口分提单管理(货代提单)**：支持货运代理签发的分提单管理，字段结构与主单一致，支持一个工作号下多个分单

### 技术变更（面向开发人员）

#### 新增实体类
1. **EsMbl** (PowerLmsData\主营业务\海运出口\EsMbl.cs)
   - 海运出口主提单(船东提单)实体
   - 命名规范：采用Es前缀(Export Seaborne)，与空运提单命名风格一致(如EaMawb)
   - 关联关系：一个工作号仅关联一个主提单
   - 核心字段：
     - 提单编号、付款方式、运输条款、前程运输
     - 港口信息(收货地、装船港、中转港、目的港及其描述)
     - 船公司、船名、航次
     - 费用描述、装船日期、开船日期、到港日期
     - 发货人/收货人/通知人信息(名称+抬头)
     - 第二通知人抬头
     - 箱号、箱量、品名、唛头
     - 件数、包装类型、毛重、体积
     - 总计字段(箱量英文合计、件数英文合计)
     - 签发信息(正本张数、签发人、签发时间、签发地点)
     - 放货方式、备注、提单附页
   - 字段特性：大部分字段为字符串且可为空，DateTime类型字段均可为空
   - 数值字段：件数(int?)、正本张数(int?)、毛重(decimal?)、体积(decimal?)

2. **EsHbl** (PowerLmsData\主营业务\海运出口\EsHbl.cs)
- 海运出口分提单(货代提单)实体
- 命名规范：采用Es前缀，与主提单命名风格一致
   - 关联关系：一个工作号下可有多个分单，分单与主单无直接外键关联
   - 字段结构：与主单完全一致，额外增加预付运费(PPD_AMT)和到付运费(CCT_AMT)字段
   - 提单编号：由货代生成，可通过"其他编码规则"模块自动生成

#### 数据库变更
- 在PowerLmsUserDbContext中注册两个新DbSet：
  - `DbSet<EsMbl> EsMbls`
  - `DbSet<EsHbl> EsHbls`
- 两个实体均在JobId字段上建立索引

#### 新增控制器和DTO
1. **EsMblController** (PowerLmsWebApi\Controllers\Business\SeaFreight\EsMblController.cs)
   - 海运出口主提单控制器
   - 实现完整CRUD操作(GetAllEsMbl、AddEsMbl、ModifyEsMbl、RemoveEsMbl)
   - 权限码：D2.1.1.2(新增)、D2.1.1.3(修改)、D2.1.1.4(删除)
   - 日志记录：创建、修改、删除操作均记录日志

2. **EsMblController.Dto** (PowerLmsWebApi\Controllers\Business\SeaFreight\EsMblController.Dto.cs)
   - 主提单DTO定义
   - DTO基类规范：
     - AddEsMblParamsDto继承自AddParamsDtoBase<EsMbl>，使用Item属性
     - AddEsMblReturnDto继承自AddReturnDtoBase，自动包含Id属性
     - ModifyEsMblParamsDto继承自ModifyParamsDtoBase<EsMbl>，使用Items属性
     - ModifyEsMblReturnDto继承自ModifyReturnDtoBase
   - 包含：GetAllEsMblReturnDto、AddEsMblParamsDto/ReturnDto、ModifyEsMblParamsDto/ReturnDto、RemoveEsMblParamsDto/ReturnDto

3. **EsHblController** (PowerLmsWebApi\Controllers\Business\SeaFreight\EsHblController.cs)
   - 海运出口分提单控制器
   - 实现完整CRUD操作(GetAllEsHbl、AddEsHbl、ModifyEsHbl、RemoveEsHbl)
   - 权限码：D2.1.1.2(新增)、D2.1.1.3(修改)、D2.1.1.4(删除)
   - 日志记录：创建、修改、删除操作均记录日志

4. **EsHblController.Dto** (PowerLmsWebApi\Controllers\Business\SeaFreight\EsHblController.Dto.cs)
   - 分提单DTO定义
   - DTO基类规范：与主提单DTO相同，从正确的泛型基类派生
   - 包含：GetAllEsHblReturnDto、AddEsHblParamsDto/ReturnDto、ModifyEsHblParamsDto/ReturnDto、RemoveEsHblParamsDto/ReturnDto

#### DTO基类修复说明
- **问题**：原始实现中Add和Modify参数DTO直接继承TokenDtoBase，没有使用标准的泛型基类
- **修复**：
  - Add参数DTO改为继承AddParamsDtoBase<T>，删除自定义实体属性，使用基类的Item属性
  - Modify参数DTO改为继承ModifyParamsDtoBase<T>，删除自定义实体属性，使用基类的Items属性
  - Add返回DTO改为继承AddReturnDtoBase，删除重复的Id属性定义
  - Modify返回DTO改为继承ModifyReturnDtoBase
- **优势**：符合框架规范，支持批量修改，便于将来迁移到GenericEfController

#### 待办事项
- 需要手工执行数据库迁移(Add-Migration和Update-Database)
- 需要实现从工作号订舱信息预填充箱型箱量、件/重/体数据的逻辑

### 影响范围
- **新增文件**：
  - PowerLmsData\主营业务\海运出口\EsMbl.cs
  - PowerLmsData\主营业务\海运出口\EsHbl.cs
  - PowerLmsWebApi\Controllers\Business\SeaFreight\EsMblController.cs
  - PowerLmsWebApi\Controllers\Business\SeaFreight\EsMblController.Dto.cs
  - PowerLmsWebApi\Controllers\Business\SeaFreight\EsHblController.cs
  - PowerLmsWebApi\Controllers\Business\SeaFreight\EsHblController.Dto.cs
- **修改文件**：
  - PowerLmsData\PowerLmsUserDbContext.cs (新增两个DbSet注册)
- **编译状态**：✅ 成功编译
- **数据库迁移**：⚠️ 需手工执行(按照开发规范，AI不生成迁移文件)
- **命名变更说明**：实体和控制器采用Es前缀(Export Seaborne)，与空运提单命名风格一致


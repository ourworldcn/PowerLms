# PowerLms 项目特定配置

<!-- PowerLms物流管理系统的项目特定配置 -->

## 1. 🏗️ 项目架构信息

### 1.1 解决方案结构
```
PowerLms解决方案架构
├── PowerLmsWebApi/     # 控制器层
├── PowerLmsServer/     # 业务逻辑层
├── PowerLmsData/       # 数据访问层
└── 基础设施 (../Bak/)  # OwDbBase + OwBaseCore核心组件
```

### 1.2 基础设施组件映射
| 功能领域 | 使用组件 | 说明 |
|---------|---------|------|
| 文件管理 | OwFileService | - |
| 工作流引擎 | OwWfManager | - |
| 权限管理 | AuthorizationManager | - |
| 组织管理 | OrgManager | - |
| 数据字典 | DataDicManager | - |
| Excel处理 | OwDataUnit + OwNpoiUnit | 禁用废弃的NpoiManager |

---

## 2. 🔒 权限体系配置

### 2.1 权限查找原则
- **叶子权限优先**：只有权限.md中没有子权限的叶子节点才有实际意义
- **权限匹配策略**：搜索权限文件，如果有叶子权限符合要求就使用
- **无权限时处理**：如果没有找到合适的权限项，则暂时忽略权限问题

### 2.2 权限节点清单

**财务管理权限**：
- `F.3.1` - 新建结算
- `F.3.2` - 修改结算  
- `F.3.3` - 撤销(删除)结算
- `F.3.4` - 结算单确认
- `F.3.5` - 结算单取消确认
- `F.6` - 财务接口（适用于导出金蝶功能）

**基础数据管理权限**：
- `B.0` - 数据字典（适用于导入导出功能)
- `B.1` - 本公司信息
- `B.3` - 币种
- `B.4` - 汇率

**OA办公自动化权限**：
- `OA.1` - 日常费用申请
- `OA.1.1` - 日常费用结算确认
- `OA.1.2` - 日常费用拆分结算
- `OA.1.3` - 日常费用撤销

**审批撤销权限**：
- `E.2` - 审批撤销（主营业务费用申请单回退）

---

## 3. 📚 项目索引文档配置

### 3.1 索引文档位置
```
.github/indexes/
├── OwBaseCore.md          # 核心基础设施（DDD、缓存、并发）
├── OwDbBase.md            # 数据库基础设施（OwFileService、EF增强）
├── OwExtensions.md        # Excel处理（NPOI扩展）
├── PowerLmsServer.md      # 业务层（Manager）
├── PowerLmsData.md        # 数据层（实体）
└── PowerLmsWebApi.md      # API层（控制器）
```

### 3.2 索引快速查找表
| 用户提到的关键词 | 优先读取的索引 |
|----------------|---------------|
| OwFileService, 文件上传/下载 | OwDbBase.md |
| Excel导入/导出, NPOI | OwExtensions.md |
| OwEventBus, DDD, 缓存 | OwBaseCore.md |
| Manager, 业务逻辑 | PowerLmsServer.md |
| 实体, 数据模型 | PowerLmsData.md |
| Controller, API | PowerLmsWebApi.md |
| PooledList, SingletonLocker | OwBaseCore.md |
| OwBatchDbWriter, EfHelper | OwDbBase.md |

---

## 4. 🔧 项目特定技术约束

### 4.1 实体字段特殊配置
- **已结算金额字段**：申请单明细直接使用`TotalSettledAmount`字段，不再动态计算
- **前端回写责任**：已结算金额的维护责任转移到前端和结算单保存逻辑

### 4.2 Excel处理特定规范
- **推荐方案**：使用`OwDataUnit.BulkInsertFromExcelWithStringList<T>`进行高性能批量操作
- **Sheet命名**：统一使用实体类型名称（如`PlCountry`），不使用数据库表名（如`pl_Countries`）

### 4.3 权限验证特殊逻辑
- **导入导出权限**：优先检查具体的导入导出权限，再检查通用数据字典权限`B.0`
- **财务导出权限**：收付款结算单导出使用`F.6`财务接口权限

---

**最后更新：** 2025-01-31  
**适用版本：** PowerLms v1.0+  
**维护者：** 开发团队
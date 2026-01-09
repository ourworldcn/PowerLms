# PowerLms 变更日志

## [未发布] - 2025-01-XX

### 业务变更（面向项目经理）

#### 数据导入导出功能修复
* 修复了港口等字典表导入导出时可空枚举字段数据丢失的问题，确保导出导入后数据完整性

#### 基础库架构优化
* 优化了类型转换基础库的架构设计，移除了业务逻辑，提升了代码可复用性和可维护性

### API变更（面向前端）

#### 数据导入导出接口修复

影响的API：
* POST /api/ImportExport/ImportMultipleTables 批量导入表数据
* GET /api/ImportExport/ExportMultipleTables 批量导出表数据
* POST /api/ImportExport/ImportSimpleDictionary 导入简单字典
* GET /api/ImportExport/ExportSimpleDictionary 导出简单字典

修复内容：
* 修复了可空枚举类型字段（如PlPort.PortType港口类型）导出导入后数据丢失的问题
* 枚举值现在正确导出为数字（如1=空运，2=海运），导入时支持数字和文本两种格式

影响范围：
* 前端无需修改代码，导入导出行为更加稳定可靠
* 历史已导出的Excel文件可能需要人工补全丢失的枚举字段数据

#### 实体复制接口行为保持
* EntityManager.Copy 方法继续支持字符串 "null" 表示清空属性值（项目约定）
* 前端传递 `{ "PropertyName": "null" }` 仍然可以正确清空对应属性

### 技术细节（面向开发团队）

#### 架构优化说明
* **OwConvert.TryChangeType**：移除了字符串 "null" 的特殊处理，回归基础类型转换职责
* **OwNpoiExtensions.ConvertFromString**：Excel导入场景专用，保留字符串 "null" 识别
* **EntityManager.Copy**：API参数场景专用，保留字符串 "null" 识别
* **设计原则**：业务逻辑上移到应用层，基础库保持纯净，提升跨项目复用能力



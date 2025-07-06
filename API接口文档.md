# PowerLms API 接口文档

## API 概览

PowerLms 提供完整的 RESTful API 接口，支持前端应用和第三方系统集成。所有 API 接口都遵循 RESTful 设计原则，使用 JSON 格式进行数据交换。

## 认证方式

### Token 认证
所有 API 请求都需要在请求头中包含有效的认证令牌：

```http
Authorization: Bearer <your-token>
```

### 获取 Token
```http
POST /api/Account/Login
Content-Type: application/json

{
  "userName": "your-username",
  "password": "your-password"
}
```

## 核心 API 控制器

### 1. 业务管理 API (PlJobController)

#### 获取业务列表
```http
GET /api/PlJob/GetPlJobs
```

**查询参数:**
- `startIndex`: 起始索引
- `count`: 返回数量
- `orderFieldName`: 排序字段
- `isDesc`: 是否降序
- `conditional`: 查询条件（键值对）

**响应示例:**
```json
{
  "result": [
    {
      "id": "guid",
      "jobNo": "JOB001",
      "customId": "customer-guid",
      "mblNo": "MBL123456",
      "hblNoString": "HBL001/HBL002",
      "loadingCode": "SHA",
      "destinationCode": "LAX",
      "etd": "2024-01-15T10:00:00Z",
      "eta": "2024-01-25T15:00:00Z",
      "jobState": 2,
      "createDateTime": "2024-01-01T08:00:00Z"
    }
  ],
  "hasError": false,
  "errorCode": 0,
  "debugMessage": null
}
```

#### 创建业务
```http
POST /api/PlJob/AddPlJob
Content-Type: application/json

{
  "jobNo": "JOB001",
  "customId": "customer-guid",
  "jobTypeId": "jobtype-guid",
  "mblNo": "MBL123456",
  "loadingCode": "SHA",
  "destinationCode": "LAX",
  "etd": "2024-01-15T10:00:00Z",
  "eta": "2024-01-25T15:00:00Z",
  "goodsName": "General Cargo",
  "consignor": "Shipper Name",
  "consignee": "Consignee Name"
}
```

#### 修改业务
```http
PUT /api/PlJob/ModifyPlJob
Content-Type: application/json

{
  "id": "job-guid",
  "jobNo": "JOB001",
  "customId": "customer-guid",
  "mblNo": "MBL123456",
  "jobState": 4
}
```

#### 删除业务
```http
DELETE /api/PlJob/RemovePlJob
Content-Type: application/json

{
  "id": "job-guid"
}
```

#### 复制业务
```http
POST /api/PlJob/CopyJob
Content-Type: application/json

{
  "sourceJobId": "source-job-guid",
  "newValues": {
    "jobNo": "NEW_JOB001",
    "etd": "2024-02-01T10:00:00Z"
  },
  "ignorePropertyNames": ["id", "createDateTime"]
}
```

### 2. 客户管理 API (CustomerController)

#### 获取客户列表
```http
GET /api/Customer/GetPlCustomers
```

#### 创建客户
```http
POST /api/Customer/AddPlCustomer
Content-Type: application/json

{
  "shortName": "CUST001",
  "fullName": "Customer Full Name",
  "englishName": "Customer English Name",
  "customerType": 1,
  "creditLimit": 100000.00,
  "currency": "USD",
  "paymentTerms": "NET 30",
  "contacts": [
    {
      "name": "Contact Name",
      "phone": "+86-138-0000-0000",
      "email": "contact@customer.com",
      "position": "Manager"
    }
  ]
}
```

### 3. 财务管理 API (FinancialController)

#### 获取费用列表
```http
GET /api/Financial/GetDocFees
```

#### 创建费用
```http
POST /api/Financial/AddDocFee
Content-Type: application/json

{
  "jobId": "job-guid",
  "feeTypeId": "feetype-guid",
  "isReceivable": true,
  "amount": 1000.00,
  "currency": "USD",
  "exchangeRate": 6.8,
  "customerId": "customer-guid",
  "description": "Ocean Freight"
}
```

#### 获取费用申请单
```http
GET /api/Financial/GetDocFeeRequisitions
```

#### 创建费用申请单
```http
POST /api/Financial/AddDocFeeRequisition
Content-Type: application/json

{
  "title": "Monthly Fee Requisition",
  "applicantId": "user-guid",
  "totalAmount": 5000.00,
  "currency": "USD",
  "items": [
    {
      "feeId": "fee-guid",
      "amount": 1000.00,
      "description": "Ocean Freight"
    }
  ]
}
```

### 4. 组织管理 API (OrganizationController)

#### 获取组织列表
```http
GET /api/Organization/GetPlOrganizations
```

#### 创建组织
```http
POST /api/Organization/AddPlOrganization
Content-Type: application/json

{
  "shortName": "ORG001",
  "fullName": "Organization Full Name",
  "parentId": "parent-org-guid",
  "organizationType": 1,
  "baseCurrency": "USD",
  "address": "Organization Address",
  "phone": "+86-21-1234-5678",
  "email": "info@organization.com"
}
```

### 5. 用户管理 API (AccountController)

#### 用户登录
```http
POST /api/Account/Login
Content-Type: application/json

{
  "userName": "username",
  "password": "password"
}
```

#### 获取用户信息
```http
GET /api/Account/GetUserInfo
```

#### 修改密码
```http
POST /api/Account/ChangePassword
Content-Type: application/json

{
  "oldPassword": "old-password",
  "newPassword": "new-password"
}
```

### 6. 税务发票 API (TaxController)

#### 获取发票列表
```http
GET /api/Tax/GetTaxInvoices
```

#### 开具发票
```http
POST /api/Tax/CreateInvoice
Content-Type: application/json

{
  "customerId": "customer-guid",
  "invoiceType": 1,
  "totalAmount": 1000.00,
  "taxRate": 0.13,
  "items": [
    {
      "name": "Service Name",
      "quantity": 1,
      "unitPrice": 1000.00,
      "taxRate": 0.13
    }
  ]
}
```

#### 查询发票状态
```http
GET /api/Tax/GetInvoiceStatus/{invoiceId}
```

### 7. 文档管理 API (FileController)

#### 上传文件
```http
POST /api/File/Upload
Content-Type: multipart/form-data

form-data:
- file: (binary)
- businessId: "business-guid"
- category: "document-category"
```

#### 下载文件
```http
GET /api/File/Download/{fileId}
```

#### 获取文件列表
```http
GET /api/File/GetFiles
```

## 通用响应格式

所有 API 响应都遵循统一的格式：

```json
{
  "result": <返回数据>,
  "hasError": false,
  "errorCode": 0,
  "debugMessage": null,
  "additionalData": null
}
```

### 错误响应
```json
{
  "result": null,
  "hasError": true,
  "errorCode": 1001,
  "debugMessage": "详细错误信息",
  "additionalData": null
}
```

## 常见错误代码

| 错误代码 | 描述 | 解决方案 |
|---------|------|----------|
| 400 | 请求参数错误 | 检查请求参数格式 |
| 401 | 未授权访问 | 检查认证令牌 |
| 403 | 权限不足 | 联系管理员分配权限 |
| 404 | 资源不存在 | 检查资源ID是否正确 |
| 500 | 服务器内部错误 | 联系技术支持 |

## 数据类型说明

### 日期时间格式
所有日期时间字段都使用 ISO 8601 格式：
```
2024-01-15T10:30:00Z
```

### 金额格式
所有金额字段都使用 decimal 类型，保留2位小数：
```json
{
  "amount": 1234.56
}
```

### GUID 格式
所有ID字段都使用标准 GUID 格式：
```
550e8400-e29b-41d4-a716-446655440000
```

## 分页查询

大部分列表查询都支持分页参数：

```http
GET /api/Controller/GetList?startIndex=0&count=20&orderFieldName=createDateTime&isDesc=true
```

**分页参数说明:**
- `startIndex`: 起始索引（从0开始）
- `count`: 返回数量
- `orderFieldName`: 排序字段名
- `isDesc`: 是否降序排列

## 条件查询

支持灵活的条件查询，使用 `conditional` 参数：

```http
GET /api/Controller/GetList?conditional[jobNo]=JOB001&conditional[customId]=customer-guid
```

**条件格式:**
- 精确匹配：`field=value`
- 范围查询：`field=startValue,endValue`
- 模糊查询：`field=*keyword*`
- 空值查询：`field=null`

## SDK 示例

### C# 客户端示例
```csharp
using System.Net.Http;
using System.Text.Json;

public class PowerLmsClient
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    
    public PowerLmsClient(string baseUrl, string token)
    {
        _baseUrl = baseUrl;
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
    }
    
    public async Task<List<PlJob>> GetJobsAsync()
    {
        var response = await _httpClient.GetAsync($"{_baseUrl}/api/PlJob/GetPlJobs");
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponse<List<PlJob>>>(content);
        return result.Result;
    }
}
```

### JavaScript 客户端示例
```javascript
class PowerLmsClient {
    constructor(baseUrl, token) {
        this.baseUrl = baseUrl;
        this.token = token;
    }
    
    async getJobs() {
        const response = await fetch(`${this.baseUrl}/api/PlJob/GetPlJobs`, {
            headers: {
                'Authorization': `Bearer ${this.token}`,
                'Content-Type': 'application/json'
            }
        });
        
        const data = await response.json();
        return data.result;
    }
    
    async createJob(jobData) {
        const response = await fetch(`${this.baseUrl}/api/PlJob/AddPlJob`, {
            method: 'POST',
            headers: {
                'Authorization': `Bearer ${this.token}`,
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(jobData)
        });
        
        const data = await response.json();
        return data.result;
    }
}
```

## 集成最佳实践

1. **错误处理**: 总是检查响应中的 `hasError` 字段
2. **令牌管理**: 实施令牌刷新机制
3. **并发控制**: 使用乐观锁避免数据冲突
4. **数据验证**: 在客户端也进行数据验证
5. **日志记录**: 记录所有API调用以便调试

---

*更多详细的API文档请参考在线Swagger文档：`{base-url}/swagger`*
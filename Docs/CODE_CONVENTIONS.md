# PowerLms ��������ĵ�

## �汾��Ϣ
- �������ڣ�2024��1��
- ��Ŀ�汾��.NET 6
- �����£�����ʵ�ʿ������̲����Ż�

---

## 1. ��Ŀ�ṹ��ܹ�

### 1.1 ��Ŀ�ֲ�
- **PowerLmsData**: ���ݲ㣬����ʵ���ࡢö�١�����ģ��
- **PowerLmsServer**: ����㣬����ҵ���߼�����������
- **PowerLmsWebApi**: API�㣬������������DTO��

### 1.2 �ļ���֯
- ��ҵ��ģ����飺����OA���������ݵ�
- �ֲ��ࣨPartial Class�����ڴ��Ϳ������Ĺ��ܷ���
- DTO��ͳһ���ڶ�Ӧ�������� `.Dto.cs` �ļ���

---

## 2. ʵ������ƹ淶

### 2.1 �����̳�
```csharp
// ��׼ʵ��̳� GuidKeyObjectBase
public class EntityName : GuidKeyObjectBase, ISpecificOrg, ICreatorInfo
```

### 2.2 �ӿ�ʵ��
- `ISpecificOrg`: ���⻧֧�֣����� `OrgId` �ֶ�
- `ICreatorInfo`: ������Ϣ������ `CreateBy` �� `CreateDateTime`

### 2.3 �ֶ�ע�͹淶
```csharp
/// <summary>
/// �ֶε�ҵ���塣��ϸ��ҵ�����˵����
/// </summary>
[Comment("���ݿ�ע��")]
[MaxLength(64)] // �ַ����ֶα���ָ������
[Required] // �����ֶ�
public string FieldName { get; set; }
```

### 2.4 �����ֶι淶
- **OrgId**: ����Id������ʱȷ���������޸�
- **CreateBy**: ������Id����ͬ�ڵǼ���Id
- **CreateDateTime**: Ĭ��ʹ�� `OwHelper.WorldNow`
- **�����ֶ�**: Ĭ��ֵ "CNY"������4��ʹ�� `Unicode(false)`
- **�����ֶ�**: Ĭ��ֵ 1.0m������ `[Precision(18, 6)]`
- **����ֶ�**: ���� `[Precision(18, 2)]`

---

## 3. ��������ƹ淶

### 3.1 �������ṹ
```csharp
[ApiController]
[Route("api/[controller]/[action]")]
public partial class EntityController : ControllerBase
{
    private readonly PowerLmsUserDbContext _DbContext;
    private readonly IServiceProvider _ServiceProvider;
    private readonly AccountManager _AccountManager;
    private readonly ILogger<EntityController> _Logger;
    private readonly EntityManager _EntityManager;
}
```

### 3.2 ��׼CRUD��������
- `GetAll{Entity}`: ��ȡ�б�֧�ַ�ҳ��������ѯ��
- `Add{Entity}`: ����
- `Modify{Entity}`: �޸�
- `Remove{Entity}`: ɾ��
- `Audit{Entity}`: ��˲���

### 3.3 Ȩ����֤ģʽ
```csharp
if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context)
    return Unauthorized();
```

### 3.4 ������ѯ�淶
```csharp
// ȷ�������ֵ䲻���ִ�Сд
var normalizedConditional = conditional != null ?
    new Dictionary<string, string>(conditional, StringComparer.OrdinalIgnoreCase) :
    null;

// Ӧ��ͨ��������ѯ
var coll = EfHelper.GenerateWhereAnd(dbSet, normalizedConditional);
```

---

## 4. DTO��ƹ淶

### 4.1 DTO�����淶
- ����DTO: `{Action}{Entity}ParamsDto`
- ����DTO: `{Action}{Entity}ReturnDto`
- �̳л���: `TokenDtoBase`, `ReturnDtoBase`, `PagingParamsDtoBase`

### 4.2 ��׼DTO�ṹ
```csharp
/// <summary>
/// ���������Ĳ�����װ�ࡣ
/// </summary>
public class ActionEntityParamsDto : TokenDtoBase
{
    /// <summary>
    /// ʵ�����ݡ�����Id�������κ�ֵ������ʱ��ָ����ֵ��
    /// </summary>
    [Required]
    public Entity Entity { get; set; }
}

/// <summary>
/// ���������ķ���ֵ��װ�ࡣ
/// </summary>
public class ActionEntityReturnDto : ReturnDtoBase
{
    /// <summary>
    /// ����ɹ������ﷵ����ʵ���Id��
    /// </summary>
    public Guid Id { get; set; }
}
```

---

## 5. ���ݲ����淶

### 5.1 Id����
```csharp
entity.GenerateNewId(); // ǿ������Id
```

### 5.2 �ֶα�������
```csharp
// ʹ��EntityManager�����޸�
if (!_EntityManager.Modify(new[] { model.Entity }))
{
    result.HasError = true;
    result.ErrorCode = 404;
    result.DebugMessage = "�޸�ʧ�ܣ���������";
    return result;
}

// ȷ�������ֶβ����޸�
var entry = _DbContext.Entry(model.Entity);
entry.Property(e => e.OrgId).IsModified = false; // ����Id�����޸�
entry.Property(e => e.CreateBy).IsModified = false;
entry.Property(e => e.CreateDateTime).IsModified = false;
```

### 5.3 ��ѯ�Ż�
```csharp
// ʹ���޸��ٲ�ѯ�������
coll = coll.OrderBy(model.OrderFieldName, model.IsDesc).AsNoTracking();

// ʹ��EntityManager���з�ҳ
var prb = _EntityManager.GetAll(coll, model.StartIndex, model.Count);
```

---

## 6. ҵ�����ʵ��

### 6.1 Ȩ�޿���
- ���ݸ��룺ͨ�� `OrgId` ȷ�����⻧���ݸ���
- ����Ȩ�ޣ�����û��Ƿ���Ȩ�����ض�����
- ״̬���ƣ���������ݲ��ɱ༭

### 6.2 �������
- �ض��ֶ�ֻ�������ʱ���ã�����㷽ʽ�������˻���
- ���״̬ͨ�� `AuditDateTime` �ж�
- ȡ�����ʱ�������ֶ�

### 6.3 ����������
```csharp
// ����ʱ���ñ�Ҫ�ֶ�
entity.OrgId = context.User.OrgId;
entity.CreateBy = context.User.Id;
entity.CreateDateTime = OwHelper.WorldNow;

// ����������ֶ�
entity.AuditDateTime = null;
entity.AuditOperatorId = null;
```

---

## 7. ������淶

### 7.1 ��׼������Ӧ
```csharp
try
{
    // ҵ���߼�
}
catch (Exception ex)
{
    _Logger.LogError(ex, "��������ʱ��������");
    result.HasError = true;
    result.ErrorCode = 500;
    result.DebugMessage = $"��������ʱ��������: {ex.Message}";
}
```

### 7.2 ҵ����֤
```csharp
if (entity == null)
{
    result.HasError = true;
    result.ErrorCode = 404;
    result.DebugMessage = "ָ����ʵ�岻����";
    return result;
}

if (!entity.CanEdit(_DbContext))
{
    result.HasError = true;
    result.ErrorCode = 403;
    result.DebugMessage = "ʵ�嵱ǰ״̬������༭";
    return result;
}
```

---

## 8. ��־��¼�淶

### 8.1 �ؼ�������־
```csharp
_Logger.LogInformation("ҵ������ɹ���������: {UserId}", context.User.Id);
_Logger.LogError(ex, "ҵ�����ʧ��");
```

### 8.2 ������Ϣ
- �ڿ����׶��ṩ��ϸ�� `DebugMessage`
- ���������б��Ⱪ¶������Ϣ

---

## 9. ��չ�����淶

### 9.1 ʵ����չ����
```csharp
public static class EntityExtensions
{
    /// <summary>
    /// ��ȡ�������ݡ�
    /// </summary>
    public static RelatedEntity GetRelated(this Entity entity, DbContext context)
    {
        return context.Set<RelatedEntity>().Find(entity.RelatedId);
    }

    /// <summary>
    /// ҵ������жϡ�
    /// </summary>
    public static bool CanEdit(this Entity entity, DbContext context = null)
    {
        return !entity.AuditDateTime.HasValue;
    }
}
```

---

## 10. �����Ż�����

### 10.1 ��ѯ�Ż�
- ʹ�� `AsNoTracking()` ����ֻ����ѯ
- ����ʹ�� `Include()` ����N+1��ѯ
- �����ݼ�ʹ�÷�ҳ��ѯ

### 10.2 �ڴ����
- ��ʱ�ͷŴ����
- ���ⲻ��Ҫ�Ķ��󴴽�
- ʹ�� `using` ��������Դ

---

## 11. �����淶

### 11.1 �ļ�����
- ʵ���ࣺ`EntityName.cs`
- ��������`EntityController.cs`
- �������ֲ��ࣺ`EntityController.Action.cs`
- DTO�ࣺ`EntityController.Dto.cs`

### 11.2 ��������
- ˽���ֶΣ�`_fieldName`
- ������`parameterName`
- ���ԣ�`PropertyName`
- ������`MethodName`

---

## 12. ע�͹淶

### 12.1 XML�ĵ�ע��
- ���й�����Ա������XMLע��
- ����ҵ������Ǽ���ʵ��
- ��������˵���ͷ���ֵ˵��

### 12.2 ����ע��
```csharp
// TODO: ��ʵ�ֹ���
// HACK: ��ʱ�������
// NOTE: ��Ҫ˵��
// FIXME: ��Ҫ�޸�������
```

---

## 13. �汾����

### 13.1 �ύ�淶
- feat: �¹���
- fix: �޸�����
- docs: �ĵ�����
- refactor: �����ع�
- test: �������

### 13.2 ��֧����
- main: ����֧
- develop: ������֧
- feature/*: ���ܷ�֧
- hotfix/*: �޸���֧

---

*���ĵ��������Ŀ��չ�������£��뿪���Ŷ��ϸ��������Ϲ淶��*
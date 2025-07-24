# PowerLms �����淶��Ҫ

## ��Ŀ�ܹ�
- **PowerLmsData**: ���ݲ� - ʵ���ࡢö�١�����ģ��
- **PowerLmsServer**: ����� - ҵ���߼�����������  
- **PowerLmsWebApi**: API�� - ��������DTO��

## ���Ĺ淶

### ʵ�����
```csharp
// ��׼ʵ��̳�
public class EntityName : GuidKeyObjectBase, ISpecificOrg, ICreatorInfo
{
    /// <summary>ҵ���ֶ���������ϸҵ�����˵����</summary>
    [Comment("���ݿ�ע��")]
    [MaxLength(64), Required]
    public string FieldName { get; set; }
}
```

**�����ֶι淶**:
- **OrgId**: ����Id������ʱȷ���������޸�
- **CreateBy/CreateDateTime**: ����ֶΣ�ʹ�� `OwHelper.WorldNow`
- **�����ֶ�**: "CNY"������4��`Unicode(false)`
- **����/���**: `[Precision(18, 6)]` / `[Precision(18, 2)]`

### ���������
```csharp
[ApiController]
[Route("api/[controller]/[action]")]
public partial class EntityController : ControllerBase
{
    private readonly PowerLmsUserDbContext _DbContext;
    private readonly AccountManager _AccountManager;
    private readonly ILogger<EntityController> _Logger;
    private readonly EntityManager _EntityManager;
}
```

**��׼��������**:
- `GetAll{Entity}`: ��ȡ�б���ҳ+������ѯ��
- `Add{Entity}`: ����
- `Modify{Entity}`: �޸�  
- `Remove{Entity}`: ɾ��
- `Audit{Entity}`: ���

### DTO���
```csharp
// ����DTO
public class ActionEntityParamsDto : TokenDtoBase
{
    [Required]
    public Entity Entity { get; set; }
}

// ����DTO  
public class ActionEntityReturnDto : ReturnDtoBase
{
    public Guid Id { get; set; }
}
```

### Ȩ����֤
```csharp
if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context)
    return Unauthorized();
```

### ���ݲ���
```csharp
// Id����
entity.GenerateNewId();

// �ֶα���
var entry = _DbContext.Entry(model.Entity);
entry.Property(e => e.OrgId).IsModified = false;
entry.Property(e => e.CreateBy).IsModified = false;

// ��ѯ�Ż�
coll = coll.OrderBy(model.OrderFieldName, model.IsDesc).AsNoTracking();
```

### ������
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
    result.DebugMessage = $"����ʧ��: {ex.Message}";
}
```

### ҵ�����
- **���ݸ���**: ͨ�� `OrgId` ʵ�ֶ��⻧����
- **�������**: `AuditDateTime` �ж����״̬
- **�ֶα���**: �ؼ�����ֶβ����޸�

### ��չ����
```csharp
public static class EntityExtensions
{
    public static bool CanEdit(this Entity entity, DbContext context = null)
    {
        return !entity.AuditDateTime.HasValue;
    }
}
```

## �����淶
- **˽���ֶ�**: `_fieldName`
- **������Ա**: `PascalCase`  
- **�ļ�����**: `EntityController.cs`, `EntityController.Dto.cs`

## �����Ż�
- ʹ�� `AsNoTracking()` ����ֻ����ѯ
- �����ݼ�ʹ�÷�ҳ��ѯ
- ʹ�� `using` ��������Դ

---
*��ϸ�淶��ο������Ŀ����ĵ�*
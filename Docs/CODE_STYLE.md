# ������淶

## �����淶
- **˽���ֶ�**: `_PascalCase`���� `_ServiceProvider`
- **������Ա**: `PascalCase`������������
- **ö��ֵ**: λ��־ʹ�� 1, 2, 4, 8��2���ݣ�

## ����ṹ
- ʹ�� `#region` �ָ���������
- �����ܺͷ������η���֯��Ա
- ƫ�õ�һ���ܵ�С�ͷ���

## .NET 6 ����ʹ��

### ��Ŀ����
```xml
<PropertyGroup>
    <TargetFramework>net6.0</TargetFramework>
    <Nullable>disable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <GenerateDocumentationFile>True</GenerateDocumentationFile>
</PropertyGroup>
```

### ȫ��Using
```xml
<ItemGroup>
    <Using Include="OW.Data" />
</ItemGroup>
```

## ����ƫ��

### �첽���
- **��ǰʵ��**: ��Ҫʹ��ͬ������
- **ԭ��**: �򻯴��븴�Ӷȣ������첽�������л�
- **����**: I/O�ܼ������ɿ����첽

### ��������
- ʹ�� `SemaphoreSlim` ���Ʋ�������
- ʹ�� `SingletonLocker` �����ؼ���Դ
- ���� `DisposeHelper.Create` ��������Դ

### ���ݿ⽻��
- ʹ�� `[NotMapped]` ��Ƿ����ݿ��ֶ�
- ����ʵ����ֱ��ʹ��ö�٣�ʹ�û�������
- ʹ�� `AsNoTracking()` �Ż�ֻ����ѯ
- ͨ�� `IsModified = false` �����ؼ��ֶ�

## Web API �淶

### �������ṹ
```csharp
[ApiController]
[Route("api/[controller]/[action]")]
public partial class EntityController : ControllerBase
{
    private readonly PowerLmsUserDbContext _DbContext;
    private readonly AccountManager _AccountManager;
    private readonly ILogger<EntityController> _Logger;
}
```

### HTTP����Լ��
- `[HttpGet]`: ��ѯ���� - `GetAll{Entity}`
- `[HttpPost]`: �������� - `Add{Entity}`
- `[HttpPut]`: ���²��� - `Modify{Entity}`
- `[HttpDelete]`: ɾ������ - `Remove{Entity}`

### ��Ӧ�ĵ��淶
```csharp
/// <response code="200">δ����ϵͳ�����󡣵����ܳ���Ӧ�ô��󣬾���μ� HasError �� ErrorCode��</response>
/// <response code="401">��Ч���ơ�</response>
/// <response code="403">Ȩ�޲��㡣</response>
/// <response code="404">ָ��Id��ʵ�岻���ڡ�</response>
```

## ��־��¼
```csharp
_Logger.LogInformation("ҵ������ɹ���������: {UserId}", context.User.Id);
_Logger.LogError(ex, "ҵ�����ʧ��");
_Logger.LogWarning("�����޸Ĳ����ڵ�ʵ�壺{Id}", item.Id);
```

## �����밲ȫ
- ʹ�� `using` ��������Դ
- ͳһToken��֤ģʽ
- ����Ȩ�޴���ķ��ʿ���
- ���ݸ���ͨ��OrgIdʵ��

---
*���淶���� .NET 6 ����Ŀʵ���ƶ�*
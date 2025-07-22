# ����淶�ͷ��

## һ�������淶

- **˽���ֶ�**��ʹ�� `_PascalCase` ��ʽ���� `_ServiceProvider`����������βע�͡�
- **������Ա**��ʹ�� `PascalCase` ��ʽ�����ƾ������ԡ�
- **ö��ֵ**��������λ��־����ѡֵΪ 1, 2, 4, 8 �ȣ�2 ���ݣ���

## ��������ṹ

- ʹ�� `#region` �ָ��������顣
- �����ܺͷ������η���֯��Ա��
- �����ӿ����ڲ�ʵ����ȷ���롣
- ƫ�õ�һ���ܵ�С�ͷ�����

## ����ע�ͷ��

- XML �ĵ�ע�ͱ�����ͬһ�У����Ծ�Ž�β��
- ��βע��˵��ʵ��ϸ�ڡ�
- ��ϸ��¼����������ֵ�͹��ܡ�

## �ġ�����ƫ��

### 4.1 �첽���
- **��ǰʵ��**����Ŀ��Ҫʹ��ͬ�����������ⲻ��Ҫ�� `async/await`
- **ԭ��**���򻯴��븴�Ӷȣ������첽�������л�����
- **�������**��I/O�ܼ��Ͳ����ɿ����첽������Ȩ�⸴�Ӷ�

### 4.2 ��������
- ʹ�� `SemaphoreSlim` ���Ʋ�������
- ʹ�� `SingletonLocker` ���йؼ���Դ����
- ���� `DisposeHelper.Create` ģʽ��������Դ

### 4.3 ���ݿ⽻��
- ʹ�� `[NotMapped]` ��Ƿ����ݿ��ֶ�
- ������ʵ����ֱ��ʹ��ö�٣�ʹ�û������Ͳ��ṩת������
- �����ʵ�����������ѯ����
- ʹ�� `AsNoTracking()` �Ż�ֻ����ѯ
- ͨ�� `IsModified = false` �����ؼ��ֶ�

## �塢.NET 6 ����ʹ��

### 5.1 ��Ŀ����
```xml
<PropertyGroup>
    <TargetFramework>net6.0</TargetFramework>
    <Nullable>disable</Nullable>        <!-- ��ȷ���ÿɿ��������� -->
    <ImplicitUsings>enable</ImplicitUsings>  <!-- ������ʽusing -->
    <GenerateDocumentationFile>True</GenerateDocumentationFile>
</PropertyGroup>
```

### 5.2 ȫ��Using����
- ����Ŀ��ʹ�� `<Using Include="OW.Data" />` ������
- ������ÿ���ļ����ظ�using����

### 5.3 �����������
- ʹ�� `GlobalSuppressions.cs` ͳһ��������������
- �ʵ����Ʋ���Ҫ�ľ��棬�� `IDE0059:����Ҫ��ֵ`

## �������ģʽ��ܹ�

### 6.1 ����ע��
- ͨ�����캯������ע������
- ʹ�ñ�׼�� .NET 6 ����ע������
- �������λ��ģʽ

### 6.2 ��չ����
- ��ǿ�������͹��ܣ��򻯷���ע��
- Ϊʵ�����ṩҵ���߼���չ����
- ���磺`CanEdit()`, `GetRelated()` ��

### 6.3 �����������
- ʵ�����������룬ҵ���߼������ݷ��ʲ�����
- ʹ����չ����ʵ��ʵ����Ϊ
- �������ݲ�Ĵ�����

## �ߡ�Web API �淶

### 7.1 �������ṹ
```csharp
[ApiController]
[Route("api/[controller]/[action]")]
public partial class EntityController : ControllerBase
{
    private readonly PowerLmsUserDbContext _DbContext;
    private readonly IServiceProvider _ServiceProvider;
    private readonly AccountManager _AccountManager;
    private readonly ILogger<EntityController> _Logger;
}
```

### 7.2 HTTP ����Լ��
- `[HttpGet]`: ��ѯ������ʹ�� `GetAll{Entity}` ����
- `[HttpPost]`: ����������ʹ�� `Add{Entity}` ����  
- `[HttpPut]`: ���²�����ʹ�� `Modify{Entity}` ����
- `[HttpDelete]`: ɾ��������ʹ�� `Remove{Entity}` ����

### 7.3 ��Ӧ�ĵ��淶
```csharp
/// <response code="200">δ����ϵͳ�����󡣵����ܳ���Ӧ�ô��󣬾���μ� HasError �� ErrorCode��</response>
/// <response code="401">��Ч���ơ�</response>
/// <response code="403">Ȩ�޲��㡣</response>
/// <response code="404">ָ��Id��ʵ�岻���ڡ�</response>
```

## �ˡ�������

### 8.1 �쳣����ģʽ
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

### 8.2 ҵ����֤
- ʹ��ͳһ�Ĵ�����Ӧ��ʽ
- �ṩ��ϸ�Ĵ�����͵�����Ϣ
- ����ϵͳ�����ҵ�����

## �š���־��¼

### 9.1 �ṹ����־
```csharp
_Logger.LogInformation("ҵ������ɹ���������: {UserId}", context.User.Id);
_Logger.LogError(ex, "ҵ�����ʧ��");
_Logger.LogWarning("�����޸Ĳ����ڵ�ʵ�壺{Id}", item.Id);
```

### 9.2 ��־����ʹ��
- `LogError`: ϵͳ������쳣
- `LogWarning`: ҵ�񾯸���쳣���  
- `LogInformation`: �ؼ�ҵ�������¼
- `LogDebug`: ����������Ϣ

## ʮ�����ܿ���

### 10.1 ��Դ����
- ʹ�� `using` ���ȷ����Դ��ʱ�ͷ�
- ʹ�� `DisposeHelper.Create` ģʽ��������Դ
- ��ʱ�ͷ����ݿ����Ӻ�����Դ

### 10.2 ��ѯ�Ż�
- ����ʱ�����;��ȣ��羫ȷ�����룩�Խ�ʡ�洢�ռ�
- ʹ�� `AsNoTracking()` ����ֻ����ѯ
- ����ʹ�÷�ҳ���������

### 10.3 �����Ż�  
- ʹ�������Ʊ������ݾ���
- ���ú��������ʱʱ��
- �Ż����ݿ�����������ѯ����

## ʮһ����ȫ�淶

### 11.1 Ȩ����֤
- ͳһ��Token��֤ģʽ
- ����Ȩ�޴���ķ��ʿ���
- ���ݸ���ͨ��OrgIdʵ��

### 11.2 ���ݱ���
- �ؼ��ֶ��޸ı���
- ����ֶεĲ��ɱ���
- ������Ϣ����־����

---

*���淶���� .NET 6 �͵�ǰ��Ŀʵ���ƶ�������ݼ�����չ��������*
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

//ע��Swagger������������һ�� Swagger �ĵ�
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Version = "v1",
        Title = $"PowerLms",
        Description = "�ӿ��ĵ�v2.0.0",
        Contact = new OpenApiContact() { }
    });
    // Ϊ Swagger ����xml�ĵ�ע��·��
    var fileNames = Directory.GetFiles(AppContext.BaseDirectory, "*ApiDoc.xml");
    foreach (var item in fileNames) //������xml�����ļ�
    {
        var xmlPath = Path.Combine(AppContext.BaseDirectory, item);
        c.IncludeXmlComments(xmlPath, true);
    }
    c.OrderActionsBy(c => c.RelativePath);
});

var app = builder.Build();

IWebHostEnvironment env = app.Environment;

// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    //�����м����������SwaggerUI��ָ��Swagger JSON�ս��
    app.UseSwaggerUI(c =>
    {
        //c.SwaggerEndpoint("/swagger/v2/swagger.json", env.EnvironmentName + $" V2");
        c.SwaggerEndpoint("/swagger/v1/swagger.json", $"{env.EnvironmentName} V1");
        c.RoutePrefix = string.Empty;//���ø��ڵ����
    });
}

app.UseAuthorization();

app.MapControllers();

app.Run();

using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

//注册Swagger生成器，定义一个 Swagger 文档
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Version = "v1",
        Title = $"PowerLms",
        Description = "接口文档v2.0.0",
        Contact = new OpenApiContact() { }
    });
    // 为 Swagger 设置xml文档注释路径
    var fileNames = Directory.GetFiles(AppContext.BaseDirectory, "*ApiDoc.xml");
    foreach (var item in fileNames) //加入多个xml描述文件
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
    //启用中间件服务生成SwaggerUI，指定Swagger JSON终结点
    app.UseSwaggerUI(c =>
    {
        //c.SwaggerEndpoint("/swagger/v2/swagger.json", env.EnvironmentName + $" V2");
        c.SwaggerEndpoint("/swagger/v1/swagger.json", $"{env.EnvironmentName} V1");
        c.RoutePrefix = string.Empty;//设置根节点访问
    });
}

app.UseAuthorization();

app.MapControllers();

app.Run();

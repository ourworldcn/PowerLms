﻿using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using OW;
using OW.Data;
using OW.EntityFrameworkCore;
using PowerLms.Data;
using PowerLmsServer.EfData;
using PowerLmsServer.Managers;
using PowerLmsWebApi;
using PowerLmsWebApi.Middleware;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using static Org.BouncyCastle.Math.EC.ECCurve;

internal class Program
{
    static void Main(string[] args)
    {
        const int timeout = 60; // 设置超时时间为60秒
        var now = DateTime.Now;
        WebApplicationBuilder builder = ConfigService(args);
        var app = builder.Build();

        IWebHostEnvironment env = app.Environment;

        var config = app.Configuration;

        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        #region 自动迁移数据库所有挂起的迁移
        try
        {
            var dbContextFactory = app.Services.GetRequiredService<IDbContextFactory<PowerLmsUserDbContext>>();
            using var dbContext = dbContextFactory.CreateDbContext();
            while (!dbContext.Database.CanConnect()) // 确保数据库连接正常
            {
                //若超时仍然无法连接，则抛出异常
                if (DateTime.Now - now > TimeSpan.FromSeconds(timeout))
                {
                    throw new Exception($"启动时等待{timeout}秒仍无法连接到数据库，请检查数据库连接配置或机器性能。");
                }
                logger.LogError("启动时数据库连接失败，正在尝试重新连接...");
                Thread.Sleep(500);
            }
            dbContext.Database.Migrate(); //自动迁移数据库所有挂起的迁移
        }
        catch (Exception ex)
        {
            logger.LogError("数据库迁移失败: {Message}", ex.Message);
            throw; // 重新抛出异常以便调试
        }
        #endregion 自动迁移数据库所有挂起的迁移

        //app.UseRouting();
        //app.UseEndpoints(endpoints =>
        //{
        //    endpoints.MapControllers();
        //});
        // Configure the HTTP request pipeline.
        app.UseResponseCompression();
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

        #region 静态资源访问
        app.UseStaticFiles();
        //var basePath = AppContext.BaseDirectory;
        //var path = Path.Combine(basePath, "Files/");
        //Directory.CreateDirectory(path);
        //// 添加MIME支持
        //var provider = new FileExtensionContentTypeProvider(new Dictionary<string, string>
        //{
        //    { ".xlsx","application/octet-stream"}
        //});
        //app.UseStaticFiles(new StaticFileOptions
        //{
        //    FileProvider = new PhysicalFileProvider(path),
        //    ContentTypeProvider = provider,
        //    RequestPath = path,
        //});

        #endregion 静态资源访问

        // 添加跨域设置
        app.UseCors("AnyOrigin");

        app.UseAuthorization();
        app.MapControllers();
        app.UseDeveloperExceptionPage();
        app.UseMiddleware<PlExceptionMiddleware>();
        app.Run();
    }

    private static WebApplicationBuilder ConfigService(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        var services = builder.Services;
        services.AddMemoryCache();

        //启用跨域
        services.AddCors(cors =>
        {
            cors.AddPolicy("AnyOrigin", o => o.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
        });


        // Add services to the container.
        services.AddResponseCompression(options =>
        {
            options.EnableForHttps = true;
        });
        //JsonSerializerSettings settings = new JsonSerializerSettings() { DateFormatString=""};
        services.AddHttpContextAccessor(); // 添加 IHttpContextAccessor 服务
        services.AddControllers().AddJsonOptions(opt =>
        {
            opt.JsonSerializerOptions.Converters.Add(new CustomsJsonConverter());
        });

        #region 配置Swagger
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
        #endregion 配置Swagger

        #region 配置数据库
        var userDbConnectionString = builder.Configuration.GetConnectionString("UserDbConnection").Replace("{Env}", builder.Environment.EnvironmentName);
        //services.AddDbContext<PowerLmsUserDbContext>(options => options.UseLazyLoadingProxies().UseSqlServer(userDbConnectionString).EnableSensitiveDataLogging());
        services.AddDbContextFactory<PowerLmsUserDbContext>(options =>
        {
            options.UseLazyLoadingProxies().UseSqlServer(userDbConnectionString).EnableSensitiveDataLogging();
        });
        services.AddOwBatchDbWriter<PowerLmsUserDbContext>();

        #endregion 配置数据库

        // 添加组织管理服务（替代 OrganizationManager 和 MerchantManager）
        services.AddOrgManager<PowerLmsUserDbContext>();

        services.AddSqlDependencyManager(); //添加SqlDependencyManager服务
        if (TimeSpan.TryParse(builder.Configuration.GetSection("WorldClockOffset").Value, out var offerset))
            OwHelper.Offset = offerset;  //配置游戏世界的时间。

        #region 配置应用的一般服务
        services.AddHostedService<InitializerService>();
        services.AddOwTaskService<PowerLmsUserDbContext>(); //添加长时间运行任务服务
        #endregion 配置应用的一般服务

        #region 配置文件服务
        // 配置 OwFileServiceOptions
        services.Configure<OwFileServiceOptions>(builder.Configuration.GetSection(OwFileServiceOptions.SectionName));
        // 添加文件服务
        services.AddOwFileService<PowerLmsUserDbContext>();
        #endregion 配置文件服务

        #region 配置 AutoMapper

        var assemblies = new Assembly[] { typeof(PowerLmsUserDbContext).Assembly, typeof(Account).Assembly, typeof(SystemResourceManager).Assembly };   //避免有尚未加载的情况
        HashSet<Assembly> hsAssm = new HashSet<Assembly>(AppDomain.CurrentDomain.GetAssemblies());
        assemblies.ForEach(c => hsAssm.Add(c));
        services.AutoRegister(hsAssm);

        services.AddAutoMapper(hsAssm);
        #endregion 配置 AutoMapper

        services.AddManualInvoicingManager(); //添加手工开票管理服务
        services.AddNuoNuoManager(); //添加诺诺开票管理服务
        return builder;
    }
}


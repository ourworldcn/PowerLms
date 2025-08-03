/*
 * PowerLms - 货运物流业务管理系统
 * Web API 应用程序入口 - 负责服务配置和中间件管道
 * 
 * 功能说明：
 * - 服务容器配置和依赖注入设置
 * - 中间件管道配置和请求处理流程
 * - Swagger API文档配置
 * - 跨域策略和静态文件服务配置
 * 
 * 技术特点：
 * - 职责分离，仅负责应用程序配置
 * - 数据库初始化委托给InitializerService
 * - 统一的异常处理和日志配置
 * - 企业级中间件配置模式
 * 
 * 作者：PowerLms开发团队
 * 创建时间：2024年
 * 最后修改：2024年12月 - 重构数据库操作职责分离
 */

using Microsoft.AspNetCore.StaticFiles;
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
        WebApplicationBuilder builder = ConfigService(args);
        var app = builder.Build();
        ConfigureMiddleware(app);
        app.Run();
    }

    /// <summary>
    /// 配置服务容器
    /// </summary>
    /// <param name="args">命令行参数</param>
    /// <returns>Web应用程序构建器</returns>
    private static WebApplicationBuilder ConfigService(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        var services = builder.Services;
        
        // 添加基础服务
        services.AddMemoryCache();
        services.AddHttpContextAccessor();
        
        // 配置跨域
        services.AddCors(cors =>
        {
            cors.AddPolicy("AnyOrigin", o => o.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
        });

        // 配置响应压缩
        services.AddResponseCompression(options =>
        {
            options.EnableForHttps = true;
        });

        // 配置控制器和JSON序列化
        services.AddControllers().AddJsonOptions(opt =>
        {
            opt.JsonSerializerOptions.Converters.Add(new CustomsJsonConverter());
        });

        // 配置Swagger
        ConfigureSwagger(services);

        // 配置数据库
        ConfigureDatabase(services, builder.Configuration, builder.Environment);

        // 配置组织管理服务
        services.AddOrgManager<PowerLmsUserDbContext>();

        // 配置SQL依赖管理
        services.AddSqlDependencyManager();

        // 配置世界时钟偏移
        if (TimeSpan.TryParse(builder.Configuration.GetSection("WorldClockOffset").Value, out var offset))
            OwHelper.Offset = offset;

        // 配置应用服务
        ConfigureApplicationServices(services);

        // 配置文件服务
        ConfigureFileServices(services, builder.Configuration);

        // 配置AutoMapper
        ConfigureAutoMapper(services);

        // 配置发票管理服务
        services.AddManualInvoicingManager();
        services.AddNuoNuoManager();

        return builder;
    }

    /// <summary>
    /// 配置Swagger文档服务
    /// </summary>
    /// <param name="services">服务集合</param>
    private static void ConfigureSwagger(IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Version = "v1",
                Title = "PowerLms",
                Description = "接口文档v2.0.0",
                Contact = new OpenApiContact() { }
            });
            
            // 为 Swagger 设置xml文档注释路径
            var fileNames = Directory.GetFiles(AppContext.BaseDirectory, "*ApiDoc.xml");
            foreach (var item in fileNames)
            {
                var xmlPath = Path.Combine(AppContext.BaseDirectory, item);
                c.IncludeXmlComments(xmlPath, true);
            }
            c.OrderActionsBy(c => c.RelativePath);
        });
    }

    /// <summary>
    /// 配置数据库服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置</param>
    /// <param name="environment">环境</param>
    private static void ConfigureDatabase(IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
    {
        var userDbConnectionString = configuration.GetConnectionString("UserDbConnection").Replace("{Env}", environment.EnvironmentName);
        services.AddDbContextFactory<PowerLmsUserDbContext>(options =>  // 使用工厂模式配置DbContext,首推直接使用范围服务容器直接获取数据库上下文，或用工厂模式获取
        {
            options.UseLazyLoadingProxies().UseSqlServer(userDbConnectionString).EnableSensitiveDataLogging();
        });
        services.AddOwBatchDbWriter<PowerLmsUserDbContext>();
    }

    /// <summary>
    /// 配置应用程序服务
    /// </summary>
    /// <param name="services">服务集合</param>
    private static void ConfigureApplicationServices(IServiceCollection services)
    {
        services.AddHostedService<InitializerService>(); // 系统初始化服务（包含数据库迁移）
        services.AddOwTaskService<PowerLmsUserDbContext>(); // 长时间运行任务服务
    }

    /// <summary>
    /// 配置文件服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置</param>
    private static void ConfigureFileServices(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<OwFileServiceOptions>(configuration.GetSection(OwFileServiceOptions.SectionName));
        services.AddOwFileService<PowerLmsUserDbContext>();
    }

    /// <summary>
    /// 配置AutoMapper
    /// </summary>
    /// <param name="services">服务集合</param>
    private static void ConfigureAutoMapper(IServiceCollection services)
    {
        var assemblies = new Assembly[] { 
            typeof(PowerLmsUserDbContext).Assembly, 
            typeof(Account).Assembly, 
            typeof(SystemResourceManager).Assembly 
        };
        HashSet<Assembly> hsAssm = new HashSet<Assembly>(AppDomain.CurrentDomain.GetAssemblies());
        assemblies.ForEach(c => hsAssm.Add(c));
        services.AutoRegister(hsAssm);
        services.AddAutoMapper(hsAssm);
    }

    /// <summary>
    /// 配置中间件管道
    /// </summary>
    /// <param name="app">Web应用程序</param>
    private static void ConfigureMiddleware(WebApplication app)
    {
        IWebHostEnvironment env = app.Environment;

        // 配置响应压缩
        app.UseResponseCompression();

        // 配置Swagger文档
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", $"{env.EnvironmentName} V1");
            c.RoutePrefix = string.Empty; // 设置根节点访问
        });

        // 配置静态文件服务
        app.UseStaticFiles();

        // 配置跨域
        app.UseCors("AnyOrigin");

        // 配置授权
        app.UseAuthorization();

        // 配置控制器路由
        app.MapControllers();

        // 配置开发异常页面
        app.UseDeveloperExceptionPage();

        // 配置自定义异常处理中间件
        app.UseMiddleware<PlExceptionMiddleware>();
    }
}


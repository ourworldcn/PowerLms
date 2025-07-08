using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using OW;
using OW.EntityFrameworkCore;
using OwDbBase.Tasks;
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

        IWebHostEnvironment env = app.Environment;

        var config = app.Configuration;

        #region �Զ�Ǩ�����ݿ����й����Ǩ��
        var dbContextFactory = app.Services.GetRequiredService<IDbContextFactory<PowerLmsUserDbContext>>();
        var dbContext = dbContextFactory.CreateDbContext();
        dbContext.Database.Migrate(); //�Զ�Ǩ�����ݿ����й����Ǩ��
        #endregion �Զ�Ǩ�����ݿ����й����Ǩ��
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
            //�����м����������SwaggerUI��ָ��Swagger JSON�ս��
            app.UseSwaggerUI(c =>
            {
                //c.SwaggerEndpoint("/swagger/v2/swagger.json", env.EnvironmentName + $" V2");
                c.SwaggerEndpoint("/swagger/v1/swagger.json", $"{env.EnvironmentName} V1");
                c.RoutePrefix = string.Empty;//���ø��ڵ����
            });
        }

        #region ��̬��Դ����
        app.UseStaticFiles();
        //var basePath = AppContext.BaseDirectory;
        //var path = Path.Combine(basePath, "Files/");
        //Directory.CreateDirectory(path);
        //// ���MIME֧��
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

        #endregion ��̬��Դ����

        // ��ӿ�������
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

        services.AddOptions().Configure<OwFileManagerOptions>(builder.Configuration.GetSection("OwFileManagerOptions"));
        //services.Configure<BatchDbWriterOptions>(builder.Configuration.GetSection("BatchDbWriterOptions"));

        //���ÿ���
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
        services.AddHttpContextAccessor(); // ��� IHttpContextAccessor ����
        services.AddControllers().AddJsonOptions(opt =>
        {
            opt.JsonSerializerOptions.Converters.Add(new CustomsJsonConverter());
        });

        #region ����Swagger
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
        #endregion ����Swagger

        #region �������ݿ�
        var userDbConnectionString = builder.Configuration.GetConnectionString("UserDbConnection").Replace("{Env}", builder.Environment.EnvironmentName);
        //services.AddDbContext<PowerLmsUserDbContext>(options => options.UseLazyLoadingProxies().UseSqlServer(userDbConnectionString).EnableSensitiveDataLogging());
        services.AddDbContextFactory<PowerLmsUserDbContext>(options =>
        {
            options.UseLazyLoadingProxies().UseSqlServer(userDbConnectionString).EnableSensitiveDataLogging();
        });
        services.AddOwBatchDbWriter<PowerLmsUserDbContext>();

        #endregion �������ݿ�

        services.AddSqlDependencyManager(); //���SqlDependencyManager����
        if (TimeSpan.TryParse(builder.Configuration.GetSection("WorldClockOffset").Value, out var offerset))
            OwHelper.Offset = offerset;  //������Ϸ�����ʱ�䡣

        #region ����Ӧ�õ�һ�����
        services.AddHostedService<InitializerService>();
        services.AddSingleton<PasswordGenerator>(); //�������ɷ���
        services.AddOwTaskService<PowerLmsUserDbContext>(); //��ӳ�ʱ�������������
        #endregion ����Ӧ�õ�һ�����

        #region ���� AutoMapper

        var assemblies = new Assembly[] { typeof(PowerLmsUserDbContext).Assembly, typeof(Account).Assembly, typeof(SystemResourceManager).Assembly };   //��������δ���ص����
        HashSet<Assembly> hsAssm = new HashSet<Assembly>(AppDomain.CurrentDomain.GetAssemblies());
        assemblies.ForEach(c => hsAssm.Add(c));
        services.AutoRegister(hsAssm);

        services.AddAutoMapper(hsAssm);
        #endregion ���� AutoMapper

        services.AddManualInvoicingManager(); //����ֹ���Ʊ�������
        services.AddNuoNuoManager(); //���ŵŵ��Ʊ�������
        return builder;
    }


}


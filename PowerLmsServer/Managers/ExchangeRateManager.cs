/*
 * �ļ�����ExchangeRateManager.cs
 * ���ߣ�OW
 * �������ڣ�2023��10��25��
 * ���������ļ����� ExchangeRateManager �����ʵ�֣����ڻ��� PlExchangeRate �����ڱ仯ʱʹ����ʧЧ��
 * ��ǰ�ļ����ݸ�����
 * - ExchangeRateManager�����ڹ��� PlExchangeRate ��Ļ��棬���ڱ�仯ʱʹ����ʧЧ��
 */

using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OW.Data;
using PowerLms.Data;
using PowerLmsServer.EfData;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace PowerLmsServer.Managers
{
    /// <summary>
    /// ���ʹ����������ڻ��� PlExchangeRate ��
    /// </summary>
    [OwAutoInjection(ServiceLifetime.Singleton, AutoCreateFirst = true)]
    public class ExchangeRateManager : IDisposable
    {
        #region ˽���ֶ�
        private readonly IDbContextFactory<PowerLmsUserDbContext> _DbContextFactory;
        private readonly IMemoryCache _Cache;
        private readonly ILogger<ExchangeRateManager> _Logger;
        private readonly IHostApplicationLifetime _ApplicationLifetime;
        private readonly SqlDependencyManager _SqlDependencyManager;
        private const string CacheKey = "ExchangeRateCache";
        private CancellationTokenSource _SqlDependencyTokenSource;
        #endregion ˽���ֶ�

        #region ���캯��
        /// <summary>
        /// ���캯������ʼ�����ݿ������Ĺ������������־��¼����
        /// </summary>
        /// <param name="dbContextFactory">���ݿ������Ĺ�����</param>
        /// <param name="cache">�ڴ滺�档</param>
        /// <param name="logger">��־��¼����</param>
        /// <param name="applicationLifetime">Ӧ�ó����������ڡ�</param>
        /// <param name="sqlDependencyManager">SQL ������������</param>
        public ExchangeRateManager(IDbContextFactory<PowerLmsUserDbContext> dbContextFactory, IMemoryCache cache, ILogger<ExchangeRateManager> logger, IHostApplicationLifetime applicationLifetime, SqlDependencyManager sqlDependencyManager)
        {
            _DbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));
            _Cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _ApplicationLifetime = applicationLifetime ?? throw new ArgumentNullException(nameof(applicationLifetime));
            _SqlDependencyManager = sqlDependencyManager ?? throw new ArgumentNullException(nameof(sqlDependencyManager));

            RegisterSqlDependency();

            // ע��Ӧ�ó���ֹͣ�¼�
            _ApplicationLifetime.ApplicationStopped.Register(OnApplicationStopped);
        }
        #endregion ���캯��

        #region ��������
        /// <summary>
        /// ��ȡ���ʵ� ILookup��
        /// </summary>
        /// <returns>���ʵ� ILookup��</returns>
        public ILookup<(string, string), PlExchangeRate> GetExchangeRates()
        {
            return _Cache.GetOrCreate(CacheKey, entry =>
            {
                using var dbContext = _DbContextFactory.CreateDbContext();
                var exchangeRates = dbContext.Set<PlExchangeRate>().AsNoTracking()
                    .OrderByDescending(rate => rate.EndData) // ȷ�������ȶ�
                    .ToList();
                _Logger.LogDebug("���� PlExchangeRate ���� {Count} ����¼��", exchangeRates.Count);
                return exchangeRates.ToLookup(rate => (rate.SCurrency, rate.DCurrency));
            });
        }

        /// <summary>
        /// ʹ����ʧЧ��
        /// </summary>
        public void InvalidateCache()
        {
            _Cache.Remove(CacheKey);
            _Logger.LogDebug("��ʹ PlExchangeRate ����ʧЧ��");
        }
        #endregion ��������

        #region ˽�з���
        /// <summary>
        /// ע�� SqlDependency ������
        /// </summary>
        private void RegisterSqlDependency()
        {
            using var dbContext = _DbContextFactory.CreateDbContext();
            var query = dbContext.Set<PlExchangeRate>().AsNoTracking().ToQueryString();
            _SqlDependencyTokenSource = _SqlDependencyManager.RegisterSqlDependency(query, dbContext.Database.GetDbConnection().ConnectionString);
            _SqlDependencyTokenSource.Token.Register(OnDependencyChange);
        }

        /// <summary>
        /// SqlDependency �仯�¼�����
        /// </summary>
        private void OnDependencyChange()
        {
            InvalidateCache();
            _Logger.LogDebug("PlExchangeRate �����仯����ʹ����ʧЧ��");
        }

        /// <summary>
        /// Ӧ�ó���ֹͣʱ���õķ�����
        /// </summary>
        private void OnApplicationStopped()
        {
            StopDatabaseListening();
        }

        /// <summary>
        /// ֹͣ���ݿ�������
        /// </summary>
        private void StopDatabaseListening()
        {
            _SqlDependencyTokenSource?.Cancel();
            _Logger.LogDebug("��ֹͣ PlExchangeRate ������ݿ�������");
        }
        #endregion ˽�з���

        #region �ͷ���Դ
        /// <summary>
        /// �ͷ���Դ��
        /// </summary>
        public void Dispose()
        {
            StopDatabaseListening();
        }
        #endregion �ͷ���Դ
    }
}


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
using OW.EntityFrameworkCore;
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
    [OwAutoInjection(ServiceLifetime.Scoped,ServiceType =typeof(IAfterDbContextSaving<PlExchangeRate>))]
    [OwAutoInjection(ServiceLifetime.Scoped)]
    public class ExchangeRateManager : IDisposable, IAfterDbContextSaving<PlExchangeRate>
    {
        #region ˽���ֶ�
        private readonly IDbContextFactory<PowerLmsUserDbContext> _DbContextFactory;
        private readonly IMemoryCache _Cache;
        private readonly ILogger<ExchangeRateManager> _Logger;
        private const string CacheKey = "ExchangeRateCache";
        #endregion ˽���ֶ�

        #region ���캯��
        /// <summary>
        /// ���캯������ʼ�����ݿ������Ĺ������������־��¼����
        /// </summary>
        /// <param name="dbContextFactory">���ݿ������Ĺ�����</param>
        /// <param name="cache">�ڴ滺�档</param>
        /// <param name="logger">��־��¼����</param>
        public ExchangeRateManager(IDbContextFactory<PowerLmsUserDbContext> dbContextFactory, IMemoryCache cache, ILogger<ExchangeRateManager> logger)
        {
            _DbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));
            _Cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        #endregion ���캯��

        #region ��������
        /// <summary>
        /// ��ȡ���ʵ� ILookup��
        /// </summary>
        /// <returns>���ʵ� ILookup�����Ѿ��������¼��㽵������</returns>
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

        #region IAfterDbContextSaving ʵ��
        /// <summary>
        /// �ڱ��� PlExchangeRate �󣬼��仯��ʹ����ʧЧ��
        /// </summary>
        /// <param name="dbContext">��ǰ DbContext ʵ����</param>
        /// <param name="serviceProvider">�����ṩ�ߡ�</param>
        /// <param name="states">״̬�ֵ䡣</param>
        public void AfterSaving(DbContext dbContext, IServiceProvider serviceProvider, Dictionary<object, object> states)
        {
            if (dbContext.ChangeTracker.Entries<PlExchangeRate>().Any(e => e.State == EntityState.Added || e.State == EntityState.Modified || e.State == EntityState.Deleted))
            {
                InvalidateCache();
                _Logger.LogDebug("PlExchangeRate �����仯����ʹ����ʧЧ��");
            }
        }
        #endregion IAfterDbContextSaving ʵ��

        #region �ͷ���Դ
        /// <summary>
        /// �ͷ���Դ��
        /// </summary>
        public void Dispose()
        {
            // �ͷ���Դ
        }
        #endregion �ͷ���Դ
    }
}


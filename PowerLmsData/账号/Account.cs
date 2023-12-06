using Microsoft.EntityFrameworkCore;
using OW.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.Intrinsics.Arm;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace PowerLms.Data
{
    /// <summary>
    /// 账号相关信息数据库类。
    /// </summary>
    [Index(nameof(Token), IsUnique = false)]
    [Index(nameof(LoginName), IsUnique = false)]
    public class Account : GuidKeyObjectBase
    {
        /// <summary>
        /// 时间戳。
        /// </summary>
        [Timestamp]
        [JsonIgnore]
        public byte[] Timestamp { get; set; }

        /// <summary>
        /// 登录名。
        /// </summary>
        [MaxLength(64)]
        [Comment("登录名")]
        public string LoginName { get; set; }

        /// <summary>
        /// 用户的显示名。
        /// </summary>
        [MaxLength(64)]
        [Comment("用户的显示名")]
        public string DisplayName { get; set; }

        /// <summary>
        /// 密码的Hash值。
        /// </summary>
        [Comment("密码的Hash值")]
        [MaxLength(32)]
        [JsonIgnore]
        public byte[] PwdHash { get; set; }

        /// <summary>
        /// 使用的首选语言标准缩写。如:zh-CN
        /// </summary>
        [Column(TypeName = "varchar")]
        [MaxLength(12)]
        [Comment("使用的首选语言标准缩写。如:zh-CN")]
        public string CurrentLanguageTag { get; set; }

        /// <summary>
        /// 当前承载此用户的服务器节点号。空则表示此用户尚未被任何节点承载（未在线）。但有节点号，不代表用户登录，可能只是维护等其他目的将用户承载到服务器中。
        /// </summary>
        public int? NodeNum
        {
            get;
            set;
        }

        /// <summary>
        /// 创建该对象的世界时间。
        /// </summary>
        [Comment("创建该对象的世界时间")]
        public DateTime CreateUtc { get; set; } = OwHelper.WorldNow;

        /// <summary>
        /// 超时时间。
        /// </summary>
        /// <value>默认值15分钟。</value>
        [JsonConverter(typeof(TimeSpanJsonConverter))]
        public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(15);

        /// <summary>
        /// 最后一次操作的时间。
        /// </summary>
        public DateTime LastModifyDateTimeUtc { get; set; } = OwHelper.WorldNow;

        /// <summary>
        /// 最近使用的Token。
        /// </summary>
        [JsonIgnore]
        [Comment("最近使用的Token")]
        public Guid? Token { get; set; }

        /// <summary>
        /// 用户状态掩码。D0=1是锁定用户，D1=1用户应尽快更改密码，创建账号时可据需要设置此位，这不是强制要求，但前端在登录后见到此位为1应引导用户去更改密码。
        /// </summary>
        [Comment("用户状态掩码。D0=1是锁定用户，D1=1用户应尽快更改密码。")]
        public byte State { get; set; }

        #region 瞬时属性

        #endregion 瞬时属性

        #region 导航属性

        /// <summary>
        /// 当前使用的组织机构Id。在登陆后要首先设置。
        /// </summary>
        [Comment("当前使用的组织机构Id。在登陆后要首先设置")]
        public Guid? OrgId { get; set; }

        #region 数据字典属性

        /// <summary>
        /// 工作状态编码。
        /// </summary>
        [Comment("工作状态编码")]
        public Guid? WorkingStatusCode { get; set; }

        /// <summary>
        /// 在职状态编码。
        /// </summary>
        [Comment("在职状态编码")]
        public Guid? IncumbencyCode { get; set; }

        /// <summary>
        /// 性别编码。
        /// </summary>
        [Comment("性别编码")]
        public Guid? GenderCode { get; set; }

        /// <summary>
        /// 学历编码。
        /// </summary>
        [Comment("学历编码")]
        public Guid? QualificationsCode { get; set; }

        /// <summary>
        /// eMail地址。
        /// </summary>
        [Comment("eMail地址")]
        [EmailAddress]
        public string EMail { get; set; }

        /// <summary>
        /// 移动电话号码。
        /// </summary>
        [Comment("移动电话号码")]
        [Phone]
        public string Mobile { get; set; }
        #endregion  数据字典属性

        #endregion 导航属性

        #region 方法
        /// <summary>
        /// 密码是否正确。
        /// </summary>
        /// <param name="pwd">密码明文。</param>
        /// <returns>true密码匹配，false密码不匹配。</returns>
        public bool IsPwd(string pwd)
        {
            if (PwdHash is null && pwd is null)
                return true;
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(pwd ?? string.Empty));
            return hash.SequenceEqual(PwdHash ?? Array.Empty<byte>());
        }

        /// <summary>
        /// 设置密码。
        /// </summary>
        /// <param name="pwd"></param>
        public void SetPwd(string pwd)
        {
            PwdHash = SHA256.HashData(Encoding.UTF8.GetBytes(pwd ?? string.Empty));
        }

        /// <summary>
        /// 获取指定密码的Hash值。
        /// </summary>
        /// <param name="pwd"></param>
        /// <returns></returns>
        public static byte[] GetPwdHash(string pwd)
        {
            return SHA256.HashData(Encoding.UTF8.GetBytes(pwd ?? string.Empty));
        }
        #endregion 方法
    }

    /// <summary>
    /// 账号所属组织机构多对多表。
    /// </summary>
    [Comment("账号所属组织机构多对多表")]
    public class AccountPlOrganization
    {
        /// <summary>
        /// 用户Id。
        /// </summary>
        [Comment("用户Id")]
        public Guid UserId { get; set; }

        /// <summary>
        /// 所属组织机构Id。
        /// </summary>
        [Comment("直属组织机构Id")]
        public Guid OrgId { get; set; }
    }
}

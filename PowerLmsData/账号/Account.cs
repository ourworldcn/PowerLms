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
    public class Account : GuidKeyObjectBase
    {
        /// <summary>
        /// 登录名。
        /// </summary>
        public string LoginName { get; set; }

        /// <summary>
        /// 密码的Hash值。
        /// </summary>
        public byte[] PwdHash { get; set; }

        /// <summary>
        /// 使用的首选语言标准缩写。如:zh-CN
        /// </summary>
        [Required]
        public string LanguageTag { get; set; }

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
        public DateTime CreateUtc { get; set; } = OwHelper.WorldNow;

        /// <summary>
        /// 超时时间。
        /// </summary>
        /// <value>默认值15分钟。</value>
        [JsonConverter(typeof(TimeSpanJsonConverter))]
        public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(15);

        #region 瞬时属性

        /// <summary>
        /// 最后一次操作的时间。
        /// </summary>
        [NotMapped]
        public DateTime LastModifyDateTimeUtc { get; set; } = OwHelper.WorldNow;

        #endregion 瞬时属性

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
            var hash = SHA1.HashData(Encoding.UTF8.GetBytes(pwd ?? string.Empty));
            return hash.SequenceEqual(PwdHash ?? Array.Empty<byte>());
        }

        /// <summary>
        /// 设置密码。
        /// </summary>
        /// <param name="pwd"></param>
        public void SetPwd(string pwd)
        {
            PwdHash = SHA1.HashData(Encoding.UTF8.GetBytes(pwd ?? string.Empty));
        }

        #endregion 方法
    }
}

using Microsoft.EntityFrameworkCore;
using OW.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerLms.Data
{
    /// <summary>
    /// 验证码信息。
    /// </summary>
    [Index(nameof(VerifyDateTime), IsUnique = false)]
    [Index(nameof(DownloadDateTime), IsUnique = false)]
    public class CaptchaInfo : GuidKeyObjectBase
    {
        public CaptchaInfo()
        {
            
        }

        public CaptchaInfo(Guid id):base(id)
        {
            
        }

        /// <summary>
        /// 答案字符串。
        /// </summary>
        public string Answer { get; set; }

        /// <summary>
        /// 创建时间。
        /// </summary>
        public DateTime CreateDateTime { get; set; }

        /// <summary>
        /// 正确校验的时间，null标识尚未正确校验。
        /// </summary>
        public DateTime? VerifyDateTime { get; set; }

        /// <summary>
        /// 下载的时间，null标识尚未下载。
        /// </summary>
        public DateTime? DownloadDateTime { get; set; }

        /// <summary>
        /// 文件全路径名。
        /// </summary>
        public string FullPath { get; set; }
    }
}

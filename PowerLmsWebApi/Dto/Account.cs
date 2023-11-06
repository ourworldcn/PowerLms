using System.ComponentModel.DataAnnotations;

namespace PowerLmsWebApi.Dto
{
    /// <summary>
    /// 登录功能参数封装类。
    /// </summary>
    public class LoginParamsDto
    {
        /// <summary>
        /// 登录名。
        /// </summary>
        [Required]
        public string LoginName { get; set; }

        /// <summary>
        /// 登录密码。
        /// </summary>
        [Required]
        public string Pwd { get; set; }

        /// <summary>
        /// 使用的首选语言标准缩写。如:zh-CN。如果省略或为空则使用上次成功登录的首选语言，如果没有指定默认为zh-CN。
        /// </summary>
        public string LanguageTag { get; set; }
    }

    /// <summary>
    /// 登录功能返回值封装类。
    /// </summary>
    public class LoginReturnDto
    {
        /// <summary>
        /// 若成功登录这里返沪票据。用于后续操作。
        /// </summary>
        public Guid Token { get; set; }
    }

}

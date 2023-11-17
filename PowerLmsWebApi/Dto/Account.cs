using PowerLms.Data;
using System.ComponentModel.DataAnnotations;

namespace PowerLmsWebApi.Dto
{
    /// <summary>
    /// 登录功能参数封装类。
    /// </summary>
    public class LoginParamsDto
    {
        /// <summary>
        /// 登录名。可用手机号，邮箱。
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

        /// <summary>
        /// 如果成功登录，这里返回直接所属的一组机构信息。
        /// </summary>
        public List<PlOrganization> Orgs { get; set; } = new List<PlOrganization>();
    }

    /// <summary>
    /// 登陆后设置用的一些必要信息功能的参数封装类。
    /// </summary>
    public class SetUserInfoParams : TokenDtoBase
    {
        /// <summary>
        /// 当前组织机构Id。
        /// </summary>
        [Required]
        public Guid CurrentOrgId { get; set; }

        /// <summary>
        /// 用户使用的首选语言。
        /// </summary>
        [Required]
        public string LanguageTag { get; set; }
    }

    /// <summary>
    /// 登陆后设置用的一些必要信息功能返回值封装类。
    /// </summary>
    public class SetUserInfoReturnDto : ReturnDtoBase
    {
    }

    /// <summary>
    /// 创建账户功能参数封装类。
    /// </summary>
    public class CreateAccountParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 账号的登录名。
        /// </summary>
        public string LoginName { get; set; }

        /// <summary>
        /// 指定的秘密，如果为null或空则自动生成一个秘密。
        /// </summary>
        public string Pwd { get; set; }
    }

    /// <summary>
    /// 创建账户功能返回值封装类。
    /// </summary>
    public class CreateAccountReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 如果创建成功这里返回用户Id。用于有权限的用户随后设置该账号的具体信息。
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// 密码的明文。这是唯一能获取密码的地方，请用户记得。
        /// </summary>
        public string Pwd { get; set; }
    }

    /// <summary>
    /// 设置/修改账号信息功能参数封装类。
    /// </summary>
    public class SetAccountInfoParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 账号的信息，可从Account/GetAccountInfo 获取，修改后调用设置/修改账号信息功能。
        /// </summary>
        public Account Account { get; set; }
    }

    /// <summary>
    /// 设置/修改账号信息功能返回值封装类。
    /// </summary>
    public class SetAccountInfoReturnDto : ReturnDtoBase
    {
    }

    /// <summary>
    /// 获取账号信息功能参数封装类。
    /// </summary>
    public class GetAccountInfoParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 要获取信息的账号唯一Id。
        /// </summary>
        public Guid UserId { get; set; }
    }

    /// <summary>
    /// 获取账号信息封装类。
    /// </summary>
    public class GetAccountInfoReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 成功时返回账号信息。
        /// </summary>
        public Account Account { get; set; }
    }

}

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

        /// <summary>
        /// LoginName的类型。1=登陆账号名，2=邮箱地址，4=手机号。
        /// </summary>
        [Range(1, 7)]
        public int EvidenceType { get; set; }
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

        /// <summary>
        /// 账户所属商户Id。如果不属于任何商户则返回null。
        /// </summary>
        public Guid? MerchantId { get; set; }

        /// <summary>
        /// 返回登录账号的信息。
        /// </summary>
        public Account User { get; set; }
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
        /// 指定的密码，如果为null或空则自动生成一个秘密。
        /// </summary>
        public string Pwd { get; set; }

        /// <summary>
        /// 这里指定除密码等敏感信息以外的信息。不可指定的会自动忽略。
        /// </summary>
        public Account Item { get; set; }
    }

    /// <summary>
    /// 创建账户功能返回值封装类。
    /// </summary>
    public class CreateAccountReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 密码的明文。这是唯一能获取密码的地方，请用户记得。
        /// </summary>
        public string Pwd { get; set; }

        /// <summary>
        /// 返回用户信息。
        /// </summary>
        public Account Result { get; set; }
    }

    /// <summary>
    /// 设置/修改账号信息功能参数封装类。
    /// </summary>
    public class ModifyAccountParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 账号的信息，可从Account/GetAccountInfo 获取，修改后调用设置/修改账号信息功能。
        /// </summary>
        public Account Item { get; set; }

        /// <summary>
        /// 有权限的用户可以使用此标志设置用户是否为超管。
        /// true设置为超管，false取消超管，省略或为null则不设置。
        /// </summary>
        public bool? IsAdmin { get; set; }

        /// <summary>
        /// 有权限的用户可以使用此标志设置用户是否为商管。
        /// true设置为商管，false取消商管，省略或为null则不设置。
        /// </summary>
        public bool? IsMerchantAdmin { get; set; }
    }

    /// <summary>
    /// 设置/修改账号信息功能返回值封装类。
    /// </summary>
    public class ModifyAccountReturnDto : ReturnDtoBase
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

using PowerLms.Data;
using System.ComponentModel.DataAnnotations;
namespace PowerLmsWebApi.Dto
{
    /// <summary>登录功能参数封装类。</summary>
    public class LoginParamsDto
    {
        /// <summary>验证码的id,就是图片文件的文件名，不带路径，可以带扩展名（但忽略扩展名）</summary>
        //[Required, MinLength(1)]
        public string CaptchaId { get; set; }
        /// <summary>验证码的答案。</summary>
        //[Required, MinLength(1)]
        public string Answer { get; set; }
        /// <summary>登录名。可用手机号，邮箱。</summary>
        [Required]
        public string LoginName { get; set; }
        /// <summary>登录密码。</summary>
        [Required]
        public string Pwd { get; set; }
        /// <summary>使用的首选语言标准缩写。如:zh-CN。如果省略或为空则使用上次成功登录的首选语言，如果没有指定默认为zh-CN。</summary>
        public string LanguageTag { get; set; }
        /// <summary>LoginName的类型。1=登陆账号名，2=邮箱地址，4=手机号。</summary>
        [Range(1, 7)]
        public int EvidenceType { get; set; }
    }
    /// <summary>登录功能返回值封装类。</summary>
    public class LoginReturnDto
    {
        /// <summary>若成功登录这里返回票据。用于后续操作。</summary>
        public Guid Token { get; set; }
        /// <summary>如果成功登录，这里返回直接所属的一组机构信息。</summary>
        public List<PlOrganization> Orgs { get; set; } = new List<PlOrganization>();
        /// <summary>账户所属商户Id。如果不属于任何商户则返回null。</summary>
        public Guid? MerchantId { get; set; }
        /// <summary>返回登录账号的信息。</summary>
        public Account User { get; set; }
    }
    /// <summary>登陆后设置用的一些必要信息功能的参数封装类。</summary>
    public class SetUserInfoParams : TokenDtoBase
    {
        /// <summary>当前组织机构Id。</summary>
        [Required]
        public Guid CurrentOrgId { get; set; }
        /// <summary>用户使用的首选语言。</summary>
        [Required]
        public string LanguageTag { get; set; }
    }
    /// <summary>登陆后设置用的一些必要信息功能返回值封装类。</summary>
    public class SetUserInfoReturnDto : ReturnDtoBase
    {
        /// <summary>权限集合，无重复。</summary>
        public List<PlPermission> Permissions { get; set; } = new List<PlPermission>();
    }
    /// <summary>创建账户功能参数封装类。</summary>
    public class CreateAccountParamsDto : TokenDtoBase
    {
        /// <summary>指定的密码，如果为null或空则自动生成一个秘密。</summary>
        public string Pwd { get; set; }
        /// <summary>这里指定除密码等敏感信息以外的信息。不可指定的会自动忽略。</summary>
        public Account Item { get; set; }
        /// <summary>用户直属商户或机构Id集合。所有Id须同属一个商户，且必须存在对应实体。</summary>
        public List<Guid> OrgIds { get; set; } = new List<Guid>();
    }
    /// <summary>创建账户功能返回值封装类。</summary>
    public class CreateAccountReturnDto : ReturnDtoBase
    {
        /// <summary>密码的明文。这是唯一能获取密码的地方，请用户记得。</summary>
        public string Pwd { get; set; }
        /// <summary>返回用户信息。</summary>
        public Account Result { get; set; }
    }
    /// <summary>设置/修改账号信息功能参数封装类。</summary>
    public class ModifyAccountParamsDto : TokenDtoBase
    {
        /// <summary>账号的信息，可从Account/GetAccountInfo 获取，修改后调用设置/修改账号信息功能。</summary>
        public Account Item { get; set; }
        /// <summary>有权限的用户可以使用此标志设置用户是否为超管。true设置为超管，false取消超管，省略或为null则不设置。</summary>
        public bool? IsAdmin { get; set; }
        /// <summary>有权限的用户可以使用此标志设置用户是否为商管。true设置为商管，false取消商管，省略或为null则不设置。</summary>
        public bool? IsMerchantAdmin { get; set; }
    }
    /// <summary>设置/修改账号信息功能返回值封装类。</summary>
    public class ModifyAccountReturnDto : ReturnDtoBase
    {
    }
    /// <summary>获取账号信息功能参数封装类。</summary>
    public class GetAccountInfoParamsDto : TokenDtoBase
    {
        /// <summary>要获取信息的账号唯一Id。</summary>
        public Guid UserId { get; set; }
    }
    /// <summary>获取账号信息封装类。</summary>
    public class GetAccountInfoReturnDto : ReturnDtoBase
    {
        /// <summary>成功时返回账号信息。</summary>
        public Account Account { get; set; }
    }
    /// <summary>设置用户的直属角色功能的参数封装类。</summary>
    public class SetRolesParamsDto : TokenDtoBase
    {
        /// <summary>账号的Id。</summary>
        public Guid UserId { get; set; }
        /// <summary>直属组织角色Id的集合。未在此集合指定的与角色的关系均被删除。</summary>
        public List<Guid> RoleIds { get; set; } = new List<Guid>();
    }
    /// <summary>设置用户的直属角色功能的返回值封装类。</summary>
    public class SetRolesReturnDto : ReturnDtoBase
    {
    }
    /// <summary>设置用户的直属机构功能的参数封装类。</summary>
    public class SetOrgsParamsDto : TokenDtoBase
    {
        /// <summary>账号的Id。</summary>
        public Guid UserId { get; set; }
        /// <summary>直属组织机构/商户Id的集合。未在此集合指定的与机构的关系均被删除。</summary>
        public List<Guid> OrgIds { get; set; } = new List<Guid>();
    }
    /// <summary>设置用户的直属机构功能的返回值封装类。</summary>
    public class SetOrgsReturnDto : ReturnDtoBase
    {
    }
    /// <summary>删除账户的功能参数封装类。</summary>
    public class RemoveAccountParamsDto : RemoveParamsDtoBase
    {
    }
    /// <summary>删除账户的功能返回值封装类。</summary>
    public class RemoveAccountReturnDto : RemoveReturnDtoBase
    {
    }
    /// <summary>重置密码功能的参数封装类。</summary>
    public class ResetPwdParamsDto : TokenDtoBase
    {
        /// <summary>要重置密码的账号的唯一Id。</summary>
        public Guid Id { get; set; }
    }
    /// <summary>重置密码功能的返回值封装类。</summary>
    public class ResetPwdReturnDto
    {
        /// <summary>密码。</summary>
        public string Pwd { get; set; }
    }
    /// <summary>修改用户自己的密码功能的参数封装类。</summary>
    public class ModifyPwdParamsDto : TokenDtoBase
    {
        /// <summary>原有密码。</summary>
        public string OldPwd { get; set; }
        /// <summary>新密码。</summary>
        public string NewPwd { get; set; }
    }
    /// <summary>修改用户自己的密码功能的返回值封装类。</summary>
    public class ModifyPwdReturnDto : ReturnDtoBase
    {
    }
    /// <summary>延迟令牌失效功能的参数封装类。</summary>
    public class NopParamsDto : TokenDtoBase
    {
    }
    /// <summary>延迟令牌失效功能的返回值封装类。</summary>
    public class NopReturnDto : ReturnDtoBase
    {
        /// <summary>新令牌。</summary>
        public Guid NewToken { get; set; }
    }
}

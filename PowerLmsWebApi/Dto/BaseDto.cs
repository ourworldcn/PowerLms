using System.ComponentModel.DataAnnotations;

namespace PowerLmsWebApi.Dto
{
    /// <summary>
    /// 带有令牌命令的入参基类。
    /// </summary>
    public class TokenDtoBase
    {
        /// <summary>
        /// 令牌。
        /// </summary>
        [Required]
        public Guid Token { get; set; }
    }

    /// <summary>
    /// 返回对象的基类。
    /// </summary>
    public class ReturnDtoBase
    {
        /// <summary>
        /// 
        /// </summary>
        public ReturnDtoBase()
        {

        }

        /// <summary>
        /// 是否有错误。不设置则使用<see cref="ErrorCode"/>来判定。
        /// </summary>
        /// <value>0没有错误，其它数值含义由应用定义。</value>
        public bool HasError { get; set; }

        /// <summary>
        /// 错误码，参见 ErrorCodes。
        /// </summary>
        public int ErrorCode { get; set; }

        /// <summary>
        /// 调试信息，如果发生错误，这里给出简要说明。
        /// </summary>
        public string DebugMessage { get; set; }

    }

}

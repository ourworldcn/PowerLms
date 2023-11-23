using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace OW
{
    /// <summary>
    /// 随机密码生成器。
    /// </summary>
    public class PasswordGenerator
    {
        public PasswordGenerator()
        {
            _Codes = Enumerable.Range(48, 10).Concat(Enumerable.Range(65, 26)).Concat(Enumerable.Range(97, 26)).Select(c => Convert.ToChar(c)).ToArray();
        }


        Random _Random = new Random();
        char[] _Codes;

        /// <summary>
        /// 生成密码。
        /// </summary>
        /// <param name="length"></param>
        /// <returns></returns>
        public string Generate(int length)
        {
            char[] ary = null;
            Span<char> pwd = length < 1024 ? stackalloc char[length] : (ary = ArrayPool<char>.Shared.Rent(length));
            try
            {
                lock (_Random)
                {
                    for (int i = 0; i < length; i++)
                    {
                        pwd[i] = Convert.ToChar(_Codes[_Random.Next(_Codes.Length)]);
                    }
                }

                return new string(pwd.Slice(0, length));

            }
            finally
            {
                if (ary is not null)
                    ArrayPool<char>.Shared.Return(ary);
            }
        }
    }
}

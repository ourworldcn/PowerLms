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
            var tmp = Enumerable.Range('0', 10).Concat(Enumerable.Range('a', 26)).Concat(Enumerable.Range('A', 26)).Select(c => Convert.ToChar(c)).ToList();
            //避开 1 I l O o 0
            var excludes = new char[] { '1', 'I', 'l', 'O', 'o', '0', };    //要排除的字符
            tmp.RemoveAll(c => excludes.Contains(c));
            _Codes = tmp.ToArray();
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

                return new string(pwd[..length]);

            }
            finally
            {
                if (ary is not null)
                    ArrayPool<char>.Shared.Return(ary);
            }
        }
    }
}

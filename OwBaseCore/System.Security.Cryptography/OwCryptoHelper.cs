/*
 * 项目：OW基础库 | 模块：加密工具
 * 功能：AES-256-GCM加密解密的纯函数实现
 * 技术要点：AES-GCM认证加密、ArrayPool、ArraySegment零拷贝返回
 * 作者：zc | 创建：2025-01 | 修改：2025-01-21 [ArraySegment+属性常量]
 */

using System;
using System.Buffers;
using System.Security.Cryptography;
using System.Text;

namespace OW.Security
{
    /// <summary>
    /// 加密解密工具类 - AES-256-GCM实现
    /// </summary>
    /// <remarks>
    /// <para>加密格式: Nonce[12] + Tag[16] + Cipher[变长], 总开销28字节</para>
    /// <para>返回ArraySegment&lt;byte&gt;包含ArrayPool数组引用及有效范围(零拷贝)</para>
    /// <para>使用完毕后可选择性归还: ArrayPool&lt;byte&gt;.Shared.Return(segment.Array)</para>
    /// </remarks>
    public static class OwCryptoHelper
    {
        #region 二进制数据加解密

        private static readonly int NonceSize = AesGcm.NonceByteSizes.MinSize;  // 12字节(96位)
        private static readonly int TagSize = AesGcm.TagByteSizes.MaxSize;      // 16字节(128位)
        private static readonly int Overhead = NonceSize + TagSize;             // 28字节

        /// <summary>
        /// 使用AES-256-GCM加密二进制数据
        /// </summary>
        /// <param name="plainData">待加密的明文数据</param>
        /// <param name="key">加密密钥(必须32字节)</param>
        /// <returns>ArraySegment包含ArrayPool数组引用(Array)及有效范围(Offset=0, Count=实际长度)</returns>
        /// <exception cref="ArgumentException">key长度不为32字节时抛出</exception>
        public static ArraySegment<byte> Encrypt(ReadOnlySpan<byte> plainData, ReadOnlySpan<byte> key)
        {
            if (key.Length != 32)
                throw new ArgumentException("密钥长度必须为32字节(AES-256)", nameof(key));
            if (plainData.Length == 0)
                return ArraySegment<byte>.Empty;
            var requiredLength = plainData.Length + Overhead;
            var result = ArrayPool<byte>.Shared.Rent(requiredLength);
            using var aesGcm = new AesGcm(key);
            Span<byte> resultSpan = result.AsSpan(0, requiredLength);
            var nonceSpan = resultSpan.Slice(0, NonceSize);
            var tagSpan = resultSpan.Slice(NonceSize, TagSize);
            var cipherSpan = resultSpan.Slice(Overhead);
            RandomNumberGenerator.Fill(nonceSpan);
            aesGcm.Encrypt(nonceSpan, plainData, cipherSpan, tagSpan);
            return new ArraySegment<byte>(result, 0, requiredLength);
        }

        /// <summary>
        /// 使用AES-256-GCM解密二进制数据
        /// </summary>
        /// <param name="encryptedData">加密数据(格式: Nonce[12] + Tag[16] + Cipher[变长])</param>
        /// <param name="key">解密密钥(必须32字节)</param>
        /// <returns>ArraySegment包含ArrayPool数组引用(Array)及有效范围(Offset=0, Count=实际长度)</returns>
        /// <exception cref="ArgumentException">key长度不为32字节或数据格式无效时抛出</exception>
        /// <exception cref="InvalidOperationException">数据被篡改或损坏时抛出</exception>
        public static ArraySegment<byte> Decrypt(ReadOnlySpan<byte> encryptedData, ReadOnlySpan<byte> key)
        {
            if (key.Length != 32)
                throw new ArgumentException("密钥长度必须为32字节(AES-256)", nameof(key));
            if (encryptedData.Length == 0)
                return ArraySegment<byte>.Empty;
            if (encryptedData.Length < Overhead)
                throw new ArgumentException($"加密数据格式无效,最小长度应为{Overhead}字节", nameof(encryptedData));
            var plainLength = encryptedData.Length - Overhead;
            var result = ArrayPool<byte>.Shared.Rent(plainLength);
            try
            {
                using var aesGcm = new AesGcm(key);
                aesGcm.Decrypt(encryptedData.Slice(0, NonceSize), encryptedData.Slice(Overhead), encryptedData.Slice(NonceSize, TagSize), result.AsSpan(0, plainLength));
                return new ArraySegment<byte>(result, 0, plainLength);
            }
            catch (CryptographicException ex)
            {
                ArrayPool<byte>.Shared.Return(result);
                throw new InvalidOperationException("数据已损坏或被篡改,验证失败", ex);
            }
        }

        #endregion 二进制数据加解密

        #region 字符串加解密

        /// <summary>
        /// 加密字符串(UTF8编码)
        /// </summary>
        /// <param name="plainText">待加密的明文字符串</param>
        /// <param name="key">加密密钥(必须32字节)</param>
        /// <returns>Base64编码的加密字符串</returns>
        /// <exception cref="ArgumentNullException">plainText为null时抛出</exception>
        /// <exception cref="ArgumentException">key长度不为32字节时抛出</exception>
        public static string EncryptString(string plainText, ReadOnlySpan<byte> key)
        {
            ArgumentNullException.ThrowIfNull(plainText);
            if (plainText.Length == 0) return string.Empty;
            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            var encrypted = Encrypt(plainBytes, key);
            try
            {
                return Convert.ToBase64String(encrypted.Array, encrypted.Offset, encrypted.Count);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(encrypted.Array);
            }
        }

        /// <summary>
        /// 解密字符串(UTF8编码)
        /// </summary>
        /// <param name="encryptedText">Base64编码的加密字符串</param>
        /// <param name="key">解密密钥(必须32字节)</param>
        /// <returns>解密后的明文字符串</returns>
        /// <exception cref="ArgumentNullException">encryptedText为null时抛出</exception>
        /// <exception cref="ArgumentException">key长度不为32字节或Base64格式无效时抛出</exception>
        /// <exception cref="InvalidOperationException">数据被篡改或损坏时抛出</exception>
        public static string DecryptString(string encryptedText, ReadOnlySpan<byte> key)
        {
            ArgumentNullException.ThrowIfNull(encryptedText);
            if (encryptedText.Length == 0) return string.Empty;
            byte[] encryptedBytes;
            try
            {
                encryptedBytes = Convert.FromBase64String(encryptedText);
            }
            catch (FormatException ex)
            {
                throw new ArgumentException("加密文本的Base64格式无效", nameof(encryptedText), ex);
            }
            var plainBytes = Decrypt(encryptedBytes, key);
            try
            {
                return Encoding.UTF8.GetString(plainBytes.Array, plainBytes.Offset, plainBytes.Count);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(plainBytes.Array);
            }
        }

        #endregion 字符串加解密

        #region 密钥管理工具

        /// <summary>
        /// 生成新的AES-256密钥(32字节)
        /// </summary>
        /// <returns>32字节的随机密钥</returns>
        public static byte[] GenerateKey()
        {
            var key = new byte[32];
            RandomNumberGenerator.Fill(key);
            return key;
        }

        /// <summary>
        /// 将密钥转换为Base64字符串
        /// </summary>
        /// <param name="key">32字节密钥</param>
        /// <returns>Base64编码的密钥字符串</returns>
        /// <exception cref="ArgumentNullException">key为null时抛出</exception>
        /// <exception cref="ArgumentException">key长度不为32字节时抛出</exception>
        public static string KeyToBase64(byte[] key)
        {
            ArgumentNullException.ThrowIfNull(key);
            if (key.Length != 32)
                throw new ArgumentException("密钥长度必须为32字节", nameof(key));
            return Convert.ToBase64String(key);
        }

        /// <summary>
        /// 从Base64字符串解析密钥
        /// </summary>
        /// <param name="base64Key">Base64编码的密钥字符串</param>
        /// <returns>32字节密钥</returns>
        /// <exception cref="ArgumentNullException">base64Key为null时抛出</exception>
        /// <exception cref="ArgumentException">Base64格式无效或长度不为32字节时抛出</exception>
        public static byte[] KeyFromBase64(string base64Key)
        {
            ArgumentNullException.ThrowIfNull(base64Key);
            byte[] key;
            try
            {
                key = Convert.FromBase64String(base64Key);
            }
            catch (FormatException ex)
            {
                throw new ArgumentException("Base64密钥格式无效", nameof(base64Key), ex);
            }
            if (key.Length != 32)
                throw new ArgumentException($"密钥长度必须为32字节,实际为{key.Length}字节", nameof(base64Key));
            return key;
        }

        #endregion 密钥管理工具
    }
}



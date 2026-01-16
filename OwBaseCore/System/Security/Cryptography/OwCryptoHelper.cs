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
    /// 加密解密工具类 - AES-GCM实现
    /// </summary>
    /// <remarks>
    /// <para>支持AES-128/192/256-GCM，密钥长度分别为16/24/32字节</para>
    /// <para>自定义加密格式: Nonce[12] + Tag[16] + Cipher[变长], 总开销28字节</para>
    /// <para>返回ArraySegment&lt;byte&gt;包含ArrayPool数组引用及有效范围(零拷贝)</para>
    /// <para>使用完毕后可选择性归还: ArrayPool&lt;byte&gt;.Shared.Return(segment.Array)</para>
    /// <para>WARNING: 加密和解密方法使用自定义数据格式，必须配对使用，不可与其他AES-GCM实现互操作</para>
    /// </remarks>
    public static class OwCryptoHelper
    {
        #region AES-GCM 二进制数据加解密

        private static readonly int NonceSize = AesGcm.NonceByteSizes.MinSize;  // 12字节(96位)
        private static readonly int TagSize = AesGcm.TagByteSizes.MaxSize;      // 16字节(128位)
        private static readonly int Overhead = NonceSize + TagSize;             // 28字节

        /// <summary>
        /// 使用AES-GCM加密二进制数据
        /// </summary>
        /// <param name="plainData">待加密的明文数据</param>
        /// <param name="key">加密密钥(支持16/24/32字节,分别对应AES-128/192/256)</param>
        /// <returns>ArraySegment包含ArrayPool数组引用(Array)及有效范围(Offset=0, Count=实际长度)</returns>
        /// <exception cref="ArgumentException">key长度不为16/24/32字节时抛出</exception>
        /// <remarks>
        /// <para>自定义加密格式: Nonce[12] + Tag[16] + Cipher[变长]，必须使用本类的DecryptAesGcm方法解密</para>
        /// <para>空数据处理: plainData.Length == 0 时仍会加密，返回28字节数据(Nonce[12] + Tag[16])</para>
        /// <para>WARNING: 加密结果使用自定义格式，不可与其他AES-GCM实现互操作，必须配对使用本类的加解密方法</para>
        /// </remarks>
        public static ArraySegment<byte> EncryptAesGcm(ReadOnlySpan<byte> plainData, ReadOnlySpan<byte> key)
        {
            if (key.Length != 16 && key.Length != 24 && key.Length != 32)
                throw new ArgumentException("密钥长度必须为16/24/32字节(AES-128/192/256)", nameof(key));
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
        /// 使用AES-GCM解密二进制数据
        /// </summary>
        /// <param name="encryptedData">加密数据(格式: Nonce[12] + Tag[16] + Cipher[变长])，必须由本类的EncryptAesGcm方法生成</param>
        /// <param name="key">解密密钥(支持16/24/32字节,分别对应AES-128/192/256)</param>
        /// <returns>ArraySegment包含ArrayPool数组引用(Array)及有效范围(Offset=0, Count=实际长度)</returns>
        /// <exception cref="ArgumentException">key长度不为16/24/32字节或数据格式无效时抛出</exception>
        /// <exception cref="InvalidOperationException">数据被篡改或损坏时抛出</exception>
        /// <remarks>
        /// <para>自定义解密格式: 仅支持本类EncryptAesGcm方法生成的数据(Nonce[12] + Tag[16] + Cipher[变长])</para>
        /// <para>空数据处理: encryptedData.Length == 28 时解密为空数组(Array.Empty&lt;byte&gt;())，encryptedData.Length &lt; 28 时抛出ArgumentException</para>
        /// <para>WARNING: 仅能解密本类加密方法生成的数据，不可用于其他AES-GCM实现的密文，必须配对使用</para>
        /// </remarks>
        public static ArraySegment<byte> DecryptAesGcm(ReadOnlySpan<byte> encryptedData, ReadOnlySpan<byte> key)
        {
            if (key.Length != 16 && key.Length != 24 && key.Length != 32)
                throw new ArgumentException("密钥长度必须为16/24/32字节(AES-128/192/256)", nameof(key));
            if (encryptedData.Length < Overhead)
                throw new ArgumentException($"加密数据格式无效,最小长度应为{Overhead}字节", nameof(encryptedData));
            var plainLength = encryptedData.Length - Overhead;
            if (plainLength == 0)
            {
                using var aesGcm = new AesGcm(key);
                aesGcm.Decrypt(encryptedData.Slice(0, NonceSize), ReadOnlySpan<byte>.Empty, encryptedData.Slice(NonceSize, TagSize), Span<byte>.Empty);
                return new ArraySegment<byte>(Array.Empty<byte>());
            }
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

        #endregion AES-GCM 二进制数据加解密

        #region 密钥管理工具

        /// <summary>
        /// 生成指定长度的AES-GCM密钥
        /// </summary>
        /// <param name="keySizeInBytes">密钥长度(字节)，有效值: 16(AES-128)、24(AES-192)、32(AES-256)</param>
        /// <returns>指定长度的随机密钥</returns>
        /// <exception cref="ArgumentException">密钥长度不为16/24/32字节时抛出</exception>
        /// <remarks>
        /// 密钥长度对应关系:
        /// - 16字节 = 128位 = AES-128-GCM
        /// - 24字节 = 192位 = AES-192-GCM
        /// - 32字节 = 256位 = AES-256-GCM (推荐，最高安全性)
        /// </remarks>
        public static byte[] GenerateKey(int keySizeInBytes = 32)
        {
            if (keySizeInBytes != 16 && keySizeInBytes != 24 && keySizeInBytes != 32)
                throw new ArgumentException("密钥长度必须为16/24/32字节(AES-128/192/256)", nameof(keySizeInBytes));
            var key = new byte[keySizeInBytes];
            RandomNumberGenerator.Fill(key);
            return key;
        }

        #endregion 密钥管理工具
    }
}



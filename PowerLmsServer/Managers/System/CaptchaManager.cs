using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PowerLms.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
#pragma warning disable CA1416 // 验证平台兼容性

namespace PowerLmsServer.Managers
{
    /// <summary>
    /// 验证码管理器。负责验证码的生成、验证和生命周期管理。
    /// 支持数学运算类型的验证码，采用JPEG格式存储图片文件。
    /// 验证成功后可选择是否保留验证码记录，默认删除以防重复使用。
    /// </summary>
    [OwAutoInjection(ServiceLifetime.Singleton)]
    public class CaptchaManager
    {
        #region 构造函数

        /// <summary>
        /// 构造函数。初始化验证码管理器实例。
        /// </summary>
        public CaptchaManager()
        {

        }

        #endregion 构造函数

        #region 公共方法

        /// <summary>
        /// 校验验证码。验证用户输入的答案是否正确，并管理验证码的生命周期。
        /// </summary>
        /// <param name="id">验证码标识符，通常为文件名（包含GUID）</param>
        /// <param name="answer">用户输入的答案</param>
        /// <param name="dbContext">数据库上下文，用于查询和操作验证码记录</param>
        /// <param name="reserved">true=强制保留验证码记录不删除，false=验证成功后删除记录和文件。默认false</param>
        /// <returns>true=验证成功，false=验证失败（答案错误、验证码不存在、已过期或已使用等）</returns>
        public bool Verify(string id, string answer, DbContext dbContext, bool reserved = false)
        {
            var fileName = Path.GetFileNameWithoutExtension(id); // 提取文件名，去除扩展名
            if (!Guid.TryParse(fileName, out Guid guid)) // 解析GUID格式的验证码ID
            {
                return false; // ID格式无效
            }
            // 查找匹配的验证码记录（同时匹配ID和答案）
            if (dbContext.Set<CaptchaInfo>().FirstOrDefault(c => c.Id == guid && c.Answer == answer) is not CaptchaInfo captchaInfo)
                return false; // 验证码不存在或答案错误
            if (captchaInfo.DownloadDateTime is null) return false; // 验证码尚未被下载过，无效
            if (captchaInfo.VerifyDateTime is not null) return false; // 验证码已被验证过，防止重复使用
            if (!reserved) // 非保留模式，验证成功后清理
            {
                try
                {
                    if (File.Exists(captchaInfo.FullPath)) // 删除验证码图片文件
                        File.Delete(captchaInfo.FullPath);
                }
                catch // 文件删除失败不影响验证结果，静默处理
                {
                }
                dbContext.Remove(captchaInfo); // 从数据库删除验证码记录
                dbContext.SaveChanges(); // 保存数据库变更
            }
            return true; // 验证成功
        }

        /// <summary>
        /// 生成新的验证码图片并返回答案。创建数学运算题目的验证码图片。
        /// </summary>
        /// <param name="fullFileName">存储文件的全路径名。扩展名虽然可以任意指定，但存储格式总是JPEG</param>
        /// <returns>验证码的正确答案字符串</returns>
        public string GetNew(string fullFileName)
        {
            var ans = GetNewQuestionString(out var gut); // 生成题目和答案
            var image = GetNewPic(gut); // 生成验证码图片
            image.Save(fullFileName, ImageFormat.Jpeg); // 保存为JPEG格式图片
            return ans; // 返回答案用于后续验证
        }

        #endregion 公共方法

        #region 私有方法

        /// <summary>
        /// 获取一个数学运算问答字符串。生成两个数字的加法运算题目。
        /// </summary>
        /// <param name="gut">输出参数，包含完整题目内容（如"25 + 7 = ?"）</param>
        /// <returns>运算结果的字符串形式，作为验证码答案</returns>
        static string GetNewQuestionString(out string gut)
        {
            var random = OwHelper.Random; // 获取线程安全的随机数生成器
            var left = random.Next(99); // 生成0-98的随机数作为被加数
            var right = random.Next(10); // 生成0-9的随机数作为加数
            gut = $"{left} + {right} = ?"; // 组装题目字符串
            return (left + right).ToString(); // 返回计算结果作为答案
        }

        /// <summary>
        /// 生成验证码图片。创建包含干扰元素的验证码图像，提高安全性。
        /// </summary>
        /// <param name="str">要显示在图片中的内容字符串</param>
        /// <returns>生成的验证码图片对象，失败时返回null</returns>
        static Image GetNewPic(string str)
        {
            var random = OwHelper.Random; // 获取随机数生成器
            var image = new Bitmap(12 * str.Length, 22); // 根据字符串长度计算图片宽度，高度固定22像素
            using var g = Graphics.FromImage(image); // 创建图形绘制对象
            try
            {
                g.Clear(Color.White); // 设置白色背景
                // 绘制背景干扰线，增加破解难度
                for (int i = 0; i < 25; i++)
                {
                    int x1 = random.Next(image.Width); // 随机起点X坐标
                    int x2 = random.Next(image.Width); // 随机终点X坐标
                    int y1 = random.Next(image.Height); // 随机起点Y坐标
                    int y2 = random.Next(image.Height); // 随机终点Y坐标
                    g.DrawLine(new Pen(Color.Silver), x1, y1, x2, y2); // 绘制银色干扰线
                }
                // 设置文字样式和渐变色画笔
                var font = new Font("Arial", 12, FontStyle.Bold | FontStyle.Italic); // 12号加粗斜体Arial字体
                var brush = new LinearGradientBrush(new Rectangle(0, 0, image.Width, image.Height), Color.Blue, Color.DarkRed, 1.2f, true); // 蓝色到深红色的线性渐变
                StringFormat drawFormat = new StringFormat(); // 字符串格式设置
                drawFormat.Alignment = StringAlignment.Center; // 水平居中对齐
                var rect = new Rectangle(Point.Empty, image.Size); // 创建绘制矩形区域
                rect.Inflate(-2, -1); // 缩小绘制区域，留出边距
                g.DrawString(str, font, brush, rect, drawFormat); // 绘制验证码文字
                // 添加前景噪点，进一步增加识别难度
                var pixelCount = image.Width * image.Height; // 计算总像素数
                for (int i = 0; i < pixelCount / 10; i++) // 添加10%的噪点
                {
                    int x = random.Next(image.Width); int y = random.Next(image.Height); // 随机位置
                    image.SetPixel(x, y, Color.FromArgb(random.Next())); // 设置随机颜色噪点
                }
                return image; // 返回生成的图片
            }
            catch // 异常处理，防止内存泄漏
            {
                g.Dispose(); // 手动释放图形对象
                image.Dispose(); // 手动释放图片对象
                return null; // 生成失败返回null
            }
        }

        #endregion 私有方法
    }
}
#pragma warning restore CA1416 // 验证平台兼容性

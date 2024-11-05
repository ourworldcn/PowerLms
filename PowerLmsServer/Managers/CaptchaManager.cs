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
    /// 验证码管理器。
    /// </summary>
    [OwAutoInjection(ServiceLifetime.Singleton)]
    public class CaptchaManager
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public CaptchaManager()
        {

        }

        /// <summary>
        /// 校验验证码。
        /// </summary>
        /// <param name="id"></param>
        /// <param name="answer"></param>
        /// <param name="dbContext"></param>
        /// <param name="reserved">true强制保留，不在数据库中删除，false如果有效验证则删除。</param>
        /// <returns></returns>
        public bool Verify(string id, string answer, DbContext dbContext, bool reserved = false)
        {
            var fileName = Path.GetFileNameWithoutExtension(id);
            if (!Guid.TryParse(fileName, out Guid guid))
            {
                return false;
            }
            if (dbContext.Set<CaptchaInfo>().FirstOrDefault(c => c.Id == guid && c.Answer == answer) is not CaptchaInfo captchaInfo)
                return false;
            if (captchaInfo.DownloadDateTime is null) return false;
            if (captchaInfo.VerifyDateTime is not null) return false;
            if (!reserved)
            {
                try
                {
                    if (File.Exists(captchaInfo.FullPath))
                        File.Delete(captchaInfo.FullPath);
                }
                catch
                {
                }
                dbContext.Remove(captchaInfo);
                dbContext.SaveChanges();
            }
            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fullFileName">存储文件的全路径名，扩展名虽然可以任意指定，但存储格式总是jpeg。</param>
        /// <returns>答案。</returns>
        public string GetNew(string fullFileName)
        {
            var ans = GetNewQuestionString(out var gut);
            var image = GetNewPic(gut);
            image.Save(fullFileName, ImageFormat.Jpeg);
            return ans;
        }

        /// <summary>
        /// 获取一个问答字符串。
        /// </summary>
        /// <param name="gut">内容。</param>
        /// <returns>答案。</returns>
        static string GetNewQuestionString(out string gut)
        {
            // 生成随机生成器
            var random = OwHelper.Random;
            var left = random.Next(99);
            var right = random.Next(10);
            gut = $"{left} + {right} = ?";
            return (left + right).ToString();
        }

        /// <summary>
        /// 生成一个验证图片。
        /// </summary>
        /// <param name="str">显示的内容。</param>
        /// <returns></returns>
        static Image GetNewPic(string str)
        {
            // 生成随机生成器
            var random = OwHelper.Random;
            var image = new Bitmap(12 * str.Length, 22);
            using var g = Graphics.FromImage(image);
            try
            {
                //清空图片背景色
                g.Clear(Color.White);
                // 画图片的背景噪音线
                for (int i = 0; i < 25; i++)
                {
                    int x1 = random.Next(image.Width);
                    int x2 = random.Next(image.Width);
                    int y1 = random.Next(image.Height);
                    int y2 = random.Next(image.Height);
                    g.DrawLine(new Pen(Color.Silver), x1, y1, x2, y2);
                }

                var font = new Font("Arial", 12, FontStyle.Bold | FontStyle.Italic);
                var brush = new LinearGradientBrush(new Rectangle(0, 0, image.Width, image.Height), Color.Blue, Color.DarkRed, 1.2f, true);
                // Set format of string.
                StringFormat drawFormat = new StringFormat();
                drawFormat.Alignment = StringAlignment.Center;
                //绘制区
                var rect = new Rectangle(Point.Empty, image.Size);
                rect.Inflate(-2, -1);
                g.DrawString(str, font, brush, rect, drawFormat);

                //画图片的前景噪音点
                var pixelCount = image.Width * image.Height;
                for (int i = 0; i < pixelCount / 10; i++)
                {
                    int x = random.Next(image.Width); int y = random.Next(image.Height);
                    image.SetPixel(x, y, Color.FromArgb(random.Next()));
                }
                return image;
            }
            catch //容错，避免内存泄漏
            {
                g.Dispose();
                image.Dispose();
                return null;
            }
        }
    }


}
#pragma warning restore CA1416 // 验证平台兼容性

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace WebApplication1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UploadController : ControllerBase
    {
        private readonly IHostingEnvironment _hostingEnvironment;
        public UploadController(IHostingEnvironment hostingEnvironment)
        {
            _hostingEnvironment = hostingEnvironment;
        }
        [HttpPost("[action]")]
        public async Task<IActionResult> SaveUpload([FromForm] IFormFile file)
        {
            string sizeStr = "50_50,70_70,100_100";//图片尺寸
            string[] sizeList = sizeStr.Split(",");
            string fName = file.FileName;
            string newFileName = string.Format("sra_{0}{1}", DateTime.Now.ToString("yyMMddHHmmssfffffff"), Path.GetExtension(file.FileName));
            string dirPath = Path.Combine(_hostingEnvironment.ContentRootPath, "Images", "Source");
            if (!Directory.Exists(dirPath))
                Directory.CreateDirectory(dirPath);
            string sourcePath = Path.Combine($"{dirPath}/" + newFileName);
            using (var stream = new FileStream(sourcePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }
            foreach (string size in sizeList)
            {
                string thumbDirPath = string.Format("{0}thumb{1}/", Path.Combine(_hostingEnvironment.ContentRootPath, "Images/"), size);
                if (!Directory.Exists(thumbDirPath))
                    Directory.CreateDirectory(thumbDirPath);
                string[] widthAndHeight = size.Split('_');
                GenerateThumb(sourcePath, thumbDirPath + newFileName, Convert.ToInt32(widthAndHeight[0]), Convert.ToInt32(widthAndHeight[1]), "Cut");
            }
            return Ok(new { name = file.FileName });
        }

        /// <summary>
        /// 生成缩略图
        /// </summary>
        /// <param name="imagePath">原图片路径</param>
        /// <param name="thumbPath">缩略图路径></param>
        /// <param name="width">宽</param>
        /// <param name="height">高</param>
        /// <param name="mode">模式</param>
        public void GenerateThumb(string imagePath, string thumbPath, int width, int height, string mode)
        {
            Image image = Image.FromFile(imagePath);

            string extension = imagePath.Substring(imagePath.LastIndexOf(".")).ToLower();
            ImageFormat imageFormat = null;
            switch (extension)
            {
                case ".jpg":
                case ".jpeg":
                    imageFormat = ImageFormat.Jpeg;
                    break;
                case ".bmp":
                    imageFormat = ImageFormat.Bmp;
                    break;
                case ".png":
                    imageFormat = ImageFormat.Png;
                    break;
                case ".gif":
                    imageFormat = ImageFormat.Gif;
                    break;
                default:
                    imageFormat = ImageFormat.Jpeg;
                    break;
            }

            int toWidth = width > 0 ? width : image.Width;
            int toHeight = height > 0 ? height : image.Height;

            int x = 0;
            int y = 0;
            int ow = image.Width;
            int oh = image.Height;

            switch (mode)
            {
                case "HW"://指定高宽缩放（可能变形）           
                    break;
                case "W"://指定宽，高按比例             
                    toHeight = image.Height * width / image.Width;
                    break;
                case "H"://指定高，宽按比例
                    toWidth = image.Width * height / image.Height;
                    break;
                case "Cut"://指定高宽裁减（不变形）           
                    if ((double)image.Width / (double)image.Height > (double)toWidth / (double)toHeight)
                    {
                        oh = image.Height;
                        ow = image.Height * toWidth / toHeight;
                        y = 0;
                        x = (image.Width - ow) / 2;
                    }
                    else
                    {
                        ow = image.Width;
                        oh = image.Width * height / toWidth;
                        x = 0;
                        y = (image.Height - oh) / 2;
                    }
                    break;
                default:
                    break;
            }

            //新建一个bmp
            Image bitmap = new Bitmap(toWidth, toHeight);

            //新建一个画板
            Graphics g = Graphics.FromImage(bitmap);

            //设置高质量插值法
            g.InterpolationMode = InterpolationMode.High;

            //设置高质量,低速度呈现平滑程度
            g.SmoothingMode = SmoothingMode.HighQuality;

            //清空画布并以透明背景色填充
            g.Clear(Color.Transparent);

            //在指定位置并且按指定大小绘制原图片的指定部分
            g.DrawImage(image,
                        new Rectangle(0, 0, toWidth, toHeight),
                        new Rectangle(x, y, ow, oh),
                        GraphicsUnit.Pixel);

            try
            {
                bitmap.Save(thumbPath, imageFormat);
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                if (g != null)
                    g.Dispose();
                if (bitmap != null)
                    bitmap.Dispose();
                if (image != null)
                    image.Dispose();
            }
        }
    }
}

using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Web;

namespace QuanLiDienThoaiDD.Areas.Admin
{
    // Only newly created, unpublished files may be removed by this helper.
    public static class VariantImageStorage
    {
        public const int MaxBytes = 2 * 1024 * 1024;
        public const string UrlFolder = "/images/variants/";

        public static string Save(HttpPostedFileBase upload, string directory)
        {
            var extension = Path.GetExtension(upload.FileName ?? "").ToLowerInvariant();
            var jpeg = extension == ".jpg" || extension == ".jpeg";
            if ((!jpeg && extension != ".png") || upload.ContentType != (jpeg ? "image/jpeg" : "image/png"))
                throw new InvalidDataException("Chỉ nhận ảnh JPEG (.jpg, .jpeg) hoặc PNG đúng định dạng.");
            if (upload.ContentLength <= 0 || upload.ContentLength > MaxBytes)
                throw new InvalidDataException("Ảnh phải có dữ liệu và không vượt quá 2 MiB.");

            using (var bytes = new MemoryStream())
            {
                var buffer = new byte[8192];
                int count;
                while ((count = upload.InputStream.Read(buffer, 0, Math.Min(buffer.Length, MaxBytes + 1 - (int)bytes.Length))) > 0)
                {
                    bytes.Write(buffer, 0, count);
                    if (bytes.Length > MaxBytes) throw new InvalidDataException("Ảnh không được vượt quá 2 MiB.");
                }
                if (bytes.Length == 0 || bytes.Length != upload.ContentLength)
                    throw new InvalidDataException("Dữ liệu ảnh không đầy đủ hoặc dung lượng không hợp lệ.");
                bytes.Position = 0;
                try
                {
                    using (var image = Image.FromStream(bytes, false, true))
                    {
                        if (image.RawFormat.Guid != (jpeg ? ImageFormat.Jpeg.Guid : ImageFormat.Png.Guid))
                            throw new InvalidDataException("Nội dung ảnh không khớp định dạng đã chọn.");
                        if (image.Width > 4096 || image.Height > 4096 || (long)image.Width * image.Height > 16000000)
                            throw new InvalidDataException("Ảnh tối đa 4096 pixel mỗi chiều và 16 triệu pixel.");
                        // Decode all pixels and re-encode, excluding metadata and appended executable payloads.
                        using (var clean = new Bitmap(image.Width, image.Height, PixelFormat.Format32bppArgb))
                        {
                            using (var graphics = Graphics.FromImage(clean)) graphics.DrawImage(image, 0, 0, image.Width, image.Height);
                            Directory.CreateDirectory(directory);
                            var name = Guid.NewGuid().ToString("N") + ".png";
                            var path = Path.Combine(directory, name);
                            // CreateNew never overwrites a pre-existing file, including a GUID collision.
                            var created = false;
                            try
                            {
                                using (var target = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                                {
                                    created = true;
                                    clean.Save(target, ImageFormat.Png);
                                    target.Flush();
                                }
                            }
                            catch
                            {
                                if (created) TryDiscard(UrlFolder + name, directory, ignored => false);
                                throw;
                            }
                            return UrlFolder + name;
                        }
                    }
                }
                catch (ArgumentException) { throw new InvalidDataException("Không đọc được nội dung ảnh JPEG/PNG."); }
                catch (ExternalException) { throw new InvalidDataException("Không giải mã hoặc lưu được ảnh."); }
                catch (OutOfMemoryException) { throw new InvalidDataException("Không giải mã được ảnh hoặc ảnh vượt khả năng xử lý."); }
            }
        }

        public static void TryDiscard(string url, string directory, Func<string, bool> isReferenced)
        {
            if (url == null || !Regex.IsMatch(url, @"\A/images/variants/[a-f0-9]{32}\.png\z")) return;
            try
            {
                // An uncertain database result must retain the file, never break a committed reference.
                if (isReferenced(url)) return;
                File.Delete(Path.Combine(directory, Path.GetFileName(url)));
            }
            catch (Exception)
            {
                Trace.TraceWarning("CAT-05: retained upload {0}; reference check or cleanup failed.", Path.GetFileName(url));
            }
        }
    }
}

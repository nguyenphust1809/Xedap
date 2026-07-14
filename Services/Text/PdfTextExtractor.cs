using System;
using System.IO;
using System.Text;
using UglyToad.PdfPig;

namespace Xedap.Services.Text
{
    public static class PdfTextExtractor
    {
        /// <summary>
        /// Đọc toàn bộ nội dung text từ file PDF.
        /// </summary>
        /// <param name="filePath">Đường dẫn tới file PDF.</param>
        /// <returns>Nội dung văn bản trong PDF.</returns>
        public static string Extract(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("Đường dẫn file không hợp lệ.", nameof(filePath));

            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Không tìm thấy file PDF: {filePath}");

            var sb = new StringBuilder();

            try
            {
                using var document = PdfDocument.Open(filePath);
                foreach (var page in document.GetPages())
                {
                    sb.AppendLine(page.Text);
                    sb.AppendLine("\n---------------------------\n"); // phân tách trang (tùy chọn)
                }
            }
            catch (Exception ex)
            {
                // Log hoặc xử lý lỗi tùy ý
                sb.AppendLine($"[Lỗi khi đọc file PDF: {ex.Message}]");
            }

            return sb.ToString();
        }
    }
}

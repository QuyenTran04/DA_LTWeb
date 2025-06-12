using Microsoft.AspNetCore.Mvc;

namespace YourProjectNamespace.Controllers
{
    public class PageController : Controller
    {
        public IActionResult Index(int id)
        {
            var goi = GetGoiVayById(id);
            if (goi == null) return NotFound();
            return View(goi);
        }

        private dynamic? GetGoiVayById(int id)
        {
            var danhSach = new[]
            {
                new { Id = 1, Title = "Vay nhu cầu nhà ở", Img = "/images/muanha.webp", MoTa = "Hỗ trợ mua nhà, xây dựng, cải tạo và sửa chữa với ưu đãi hấp dẫn." },
                new { Id = 2, Title = "Vay kinh doanh", Img = "/images/kinhdoanh.jpg", MoTa = "Hỗ trợ vốn mở rộng kinh doanh với thủ tục nhanh gọn, giải ngân linh hoạt." },
                new { Id = 3, Title = "Vay du lịch", Img = "/images/dulich.webp", MoTa = "Thỏa sức đi du lịch, tận hưởng cuộc sống với khoản vay linh hoạt." },
                new { Id = 4, Title = "Vay du học", Img = "/images/duhoc.webp", MoTa = "Hỗ trợ chi phí du học cho tương lai vững chắc." },
                new { Id = 5, Title = "Vay tín chấp", Img = "/images/tinchap.webp", MoTa = "Không cần tài sản thế chấp, thủ tục đơn giản." },
                new { Id = 6, Title = "Vay trả góp", Img = "/images/muanha.webp", MoTa = "Mua sắm trả góp dễ dàng, không lo tài chính." }
            };

            return danhSach.FirstOrDefault(x => x.Id == id);
        }
    }
}

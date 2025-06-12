using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VAYTIEN.Models;
using System.Linq;
using System.Threading.Tasks;

namespace VAYTIEN.Areas.Admin.Controllers
{
    // Đảm bảo chỉ có Admin mới có quyền xem toàn bộ lịch sử giao dịch
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class GiaoDichController : Controller
    {
        private readonly QlvayTienContext _context;

        public GiaoDichController(QlvayTienContext context)
        {
            _context = context;
        }

        // GET: Admin/GiaoDich
        // Hiển thị trang danh sách tất cả giao dịch
        public async Task<IActionResult> Index()
        {
            var allTransactions = await _context.GiaoDiches
                                        .OrderByDescending(g => g.NgayGd)
                                        .ThenByDescending(g => g.MaGiaoDich)
                                        .ToListAsync();

            return View(allTransactions);
        }
    }
}

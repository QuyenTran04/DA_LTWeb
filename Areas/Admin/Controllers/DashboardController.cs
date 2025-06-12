using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration; // Thêm using này
using System.Threading.Tasks;
using VAYTIEN.Models;

namespace VAYTIEN.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,NhanVien")] // Bảo vệ controller
    public class DashboardController : Controller
    {
        private readonly QlvayTienContext _context;
        private readonly IConfiguration _configuration; // 1. Khai báo IConfiguration

        // 2. Inject IConfiguration vào constructor
        public DashboardController(QlvayTienContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration; // Gán giá trị
        }

        public async Task<IActionResult> Index()
        {
            // Lấy các thống kê tổng quan
            ViewBag.TotalCustomers = await _context.KhachHangs.CountAsync();
            ViewBag.TotalContracts = await _context.HopDongVays.CountAsync();
            ViewBag.TotalPending = await _context.HopDongVays.CountAsync(h => h.TinhTrang == "Chờ phê duyệt");

            // Lấy số dư tài khoản công ty từ appsettings.json
            var companyAccountNumber = _configuration["AppSettings:CompanyBankAccountNumber"];
            if (!string.IsNullOrEmpty(companyAccountNumber))
            {
                var companyAccount = await _context.TaiKhoanNganHangs
                    .AsNoTracking()
                    .FirstOrDefaultAsync(tk => tk.SoTaiKhoan == companyAccountNumber);

                ViewBag.CompanyBalance = companyAccount?.SoDu ?? 0;
            }
            else
            {
                ViewBag.CompanyBalance = 0;
            }

            return View();
        }

        // Chức năng tìm kiếm đã được chuyển sang các controller tương ứng (ví dụ: KhachHangController)
        // để đảm bảo DashboardController chỉ làm nhiệm vụ thống kê.
    }
}

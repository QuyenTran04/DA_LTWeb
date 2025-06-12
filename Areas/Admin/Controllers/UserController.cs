using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VAYTIEN.Models;
using System.Linq;
using System.Threading.Tasks;

namespace VAYTIEN.Areas.Admin.Controllers
{
    // Đảm bảo chỉ có Admin mới có quyền truy cập chức năng này
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class UserController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public UserController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        // GET: Admin/User
        // Hiển thị trang danh sách tất cả người dùng
        public async Task<IActionResult> Index()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            // Lấy danh sách người dùng, trừ tài khoản admin đang đăng nhập
            var users = await _userManager.Users
                                        .Where(u => u.Id != currentUser.Id)
                                        .ToListAsync();
            return View(users);
        }

        // POST: Admin/User/ToggleLock/some-guid-id
        // Xử lý khi admin bấm nút Khóa/Mở khóa
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleLock(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            // Kiểm tra xem tài khoản có đang bị khóa hay không
            if (await _userManager.IsLockedOutAsync(user))
            {
                // Nếu đang bị khóa -> Mở khóa
                await _userManager.SetLockoutEndDateAsync(user, null);
                TempData["Success"] = $"Đã mở khóa tài khoản cho người dùng {user.UserName}.";
            }
            else
            {
                await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.Now.AddYears(100));
                TempData["Success"] = $"Đã khóa tài khoản của người dùng {user.UserName}.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}

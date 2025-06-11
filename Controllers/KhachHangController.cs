using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Text.Json;
using VAYTIEN.Models;

namespace VAYTIEN.Controllers
{
    [Authorize]
    public class KhachHangController : Controller
    {
        private readonly QlvayTienContext _context;

        public KhachHangController(QlvayTienContext context)
        {
            _context = context;
        }
        [Authorize(Roles = SD.Role_Customer)]
        public async Task<IActionResult> TrangThaiVay()
        {
            var email = User.Identity?.Name;
            var kh = await _context.KhachHangs.FirstOrDefaultAsync(k => k.Email == email);
            if (kh == null) return RedirectToAction("CreateStep1");

            var hopDongs = await _context.HopDongVays
                .Where(h => h.MaKh == kh.MaKh)
                .OrderByDescending(h => h.NgayVay)
                .ToListAsync();

            return View(hopDongs); // tạo View để hiển thị danh sách trạng thái vay
        }


        // Bước 1 - Form thông tin cá nhân
        public IActionResult CreateStep1()
        {
            ViewBag.DoiTuongVayList = _context.DoiTuongVays.ToList();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateStep1(KhachHang kh, IFormFile? anhFile)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.DoiTuongVayList = _context.DoiTuongVays.ToList();
                return View(kh);
            }

            if (kh.DoiTuongVayMaDoiTuongVay == null || kh.DoiTuongVayMaDoiTuongVay == 0)
            {
                ModelState.AddModelError("DoiTuongVayMaDoiTuongVay", "Vui lòng chọn đối tượng vay");
                ViewBag.DoiTuongVayList = _context.DoiTuongVays.ToList();
                return View(kh);
            }

            if (anhFile != null && anhFile.Length > 0)
            {
                var fileName = Path.GetFileName(anhFile.FileName);
                var filePath = Path.Combine("wwwroot/uploads", fileName);
                using var stream = new FileStream(filePath, FileMode.Create);
                await anhFile.CopyToAsync(stream);
                kh.AnhDinhKem = "/uploads/" + fileName;
            }

            TempData["KhachHang"] = JsonSerializer.Serialize(kh);
            return RedirectToAction("CreateStep2");
        }

        // Bước 2 - Form thông tin vay
        public IActionResult CreateStep2()
        {
            ViewBag.LoaiVayList = _context.LoaiVays.ToList();
            ViewBag.LoaiTienTeList = _context.LoaiTienTes.ToList();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateStep2(HopDongVay hopDong, TaiSanTheChap taiSan)
        {
            if (TempData["KhachHang"] == null)
                return RedirectToAction("CreateStep1");

            var khachHang = JsonSerializer.Deserialize<KhachHang>(TempData["KhachHang"]!.ToString()!);
            khachHang.Email = User.Identity?.Name;

            var existingKH = await _context.KhachHangs.FirstOrDefaultAsync(kh => kh.Email == khachHang.Email);
            if (existingKH != null)
            {
                khachHang = existingKH;
            }
            else
            {
                _context.KhachHangs.Add(khachHang);
                await _context.SaveChangesAsync();
            }

            hopDong.MaKh = khachHang.MaKh;
            hopDong.TinhTrang = "Chờ phê duyệt";

            if (!hopDong.NgayHetHan.HasValue && hopDong.NgayVay.HasValue && hopDong.KyHanThang.HasValue)
            {
                hopDong.NgayHetHan = hopDong.NgayVay.Value.AddMonths(hopDong.KyHanThang.Value);
            }

            _context.HopDongVays.Add(hopDong);
            await _context.SaveChangesAsync();

            taiSan.MaHopDong = hopDong.MaHopDong;
            _context.TaiSanTheChaps.Add(taiSan);
            await _context.SaveChangesAsync();

            if (hopDong.KyHanThang.HasValue && hopDong.SoTienVay.HasValue && hopDong.NgayVay.HasValue)
            {
                var kyHan = hopDong.KyHanThang.Value;
                var soTien = hopDong.SoTienVay.Value / kyHan;
                var ngayTra = hopDong.NgayVay.Value;

                for (int i = 1; i <= kyHan; i++)
                {
                    var lich = new LichTraNo
                    {
                        MaHopDong = hopDong.MaHopDong,
                        KyHanThu = i,
                        NgayTra = ngayTra.AddMonths(i),
                        SoTienPhaiTra = Math.Round(soTien, 0),
                        TrangThai = "Chưa trả"
                    };
                    _context.LichTraNos.Add(lich);
                }

                await _context.SaveChangesAsync();
            }

            return RedirectToAction("XacNhan");
        }

        public IActionResult XacNhan()
        {
            return View();
        }

        [Authorize]
        public async Task<IActionResult> ThongTinCaNhan()
        {
            var userEmail = User.Identity?.Name;
            if (string.IsNullOrEmpty(userEmail))
                return RedirectToAction("Login", "Account");

            var khachHang = await _context.KhachHangs.FirstOrDefaultAsync(kh => kh.Email == userEmail);
            if (khachHang == null)
                return RedirectToAction("CreateStep1");

            return View(khachHang);
        }

        // ✅ Xem lịch sử vay (chỉ hợp đồng đã duyệt)
        [Authorize(Roles = SD.Role_Customer)]
        public async Task<IActionResult> ThongTinVay()
        {
            var email = User.Identity?.Name;
            var khachHang = await _context.KhachHangs.FirstOrDefaultAsync(kh => kh.Email == email);
            if (khachHang == null) return RedirectToAction("CreateStep1");

            var hopDongs = await _context.HopDongVays
                .Where(h => h.MaKh == khachHang.MaKh && h.TinhTrang == "Đã duyệt")
                .Include(h => h.LichTraNos)
                .ToListAsync();

            var viewModel = new ThongTinVayViewModel
            {
                TongSoTienVay = hopDongs.Sum(h => h.SoTienVay ?? 0),
                HopDongs = hopDongs.Select(h => new ThongTinHopDongViewModel
                {
                    MaHopDong = h.MaHopDong,
                    SoTienVay = h.SoTienVay,
                    NgayVay = h.NgayVay,
                    NgayHetHan = h.NgayHetHan,
                    KyHanThang = h.KyHanThang,
                    LaiSuat = h.LaiSuat,
                    LichTra = h.LichTraNos.Select(l => new LichTraViewModel
                    {
                        KyHanThu = l.KyHanThu ?? 0,
                        NgayTra = l.NgayTra,
                        SoTienPhaiTra = l.SoTienPhaiTra,
                        TrangThai = l.TrangThai
                    }).OrderBy(x => x.KyHanThu).ToList()
                }).ToList()
            };

            return View(viewModel);
        }
        

    }
}

﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VAYTIEN.Models;
using VAYTIEN.Services;

namespace VAYTIEN.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class HopDongController : Controller
    {
        private readonly QlvayTienContext _context;
        private readonly PdfGenerator _pdfGenerator;
        private readonly EmailSender _emailSender;

        public HopDongController(QlvayTienContext context, PdfGenerator pdfGenerator, EmailSender emailSender)
        {
            _context = context;
            _pdfGenerator = pdfGenerator;
            _emailSender = emailSender;
        }

        public IActionResult ChoPheDuyet()
        {
            var list = _context.HopDongVays
                .Include(h => h.MaKhNavigation)
                .Where(h => h.TinhTrang == "Chờ phê duyệt")
                .ToList();

            return View(list);
        }

        public IActionResult TongHopDong()
        {
            var list = _context.HopDongVays
                .Include(h => h.MaKhNavigation)
                .Include(h => h.MaLoaiVayNavigation)
                .Include(h => h.MaLoaiTienTeNavigation)
                .Include(h => h.MaNvNavigation)
                .ToList();

            return View(list);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PheDuyet(int id)
        {
            var hd = await _context.HopDongVays
                .Include(h => h.MaKhNavigation)
                .FirstOrDefaultAsync(h => h.MaHopDong == id);
            if (hd == null) return NotFound();

            hd.TinhTrang = "Đã duyệt";
            await _context.SaveChangesAsync();

            // Tạo PDF & gửi email
            var pdfPath = _pdfGenerator.GenerateHopDongTinDungPdf(hd, hd.MaKhNavigation!);
            await _emailSender.SendEmailAsync(
                hd.MaKhNavigation!.Email!,
                "Thông báo phê duyệt hợp đồng tín dụng",
                @"
        <p>Kính gửi Quý khách <strong>" + hd.MaKhNavigation.HoTen + @"</strong>,</p>
        <p>Ngân hàng chúng tôi trân trọng thông báo: yêu cầu vay vốn của Quý khách đã được phê duyệt.</p>
        <p>Quý khách vui lòng xem chi tiết nội dung trong hợp đồng tín dụng đính kèm.</p>
        <p>Vui lòng kiểm tra kỹ thông tin và liên hệ lại Chi nhánh/Phòng giao dịch gần nhất để hoàn tất thủ tục nhận tiền vay.</p>
        <br/>
        <p>Trân trọng,</p>
        <p><strong>Sacombank</strong></p>
    ",
                pdfPath
            );


            TempData["Success"] = $"✅ Hợp đồng #{id} đã được duyệt và gửi email.";
            return RedirectToAction("ChoPheDuyet");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TuChoi(int id)
        {
            var hd = await _context.HopDongVays.FirstOrDefaultAsync(h => h.MaHopDong == id);
            if (hd == null) return NotFound();

            hd.TinhTrang = "Đã từ chối";
            await _context.SaveChangesAsync();

            TempData["Error"] = $"❌ Hợp đồng #{id} đã bị từ chối.";
            return RedirectToAction("ChoPheDuyet");
        }



    }
}

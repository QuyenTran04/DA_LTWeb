﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VAYTIEN.Models;
using System.Linq;
using System.Threading.Tasks;

namespace VAYTIEN.Areas.Admin.Controllers
{
    // Đảm bảo chỉ Admin hoặc Nhân viên mới có thể truy cập
    [Area("Admin")]
    [Authorize(Roles = "Admin,NhanVien")]
    public class LoaiVayController : Controller
    {
        private readonly QlvayTienContext _context;

        public LoaiVayController(QlvayTienContext context)
        {
            _context = context;
        }

        // GET: Admin/LoaiVay
        public async Task<IActionResult> Index()
        {
            var list = await _context.LoaiVays.ToListAsync();
            return View(list);
        }

        // GET: Admin/LoaiVay/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Admin/LoaiVay/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("TenLoaiVay,LaiSuat,KyHan,GhiChu")] LoaiVay loaiVay)
        {
            if (ModelState.IsValid)
            {
                _context.Add(loaiVay);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Tạo mới loại vay thành công!";
                return RedirectToAction(nameof(Index));
            }
            return View(loaiVay);
        }

        // GET: Admin/LoaiVay/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var loaiVay = await _context.LoaiVays.FindAsync(id);
            if (loaiVay == null)
            {
                return NotFound();
            }
            return View(loaiVay);
        }

        // POST: Admin/LoaiVay/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("MaLoaiVay,TenLoaiVay,LaiSuat,KyHan,GhiChu")] LoaiVay loaiVay)
        {
            if (id != loaiVay.MaLoaiVay)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(loaiVay);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Cập nhật loại vay thành công!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.LoaiVays.Any(e => e.MaLoaiVay == loaiVay.MaLoaiVay))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(loaiVay);
        }

        // POST: Admin/LoaiVay/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var loaiVay = await _context.LoaiVays.FindAsync(id);
            if (loaiVay == null)
            {
                TempData["Error"] = "Không tìm thấy loại vay để xóa!";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                _context.LoaiVays.Remove(loaiVay);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Xóa loại vay thành công!";
            }
            catch (DbUpdateException)
            {
                TempData["Error"] = "Không thể xóa loại vay này vì đang được sử dụng trong các hợp đồng.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}

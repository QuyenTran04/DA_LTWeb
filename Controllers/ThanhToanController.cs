
using MailKit.Search;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;
using VAYTIEN.Models;
using static Org.BouncyCastle.Crypto.Engines.SM2Engine;

public class ThanhToanController : Controller
{
    private readonly QlvayTienContext _context;
    private readonly MoMoService _momoService;
    private readonly VnpayService _vnpayService;

    public ThanhToanController(QlvayTienContext context, MoMoService momoService, VnpayService vnpayService)
    {
        _context = context;
        _momoService = momoService;
        _vnpayService = vnpayService;
    }

    // GET: /ThanhToan/ChiTiet?maHopDong=11&kyHanThu=1
    public async Task<IActionResult> ChiTiet(int maHopDong, int kyHanThu)
    {
        var hopDong = await _context.HopDongVays.FindAsync(maHopDong);
        if (hopDong == null) return NotFound();

        var kh = await _context.KhachHangs
            .FirstOrDefaultAsync(k => k.MaKh == hopDong.MaKh);
        if (kh == null) return NotFound();

        var lichTra = await _context.LichTraNos
            .FirstOrDefaultAsync(x => x.MaHopDong == maHopDong && x.KyHanThu == kyHanThu);

        var viewModel = new ThanhToanViewModel
        {
            MaHopDong = maHopDong,
            KyHan = kyHanThu,
            TenKhachHang = kh.HoTen,
            NgayTra = lichTra?.NgayTra?.ToDateTime(TimeOnly.MinValue) ?? DateTime.Today,
            SoTienPhaiTra = lichTra?.SoTienPhaiTra ?? 0,
            TrangThai = lichTra?.TrangThai ?? "Chưa trả"
        };

        return View(viewModel);
    }
    [HttpPost]
    public async Task<IActionResult> ThucHien(ThanhToanViewModel model)
    {
        var lichTra = await _context.LichTraNos
            .FirstOrDefaultAsync(x => x.MaHopDong == model.MaHopDong && x.KyHanThu == model.KyHan);

        if (lichTra == null) return NotFound();

        if (lichTra.TrangThai != "Đã trả")
        {
            if (model.PhuongThuc == "Momo")
            {

                string orderId = $"{model.MaHopDong}_{model.KyHan}_{Guid.NewGuid().ToString().Substring(0, 8)}";

                string orderInfo = $"Thanh toan HD#{model.MaHopDong} Ky#{model.KyHan}";
                string returnUrl = Url.Action("MoMoReturn", "ThanhToan", null, Request.Scheme)!;
                string notifyUrl = Url.Action("MoMoNotify", "ThanhToan", null, Request.Scheme)!;

                var payUrl = await _momoService.CreatePaymentAsync(orderId, (long)model.SoTienPhaiTra, orderInfo, returnUrl, notifyUrl);
                return Redirect(payUrl);
            }

            else if (model.PhuongThuc == "VNPAY")
            {
                // Tích hợp tương tự cho VNPAY nếu có
                return Redirect($"https://vnpay.vn/pay?amount={model.SoTienPhaiTra}&ref={model.MaHopDong}-{model.KyHan}");
            }

            // Nếu không chọn cổng nào, xử lý nội bộ (không khuyến nghị)
            lichTra.TrangThai = "Đã trả";
            lichTra.NgayTra = DateOnly.FromDateTime(DateTime.Now);
            await _context.SaveChangesAsync();
        }
        if (model.PhuongThuc == "VNPAY")
        {
            // Tạo orderId duy nhất giống như MoMo
            string orderId = $"{model.MaHopDong}_{model.KyHan}_{Guid.NewGuid().ToString().Substring(0, 8)}";
            string orderInfo = $"Thanh toan HD#{model.MaHopDong} Ky#{model.KyHan}";
            string returnUrl = Url.Action("VnpayReturn", "ThanhToan", null, Request.Scheme)!;

            var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
            var payUrl = _vnpayService.CreatePaymentUrl((long)model.SoTienPhaiTra, orderId, orderInfo, clientIp);
            return Redirect(payUrl);
        }


        return RedirectToAction("ThongTinVay", "KhachHang");
    }
    // Action MoMo chuyển về sau khi thanh toán (redirect khách)
    public async Task<IActionResult> MoMoReturn()
    {
        // Lấy các giá trị MoMo trả về (query string)
        var orderId = Request.Query["orderId"];
    var resultCode = Request.Query["resultCode"];

        if (resultCode == "0")
        {
            // Đã thanh toán thành công – update trạng thái
            var ids = orderId.ToString().Split('_');
    int maHopDong = int.Parse(ids[0]);
    int kyHan = int.Parse(ids[1]);

    var lichTra = await _context.LichTraNos
        .FirstOrDefaultAsync(x => x.MaHopDong == maHopDong && x.KyHanThu == kyHan);

            if (lichTra != null)
            {
                lichTra.TrangThai = "Đã trả";
                lichTra.NgayTra = DateOnly.FromDateTime(DateTime.Now);

                var hopDong = await _context.HopDongVays.FindAsync(maHopDong);
                if (hopDong != null && lichTra.SoTienPhaiTra.HasValue)
                {
                    hopDong.SoTienConLai = (hopDong.SoTienConLai ?? hopDong.SoTienVay) - lichTra.SoTienPhaiTra.Value;
                }

                await _context.SaveChangesAsync();
            }

            ViewBag.Message = "Thanh toán thành công!";
        }
        else
            {
                ViewBag.Message = "Thanh toán không thành công hoặc bị hủy.";
            }
        return View();
    }


    // Action nhận webhook MoMo (IPN, hệ thống gọi tự động)
    [HttpPost] 
    public async Task<IActionResult> MoMoNotify()
    {
        using var reader = new StreamReader(Request.Body, Encoding.UTF8);
        var body = await reader.ReadToEndAsync();

        dynamic notifyData = Newtonsoft.Json.JsonConvert.DeserializeObject(body);
        string orderId = notifyData.orderId;
        string resultCode = notifyData.resultCode;

        if (resultCode == "0")
        {
            var ids = orderId.ToString().Split('_');
            int maHopDong = int.Parse(ids[0]);
            int kyHan = int.Parse(ids[1]);

            var lichTra = await _context.LichTraNos
                .FirstOrDefaultAsync(x => x.MaHopDong == maHopDong && x.KyHanThu == kyHan);

            if (lichTra != null)
            {
                lichTra.TrangThai = "Đã trả";
                lichTra.NgayTra = DateOnly.FromDateTime(DateTime.Now);

                var hopDong = await _context.HopDongVays.FindAsync(maHopDong);
                if (hopDong != null && lichTra.SoTienPhaiTra.HasValue)
                {
                    hopDong.SoTienConLai = (hopDong.SoTienConLai ?? hopDong.SoTienVay) - lichTra.SoTienPhaiTra.Value;
                }

                await _context.SaveChangesAsync();
            }
        }

       
        return Ok();
    }
    public async Task<IActionResult> VnpayReturn()
    {
        var queryCollection = Request.Query;
        if (queryCollection.Count > 0 && queryCollection.ContainsKey("vnp_ResponseCode") && queryCollection["vnp_ResponseCode"] == "00")
        {
            var orderId = queryCollection["vnp_TxnRef"].ToString();

            var success = await ProcessSuccessfulPaymentAndUpdateDbAsync(orderId);
            if (success)
            {
                TempData["Success"] = "Thanh toán VNPay thành công! Dư nợ của bạn đã được cập nhật.";
            }
            else
            {
                TempData["Error"] = "Giao dịch đã được xử lý trước đó hoặc có lỗi xảy ra.";
            }
        }
        else
        {
            TempData["Error"] = "Thanh toán VNPay không thành công hoặc đã bị hủy.";
        }

        return RedirectToAction("ThongTinVay", "KhachHang");
    }


    // Hàm dùng chung để xử lý và cập nhật database
    private async Task<bool> ProcessSuccessfulPaymentAndUpdateDbAsync(string orderId)
    {
        try
        {
            var ids = orderId.Split('_');
            if (ids.Length < 2) return false;

            int maHopDong = int.Parse(ids[0]);
            int kyHan = int.Parse(ids[1]);

            var lichTra = await _context.LichTraNos
                .FirstOrDefaultAsync(x => x.MaHopDong == maHopDong && x.KyHanThu == kyHan);

            // Rất quan trọng: Kiểm tra xem đã xử lý chưa để tránh cập nhật 2 lần
            if (lichTra != null && lichTra.TrangThai != "Đã trả")
            {
                lichTra.TrangThai = "Đã trả";
                lichTra.NgayTra = DateOnly.FromDateTime(DateTime.Now);

                // --- Cập nhật số tiền còn lại ---
                var hopDong = await _context.HopDongVays.FirstOrDefaultAsync(h => h.MaHopDong == maHopDong);
                if (hopDong != null && lichTra.SoTienPhaiTra.HasValue)
                {
                    // Nếu SoTienConLai null thì mặc định bằng SoTienVay
                    if (!hopDong.SoTienConLai.HasValue && hopDong.SoTienVay.HasValue)
                    {
                        hopDong.SoTienConLai = hopDong.SoTienVay;
                    }
                    // Trừ tiền kỳ hạn vừa trả (không âm)
                    hopDong.SoTienConLai = Math.Max(0, (hopDong.SoTienConLai ?? 0) - lichTra.SoTienPhaiTra.Value);
                }

                // Lưu thay đổi trạng thái kỳ trả nợ + hợp đồng
                await _context.SaveChangesAsync();

                return true;
            }
        }
        catch
        {
            // Ghi log lỗi ở đây nếu cần
            return false;
        }

        return false;
    }

}

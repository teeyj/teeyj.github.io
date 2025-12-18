using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PuppeteerSharp;
using SkiaSharp;
using System.Net.NetworkInformation;
using ZXing;
using ZXing.QrCode;

namespace Demo.Controllers;

public class HistoryController : Controller
{
    private readonly DB db;
    private readonly Helper hp;

    public HistoryController(DB db, Helper hp)
    {
        this.db = db;
        this.hp = hp;
    }

    [Authorize(Roles = "Member, Admin")]
    public IActionResult History()
    {
        string MemberEmail = User.Identity?.Name;
        var reservations = db.Reservations.Where(r => r.MemberEmail == MemberEmail).ToList();
        List<HistoryVM> vms = new List<HistoryVM>();
        foreach (var reservation in reservations)
        {
            var reservationLines = db.ReservationLines.Where(rl => rl.ReservationId == reservation.ReservationId).ToList();
            var payment = db.Payments.FirstOrDefault(p => p.PaymentId == reservation.PaymentId);
            var vm = new HistoryVM()
            {
                ReservationId = reservation.ReservationId,
                CourseCount = reservation.CourseCount,
                Date = reservation.Date,
                Time = reservationLines.Select(l => l.Time).ToList(),
                Price = payment.Price,
                DiscountType = reservation.DiscountType,
                DiscountValue = reservation.DiscountValue,
                CourseType = reservation.CourseType,
                Total = payment.Total,
                SubTotal = reservationLines.Select(l => l.SubTotal).ToList(),
            };
            vms.Add(vm);
        }

        return View(vms);
    }

    [Authorize(Roles = "Member")]
    public async Task<IActionResult> Detail(int ReservationId)
    {
        string MemberEmail = User.Identity?.Name;
        var vm = await hp.GetReceiptDataFromDatabase(ReservationId, MemberEmail);
        if (vm == null)
        {
            return NotFound("Detail not found.");
        }

        string localIP = hp.GetLocalWirelessIPv4Address();
        int port = Request.Host.Port ?? 7023;
        string qrContent = $"https://localhost:{port}/download/{MemberEmail}/{ReservationId}";

        var writer = new ZXing.SkiaSharp.BarcodeWriter
        {
            Format = BarcodeFormat.QR_CODE,
            Options = new QrCodeEncodingOptions
            {
                Height = 200,
                Width = 200
            }
        };

        using (var skBitmap = writer.Write(qrContent))
        {
            using (var data = skBitmap.Encode(SKEncodedImageFormat.Png, 100))
            {
                vm.QrCodeImageBase64 = "data:image/png;base64," + Convert.ToBase64String(data.ToArray());
            }
        }

        return View(vm);
    }

    [HttpGet("download/{Email}/{ReservationId}")]
    public async Task<IActionResult> DownloadReceipt(string Email, int ReservationId)
    {
        var u = db.Users.Find(Email);
        hp.SignIn(u.Email, u.Role, false);
        var receiptData = await hp.GetReceiptDataFromDatabase(ReservationId, Email);
        if (receiptData == null)
        {
            return NotFound("Detail not found.");
        }

        receiptData.QrCodeImageBase64 = null;
        string htmlContent = await hp.RenderViewAsync("History/ReceiptTemplate", receiptData);

        await new BrowserFetcher().DownloadAsync();
        using var browser = await Puppeteer.LaunchAsync(new LaunchOptions { Headless = true });
        using var page = await browser.NewPageAsync();

        await page.SetContentAsync(htmlContent);

        var pdfStream = await page.PdfStreamAsync(new PdfOptions { Format = PuppeteerSharp.Media.PaperFormat.A4, PrintBackground = true });

        return File(pdfStream, "application/pdf", $"Receipt-{ReservationId}.pdf");
    }
}
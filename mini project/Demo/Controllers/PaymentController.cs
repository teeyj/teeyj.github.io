using Demo.Models;
using Demo.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Demo.Controllers;

public class PaymentController : Controller
{
    private readonly DB db;
    private readonly IEmailService _emailService;
    private readonly Helper hp;

    public PaymentController(DB db, IEmailService emailService, Helper hp)
    {
        this.db = db;
        _emailService = emailService;
        this.hp = hp;
    }

    public IActionResult Payment()
    {
        var json = TempData["Reservation"] as string;
        if (string.IsNullOrEmpty(json))
            return RedirectToAction("Index", "Product");

        var model = JsonSerializer.Deserialize<Reservation>(json);

        if (model == null)
            return RedirectToAction("Index", "Product");

        var course = db.Courses.FirstOrDefault(c => c.CourseId == model.CourseId);
        if (course == null)
            return RedirectToAction("Index", "Product");

        var json1 = TempData["TimeOnLine"] as string;
        if (string.IsNullOrEmpty(json1))
            return RedirectToAction("Index", "Product");

        var reservationLines = JsonSerializer.Deserialize<List<ReservationLine>>(json1);

        if (reservationLines == null)
            return RedirectToAction("Index", "Product");

        List<TimeOnly> timeOnLine = new List<TimeOnly>();

        foreach (var line in reservationLines)
        {
            timeOnLine.Add(line.Time);
        }
        decimal amount = timeOnLine.Count;

        var vm = new ReservationVM
        {
            Id = model.ReservationId,
            CourseId = model.CourseId,
            CourseType = model.CourseType,
            Date = model.Date,
            Time = timeOnLine,
            CourseCount = model.CourseCount,
            Price = course.Price,
            SubTotal = course.Price * model.CourseCount,
            Total = course.Price * model.CourseCount * amount,
        };

        TempData["Reservation"] = json;
        TempData["TimeOnLine"] = json1;

        var json2 = TempData["Discount"] as string;
        if (string.IsNullOrEmpty(json2))
        {
            return View(vm);
        }

        var discount = JsonSerializer.Deserialize<Discount>(json2);
        if (discount == null)
        {
            return View(vm);
        }

        vm = new ReservationVM
        {
            Id = model.ReservationId,
            CourseId = model.CourseId,
            CourseType = model.CourseType,
            Date = model.Date,
            Time = timeOnLine,
            CourseCount = model.CourseCount,
            Price = course.Price,
            SubTotal = course.Price * model.CourseCount,
            Total = course.Price * model.CourseCount * amount,
            discount = discount
        };

        if (discount != null)
        {
            TempData["Discount"] = json2;
        }

        return View(vm);
    }

    private string NextId()
    {
        string max = db.Payments.Max(p => p.PaymentId) ?? "P000";
        int n = int.Parse(max[1..]);
        return (n + 1).ToString("'P'000");
    }


    [HttpPost]
    public async Task<IActionResult> PaymentConfirmed(PaymentVM Model, decimal SubTotal, decimal Total)
    {
        if (Model.PaymentMethod == null)
        {
            TempData["Info"] = "Please select payment method";
            return RedirectToAction("PaymentMethod", new { Total = Total, SubTotal = SubTotal });
        }

        if (Model.PaymentMethod == "Card")
        {
            if (string.IsNullOrEmpty(Model.CardNumber) || !System.Text.RegularExpressions.Regex.IsMatch(Model.CardNumber, @"^\d{12}$"))
            {
                ModelState.AddModelError("CardNumber", "Credit card number must be exactly 12 digits");
            }
            if (Model.SelectBank == null)
            {
                TempData["Info"] = "Please select bank";
                return RedirectToAction("PaymentMethod", new { Total = Total, SubTotal = SubTotal });
            }
            if (Model.CardNumber == null)
            {
                TempData["Info"] = "Please enter card number";
                return RedirectToAction("PaymentMethod", new { Total = Total, SubTotal = SubTotal });
            }
        }

        if (Model.PaymentMethod == "TnG")
        {
            if (string.IsNullOrEmpty(Model.TngNumber) || !System.Text.RegularExpressions.Regex.IsMatch(Model.TngNumber, @"^\d{9,10}$"))
            {
                ModelState.AddModelError("TngNumber", "Phone number must be exactly 9 or 10 digits");
            }
            if (Model.TngNumber == null)
            {
                TempData["Info"] = "Please enter phone number";
                return RedirectToAction("PaymentMethod", new { Total = Total, SubTotal = SubTotal });
            }
        }

        if (!ModelState.IsValid)
        {
            TempData["Info"] = "";
            return RedirectToAction("PaymentMethod", new { Total = Total, SubTotal = SubTotal });
        }

        var json = TempData["Reservation"] as string;
        if (string.IsNullOrEmpty(json))
            return RedirectToAction("Index", "Product");

        string MemberEmail = User.Identity?.Name;
        var Ewallet = db.eWallets.FirstOrDefault(e => e.MemberEmail == MemberEmail);
        if (Model.PaymentMethod == "EWallet")
        {
            if (Ewallet.Balance < Total)
            {
                TempData["Info"] = "Insufficient E-wallet balance. Please top-up to processed.";
                return RedirectToAction("PaymentMethod", new { Total = Total, SubTotal = SubTotal });
            }

            decimal balance = Ewallet.Balance - Total;
            Ewallet.LastUpdated = DateTime.UtcNow;
            Ewallet.Balance = balance;
            db.SaveChanges();

            var eWalletTransaction = new EWalletTransaction
            {
                MemberEmail = MemberEmail,
                TransactionType = "Payment",
                Amount = Total,
            };
            db.eWalletTransactions.Add(eWalletTransaction);
            db.SaveChanges();
        }

        var model = JsonSerializer.Deserialize<Reservation>(json);

        var json2 = TempData["Discount"] as string;

        var payment = new Payment
        {
            PaymentId = NextId(),
            Price = SubTotal,
            Total = Total,
        };

        if (!string.IsNullOrEmpty(json2))
        {
            var discount = JsonSerializer.Deserialize<Discount>(json2);

            if (discount != null)
            {
                var discountFromDb = db.Discounts.Find(discount.DiscountId);
                discountFromDb.UsedCount += 1;
                payment.DiscountId = discountFromDb.DiscountId;
                model.DiscountValue = discountFromDb.DiscountValue;
                model.DiscountType = discountFromDb.DiscountType;
            }
        }
        model.PaymentId = payment.PaymentId;
        db.Reservations.Add(model);
        db.Payments.Add(payment);
        db.SaveChanges();

        var json1 = TempData["TimeOnLine"] as string;
        if (!string.IsNullOrEmpty(json1))
        {
            var reservationLines = JsonSerializer.Deserialize<List<ReservationLine>>(json1);

            foreach (var line in reservationLines)
            {
                var reservationLine = new ReservationLine
                {
                    ReservationId = model.ReservationId,
                    Time = line.Time,
                    SubTotal = SubTotal,
                };
                db.ReservationLines.Add(reservationLine);
            }
        }
        db.SaveChanges();

        TempData["Discount"] = null;

        return RedirectToAction("SendReceipt", new { reservationId = model.ReservationId });
    }

    public IActionResult ApplyDiscount()
    {
        return View();
    }

    public IActionResult Apply(string Code)
    {
        var model = db.Discounts.FirstOrDefault(d => d.Code == Code);

        if (model == null)
        {
            TempData["Info"] = "cannot found code";
            TempData["Discount"] = null;
            return RedirectToAction("ApplyDiscount");
        }

        if (model.IsActive == true)
        {
            if (model.UsageLimit != null)
            {
                if (model.UsageLimit > model.UsedCount)
                {
                    TempData["Discount"] = System.Text.Json.JsonSerializer.Serialize(model);
                    TempData["Info"] = "Successful";
                    return RedirectToAction("Payment", "Payment");
                }
                else
                {
                    TempData["Info"] = "This code already limit";
                    return RedirectToAction("ApplyDiscount");
                }
            }
            else
            {
                TempData["Discount"] = System.Text.Json.JsonSerializer.Serialize(model);
                TempData["Info"] = "Successful";
                return RedirectToAction("Payment", "Payment");
            }
        }
        else if (model.IsActive == false)
        {
            TempData["Info"] = "this code is not active";
            return RedirectToAction("ApplyDiscount");
        }

        return RedirectToAction("Payment", "Payment");
    }

    [HttpGet]
    public IActionResult PaymentMethod(decimal SubTotal, decimal Total)
    {
        var model = new PaymentVM
        {

        };
        ViewBag.Subtotal = SubTotal;
        ViewBag.Total = Total;
        return View(model);
    }

    [HttpGet]
    public IActionResult SendReceipt(int reservationId)
    {
        ViewBag.ReservationId = reservationId;
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> SendReceiptEmail(int reservationId, string email)
    {
        if (!string.IsNullOrEmpty(email))
        {
            var reservation = await db.Reservations.FindAsync(reservationId);
            if (reservation != null)
            {
                var reservationLine = db.ReservationLines.Where(l => l.ReservationId == reservationId);
                var payment = db.Payments.FirstOrDefault(p => p.PaymentId == reservation.PaymentId);
                List<TimeOnly> timeOnlies = new List<TimeOnly>();
                List<decimal> SubTotalOnLine = new List<decimal>();
                foreach (var line in reservationLine)
                {
                    timeOnlies.Add(line.Time);
                    SubTotalOnLine.Add(line.SubTotal);
                }

                var receiptData = new HistoryVM()
                {
                    ReservationId = reservation.ReservationId,
                    CourseType = reservation.CourseType,
                    Date = reservation.Date,
                    Time = timeOnlies,
                    CourseCount = reservation.CourseCount,
                    Price = payment.Price,
                    DiscountType = reservation.DiscountType,
                    DiscountValue = reservation.DiscountValue,
                    SubTotal = SubTotalOnLine,
                    Total = payment.Total
                };
                string subject = $"Your Booking Receipt (ID: {reservation.ReservationId})";
                string message = "Here is your e-receipt for the reservation.";
                string htmlContent = await hp.RenderViewAsync("History/ReceiptTemplate", receiptData);
                string fileName = $"Receipt-{reservationId}.pdf";

                await _emailService.SendEmailAsync(email, subject, htmlContent, fileName);
            }
        }
        return RedirectToAction("BookingComplete", new { id = reservationId });
    }

    [HttpGet]
    public IActionResult BookingComplete(int id)
    {
        TempData["Info"] = "Your booking has been successfully confirmed!";
        return RedirectToAction("Detail", "History", new { ReservationId = id });
    }
}
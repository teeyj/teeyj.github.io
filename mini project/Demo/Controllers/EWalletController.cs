using Demo.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Demo.Controllers;

[Authorize(Roles = "Member")]
public class EWalletController : Controller
{
    private readonly DB db;

    public EWalletController(DB db)
    {
        this.db = db;
    }

    [HttpGet]
    [Authorize(Roles = "Member")]
    public IActionResult TopUp(decimal? amount)
    {
        string MemberEmail = User.Identity?.Name;
        var Ewallet = db.eWallets.FirstOrDefault(e => e.MemberEmail == MemberEmail);
        if (Ewallet == null)
        {
            return NotFound();
        }

        var newEwallet = new EWalletVM
        {
            Balance = Ewallet.Balance,
        };

        var vm = new TopUpVM
        {
            EWalletVM = newEwallet,
        };

        ViewBag.Amount = amount;
        if (amount == null)
        {
            ViewBag.Amount = 0;
        }

        return View(vm);
    }

    [HttpPost]
    [Authorize(Roles = "Member")]
    public IActionResult TopUp(TopUpVM model)
    {
        if (model.PaymentVM.PaymentMethod == null)
        {
            TempData["Info"] = "Please select payment method";
            return RedirectToAction("TopUp", new { amount = model.EWalletVM.Amount });
        }

        if (model.PaymentVM.PaymentMethod == "Card")
        {
            if (string.IsNullOrEmpty(model.PaymentVM.CardNumber) || !System.Text.RegularExpressions.Regex.IsMatch(model.PaymentVM.CardNumber, @"^\d{12}$"))
            {
                ModelState.AddModelError("PaymentVM.CardNumber", "Credit card number must be exactly 12 digits");
            }
            if (model.PaymentVM.SelectBank == null)
            {
                TempData["Info"] = "Please select bank";
                return RedirectToAction("TopUp", new { amount = model.EWalletVM.Amount });
            }
            if (model.PaymentVM.CardNumber == null)
            {
                TempData["Info"] = "Please enter card number";
                return RedirectToAction("TopUp", new { amount = model.EWalletVM.Amount });
            }
        }

        if (model.PaymentVM.PaymentMethod == "TnG")
        {
            if (string.IsNullOrEmpty(model.PaymentVM.TngNumber) || !System.Text.RegularExpressions.Regex.IsMatch(model.PaymentVM.TngNumber, @"^\d{9,10}$"))
            {
                ModelState.AddModelError("PaymentVM.TngNumber", "Phone number must be exactly 9 or 10 digits");
            }
            if (model.PaymentVM.TngNumber == null)
            {
                TempData["Info"] = "Please enter phone number";
                return RedirectToAction("TopUp", new { amount = model.EWalletVM.Amount });
            }
        }

        if (!ModelState.IsValid)
        {
            TempData["Info"] = "";
            return RedirectToAction("TopUp", new { amount = model.EWalletVM.Amount });
        }

        string MemberEmail = User.Identity?.Name;
        var Ewallet = db.eWallets.FirstOrDefault(e => e.MemberEmail == MemberEmail);

        decimal balance = Ewallet.Balance + model.EWalletVM.Amount;
        Ewallet.LastUpdated = DateTime.UtcNow;
        Ewallet.Balance = balance;
        db.SaveChanges();

        var eWalletTransaction = new EWalletTransaction
        {
            MemberEmail = MemberEmail,
            TransactionType = model.EWalletVM.TransactionType,
            Amount = model.EWalletVM.Amount,
        };
        db.eWalletTransactions.Add(eWalletTransaction);
        db.SaveChanges();

        TempData["Info"] = "Top up successful!";
        return RedirectToAction("Index", "Home");
    }

    [Authorize(Roles = "Member")]
    public IActionResult TransactionHistory()
    {
        var memberEmail = User.Identity?.Name;
        var transactions = db.eWalletTransactions
                    .Where(t => t.MemberEmail == memberEmail)
                    .OrderByDescending(t => t.TransactionDate)
                    .ToList();

        return View(transactions);
    }
}

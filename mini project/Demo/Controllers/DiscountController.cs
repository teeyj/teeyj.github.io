using Demo.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
namespace Demo.Controllers;

[Authorize(Roles = "Admin")]
public class DiscountController : Controller
{
    private readonly DB db;

    public DiscountController(DB db)
    {
        this.db = db;
    }

    public IActionResult Index()
    {
        var m = db.Discounts;
        if (Request.IsAjax()) return PartialView("_Index", m);

        return View(m);
    }

    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    public IActionResult Create(DiscountVM discount)
    {
        if (ModelState.IsValid)
        {
            var D = db.Discounts.FirstOrDefault(d => d.Code == discount.Code);
            if (D != null)
            {
                TempData["Info"] = "This code already exists. Please enter a different one (without upper or lower)";
                return View(discount);
            }
            if (discount.Type == "percentage")
            {
                discount.Value = discount.Value / 100;
            }
            var d = new Discount
            {
                DiscountType = discount.Type,
                Code = discount.Code,
                DiscountValue = discount.Value,
                IsActive = true,
                UsageLimit = discount.UsageLimit,
            };
            db.Discounts.Add(d);
            db.SaveChanges();
            TempData["Info"] = "Create Discount Code Successful";

            return RedirectToAction("Index");
        }
        return View(discount);
    }

    public IActionResult Update(int Id)
    {
        var discount = db.Discounts.FirstOrDefault(x => x.DiscountId == Id);
        if (discount == null)
        {
            return NotFound();
        }

        var vm = new DiscountVM
        {
            IsActive = discount.IsActive,
            UsageLimit = discount.UsageLimit,
            Type = discount.DiscountType,
            Code = discount.Code,
            Value = discount.DiscountValue,
            UsedCount = discount.UsedCount,
        };
        TempData["discountVM"] = JsonSerializer.Serialize(discount);
        return View(vm);
    }

    [HttpPost]
    public IActionResult Update(DiscountVM discount)
    {
        var json = TempData["discountVM"] as string;
        if (string.IsNullOrEmpty(json))
            return RedirectToAction("Index");

        var discountGet = JsonSerializer.Deserialize<Discount>(json);

        if (ModelState.IsValid)
        {
            var D = db.Discounts.FirstOrDefault(d => d.Code.ToLower() == discount.Code.ToLower());
            if (discountGet != null)
            {
                var discountDetermine = db.Discounts.FirstOrDefault(d => d.DiscountId == discountGet.DiscountId);
                if (discountDetermine != null)
                {
                    if (D != null && D.DiscountId != discountDetermine.DiscountId)
                    {
                        TempData["discountVM"] = JsonSerializer.Serialize(discountGet);
                        TempData["Info"] = "This code already exists. Please enter a different one (without upper or lower)";
                        return View(discount);
                    }
                }
            }

            if (discount.Type == "percentage")
            {
                discount.Value = discount.Value / 100;
            }
            if (discount.IsReset == true)
            {
                discount.UsedCount = 0;
            }
            else if (discount.UsageLimit < discount.UsedCount)
            {
                TempData["discountVM"] = JsonSerializer.Serialize(discountGet);
                TempData["Info"] = "Please measure your limit is greater or equal than used amount";
                return View(discount);
            }
            if (discountGet != null)
            {
                var d = db.Discounts.FirstOrDefault(d => d.DiscountId == discountGet.DiscountId);
                d.DiscountType = discount.Type;
                d.Code = discount.Code;
                d.DiscountValue = discount.Value;
                d.IsActive = discount.IsActive;
                d.UsageLimit = discount.UsageLimit;
                d.UsedCount = discount.UsedCount;
                db.Discounts.Update(d);
            }
            db.SaveChanges();
            TempData["Info"] = "Update Discount Code Successful";
            TempData["discountVM"] = JsonSerializer.Serialize(discountGet);

            return RedirectToAction("Index");
        }
        TempData["discountVM"] = JsonSerializer.Serialize(discountGet);
        return View(discount);

    }

    [HttpPost]
    public IActionResult Delete(int Id)
    {
        var discount = db.Discounts.FirstOrDefault(d => d.DiscountId == Id);
        if (discount == null)
        {
            TempData["Info"] = "Discount Code Cannot No Found";
            return RedirectToAction("Index");
        }
        db.Discounts.Remove(discount);
        db.SaveChanges();
        TempData["Info"] = "Delete Discount Code Successful";

        return RedirectToAction("Index");
    }
}
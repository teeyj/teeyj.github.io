using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Demo.Controllers;

[Authorize(Roles = "Member")]
public class ReservationController : Controller
{
    private readonly DB db;

    public ReservationController(DB db)
    {
        this.db = db;
    }

    [HttpPost]
    [Authorize(Roles = "Member")]
    public IActionResult Book(string CourseType, DateOnly Date, string MemberEmail, string CourseId, int CourseCount, List<TimeOnly> Times)
    {
        if (Times == null || Times.Count == 0)
        {
            TempData["Error"] = "Please select at least one time slot.";
            return RedirectToAction("Details", "Product", new { id = CourseId });
        }

        List<Reservation> reservations = new List<Reservation>();
        List<ReservationLine> reservationLines = new List<ReservationLine>();

        foreach (var time in Times)
        {
            if (Date < DateOnly.FromDateTime(DateTime.Today) ||
                (Date == DateOnly.FromDateTime(DateTime.Today) && time < TimeOnly.FromDateTime(DateTime.Now)))
            {
                TempData["Error"] = "Some selected time slots are expired.";
                return RedirectToAction("Details", "Product", new { id = CourseId });
            }

            int alreadyBooked = db.ReservationLines
                .Where(r => r.Reservation.CourseType == CourseType && r.Reservation.Date == Date && r.Time == time)
                .Sum(r => r.Reservation.CourseCount);

            int maxCourt = 20;
            int remaining = maxCourt - alreadyBooked;

            if (remaining <= 0)
            {
                TempData["Error"] = $"Time slot {time:HH:mm} is fully booked.";
                return RedirectToAction("Details", "Product", new { id = CourseId });
            }
            else if (CourseCount > remaining)
            {
                TempData["Error"] = $"Only {remaining} court(s) left at {time:HH:mm}.";
                return RedirectToAction("Details", "Product", new { id = CourseId });
            }

            var reservationLine = new ReservationLine
            {
                Time = time,
            };

            reservationLines.Add(reservationLine);
        }

        var Course = db.Courses.FirstOrDefault(c => c.Name == CourseType);

        var reservation = new Reservation
        {
            CourseType = CourseType,
            Date = Date,
            CourseCount = CourseCount,
            MemberEmail = MemberEmail,
            CourseId = CourseId,
        };

        TempData["Reservation"] = JsonSerializer.Serialize(reservation);
        TempData["TimeOnLine"] = JsonSerializer.Serialize(reservationLines);
        return RedirectToAction("Payment", "Payment", new { id = CourseId });
    }
}
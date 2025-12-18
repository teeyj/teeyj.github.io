using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Demo.Controllers;

public class ProductController : Controller
{
    private readonly DB db;
    private readonly Helper hp;

    public ProductController(DB db, Helper hp)
    {
        this.db = db;
        this.hp = hp;
    }

    // GET: Product/Index  neww......................................................................
    public IActionResult Index(string search)
    {
        var query = db.Courses.AsQueryable();

        if (!User.IsInRole("Admin"))
        {
            query = query.Where(c => c.IsActive);
        }


        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(p =>
            p.Name.Contains(search));
        }
        var products = query.ToList();
        return View(products);
    }
    // end ........................................................................................




    // get product.checkId new ............ start ........................
    public bool CheckId(string id)
    {
        return !db.Courses.Any(c => c.CourseId == id);
    }

    private string NextId()
    {
        string max = db.Courses.Max(C => C.CourseId) ?? "C000";
        int n = int.Parse(max[1..]);
        return (n + 1).ToString("'C'000");
    }


    // GET: Product/Create
    [Authorize(Roles = "Admin")]
    public IActionResult Create()
    {
        var vm = new CourseVM
        {
            Id = NextId(),
            Price = 0.01m,
        };
        return View(vm);

    }

    // POST: Product/Create
    [Authorize(Roles = "Admin")]
    [HttpPost]
    public IActionResult Create(CourseVM vm)
    {
        if (!ModelState.IsValid("Id") && db.Courses.Any(c => c.CourseId == vm.Id))
        {
            ModelState.AddModelError("Id", "Duplicated Id.");
        }
        // end ....................................................................................
        if (vm.Photo != null)
        {
            var err = hp.ValidatePhoto(vm.Photo);
            if (!string.IsNullOrEmpty(err))
            {
                ModelState.AddModelError("Photo", err);
            }
        }

        if (vm.Photo == null)
        {
            ModelState.AddModelError("Photo", "Main photo is required");
        }

        // ✅ validate multiple（Photos）
        if (vm.Photos != null && vm.Photos.Count > 0)
        {
            foreach (var photo in vm.Photos)
            {
                if (photo != null)
                {
                    var err = hp.ValidatePhoto(photo);
                    if (!string.IsNullOrEmpty(err))
                    {
                        ModelState.AddModelError("Photos", err);
                        break; // once error , stop
                    }
                }
            }
        }

        if (!ModelState.IsValid)
        {
            return View(vm);
        }

        // ✅ save main photo
        string mainPhotoFileName = "";
        if (vm.Photo != null)
        {
            mainPhotoFileName = hp.SavePhoto(vm.Photo, "products");
        }

        var c = new Course
        {
            CourseId = vm.Id,
            Name = vm.Name,
            Price = vm.Price,
            PhotoURL = mainPhotoFileName,
            IsActive = vm.IsActive,
        };

        db.Courses.Add(c);
        db.SaveChanges();

        // ✅ save multiple photo
        if (vm.Photos != null && vm.Photos.Count > 0)
        {
            foreach (var photo in vm.Photos)
            {
                if (photo != null)
                {
                    string fileName = hp.SavePhoto(photo, "products");

                    var productPhoto = new CoursePhoto
                    {
                        Course = c,
                        ProductId = c.CourseId,
                        PhotoURL = fileName
                    };

                    db.CoursePhotos.Add(productPhoto);
                }
            }

            db.SaveChanges();
        }

        return RedirectToAction("Index");
    }



    // newwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwww
    // GET: Product/Update/{id}
    [Authorize(Roles = "Admin")]
    public IActionResult Update(string id)
    {
        var course = db.Courses
        .Include(c => c.Photos)
        .FirstOrDefault(c => c.CourseId == id);
        if (course == null) return NotFound();

        var vm = new CourseAB
        {
            Id = course.CourseId,
            Name = course.Name,
            Price = course.Price,
            PhotoURL = course.PhotoURL,
            PhotoURLs = course.Photos.Select(c => c.PhotoURL).ToList(),
            IsActive = course.IsActive,
        };
        return View(vm);
    }
    // newwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwww
    // POST: Product/Update
    [Authorize(Roles = "Admin")]
    [HttpPost]
    public IActionResult Update(CourseAB ab)
    {
        if (!ModelState.IsValid)
            return View("Update", ab);

        var course = db.Courses
            .Include(c => c.Photos)
            .FirstOrDefault(c => c.CourseId == ab.Id);

        if (course == null) return NotFound();

        // =========================
        // BASIC INFO
        // =========================
        course.Name = ab.Name;
        course.Price = ab.Price;
        course.IsActive = ab.IsActive;

        // =========================
        // MAIN PHOTO
        // =========================
        if (ab.Photo != null)
        {
            var err = hp.ValidatePhoto(ab.Photo);
            if (!string.IsNullOrEmpty(err))
            {
                ModelState.AddModelError("Photo", err);
                ab.PhotoURL = course.PhotoURL;
                ab.PhotoURLs = course.Photos.Select(p => p.PhotoURL).ToList();
                return View("Update", ab);
            }

            course.PhotoURL = hp.SavePhoto(ab.Photo, "products");
        }

        // =========================
        // DELETE SELECTED OLD PHOTOS
        // =========================
        if (ab.DeletePhotoURLs != null && ab.DeletePhotoURLs.Count > 0)
        {
            var photosToDelete = course.Photos
                .Where(p => ab.DeletePhotoURLs.Contains(p.PhotoURL))
                .ToList();

            db.CoursePhotos.RemoveRange(photosToDelete);
        }

        // =========================
        // ADD NEW MULTIPLE PHOTOS
        // =========================
        if (ab.Photos != null && ab.Photos.Count > 0)
        {
            foreach (var photo in ab.Photos)
            {
                var err = hp.ValidatePhoto(photo);
                if (!string.IsNullOrEmpty(err))
                {
                    ModelState.AddModelError("Photos", err);
                    ab.PhotoURL = course.PhotoURL;
                    ab.PhotoURLs = course.Photos.Select(p => p.PhotoURL).ToList();
                    return View("Update", ab);
                }

                string fileName = hp.SavePhoto(photo, "products");

                db.CoursePhotos.Add(new CoursePhoto
                {
                    Course = course,
                    ProductId = course.CourseId,
                    PhotoURL = fileName
                });
            }
        }

        db.SaveChanges();
        return RedirectToAction("Index");
    }

    // GET: Product/Detail/{id}?dayOffset=0
    [Authorize(Roles = "Member, Admin")]
    public IActionResult Details(string id, DateOnly? selectedDate = null)
    {
        var course = db.Courses.Find(id);
        if (course == null) return NotFound();

        var date = selectedDate ?? DateOnly.FromDateTime(DateTime.Today);
        ViewBag.SelectedDate = date;

        var times = new List<TimeOnly>
{
    new TimeOnly(9, 0),   // Slot 1
    new TimeOnly(14, 0),  // Slot 2
};
        var reservations = db.Reservations
            .Where(r => r.Date == date && r.CourseId == course.CourseId)
            .ToList();

        var reservationLines = new List<ReservationLine>();

        foreach (var reservation in reservations)
        {
            reservationLines = db.ReservationLines
            .Where(r => r.Reservation.Date == date && r.Reservation.CourseId == course.CourseId)
            .ToList();
        }

        var photoList = db.CoursePhotos
       .Where(c => c.ProductId == id)
       .Select(c => c.PhotoURL)
       .ToList();

        ViewBag.PhotoList = photoList;
        ViewBag.TimeSlots = times;
        ViewBag.Reservations = reservations;
        ViewBag.ReservationLines = reservationLines;
        TempData["Discount"] = null;

        return View(course);
    }

}
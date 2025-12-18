using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Net.Mail;

namespace Demo.Controllers;

public class AccountController : Controller
{
    private readonly DB db;
    private readonly IWebHostEnvironment en;
    private readonly Helper hp;
    private readonly RecaptchaService _recaptcha;

    public AccountController(DB db, IWebHostEnvironment en, Helper hp, RecaptchaService recaptcha)
    {
        this.db = db;
        this.en = en;
        this.hp = hp;
        this._recaptcha = recaptcha;
    }

    // GET: Account/Login
    public IActionResult Login()
    {
        HttpContext.Session.Remove("FailedAttempts");
        HttpContext.Session.Remove("LockoutEndTime");

        return View();
    }

    // POST: Account/Login
    [HttpPost]
    public IActionResult Login(LoginVM vm, string? returnURL)
    {
        // read the locked time from session
        var lockoutEndString = HttpContext.Session.GetString("LockoutEndTime");
        if (!string.IsNullOrEmpty(lockoutEndString))
        {
            var lockoutEnd = DateTime.Parse(lockoutEndString);

            // check the lock time is finished or not
            if (DateTime.Now < lockoutEnd)
            {
                ModelState.AddModelError("", "Your account is temporarily locked. Please try again later.");
                return View(vm);
            }
        }

        if (!ModelState.IsValid)
        {
            return View(vm);
        }

        var u = db.Users.Find(vm.Email);

        if (u == null || !hp.VerifyPassword(u.Hash, vm.Password))
        {
            //record the enter wrong password attempts
            int failedAttempts = HttpContext.Session.GetInt32("FailedAttempts") ?? 0;
            failedAttempts++;

            if (failedAttempts >= 3)
            {
                var lockoutTime = DateTime.Now.AddSeconds(30);
                HttpContext.Session.SetString("LockoutEndTime", lockoutTime.ToString("o")); // use standard way to save time
                HttpContext.Session.Remove("FailedAttempts"); // clear the timer
                ModelState.AddModelError("", "You have failed to enter the correct email or password 3 times. You are now locked for 30 seconds.");
            }
            else
            {
                // update the sessions fail attempt
                HttpContext.Session.SetInt32("FailedAttempts", failedAttempts);
                ModelState.AddModelError("", "Email or Password incorrect.");
            }

            return View(vm);
        }

        var recaptchaToken = Request.Form["g-recaptcha-response"];

        if (string.IsNullOrEmpty(recaptchaToken) || false)
        {
            ModelState.AddModelError("", "reCAPTCHA validation failed. Please prove you are not a robot.");
            return View(vm);
        }

        if (u == null)
        {
            return RedirectToAction("Login");
        }

        hp.SignIn(u.Email, u.Role, vm.RememberMe);

        TempData["Info"] = "Login successfully.";

        if (!string.IsNullOrEmpty(returnURL) && Url.IsLocalUrl(returnURL))
        {
            return Redirect(returnURL);
        }
        else
        {
            HttpContext.Session.Remove("FailedAttempts");
            HttpContext.Session.Remove("LockoutEndTime");
            return RedirectToAction("Index", "Home");
        }
    }

    // GET: Account/Logout
    public IActionResult Logout(string? returnURL)
    {
        TempData["Info"] = "Logout successfully.";

        hp.SignOut();

        return RedirectToAction("Index", "Home");
    }

    // GET: Account/AccessDenied
    public IActionResult AccessDenied(string? returnURL)
    {
        return View();
    }

    // ------------------------------------------------------------------------
    // Others
    // ------------------------------------------------------------------------

    // GET: Account/CheckEmail
    public bool CheckEmail(string email)
    {
        return !db.Users.Any(u => u.Email == email);
    }

    // GET: Account/Register
    public IActionResult Register()
    {
        var model = new RegisterVM();

        // if the email has validate, automatic fill in
        var verifiedEmail = HttpContext.Session.GetString("VerifiedEmail");
        if (!string.IsNullOrEmpty(verifiedEmail))
        {
            model.Email = verifiedEmail;
            model.IsVerified = true;
        }

        return View(model);
    }

    public IActionResult ResendRegisterCode()
    {
        string email = HttpContext.Session.GetString("RegisterEmail");
        if (string.IsNullOrEmpty(email))
        {
            TempData["Error"] = "Please start the register process again.";
            return RedirectToAction("Register");
        }

        var u = new User { Email = email, Name = "New User" };

        string code = new Random().Next(100000, 999999).ToString();
        HttpContext.Session.SetString("RegisterCode", code);
        HttpContext.Session.SetString("RegisterCodeExpire", DateTime.UtcNow.AddMinutes(1).ToString("o"));

        SendRegisterVerificationCodeEmail(u, code);

        TempData["Info"] = "Verification code resent.";
        return RedirectToAction("VerifyRegisterCode");
    }

    // POST: Account/Register
    [HttpPost]
    public IActionResult Register(RegisterVM vm)
    {
        var verifiedEmail = HttpContext.Session.GetString("VerifiedEmail");

        if (!ModelState.IsValid)
        {
            var newEWallet = new EWallet
            {
                MemberEmail = vm.Email,
                Balance = 0.00m,
                LastUpdated = DateTime.Now,
            };

            db.Members.Add(new()
            {
                Email = vm.Email,
                Hash = hp.HashPassword(vm.Password),
                Name = vm.Name,
                PhotoURL = hp.SavePhoto(vm.Photo, "photos"),
                eWallets = newEWallet
            });

            db.SaveChanges();

            // after register success delete Session
            HttpContext.Session.Remove("RegisterEmail");
            HttpContext.Session.Remove("RegisterCode");
            HttpContext.Session.Remove("RegisterCodeExpire");
            HttpContext.Session.Remove("VerifiedEmail");

            TempData["Info"] = "Register successfully. Please login.";
            return RedirectToAction("Login");
        }

        return RedirectToAction("SubmitRegister", "Account", new { email = vm.Email });
    }

    // GET: Account/UpdatePassword
    [Authorize]
    public IActionResult UpdatePassword()
    {
        return View();
    }

    // POST: Account/UpdatePassword
    [Authorize]
    [HttpPost]
    public IActionResult UpdatePassword(UpdatePasswordVM vm)
    {
        var u = db.Users.Find(User.Identity!.Name);
        if (u == null) return RedirectToAction("Index", "Home");

        if (!hp.VerifyPassword(u.Hash, vm.Current))
        {
            ModelState.AddModelError("Current", "Current Password not matched.");
        }

        if (ModelState.IsValid)
        {
            u.Hash = hp.HashPassword(vm.New);
            db.SaveChanges();

            TempData["Info"] = "Password updated.";
            return RedirectToAction();
        }

        return View();
    }

    // GET: Account/UpdateProfile
    [Authorize(Roles = "Member")]
    public IActionResult UpdateProfile()
    {
        var m = db.Members.Find(User.Identity!.Name);
        if (m == null) return RedirectToAction("Index", "Home");

        var vm = new UpdateProfileVM
        {
            Email = m.Email,
            Name = m.Name,
            PhotoURL = m.PhotoURL,
        };

        return View(vm);
    }

    // POST: Account/UpdateProfile
    [Authorize(Roles = "Member")]
    [HttpPost]
    public IActionResult UpdateProfile(UpdateProfileVM vm)
    {
        var m = db.Members.Find(User.Identity!.Name);
        if (m == null) return RedirectToAction("Index", "Home");

        if (vm.Photo != null)
        {
            var err = hp.ValidatePhoto(vm.Photo);
            if (err != "") ModelState.AddModelError("Photo", err);
        }

        if (ModelState.IsValid)
        {
            m.Name = vm.Name;

            if (vm.Photo != null)
            {
                hp.DeletePhoto(m.PhotoURL, "photos");
                m.PhotoURL = hp.SavePhoto(vm.Photo, "photos");
            }

            db.SaveChanges();

            TempData["Info"] = "Profile updated.";
            return RedirectToAction();
        }

        vm.Email = m.Email;
        vm.PhotoURL = m.PhotoURL;
        return View(vm);
    }

    // GET: Account/ResetPassword
    // STEP 1 - request valification code
    public IActionResult ResetPassword()
    {
        return View(new ResetPasswordVM());
    }

    [HttpPost]
    public IActionResult ResetPassword(ResetPasswordVM vm)
    {
        var u = db.Users.Find(vm.Email);
        if (u == null)
        {
            ModelState.AddModelError("Email", "Email not found.");
            return View(vm);
        }

        string code = new Random().Next(100000, 999999).ToString();


        HttpContext.Session.SetString("ResetEmail", vm.Email);
        HttpContext.Session.SetString("ResetCode", code);
        HttpContext.Session.SetString("ResetCodeExpire", DateTime.Now.AddMinutes(1).ToString());
        HttpContext.Session.Remove("VerifiedEmail"); // delete before status

        SendVerificationCodeEmail(u, code);

        return RedirectToAction("VerifyCode");
    }

    // STEP 1.5 - resend code
    public IActionResult ResendCode()
    {
        string email = HttpContext.Session.GetString("ResetEmail");
        if (string.IsNullOrEmpty(email))
        {
            TempData["Error"] = "Please start the reset process again.";
            return RedirectToAction("ResetPassword");
        }

        var u = db.Users.Find(email);
        if (u == null)
        {
            TempData["Error"] = "User not found.";
            return RedirectToAction("ResetPassword");
        }

        string code = new Random().Next(100000, 999999).ToString();
        HttpContext.Session.SetString("ResetCode", code);
        HttpContext.Session.SetString("ResetCodeExpire", DateTime.UtcNow.AddMinutes(1).ToString("o"));

        SendVerificationCodeEmail(u, code);

        TempData["Info"] = "Verification code resent.";
        return RedirectToAction("VerifyCode");
    }

    // STEP 2 - validate the varification code

    public IActionResult VerifyCode()
    {
        var email = HttpContext.Session.GetString("ResetEmail");
        var expireStr = HttpContext.Session.GetString("ResetCodeExpire");

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(expireStr))
        {
            TempData["Error"] = "Verification code expired. Please request again.";
            return RedirectToAction("ResetPassword");
        }

        // ✅ transfer the expire time to front end
        ViewBag.ExpireTime = DateTime.Parse(expireStr).ToString("yyyy-MM-ddTHH:mm:ss");

        return View(new VerifyCodeVM { Email = email });
    }

    [HttpPost]
    public IActionResult VerifyCode(VerifyCodeVM vm)
    {

        string savedCode = HttpContext.Session.GetString("ResetCode");
        string expireStr = HttpContext.Session.GetString("ResetCodeExpire");

        if (string.IsNullOrEmpty(savedCode) || string.IsNullOrEmpty(expireStr))
        {
            ModelState.AddModelError("", "Verification code expired or not found. Please request again.");
            return ViewWithResetExpireTime(vm);
        }

        if (!DateTime.TryParse(expireStr, out DateTime expire))
        {
            ModelState.AddModelError("", "Verification code invalid. Please request again.");
            return ViewWithResetExpireTime(vm);
        }

        if (DateTime.Now > expire)
        {
            ModelState.AddModelError("", "Verification code expired.");
            return ViewWithResetExpireTime(vm);
        }

        if (vm.Code != savedCode)
        {
            ModelState.AddModelError("", "Invalid verification code.");
            return ViewWithResetExpireTime(vm);
        }

        // after validate pass
        HttpContext.Session.SetString("VerifiedEmail", vm.Email);

        // jump to setNewpassword
        return RedirectToAction("SetNewPassword");
    }


    private IActionResult ViewWithResetExpireTime(VerifyCodeVM vm)
    {
        var expireTime = HttpContext.Session.GetString("ResetCodeExpire");
        if (!string.IsNullOrEmpty(expireTime))
        {
            ViewBag.ExpireTime = DateTime.Parse(expireTime).ToString("yyyy-MM-ddTHH:mm:ss");
        }
        return View("VerifyCode", vm);
    }


    // STEP 3 - set new password
    public IActionResult SetNewPassword()
    {
        if (string.IsNullOrEmpty(HttpContext.Session.GetString("VerifiedEmail")))
        {
            TempData["Error"] = "Please verify your email first.";
            return RedirectToAction("ResetPassword");
        }
        return View(new SetNewPasswordVM { Email = HttpContext.Session.GetString("VerifiedEmail") });
    }

    [HttpPost]
    public IActionResult SetNewPassword(SetNewPasswordVM vm)
    {
        if (string.IsNullOrEmpty(HttpContext.Session.GetString("VerifiedEmail")))
        {
            TempData["Error"] = "Please verify your email first.";
            return RedirectToAction("ResetPassword");
        }

        if (!ModelState.IsValid)
        {
            return View(vm);
        }

        var u = db.Users.Find(vm.Email);
        if (u == null)
        {
            TempData["Error"] = "User not found.";
            return RedirectToAction("ResetPassword");
        }

        u.Hash = hp.HashPassword(vm.NewPassword);
        db.SaveChanges();

        // delete Session
        HttpContext.Session.Remove("ResetEmail");
        HttpContext.Session.Remove("ResetCode");
        HttpContext.Session.Remove("ResetCodeExpire");
        HttpContext.Session.Remove("VerifiedEmail");

        TempData["Info"] = "Password updated. Please login.";
        return RedirectToAction("Login");
    }

    private void SendVerificationCodeEmail(User u, string code)
    {
        var mail = new MailMessage();
        mail.To.Add(new MailAddress(u.Email, u.Name));
        mail.Subject = "Password Reset Verification Code";
        mail.IsBodyHtml = true;

        mail.Body = $@"
        <p>Dear {u.Name},</p>
        <p>Your password reset verification code is:</p>
        <h1 style='color: red'>{code}</h1>
        <p>This code will expire in 1 minutes.</p>
        <p>From, 🐱 Super Admin</p>";

        hp.SendEmail(mail);
    }

    private void SendRegisterVerificationCodeEmail(User u, string code)
    {
        var mail = new MailMessage();
        mail.To.Add(new MailAddress(u.Email, u.Name));
        mail.Subject = "Register account Verification Code";
        mail.IsBodyHtml = true;

        mail.Body = $@"
        <p>Dear {u.Name},</p>
        <p>Your Register account verification code is:</p>
        <h1 style='color: red'>{code}</h1>
        <p>This code will expire in 1 minutes.</p>
        <p>From, 🐱 Super Admin</p>";

        hp.SendEmail(mail);
    }


    public class ResetPasswordVM
    {
        [Required(ErrorMessage = "Email is required.")]
        [StringLength(100)]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        public string Email { get; set; }
    }

    public class VerifyCodeVM
    {
        [Required(ErrorMessage = "Email is required.")]
        [StringLength(100)]
        [EmailAddress]
        public string Email { get; set; }

        [Required(ErrorMessage = "Verification code is required.")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "Code must be 6 digits.")]
        public string Code { get; set; }
    }

    public class SetNewPasswordVM
    {
        [Required(ErrorMessage = "Email is required.")]
        [StringLength(100)]
        [EmailAddress]
        public string Email { get; set; }

        [Required(ErrorMessage = "New password is required.")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters.")]
        public string NewPassword { get; set; }

        [Required(ErrorMessage = "Please confirm your password.")]
        [Compare("NewPassword", ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; }
    }

    // STEP 1 - send valification code for register
    [HttpPost]
    public IActionResult SendRegisterCode(RegisterVM vm)
    {
        if (db.Users.Any(u => u.Email == vm.Email))
        {
            ModelState.AddModelError("Email", "Duplicated Email.");
            return View("Register", vm);
        }

        string code = new Random().Next(100000, 999999).ToString();

        HttpContext.Session.SetString("RegisterEmail", vm.Email);
        HttpContext.Session.SetString("RegisterCode", code);
        HttpContext.Session.SetString("RegisterCodeExpire", DateTime.Now.AddMinutes(1).ToString());

        var u = new User { Email = vm.Email, Name = vm.Name ?? "New User" };
        SendRegisterVerificationCodeEmail(u, code);

        return RedirectToAction("VerifyRegisterCode");
    }

    // STEP 2 - show varification page
    public IActionResult VerifyRegisterCode()
    {
        var email = HttpContext.Session.GetString("RegisterEmail");
        var expireStr = HttpContext.Session.GetString("RegisterCodeExpire");

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(expireStr))
        {
            return RedirectToAction("Register");
        }

        ViewBag.ExpireTime = DateTime.Parse(expireStr).ToString("yyyy-MM-ddTHH:mm:ss");

        return View(new VerifyCodeVM { Email = email });
    }

    // STEP 2.5 - validate the verification code newwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwww19/9/25
    [HttpPost]
    public IActionResult VerifyRegisterCode(VerifyCodeVM vm)
    {
        string savedCode = HttpContext.Session.GetString("RegisterCode");
        string expireStr = HttpContext.Session.GetString("RegisterCodeExpire");

        if (string.IsNullOrEmpty(savedCode) || string.IsNullOrEmpty(expireStr))
        {
            ModelState.AddModelError("", "The verification code has expired, please resend");
            return ViewWithExpireTime(vm);
        }

        if (!DateTime.TryParse(expireStr, out DateTime expire))
        {
            ModelState.AddModelError("", "Invalid verification code.");
            return ViewWithExpireTime(vm);
        }

        if (DateTime.Now > expire)
        {
            ModelState.AddModelError("", "Verification code has expired");
            return ViewWithExpireTime(vm);
        }

        if (vm.Code != savedCode)
        {
            ModelState.AddModelError("", "Verification code error");
            return ViewWithExpireTime(vm);
        }


        HttpContext.Session.SetString("VerifiedEmail", vm.Email);

        TempData["Info"] = "Email verification is successful, please continue to complete the registration";
        return RedirectToAction("SubmitRegister", "Account", new { email = vm.Email });
    }

    private IActionResult ViewWithExpireTime(VerifyCodeVM vm)
    {
        var expireTime = HttpContext.Session.GetString("RegisterCodeExpire");
        if (!string.IsNullOrEmpty(expireTime))
        {
            ViewBag.ExpireTime = DateTime.Parse(expireTime).ToString("yyyy-MM-ddTHH:mm:ss");
        }
        return View("VerifyRegisterCode", vm);
    }

    //                          19/9/25,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,
    public IActionResult SubmitRegister(string email)
    {
        var model = new RegisterVM
        {
            Email = email,
            IsVerified = true,
        };
        return View(model);
    }

    [HttpGet("QRCodeUsed/{Email}")]
    public IActionResult QrCodeLogin(string Email)
    {
        var u = db.Users.Find(Email);

        if (u == null)
        {
            return RedirectToAction("Login");
        }

        hp.SignIn(u.Email, u.Role, false);

        return RedirectToAction("Index", "Home");
    }
}
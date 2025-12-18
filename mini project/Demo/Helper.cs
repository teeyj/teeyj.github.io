using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SkiaSharp;
using System.Net;
using System.Net.Mail;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security.Claims;
using System.Text.RegularExpressions;
using ZXing;
using ZXing.QrCode;

namespace Demo;

public class Helper
{
    private readonly IWebHostEnvironment en;
    private readonly IHttpContextAccessor ct;
    private readonly IConfiguration cf;
    private readonly IServiceProvider _serviceProvider;
    private readonly DB db;

    public Helper(IWebHostEnvironment en,
                  IHttpContextAccessor ct,
                  IConfiguration cf,
                  IServiceProvider serviceProvider,
                  DB db)
    {
        this.en = en;
        this.ct = ct;
        this.cf = cf;
        this._serviceProvider = serviceProvider;
        this.db = db;
    }

    // ------------------------------------------------------------------------
    // Photo Upload Helper Functions
    // ------------------------------------------------------------------------

    public string ValidatePhoto(IFormFile f)
    {
        var reType = new Regex(@"^image\/(jpeg|png)$", RegexOptions.IgnoreCase);
        var reName = new Regex(@"^.+\.(jpeg|jpg|png)$", RegexOptions.IgnoreCase);

        if (!reType.IsMatch(f.ContentType) || !reName.IsMatch(f.FileName))
        {
            return "Only JPG and PNG photo is allowed.";
        }
        else if (f.Length > 1 * 1024 * 1024)
        {
            return "Photo size cannot more than 1MB.";
        }

        return "";
    }

    public string SavePhoto(IFormFile f, string folder)
    {
        var folderPath = Path.Combine(en.WebRootPath, folder);
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        var file = Guid.NewGuid().ToString("n") + ".jpg";
        var path = Path.Combine(folderPath, file);

        var options = new ResizeOptions
        {
            Size = new Size(200, 200),
            Mode = ResizeMode.Crop,
        };

        if (f == null)
        {
            var File = Guid.NewGuid().ToString("n") + ".jpg";
            var sourcePath = Path.Combine(folderPath, "photo.jpg");
            var filePath = Path.Combine(folderPath, File);
            System.IO.File.Copy(sourcePath, filePath);
            return File;
        }

        using var stream = f.OpenReadStream();
        using var img = Image.Load(stream);
        img.Mutate(x => x.Resize(options));
        img.Save(path);

        return file;
    }

    public void DeletePhoto(string file, string folder)
    {
        file = Path.GetFileName(file);
        var path = Path.Combine(en.WebRootPath, folder, file);
        File.Delete(path);
    }



    // ------------------------------------------------------------------------
    // Security Helper Functions
    // ------------------------------------------------------------------------

    private readonly PasswordHasher<object> ph = new();

    public string HashPassword(string password)
    {
        return ph.HashPassword(0, password);
    }

    public bool VerifyPassword(string hash, string password)
    {
        return ph.VerifyHashedPassword(0, hash, password)
               == PasswordVerificationResult.Success;
    }

    public void SignIn(string email, string role, bool rememberMe)
    {
        List<Claim> claims =
        [
            new(ClaimTypes.Name, email),
            new(ClaimTypes.Role, role),
        ];

        ClaimsIdentity identity = new(claims, "Cookies");

        ClaimsPrincipal principal = new(identity);

        AuthenticationProperties properties = new()
        {
            IsPersistent = rememberMe,
        };

        ct.HttpContext!.SignInAsync(principal, properties);
    }

    public void SignOut()
    {
        ct.HttpContext!.SignOutAsync();
    }

    public string RandomPassword()
    {
        string s = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        string password = "";

        Random r = new();

        for (int i = 1; i <= 10; i++)
        {
            password += s[r.Next(s.Length)];
        }

        return password;
    }



    // ------------------------------------------------------------------------
    // Email Helper Functions
    // ------------------------------------------------------------------------

    public void SendEmail(MailMessage mail)
    {
        string user = cf["Smtp:User"] ?? "";
        string pass = cf["Smtp:Pass"] ?? "";
        string name = cf["Smtp:Name"] ?? "";
        string host = cf["Smtp:Host"] ?? "";
        int port = cf.GetValue<int>("Smtp:Port");

        mail.From = new MailAddress(user, name);

        using var smtp = new SmtpClient
        {
            Host = host,
            Port = port,
            EnableSsl = true,
            Credentials = new NetworkCredential(user, pass),
        };

        smtp.Send(mail);
    }



    // ------------------------------------------------------------------------
    // DateTime Helper Functions
    // ------------------------------------------------------------------------

    // Return January (1) to December (12)
    public SelectList GetMonthList()
    {
        var list = new List<object>();

        for (int n = 1; n <= 12; n++)
        {
            list.Add(new
            {
                Id = n,
                Name = new DateTime(1, n, 1).ToString("MMMM"),
            });
        }

        return new SelectList(list, "Id", "Name");
    }

    // Return min to max years
    public SelectList GetYearList(int min, int max, bool reverse = false)
    {
        var list = new List<int>();

        for (int n = min; n <= max; n++)
        {
            list.Add(n);
        }

        if (reverse) list.Reverse();

        return new SelectList(list);
    }



    // ------------------------------------------------------------------------
    // Shopping Cart Helper Functions
    // ------------------------------------------------------------------------

    public Dictionary<string, int> GetCart()
    {
        return ct.HttpContext!.Session.Get<Dictionary<string, int>>("Cart") ?? [];
    }

    public void SetCart(Dictionary<string, int>? dict = null)
    {
        if (dict == null)
        {
            ct.HttpContext!.Session.Remove("Cart");
        }
        else
        {
            ct.HttpContext!.Session.Set("Cart", dict);
        }
    }

    public string GetLocalWirelessIPv4Address()
    {
        foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 && ni.OperationalStatus == OperationalStatus.Up)
            {
                var ipProps = ni.GetIPProperties();

                foreach (var ip in ipProps.UnicastAddresses)
                {
                    if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        return ip.Address.ToString();
                    }
                }
            }
        }
        return "N/A";
    }

    public async Task<string> RenderViewAsync<TModel>(string viewName, TModel model)
    {
        var httpContext = new DefaultHttpContext { RequestServices = _serviceProvider };
        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        var viewEngine = _serviceProvider.GetRequiredService<ICompositeViewEngine>();
        var viewResult = viewEngine.FindView(actionContext, viewName, false);

        if (viewResult.View == null)
        {
            var searchedLocations = string.Join(", ", viewResult.SearchedLocations);
            throw new ArgumentNullException(
                $"'{viewName}' does not match any available view. The following locations were searched: {searchedLocations}");
        }

        using (var writer = new StringWriter())
        {
            var viewDataDictionary = new ViewDataDictionary<TModel>(new EmptyModelMetadataProvider(), new ModelStateDictionary())
            {
                Model = model
            };

            var viewContext = new ViewContext(
                actionContext,
                viewResult.View,
                viewDataDictionary,
                new TempDataDictionary(actionContext.HttpContext, _serviceProvider.GetRequiredService<ITempDataProvider>()),
                writer,
                new HtmlHelperOptions()
            );

            await viewResult.View.RenderAsync(viewContext);
            return writer.GetStringBuilder().ToString();
        }
    }

    public async Task<HistoryVM> GetReceiptDataFromDatabase(int ReservationId, string MemberEmail)
    {
        var reservation = db.Reservations.FirstOrDefault(r => r.MemberEmail == MemberEmail && r.ReservationId == ReservationId);
        if (reservation == null)
        {
            return null;
        }

        var reservationLine = db.ReservationLines.Where(l => l.ReservationId == ReservationId);
        List<TimeOnly> timeOnlies = new List<TimeOnly>();
        List<decimal> SubTotalOnLine = new List<decimal>();
        foreach (var line in reservationLine)
        {
            timeOnlies.Add(line.Time);
            SubTotalOnLine.Add(line.SubTotal);
        }

        var payment = db.Payments.FirstOrDefault(p => p.PaymentId == reservation.PaymentId);
        var vm = new HistoryVM()
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

        return vm;
    }

    public static string GetQrCodeImageBase64()
    {
        string localIP = GetLocalWirelessIPv4AddressStatic();
        int port = 7023;
        string qrContent = $"https://{localIP}:{port}";

        var writer = new ZXing.SkiaSharp.BarcodeWriter
        {
            Format = BarcodeFormat.QR_CODE,
            Options = new QrCodeEncodingOptions
            {
                Height = 200,
                Width = 200
            }
        };

        string QrCodeImageBase64 = "";
        using (var skBitmap = writer.Write(qrContent))
        {
            using (var data = skBitmap.Encode(SKEncodedImageFormat.Png, 100))
            {
                QrCodeImageBase64 = "data:image/png;base64," + Convert.ToBase64String(data.ToArray());
                return QrCodeImageBase64;
            }
        }
    }

    public static string GetQrCodeImageBase64Login(string? memberEmail)
    {
        string localIP = GetLocalWirelessIPv4AddressStatic();
        int port = 7023;
        string qrContent = $"https://{localIP}:{port}/QRCodeUsed/{memberEmail}";

        var writer = new ZXing.SkiaSharp.BarcodeWriter
        {
            Format = BarcodeFormat.QR_CODE,
            Options = new QrCodeEncodingOptions
            {
                Height = 200,
                Width = 200
            }
        };

        string QrCodeImageBase64 = "";
        using (var skBitmap = writer.Write(qrContent))
        {
            using (var data = skBitmap.Encode(SKEncodedImageFormat.Png, 100))
            {
                QrCodeImageBase64 = "data:image/png;base64," + Convert.ToBase64String(data.ToArray());
                return QrCodeImageBase64;
            }
        }
    }

    public static string GetLocalWirelessIPv4AddressStatic()
    {
        foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 && ni.OperationalStatus == OperationalStatus.Up)
            {
                var ipProps = ni.GetIPProperties();

                foreach (var ip in ipProps.UnicastAddresses)
                {
                    if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        return ip.Address.ToString();
                    }
                }
            }
        }
        return "N/A";
    }

}
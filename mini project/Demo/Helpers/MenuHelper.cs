using System.Security.Claims;

namespace Demo.Helpers
{
    public class MenuItem
    {
        public int Id { get; set; }
        public string Text { get; set; }
        public string Url { get; set; }
        public string Icon { get; set; }
        public string[] RequiredRoles { get; set; }
        public bool IsAnonymous { get; set; } = false;
        public bool IsAuthenticated { get; set; } = false;
        public bool IsHeader { get; set; } = false;
    }

    public static class MenuService
    {
        private static readonly List<MenuItem> AllMenuItems = new List<MenuItem>
        {
            new MenuItem { Id = 1, IsHeader = true, Text = "Shopping", IsAnonymous = true, IsAuthenticated = true },
            new MenuItem { Id = 2, Text = "Index", Url = "/", Icon = "🏠", IsAnonymous = true, IsAuthenticated = true },
            new MenuItem { Id = 3, Text = "Course", Url = "/Product/Index", Icon = "🏟", IsAnonymous = true, IsAuthenticated = true },

            new MenuItem { Id = 4, Text = "Discount", Url = "/Discount/Index", Icon = "🔖", RequiredRoles = new[] { "Admin" } },
            new MenuItem { Id = 5, IsHeader = true, Text = "Reports", RequiredRoles = new[] { "Admin" } },
            new MenuItem { Id = 6, Text = "Booking Records", Url = "/Report/BookingReport", Icon = "🧾", RequiredRoles = new[] { "Admin" } },
            new MenuItem { Id = 7, Text = "Course Usage", Url = "/Report/CourseUsage", Icon = "📊", RequiredRoles = new[] { "Admin" } },
            new MenuItem { Id = 8, Text = "Activity Trend", Url = "/Report/ActivityTrend", Icon = "📈", RequiredRoles = new[] { "Admin" } },
            new MenuItem { Id = 9, Text = "User Activity", Url = "/Report/MemberActivity", Icon = "⭐", RequiredRoles = new[] { "Admin" } },

            new MenuItem { Id = 10, Text = "My Reservation", Url = "/History/History", Icon = "📑", RequiredRoles = new[] { "Member" } },
            new MenuItem { Id = 11, Text = "My Usage Report", Url = "/Report/MyUsage", Icon = "🕒", RequiredRoles = new[] { "Member" } },
            new MenuItem { Id = 18, Text = "Update Profile", Url = "/Account/UpdateProfile", Icon = "👤", RequiredRoles = new[] { "Member" } },
            new MenuItem { Id = 19, IsHeader = true, Text = "Training Cube E-Wallet", RequiredRoles = new[] { "Member" } },
            new MenuItem { Id = 20, Text = "Top-up", Url = "/EWallet/TopUp", Icon = "💰", RequiredRoles = new[] { "Member" } },
            new MenuItem { Id = 21, Text = "E-wallet History", Url = "/EWallet/TransactionHistory", Icon = "📑", RequiredRoles = new[] { "Member" } },

            new MenuItem { Id = 12, IsHeader = true, Text = "Account", IsAnonymous = true, IsAuthenticated = true },
            new MenuItem { Id = 13, Text = "Update Password", Url = "/Account/UpdatePassword", Icon = "🔑", IsAuthenticated = true },
            new MenuItem { Id = 14, Text = "Logout", Url = "/Account/Logout", Icon = "🚪", IsAuthenticated = true },
            new MenuItem { Id = 15, Text = "Login", Url = "/Account/Login", Icon = "🔓", IsAnonymous = true },
            new MenuItem { Id = 16, Text = "Register", Url = "/Account/Register", Icon = "✍️", IsAnonymous = true },
            new MenuItem { Id = 17, Text = "Forgot Password", Url = "/Account/ResetPassword", Icon = "♻️", IsAnonymous = true },

            new MenuItem { Id = 22, Text = "About", Url = "/Home/AboutWe", Icon = "👥", IsAnonymous = true, IsAuthenticated = true }
        };

        public static List<MenuItem> GetMenuItemsForUser(ClaimsPrincipal user, string menuOrder)
        {
            var visibleItems = AllMenuItems.Where(item =>
            {
                if (!user.Identity.IsAuthenticated)
                {
                    return item.IsAnonymous;
                }

                if (item.IsAuthenticated) return true;
                if (item.RequiredRoles != null && item.RequiredRoles.Any(role => user.IsInRole(role))) return true;

                return false;
            }).ToList();

            if (!string.IsNullOrEmpty(menuOrder))
            {
                try
                {
                    var orderedIds = menuOrder.Split(',').Select(int.Parse).ToList();
                    visibleItems = visibleItems.OrderBy(item => orderedIds.IndexOf(item.Id) == -1 ? int.MaxValue : orderedIds.IndexOf(item.Id)).ToList();
                }
                catch
                {
                    return visibleItems;
                }
            }

            return visibleItems;
        }
    }
}
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Demo.Models;

#nullable disable warnings

// View Models ----------------------------------------------------------------

public class LoginVM
{
    [StringLength(100)]
    [EmailAddress]
    public string Email { get; set; }

    [StringLength(100, MinimumLength = 6)]
    [DataType(DataType.Password)]
    public string Password { get; set; }

    public bool RememberMe { get; set; }
}

public class RegisterVM
{
    [StringLength(100)]
    [EmailAddress]
    [Remote("CheckEmail", "Account", ErrorMessage = "Duplicated {0}.")]
    public string Email { get; set; }

    [StringLength(100, MinimumLength = 6)]
    [DataType(DataType.Password)]
    public string Password { get; set; }

    [StringLength(100, MinimumLength = 6)]
    [Compare("Password")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirm Password")]
    public string Confirm { get; set; }

    [StringLength(100)]
    public string Name { get; set; }

    public IFormFile Photo { get; set; }

    // new ttttt
    public bool IsVerified { get; set; } = false;

    [Required]
    public string Code { get; set; }
}

public class UpdatePasswordVM
{
    [StringLength(100, MinimumLength = 6)]
    [DataType(DataType.Password)]
    [Display(Name = "Current Password")]
    public string Current { get; set; }

    [StringLength(100, MinimumLength = 6)]
    [DataType(DataType.Password)]
    [Display(Name = "New Password")]
    public string New { get; set; }

    [StringLength(100, MinimumLength = 6)]
    [Compare("New")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirm Password")]
    public string Confirm { get; set; }
}

public class UpdateProfileVM
{
    public string? Email { get; set; }

    [StringLength(100)]
    public string Name { get; set; }

    public string? PhotoURL { get; set; }

    public IFormFile? Photo { get; set; }
}

public class ResetPasswordVM
{
    [StringLength(100)]
    [EmailAddress]
    public string Email { get; set; }
}

public class VerifyCodeVM
{
    [Required]
    [EmailAddress]
    public string Email { get; set; }

    [Required]
    [StringLength(6, MinimumLength = 4, ErrorMessage = "Code must be 4-6 digits.")]
    public string Code { get; set; }
}

public class SetNewPasswordVM
{
    [Required]
    [EmailAddress]
    public string Email { get; set; }

    [Required]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters.")]
    [DataType(DataType.Password)]
    public string NewPassword { get; set; }

    [Required]
    [Compare("NewPassword", ErrorMessage = "Passwords do not match.")]
    [DataType(DataType.Password)]
    public string ConfirmPassword { get; set; }
}

public class EmailVM
{
    [StringLength(100)]
    [EmailAddress]
    public string Email { get; set; }

    public string Subject { get; set; }

    public string Body { get; set; }

    public bool IsBodyHtml { get; set; }
}

public class CourseVM //create
{
    [Required]
    public string Id { get; set; }

    [Required]
    public string Name { get; set; }

    [Required]
    public decimal Price { get; set; }

    public IFormFile? Photo { get; set; }

    public List<IFormFile> Photos { get; set; } = new();
    public List<string>? ExistingPhotoPaths { get; set; }

    [Required]
    public bool IsActive { get; set; }
}

public class CourseAB //update
{
    [Required]
    public string Id { get; set; }

    [Required]
    public string Name { get; set; }

    [Required]
    public decimal Price { get; set; }

    public string? PhotoURL { get; set; }

    public List<string>? PhotoURLs { get; set; }
    public IFormFile? Photo { get; set; }
    public List<IFormFile>? Photos { get; set; }

    [Required]
    public bool IsActive { get; set; }

    public List<string>? DeletePhotoURLs { get; set; }

}

public class ReservationVM
{
    [Required]
    public int Id { get; set; }

    [Required]
    public string CourseId { get; set; }

    [Required]
    public string CourseType { get; set; }

    [Required]
    public DateOnly Date { get; set; }

    [Required]
    public List<TimeOnly> Time { get; set; }

    [Required]
    public int CourseCount { get; set; }

    [Required]
    public decimal Price { get; set; }

    public Discount? discount { get; set; }

    public decimal SubTotal { get; set; }

    public decimal Total { get; set; }
}

//set
public class DiscountVM
{
    [Required]
    public string Type { get; set; }

    [Required]
    public string Code { get; set; }

    [Required]
    public decimal Value { get; set; }

    [Required]
    public bool IsActive { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Usage Limit must be integer")]
    public int? UsageLimit { get; set; }

    public int UsedCount { get; set; }

    public bool IsReset { get; set; }
}

public class HistoryVM
{
    [Required]
    public int ReservationId { get; set; }

    [Required]
    public string CourseType { get; set; }

    [Required]
    public DateOnly Date { get; set; }

    [Required]
    public List<TimeOnly> Time { get; set; }

    [Required]
    public int CourseCount { get; set; }

    [Required]
    public decimal Price { get; set; }

    public string? DiscountType { get; set; }

    public decimal? DiscountValue { get; set; }

    public List<decimal> SubTotal { get; set; }

    public decimal Total { get; set; }

    public string? QrCodeImageBase64 { get; set; }
}
public class PaymentVM
{
    [Required]
    public string PaymentMethod { get; set; } // "Card" or "TnG"

    [RegularExpression(@"^\d{12}$", ErrorMessage = "Credit card number must be exactly 12 digits")]
    public string? CardNumber { get; set; }

    [RegularExpression(@"^\d{9,10}$", ErrorMessage = "Phone number must be exactly 9 or 10 digits")]
    public string? TngNumber { get; set; }

    public string? SelectBank { get; set; }
}

// 1. 更新这个 ViewModel，增加总消费金额
public class UserActivityViewModel
{
    [Display(Name = "Member Name")]
    public string MemberName { get; set; }

    [Display(Name = "Total Bookings")]
    public int TotalBookings { get; set; }

    [Display(Name = "Total Hours Booked")]
    public int TotalHours { get; set; }

    [Display(Name = "Total Spent")]
    [DisplayFormat(DataFormatString = "{0:C}")]
    public decimal TotalSpent { get; set; }
}

public class UserActivityPageVM
{
    public List<UserActivityViewModel> Activities { get; set; }

    public string Period { get; set; }
    public DateOnly? StartDateInput { get; set; }
    public DateOnly? EndDateInput { get; set; }
    public string MemberSearchFilter { get; set; }
    public string ReportTitle { get; set; }

    public string SortField { get; set; }
    public string SortDirection { get; set; }

    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
}

public class PeakTimeViewModel
{
    public string TimeSlot { get; set; }
    public int BookingCount { get; set; }
}

public class CourseUsageViewModel
{
    // 用于下方表格的数据
    public string CourseTitle { get; set; }
    public int TotalBookings { get; set; }
    public decimal Revenue { get; set; }
}

public class CourseUsagePageVM
{
    // 1. 顶部统计卡片数据
    public int GlobalTotalBookings { get; set; }
    public double AvgBookingsPerCourse { get; set; }
    public decimal GlobalTotalRevenue { get; set; }

    // 2. 图表数据 (Arrays for Chart.js)
    public string[] ChartLabels { get; set; } // 课程名字
    public int[] ChartValues { get; set; }    // 预订数量

    // 3. 底部表格数据列表
    public List<CourseUsageViewModel> UsageStats { get; set; }
}

public class ActivityTrendPageVM
{
    public int TotalBookings { get; set; }
    public decimal TotalRevenue { get; set; }
    public double AvgBookingsPerDay { get; set; }

    public string PeakDayDate { get; set; }
    public int PeakDayCount { get; set; }

    public string[] ChartLabels { get; set; } 
    public int[] BookingData { get; set; }
    public decimal[] RevenueData { get; set; } 
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}

// 在 Demo.Models 命名空间下添加

// 1. 左侧列表：单个会员的摘要信息
public class MemberSummaryViewModel
{
    public string Email { get; set; } // 用 Email 作为 ID
    public string Name { get; set; }
    public string PhotoURL { get; set; }
    public int TotalBookings { get; set; }
    public decimal TotalSpent { get; set; }
}

public class MemberBookingHistoryVM
{
    public int BookingId { get; set; }
    public string CourseTitle { get; set; }
    public string ClassDate { get; set; } 
    public string Status { get; set; } 
    public decimal Amount { get; set; }
}

public class MemberActivityPageVM
{
    public string SearchQuery { get; set; }
    public List<MemberSummaryViewModel> MembersList { get; set; }
    public MemberSummaryViewModel SelectedMemberInfo { get; set; }
    public List<MemberBookingHistoryVM> BookingHistory { get; set; }
    public string[] ChartLabels { get; set; } 
    public int[] ChartValues { get; set; }
}
public class CourseBreakdownVM
{
    public string CourseName { get; set; }
    public int Hours { get; set; }    
    public int Sessions { get; set; } 
}
public class MyUsagePageVM
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int TotalHours { get; set; }
    public int TotalCourses { get; set; }
    public int TotalSessions { get; set; }

    public string[] ChartLabels { get; set; } 
    public int[] ChartValues { get; set; }   

    public List<CourseBreakdownVM> CourseBreakdown { get; set; }
}
public class BookingRecordViewModel
{
    [Display(Name = "Booking ID")]
    public int BookingId { get; set; }

    [Display(Name = "Course Name")]
    public string CourseName { get; set; }

    [Display(Name = "Member Name")]
    public string MemberName { get; set; }

    [Display(Name = "Date")]
    [DataType(DataType.Date)]
    public DateTime BookingDate { get; set; }

    //New field to show specific time slots
    [Display(Name = "Time Slots")]
    public string TimeSlots { get; set; }

    [Display(Name = "Total Paid")]
    [DisplayFormat(DataFormatString = "{0:C}")]
    public decimal Total { get; set; }

    [Display(Name = "Discount")]
    public string DiscountInfo { get; set; }
}

public class BookingReportPageVM
{
    public List<BookingRecordViewModel> Bookings { get; set; }

    public string CourseTypeFilter { get; set; }
    public string MemberSearchFilter { get; set; }
    public DateTime? StartDateFilter { get; set; }
    public DateTime? EndDateFilter { get; set; }

    public List<string> AllCourseTypes { get; set; }

    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
    public string SortField { get; set; }
    public string SortDirection { get; set; }
}

public class DateTrendViewModel
{
    public string DateLabel { get; set; }

    public int BookingCount { get; set; }
}

public class EWalletVM
{
    [Required]
    [Precision(10, 2)]
    public decimal Balance { get; set; }
    [Required]
    public string TransactionType { get; set; }
    [Required]
    [Precision(10, 2)]
    public decimal Amount { get; set; }
    [Required]
    public DateTime TransactionDate { get; set; }
}

public class TopUpVM
{
    public PaymentVM PaymentVM { get; set; }
    public EWalletVM EWalletVM { get; set; }
}
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ClosedXML.Excel;


namespace Demo.Controllers
{
    [Authorize]
    public class ReportController : Controller
    {
        private readonly DB db;

        public ReportController(DB db)
        {
            this.db = db;
        }

        // ==========================================
        // Function: Booking Report (Course Booking Records)
        // ==========================================
        [Authorize(Roles = "Admin")]
        public IActionResult BookingReport(
            string courseTypeFilter, // 以前是 courtTypeFilter
            string memberSearchFilter,
            DateTime? startDateFilter,
            DateTime? endDateFilter,
            string sortField = "BookingDate",
            string sortDirection = "desc",
            int page = 1)
        {
            // 1. 基础查询：Reservation + Member + Payment (为了获取 Total)
            // 我们必须 Join Payment 表，因为 Reservation 表里没有 Total 字段
            var query = from r in db.Reservations.Include(m => m.Member)
                        join p in db.Payments on r.PaymentId equals p.PaymentId
                        select new
                        {
                            Reservation = r,
                            MemberName = r.Member.Name,
                            Total = p.Total
                        };

            // 2. 筛选 (Filtering)
            if (startDateFilter.HasValue)
            {
                var startDate = DateOnly.FromDateTime(startDateFilter.Value);
                query = query.Where(x => x.Reservation.Date >= startDate);
            }
            if (endDateFilter.HasValue)
            {
                var endDate = DateOnly.FromDateTime(endDateFilter.Value);
                query = query.Where(x => x.Reservation.Date <= endDate);
            }

            // 按课程类型筛选
            if (!string.IsNullOrEmpty(courseTypeFilter) && courseTypeFilter != "All")
            {
                query = query.Where(x => x.Reservation.CourseType == courseTypeFilter);
            }

            // 按会员筛选
            if (!string.IsNullOrEmpty(memberSearchFilter))
            {
                query = query.Where(x => x.MemberName.Contains(memberSearchFilter) || x.Reservation.MemberEmail.Contains(memberSearchFilter));
            }

            // 3. 排序 (Sorting)
            switch (sortField)
            {
                case "CourseName":
                    query = sortDirection == "asc" ? query.OrderBy(x => x.Reservation.CourseType) : query.OrderByDescending(x => x.Reservation.CourseType);
                    break;
                case "MemberName":
                    query = sortDirection == "asc" ? query.OrderBy(x => x.MemberName) : query.OrderByDescending(x => x.MemberName);
                    break;
                case "Total":
                    query = sortDirection == "asc" ? query.OrderBy(x => x.Total) : query.OrderByDescending(x => x.Total);
                    break;
                case "BookingId":
                    query = sortDirection == "asc" ? query.OrderBy(x => x.Reservation.ReservationId) : query.OrderByDescending(x => x.Reservation.ReservationId);
                    break;
                case "BookingDate":
                default:
                    query = sortDirection == "asc" ? query.OrderBy(x => x.Reservation.Date) : query.OrderByDescending(x => x.Reservation.Date);
                    break;
            }

            // 4. 分页 (Pagination)
            const int PageSize = 10;
            int totalRecords = query.Count();
            int totalPages = (int)Math.Ceiling(totalRecords / (double)PageSize);
            if (page < 1) page = 1;
            if (page > totalPages && totalPages > 0) page = totalPages;

            var paginatedData = query
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToList();

            // 5. 数据转换 (Mapping)
            // 需要再次查询 ReservationLines 来获取时间段
            var records = paginatedData.Select(item => new BookingRecordViewModel
            {
                BookingId = item.Reservation.ReservationId,
                CourseName = item.Reservation.CourseType, // 对应数据库的 CourseType
                MemberName = item.MemberName,
                BookingDate = item.Reservation.Date.ToDateTime(TimeOnly.MinValue),
                Total = item.Total,
                // 获取具体的时间段
                TimeSlots = string.Join(", ", db.ReservationLines
                                                .Where(rl => rl.ReservationId == item.Reservation.ReservationId)
                                                .Select(rl => rl.Time.ToString("HH:mm"))
                                                .ToList())
            }).ToList();

            // 6. 准备 View Model 返回给前端
            var pageViewModel = new BookingReportPageVM
            {
                Bookings = records,
                // 这里把 Products 改为了 Courses，确保你的 DB Context 里有 Courses 这个 DbSet
                AllCourseTypes = db.Courses.Select(c => c.Name).Distinct().ToList(),
                CourseTypeFilter = courseTypeFilter,
                MemberSearchFilter = memberSearchFilter,
                StartDateFilter = startDateFilter,
                EndDateFilter = endDateFilter,
                CurrentPage = page,
                TotalPages = totalPages,
                SortField = sortField,
                SortDirection = sortDirection
            };

            return View(pageViewModel);
        }

        // ==========================================
        // Excel Export Function
        // ==========================================
        [Authorize(Roles = "Admin")]
        public IActionResult ExportBookingReportToExcel(string courseTypeFilter, string memberSearchFilter, DateTime? startDateFilter, DateTime? endDateFilter)
        {
            // 逻辑与上面相同，只是没有分页
            var query = from r in db.Reservations.Include(m => m.Member)
                        join p in db.Payments on r.PaymentId equals p.PaymentId
                        select new { Reservation = r, Total = p.Total, MemberName = r.Member.Name };

            if (startDateFilter.HasValue)
            {
                var startDate = DateOnly.FromDateTime(startDateFilter.Value);
                query = query.Where(x => x.Reservation.Date >= startDate);
            }
            if (endDateFilter.HasValue)
            {
                var endDate = DateOnly.FromDateTime(endDateFilter.Value);
                query = query.Where(x => x.Reservation.Date <= endDate);
            }
            if (!string.IsNullOrEmpty(courseTypeFilter) && courseTypeFilter != "All")
            {
                query = query.Where(x => x.Reservation.CourseType == courseTypeFilter);
            }
            if (!string.IsNullOrEmpty(memberSearchFilter))
            {
                query = query.Where(x => x.MemberName.Contains(memberSearchFilter) || x.Reservation.MemberEmail.Contains(memberSearchFilter));
            }

            var allRecords = query.OrderByDescending(x => x.Reservation.Date).ToList()
                .Select(x => new BookingRecordViewModel
                {
                    BookingId = x.Reservation.ReservationId,
                    CourseName = x.Reservation.CourseType,
                    MemberName = x.MemberName,
                    BookingDate = x.Reservation.Date.ToDateTime(TimeOnly.MinValue),
                    Total = x.Total,
                    TimeSlots = string.Join(", ", db.ReservationLines
                                                    .Where(rl => rl.ReservationId == x.Reservation.ReservationId)
                                                    .Select(rl => rl.Time.ToString("HH:mm"))
                                                    .ToList()),
                    // 假设 DiscountType 和 Value 在 Reservation 表里
                    DiscountInfo = x.Reservation.DiscountType != null ? $"{x.Reservation.DiscountType} ({x.Reservation.DiscountValue:N2})" : "None"
                }).ToList();

            var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Booking Records");

            worksheet.Cell(1, 1).Value = "Booking ID";
            worksheet.Cell(1, 2).Value = "Course Name";
            worksheet.Cell(1, 3).Value = "Member Name";
            worksheet.Cell(1, 4).Value = "Booking Date";
            worksheet.Cell(1, 5).Value = "Time Slots";
            worksheet.Cell(1, 6).Value = "Total Paid";
            worksheet.Cell(1, 7).Value = "Discount";

            worksheet.Row(1).Style.Font.Bold = true;
            worksheet.Row(1).Style.Fill.BackgroundColor = XLColor.LightGray;

            int currentRow = 2;
            foreach (var record in allRecords)
            {
                worksheet.Cell(currentRow, 1).Value = record.BookingId;
                worksheet.Cell(currentRow, 2).Value = record.CourseName;
                worksheet.Cell(currentRow, 3).Value = record.MemberName;
                worksheet.Cell(currentRow, 4).Value = record.BookingDate;
                worksheet.Cell(currentRow, 5).Value = record.TimeSlots;
                worksheet.Cell(currentRow, 6).Value = record.Total;
                worksheet.Cell(currentRow, 7).Value = record.DiscountInfo;
                currentRow++;
            }

            worksheet.Columns().AdjustToContents();

            using (var stream = new MemoryStream())
            {
                workbook.SaveAs(stream);
                return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"BookingReport_{DateTime.Now:yyyyMMdd}.xlsx");
            }
        }
        // 在 ReportController 类中添加以下 Action

        [Authorize(Roles = "Admin")] // 只有管理员能看这种统计
        public IActionResult CourseUsage()
        {
            // 1. 获取所有预订并关联支付表 (为了算钱)
            var query = from r in db.Reservations
                        join p in db.Payments on r.PaymentId equals p.PaymentId
                        select new { CourseName = r.CourseType, Total = p.Total };

            var rawData = query.ToList();

            // 2. 按课程分组统计 (表格数据)
            var stats = rawData
                .GroupBy(x => x.CourseName)
                .Select(g => new CourseUsageViewModel
                {
                    CourseTitle = g.Key,
                    TotalBookings = g.Count(),
                    Revenue = g.Sum(x => x.Total)
                })
                .OrderByDescending(x => x.TotalBookings) // 默认按预订数排序
                .ToList();

            // 3. 计算顶部卡片的全局数据
            int globalBookings = stats.Sum(x => x.TotalBookings);
            decimal globalRevenue = stats.Sum(x => x.Revenue);
            double avgBookings = stats.Any() ? Math.Round((double)globalBookings / stats.Count, 1) : 0;

            // 4. 准备 View Model
            var vm = new CourseUsagePageVM
            {
                GlobalTotalBookings = globalBookings,
                GlobalTotalRevenue = globalRevenue,
                AvgBookingsPerCourse = avgBookings,
                UsageStats = stats,

                // 准备图表数据 (X轴名字，Y轴数值)
                ChartLabels = stats.Select(x => x.CourseTitle).ToArray(),
                ChartValues = stats.Select(x => x.TotalBookings).ToArray()
            };

            return View(vm);
        }
        // 在 ReportController 中添加

        [Authorize(Roles = "Admin")]
        public IActionResult ActivityTrend(DateTime? startDate, DateTime? endDate)
        {
            // 1. 设置默认日期 (如果没有选，默认显示最近 7 天，或者本月)
            // 这里的逻辑模仿了 React 代码里的 "Activity Trend"
            var today = DateOnly.FromDateTime(DateTime.Today);

            // 如果用户没选日期，默认显示最近 7 天
            var start = startDate.HasValue ? DateOnly.FromDateTime(startDate.Value) : today.AddDays(-6);
            var end = endDate.HasValue ? DateOnly.FromDateTime(endDate.Value) : today;

            // 2. 查询数据 (关联 Payment 算钱)
            var query = from r in db.Reservations
                        join p in db.Payments on r.PaymentId equals p.PaymentId
                        where r.Date >= start && r.Date <= end
                        select new { Date = r.Date, Total = p.Total };

            var rawData = query.ToList();

            // 3. 按日期分组统计
            // 注意：为了图表连续，我们最好填补那些没有预订的日期为 0
            var dailyStats = new List<dynamic>();

            for (var date = start; date <= end; date = date.AddDays(1))
            {
                var dayData = rawData.Where(x => x.Date == date).ToList();
                dailyStats.Add(new
                {
                    Date = date,
                    Bookings = dayData.Count,
                    Revenue = dayData.Sum(x => x.Total)
                });
            }

            // 4. 计算统计卡片数据
            int totalBookings = rawData.Count;
            decimal totalRevenue = rawData.Sum(x => x.Total);
            int daysCount = (end.DayNumber - start.DayNumber) + 1;
            double avgBookings = daysCount > 0 ? Math.Round((double)totalBookings / daysCount, 1) : 0;

            // 找高峰日
            var peakDay = dailyStats.OrderByDescending(x => x.Bookings).FirstOrDefault();

            // 5. 准备 ViewModel
            var vm = new ActivityTrendPageVM
            {
                TotalBookings = totalBookings,
                TotalRevenue = totalRevenue,
                AvgBookingsPerDay = avgBookings,

                PeakDayDate = peakDay?.Date.ToString("dd/MM") ?? "-",
                PeakDayCount = peakDay?.Bookings ?? 0,

                // 图表数据
                ChartLabels = dailyStats.Select(x => ((DateOnly)x.Date).ToString("dd/MM")).ToArray(),
                BookingData = dailyStats.Select(x => (int)x.Bookings).ToArray(),
                RevenueData = dailyStats.Select(x => (decimal)x.Revenue).ToArray(),

                // 回填筛选器
                StartDate = start.ToDateTime(TimeOnly.MinValue),
                EndDate = end.ToDateTime(TimeOnly.MinValue)
            };

            return View(vm);
        }
        // 在 ReportController 类中添加

        [Authorize(Roles = "Admin")]
        public IActionResult MemberActivity(string searchQuery, string memberEmail)
        {
            // 1. 获取所有会员的基础数据 (左侧列表用)
            // 我们需要 Join Reservations 和 Payments 来计算总数
            var membersQuery = db.Members.AsQueryable();

            if (!string.IsNullOrEmpty(searchQuery))
            {
                membersQuery = membersQuery.Where(m => m.Name.Contains(searchQuery) || m.Email.Contains(searchQuery));
            }

            // 先查出会员列表
            var members = membersQuery.Select(m => new MemberSummaryViewModel
            {
                Email = m.Email,
                Name = m.Name,
                PhotoURL = m.PhotoURL,
                // 子查询计算总预订数
                TotalBookings = db.Reservations.Count(r => r.MemberEmail == m.Email),
                // 子查询计算总消费 (关联 Payment)
                TotalSpent = (from r in db.Reservations
                              join p in db.Payments on r.PaymentId equals p.PaymentId
                              where r.MemberEmail == m.Email
                              select p.Total).Sum()
            }).ToList();

            // 2. 准备 ViewModel
            var vm = new MemberActivityPageVM
            {
                SearchQuery = searchQuery,
                MembersList = members,
                ChartLabels = new string[] { }, // 初始化空数组防止报错
                ChartValues = new int[] { }
            };

            // 3. 如果用户选择了某个会员 (点击了列表)
            if (!string.IsNullOrEmpty(memberEmail))
            {
                // 获取选中会员的基本信息
                vm.SelectedMemberInfo = members.FirstOrDefault(m => m.Email == memberEmail);

                if (vm.SelectedMemberInfo != null)
                {
                    // A. 获取预订历史
                    var historyQuery = from r in db.Reservations
                                       join p in db.Payments on r.PaymentId equals p.PaymentId
                                       where r.MemberEmail == memberEmail
                                       orderby r.Date descending
                                       select new MemberBookingHistoryVM
                                       {
                                           BookingId = r.ReservationId,
                                           CourseTitle = r.CourseType, // 或者是 r.Course.Name 如果你有关联
                                           ClassDate = r.Date.ToString("yyyy-MM-dd"),
                                           Amount = p.Total,
                                           Status = "Completed"
                                       };

                    vm.BookingHistory = historyQuery.ToList();

                    // B. 获取图表数据 (最近 6 个月)
                    // 这是一个简单的按月分组统计
                    var sixMonthsAgo = DateOnly.FromDateTime(DateTime.Today.AddMonths(-5));
                    var startOfSixMonths = new DateOnly(sixMonthsAgo.Year, sixMonthsAgo.Month, 1);

                    var chartData = db.Reservations
                        .Where(r => r.MemberEmail == memberEmail && r.Date >= startOfSixMonths)
                        .GroupBy(r => new { r.Date.Year, r.Date.Month })
                        .Select(g => new
                        {
                            Year = g.Key.Year,
                            Month = g.Key.Month,
                            Count = g.Count()
                        })
                        .ToList();

                    // 补全数据 (确保最近6个月每个月都有数据，没有则是0)
                    var labels = new List<string>();
                    var values = new List<int>();

                    for (int i = 5; i >= 0; i--)
                    {
                        var d = DateTime.Today.AddMonths(-i);
                        labels.Add(d.ToString("MMM")); // "Nov", "Dec"

                        var dataForMonth = chartData.FirstOrDefault(x => x.Year == d.Year && x.Month == d.Month);
                        values.Add(dataForMonth?.Count ?? 0);
                    }

                    vm.ChartLabels = labels.ToArray();
                    vm.ChartValues = values.ToArray();
                }
            }

            return View(vm);
        }
        // 在 ReportController 中添加

        [Authorize(Roles = "Member")] // 只有会员能访问
        public IActionResult MyUsage(DateTime? startDate, DateTime? endDate)
        {
            // 1. 获取当前登录会员的 Email
            var memberEmail = User.Identity.Name;

            // 2. 处理日期范围 (默认显示最近 7 天)
            var today = DateOnly.FromDateTime(DateTime.Today);
            var start = startDate.HasValue ? DateOnly.FromDateTime(startDate.Value) : today.AddDays(-6);
            var end = endDate.HasValue ? DateOnly.FromDateTime(endDate.Value) : today;

            // 3. 查询基础数据 (当前会员 + 日期范围内)
            var query = db.Reservations
                          .Where(r => r.MemberEmail == memberEmail && r.Date >= start && r.Date <= end);

            var rawData = query.ToList();

            // 4. 统计卡片数据
            // 假设 CourseCount 代表小时数/时长
            int totalHours = rawData.Sum(r => r.CourseCount);
            int totalSessions = rawData.Count();
            // 参加过多少种不同的课程
            int totalCourses = rawData.Select(r => r.CourseType).Distinct().Count();

            // 5. 准备图表数据 (按日期分组)
            // 为了让图表连续，我们生成 start 到 end 的每一天
            var chartLabels = new List<string>();
            var chartValues = new List<int>();

            for (var date = start; date <= end; date = date.AddDays(1))
            {
                // 如果跨度小于等于7天，显示星期几 (Mon, Tue)，否则显示日期 (01 Nov)
                bool showDayName = (end.DayNumber - start.DayNumber) <= 7;
                chartLabels.Add(showDayName ? date.DayOfWeek.ToString().Substring(0, 3) : date.ToString("dd MMM"));

                // 计算这一天的总时长
                int hoursThatDay = rawData.Where(r => r.Date == date).Sum(r => r.CourseCount);
                chartValues.Add(hoursThatDay);
            }

            // 6. 准备底部课程细分列表 (按课程类型分组)
            var breakdown = rawData
                .GroupBy(r => r.CourseType)
                .Select(g => new CourseBreakdownVM
                {
                    CourseName = g.Key,
                    Sessions = g.Count(),
                    Hours = g.Sum(r => r.CourseCount)
                })
                .OrderByDescending(x => x.Hours)
                .ToList();

            // 7. 组装 ViewModel
            var vm = new MyUsagePageVM
            {
                StartDate = start.ToDateTime(TimeOnly.MinValue),
                EndDate = end.ToDateTime(TimeOnly.MinValue),

                TotalHours = totalHours,
                TotalCourses = totalCourses,
                TotalSessions = totalSessions,

                ChartLabels = chartLabels.ToArray(),
                ChartValues = chartValues.ToArray(),

                CourseBreakdown = breakdown
            };

            return View(vm);
        }
    }
}
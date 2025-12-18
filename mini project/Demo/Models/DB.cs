using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Demo.Models;

#nullable disable warnings

public class DB : DbContext
{
    public DB(DbContextOptions options) : base(options) { }

    // DB Sets
    public DbSet<User> Users { get; set; }
    public DbSet<Admin> Admins { get; set; }
    public DbSet<Member> Members { get; set; }

    public DbSet<Course> Courses { get; set; }
    //new abbbbb
    //neeed add-migration name :ProductPhoto
    public DbSet<CoursePhoto> CoursePhotos { get; set; }
    public DbSet<Reservation> Reservations { get; set; }
    public DbSet<Discount> Discounts { get; set; }
    public DbSet<ReservationLine> ReservationLines { get; set; }
    public DbSet<EWallet> eWallets { get; set; }
    public DbSet<EWalletTransaction> eWalletTransactions { get; set; }
    public DbSet<Payment> Payments { get; set; }
}

// Entity Classes -------------------------------------------------------------

public class User
{
    [Key, MaxLength(100)]
    public string Email { get; set; }
    [MaxLength(100)]
    public string Hash { get; set; }
    [MaxLength(100)]
    public string Name { get; set; }

    public string Role => GetType().Name;
}

public class Admin : User
{

}

public class Member : User
{
    [MaxLength(100)]
    public string PhotoURL { get; set; }

    // Navigation Properties
    public EWallet eWallets { get; set; }
}

// Product, Order, OrderLine

public class Course
{
    [Key, MaxLength(4)]
    public string CourseId { get; set; }
    [MaxLength(100)]
    public string Name { get; set; }
    [Precision(6, 2)]
    public decimal Price { get; set; }
    [MaxLength(100)]
    public string PhotoURL { get; set; }

    [MaxLength(100)]
    public string? AdminEmail { get; set; }

    [ForeignKey("AdminEmail")]
    public Admin Admin { get; set; }

    // Navigation Properties
    public List<CoursePhoto> Photos { get; set; } = new();

    [Required]
    public bool IsActive { get; set; }

}

//new one abbbb
public class CoursePhoto
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string PhotoURL { get; set; }

    // Foreign Key
    [Required]
    [MaxLength(4)]
    public string ProductId { get; set; }

    // Navigation Property
    public Course Course { get; set; }
}

public class Reservation
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int ReservationId { get; set; }

    [Required]
    public string CourseType { get; set; }

    [Required]
    public DateOnly Date { get; set; }

    [Required]
    public int CourseCount { get; set; }

    public string? DiscountType { get; set; }

    public decimal? DiscountValue { get; set; }

    [Required]
    public string MemberEmail { get; set; }

    public string PaymentId { get; set; }

    public string? CourseId { get; set; }

    // Navigation Property
    public Member Member { get; set; }
}

public class Discount
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int DiscountId { get; set; }

    [Required]
    public string DiscountType { get; set; }

    [Required]
    public string Code { get; set; }

    public decimal DiscountValue { get; set; }

    [Required]
    public bool IsActive { get; set; }

    public int? UsageLimit { get; set; }

    public int UsedCount { get; set; } = 0;

    public string? AdminEmail { get; set; }
}

public class ReservationLine
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int ReservationLineId { get; set; }

    public int ReservationId { get; set; }

    public decimal SubTotal { get; set; }

    [Required]
    public TimeOnly Time { get; set; }

    // Navigation
    public Reservation Reservation { get; set; }
}

public class EWallet
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int EWalletId { get; set; }

    [Precision(10, 2)]
    public decimal Balance { get; set; } = 0.00m;

    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    // Foreign Key
    [Required]
    [MaxLength(100)]
    public string MemberEmail { get; set; }

    // Navigation Property
    public Member Member { get; set; }
}

public class EWalletTransaction
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int TransactionId { get; set; }

    [Required]
    public string MemberEmail { get; set; }

    [Required]
    public string TransactionType { get; set; }

    [Required]
    [Precision(10, 2)]
    public decimal Amount { get; set; }

    [Required]
    public DateTime TransactionDate { get; set; } = DateTime.UtcNow;

    [MaxLength(255)]
    public string? Description { get; set; }

    // Navigation Property
    public Member Member { get; set; }
}

public class Payment
{
    [Key]
    [MaxLength(100)]
    public string PaymentId { get; set; }

    [Required]
    public decimal Price { get; set; }

    [Required]
    public decimal Total { get; set; }

    public int? DiscountId { get; set; }
}
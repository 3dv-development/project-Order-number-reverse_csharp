using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectOrderNumberSystem.Models
{
    /// <summary>
    /// 社員マスタ
    /// </summary>
    [Table("employees")]
    public class Employee
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("employee_id")]
        [StringLength(50)]
        public string EmployeeId { get; set; } = string.Empty;

        [Required]
        [Column("name")]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Column("email")]
        [StringLength(200)]
        public string? Email { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        [Column("role")]
        [StringLength(20)]
        public string Role { get; set; } = "user"; // "admin" or "user"

        /// <summary>
        /// 管理者権限をチェック
        /// </summary>
        public bool IsAdmin()
        {
            return Role == "admin";
        }
    }
}

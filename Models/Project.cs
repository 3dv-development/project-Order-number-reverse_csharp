using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectOrderNumberSystem.Models
{
    /// <summary>
    /// 受注案件モデル
    /// </summary>
    [Table("projects")]
    public class Project
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("project_number")]
        [StringLength(7)]
        public string ProjectNumber { get; set; } = string.Empty;

        [Required]
        [Column("category")]
        [StringLength(2)]
        public string Category { get; set; } = string.Empty;

        [Required]
        [Column("staff_id")]
        [StringLength(50)]
        public string StaffId { get; set; } = string.Empty;

        [Required]
        [Column("staff_name")]
        [StringLength(100)]
        public string StaffName { get; set; } = string.Empty;

        [Column("case_number")]
        [StringLength(50)]
        public string? CaseNumber { get; set; }

        [Required]
        [Column("project_name")]
        [StringLength(200)]
        public string ProjectName { get; set; } = string.Empty;

        [Required]
        [Column("client_name")]
        [StringLength(200)]
        public string ClientName { get; set; } = string.Empty;

        [Required]
        [Column("budget")]
        public int Budget { get; set; }

        [Required]
        [Column("deadline")]
        public DateTime Deadline { get; set; }

        [Column("remarks")]
        public string? Remarks { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // リレーション
        public virtual ICollection<EditHistory> EditHistory { get; set; } = new List<EditHistory>();

        /// <summary>
        /// カテゴリ名を取得
        /// </summary>
        public string GetCategoryName()
        {
            return Category switch
            {
                "02" => "設計",
                "03" => "トレーニング・たよれーる・データ販売",
                "04" => "製品販売",
                "06" => "システム受託",
                "07" => "システム小規模開発",
                "08" => "付帯業務",
                _ => "不明"
            };
        }
    }
}

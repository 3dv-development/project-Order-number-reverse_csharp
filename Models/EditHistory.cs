using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectOrderNumberSystem.Models
{
    /// <summary>
    /// 編集履歴モデル
    /// </summary>
    [Table("edit_history")]
    public class EditHistory
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("project_id")]
        public int ProjectId { get; set; }

        [Required]
        [Column("editor_id")]
        [StringLength(50)]
        public string EditorId { get; set; } = string.Empty;

        [Required]
        [Column("editor_name")]
        [StringLength(100)]
        public string EditorName { get; set; } = string.Empty;

        [Required]
        [Column("edit_type")]
        [StringLength(20)]
        public string EditType { get; set; } = string.Empty; // "create" or "update"

        [Column("changes")]
        public string? Changes { get; set; } // JSON形式

        [Column("edited_at")]
        public DateTime EditedAt { get; set; } = DateTime.UtcNow;

        // リレーション
        [ForeignKey("ProjectId")]
        public virtual Project? Project { get; set; }
    }
}

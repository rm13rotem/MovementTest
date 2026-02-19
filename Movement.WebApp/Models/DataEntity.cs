using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace Movement.WebApp.Models
{
    public class DataEntity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [NotNull]
        public string GuidId { get; set; }

        [NotNull]
        public string Value { get; set; }

        public bool IsDeleted { get; set; } = false;

        public DataEntity()
        {
            if (string.IsNullOrWhiteSpace(GuidId))
                GuidId = Guid.NewGuid().ToString();
            if (string.IsNullOrWhiteSpace(Value))
                Value = "{}";
        }
    }
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EVEMakeMoney.Api.Data
{
    public class CharacterEntity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public UserEntity? User { get; set; }

        [Required]
        public long CharacterId { get; set; }

        [Required]
        [MaxLength(100)]
        public string CharacterName { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? CorporationName { get; set; }

        public long? CorporationId { get; set; }

        [MaxLength(100)]
        public string? AllianceName { get; set; }

        public long? AllianceId { get; set; }

        [MaxLength(500)]
        public string? AccessToken { get; set; }

        [MaxLength(500)]
        public string? RefreshToken { get; set; }

        public DateTime? TokenExpiresAt { get; set; }

        [MaxLength(100)]
        public string? CharacterOwnerHash { get; set; }

        [MaxLength(2000)]
        public string? Scopes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? LastUsedAt { get; set; }
    }
}

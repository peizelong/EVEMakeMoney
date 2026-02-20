using System.ComponentModel.DataAnnotations;

namespace EVEMakeMoney.Api.Data
{
    public class UserEntity
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [MaxLength(50)]
        public string Username { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(255)]
        public string PasswordHash { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(100)]
        public string Email { get; set; } = string.Empty;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? LastLoginAt { get; set; }
        
        public string? RefreshToken { get; set; }
        
        public DateTime? RefreshTokenExpiryTime { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;

namespace ShareVault.API.Models
{
    public class Role
    {
        [Key]
        public required string Id { get; set; }
        public required string Name { get; set; }
        public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    }
}

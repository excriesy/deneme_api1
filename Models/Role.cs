using System.ComponentModel.DataAnnotations.Schema;

namespace ShareVault.API.Models
{
    public class Role
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;

        [NotMapped]
        public ICollection<UserRole> UserRoles { get; set; }
    }

}

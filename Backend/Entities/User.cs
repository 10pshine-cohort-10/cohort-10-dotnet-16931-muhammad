using System.ComponentModel.DataAnnotations;
using System.Data;

namespace TaskManagement.API.Entities;

public class User
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress, MaxLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    public int RoleId { get; set; }
    public Role Role { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation (for later Task module)
    // public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
}
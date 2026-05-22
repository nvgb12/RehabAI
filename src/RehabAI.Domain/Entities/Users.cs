using RehabAI.Domain.Enums;

namespace RehabAI.Domain.Entities;

public class User : BaseEntity
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? PasswordHash { get; set; }
    public AccountStatus Status { get; set; } = AccountStatus.PendingEmail;
    public bool EmailConfirmed { get; set; }

    public PatientProfile? PatientProfile { get; set; }
    public DoctorProfile? DoctorProfile { get; set; }
    public ICollection<UserRoleAssignment> Roles { get; set; } = new List<UserRoleAssignment>();
    public ICollection<ChatSession> ChatSessions { get; set; } = new List<ChatSession>();
    public ICollection<Order> Orders { get; set; } = new List<Order>();
}

public class Role : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ICollection<UserRoleAssignment> Users { get; set; } = new List<UserRoleAssignment>();
}

public class UserRoleAssignment
{
    public Guid UserId { get; set; }
    public Guid RoleId { get; set; }

    public User? User { get; set; }
    public Role? Role { get; set; }
}

public class UserToken : BaseEntity
{
    public Guid UserId { get; set; }
    public UserTokenType TokenType { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? UsedAt { get; set; }

    public User? User { get; set; }
}

public class PatientProfile : BaseEntity
{
    public Guid UserId { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public string? Address { get; set; }
    public string? ProfileImageUrl { get; set; }
    public User? User { get; set; }
}

public class DoctorProfile : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid SpecialtyId { get; set; }
    public string? Bio { get; set; }
    public string? AvatarUrl { get; set; }
    public bool PublicProfileApproved { get; set; }
    public DoctorProfileReviewStatus PublicProfileReviewStatus { get; set; } = DoctorProfileReviewStatus.Draft;
    public DateTimeOffset? SubmittedForReviewAt { get; set; }
    public DateTimeOffset? ReviewedAt { get; set; }
    public Guid? ReviewedByAdminId { get; set; }
    public string? PublicProfileRejectionReason { get; set; }
    public decimal CommissionRate { get; set; }
    public User? User { get; set; }
    public Specialty? Specialty { get; set; }
    public ICollection<DoctorScheduleSlot> ScheduleSlots { get; set; } = new List<DoctorScheduleSlot>();
    public ICollection<DoctorCredentialDocument> CredentialDocuments { get; set; } = new List<DoctorCredentialDocument>();
}

public class Specialty : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<DoctorProfile> Doctors { get; set; } = new List<DoctorProfile>();
}

public class DoctorCredentialDocument : BaseEntity
{
    public Guid DoctorProfileId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string StorageKey { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string MalwareScanStatus { get; set; } = "Pending";
    public bool StorageSkippedByPolicy { get; set; }
    public Guid? UploadedByUserId { get; set; }
    public DoctorProfile? DoctorProfile { get; set; }
}

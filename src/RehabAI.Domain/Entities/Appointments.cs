using RehabAI.Domain.Enums;

namespace RehabAI.Domain.Entities;

public class MedicalService : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DurationMinutes { get; set; }
    public decimal Price { get; set; }
    public string Currency { get; set; } = "VND";
    public bool OnlinePaymentEnabled { get; set; }
    public bool AutoConfirmEnabled { get; set; }
    public bool NoShowFeeEnabled { get; set; }
    public decimal? NoShowFeeAmount { get; set; }
    public bool IsActive { get; set; } = true;
}

public class DoctorService : BaseEntity
{
    public Guid DoctorProfileId { get; set; }
    public Guid MedicalServiceId { get; set; }
    public bool IsActive { get; set; } = true;

    public DoctorProfile? DoctorProfile { get; set; }
    public MedicalService? MedicalService { get; set; }
}

public class DoctorScheduleSlot : BaseEntity
{
    public Guid DoctorProfileId { get; set; }
    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset EndTime { get; set; }
    public ScheduleSlotStatus Status { get; set; } = ScheduleSlotStatus.Available;
    public DateTimeOffset? ReservedUntil { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public DoctorProfile? DoctorProfile { get; set; }
}

public class Appointment : BaseEntity
{
    public Guid PatientId { get; set; }
    public Guid DoctorProfileId { get; set; }
    public Guid MedicalServiceId { get; set; }
    public Guid? DoctorScheduleSlotId { get; set; }
    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset EndTime { get; set; }
    public AppointmentStatus Status { get; set; } = AppointmentStatus.Pending;
    public string? Notes { get; set; }
    public string? CancellationReason { get; set; }
    public DateTimeOffset? SoftReservedUntil { get; set; }

    public DoctorScheduleSlot? DoctorScheduleSlot { get; set; }
}

public class Dispute : BaseEntity
{
    public Guid AppointmentId { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DisputeStatus Status { get; set; } = DisputeStatus.Open;
    public string Reason { get; set; } = string.Empty;
    public string? ResolutionNote { get; set; }
}

public class Review : BaseEntity
{
    public Guid AppointmentId { get; set; }
    public Guid PatientId { get; set; }
    public Guid DoctorProfileId { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public ReviewStatus Status { get; set; } = ReviewStatus.Visible;
}

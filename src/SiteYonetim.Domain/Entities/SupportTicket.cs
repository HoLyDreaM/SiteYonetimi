namespace SiteYonetim.Domain.Entities;

/// <summary>
/// Destek talebi (Daire sakininden yöneticiye)
/// </summary>
public class SupportTicket : BaseEntity
{
    public Guid SiteId { get; set; }
    public Guid? ApartmentId { get; set; }
    public Guid? ResidentId { get; set; }
    /// <summary>Destek kaydını oluşturan üye (giriş yapmış kullanıcı)</summary>
    public Guid? CreatedByUserId { get; set; }
    /// <summary>İletişim adı (Blok/Daire seçilmeden formdan gelen)</summary>
    public string? ContactName { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    /// <summary>Blok numarası (formdan)</summary>
    public string? BlockNumber { get; set; }
    /// <summary>Kat numarası (formdan)</summary>
    public int? FloorNumber { get; set; }
    /// <summary>0=Öneri, 1=Şikayet, 2=İstek</summary>
    public TicketTopicType TopicType { get; set; } = TicketTopicType.Suggestion;
    public string Subject { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public TicketStatus Status { get; set; }
    public TicketPriority Priority { get; set; }
    public Guid? AssignedToUserId { get; set; }
    public DateTime? ClosedAt { get; set; }
    public string? Response { get; set; }

    public Site Site { get; set; } = null!;
    public Apartment? Apartment { get; set; }
    public Resident? Resident { get; set; }
    public User? CreatedByUser { get; set; }
    public User? AssignedToUser { get; set; }
    public ICollection<SupportTicketMessage> Messages { get; set; } = new List<SupportTicketMessage>();
    public ICollection<SupportTicketAttachment> Attachments { get; set; } = new List<SupportTicketAttachment>();
}

public class SupportTicketAttachment : BaseEntity
{
    public Guid SupportTicketId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;

    public SupportTicket SupportTicket { get; set; } = null!;
}

public enum TicketStatus
{
    Open = 0,
    InProgress = 1,
    Resolved = 2,
    Closed = 3
}

public enum TicketPriority
{
    Low = 0,
    Medium = 1,
    High = 2
}

public enum TicketTopicType
{
    Suggestion = 0,  // Öneri
    Complaint = 1,   // Şikayet
    Request = 2      // İstek
}

public class SupportTicketMessage : BaseEntity
{
    public Guid SupportTicketId { get; set; }
    public Guid? UserId { get; set; }
    public bool IsFromResident { get; set; }
    public string Message { get; set; } = string.Empty;

    public SupportTicket SupportTicket { get; set; } = null!;
    public User? User { get; set; }
}

namespace SiteYonetim.Domain.Entities;

/// <summary>
/// Anket
/// </summary>
public class Survey : BaseEntity
{
    public Guid SiteId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; } = true;

    public Site Site { get; set; } = null!;
    public ICollection<SurveyQuestion> Questions { get; set; } = new List<SurveyQuestion>();
    public ICollection<SurveyVote> Votes { get; set; } = new List<SurveyVote>();
}

public class SurveyQuestion : BaseEntity
{
    public Guid SurveyId { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public int Order { get; set; }
    public bool IsMultipleChoice { get; set; }

    public Survey Survey { get; set; } = null!;
    public ICollection<SurveyOption> Options { get; set; } = new List<SurveyOption>();
}

public class SurveyOption : BaseEntity
{
    public Guid SurveyQuestionId { get; set; }
    public string OptionText { get; set; } = string.Empty;
    public int Order { get; set; }

    public SurveyQuestion SurveyQuestion { get; set; } = null!;
    public ICollection<SurveyVote> Votes { get; set; } = new List<SurveyVote>();
}

public class SurveyVote : BaseEntity
{
    public Guid SurveyId { get; set; }
    public Guid SurveyOptionId { get; set; }
    public Guid ApartmentId { get; set; }
    public Guid? ResidentId { get; set; }

    public Survey Survey { get; set; } = null!;
    public SurveyOption SurveyOption { get; set; } = null!;
    public Apartment Apartment { get; set; } = null!;
    public Resident? Resident { get; set; }
}

using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using FluentValidation;

namespace VideoPlatform.Models;

public class Comment
{
    [Key]
    public int Id { get; set; }
    public string Content { get; set; }
    public DateTime CreatedOn { get; set; }
    public string VideoId { get; set; }
    [ForeignKey("VideoId")]
    public Video Video { get; set; }
    public int UserId { get; set; }
    [ForeignKey("UserId")]
    public User User { get; set; }

}

public class CommentPost
{
    public string Content { get; set; }
    public string VideoId { get; set; }
}

public class CommentValidator : AbstractValidator<CommentPost>
{
    public CommentValidator()
    {
        RuleFor(c => c.Content).NotNull().NotEmpty();
        RuleFor(c => c.VideoId).NotNull().NotEmpty();
    }
}

public class CommentPut
{
    public string Content { get; set; }
    
}

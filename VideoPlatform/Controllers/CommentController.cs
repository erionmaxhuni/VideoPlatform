using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Data.Common;
using System.Xml.Linq;
using VideoPlatform.Data;
using VideoPlatform.Models;
using VideoPlatform.Services;
using DbConnection = VideoPlatform.Data.DbConnection;

namespace VideoPlatform.Controllers;
[ApiController]
[Route("api/reactions/comments")]
public class CommentController : ControllerBase
{
    private readonly DbConnection _dbConnection;
    private readonly IValidator<CommentPost> _validator;
    private readonly TokenService _tokenService;

    public CommentController(DbConnection dbConnection, IValidator<CommentPost> validator,
                             TokenService tokenService)
    {
        _dbConnection = dbConnection;
        _validator = validator;
        _tokenService = tokenService;
    }

    [HttpGet("video/{videoid}")]
    public ActionResult<List<Comment>> Get(string videoid)
    {
        Console.WriteLine("Request");
        var comments = _dbConnection.Comments.Where(c => c.VideoId == videoid)
            .Select((c) => new {
                c.Id,
                c.UserId,
                c.Content,
                c.VideoId,
                c.CreatedOn,
                User = new
                {
                    c.User.Username,
                    c.User.Id
                }
            }).ToList();

        return Ok(JsonConvert.SerializeObject(comments));
    }

    [HttpGet("user/")]
    [Authorize]
    public ActionResult<List<Comment>> GetUserComments()
    {
        var authHeader = HttpContext.Request.Headers.Authorization.ToString();

        var tokenPayload = _tokenService.GetTokenPayload(authHeader);
        var userIdIsValid = int.TryParse(tokenPayload["Id"].ToString(), out var userid);

        if (!userIdIsValid)
            return BadRequest("Invalid user id");

        var userComments = _dbConnection.Comments.Where(c => c.UserId == userid).OrderByDescending(c => c.CreatedOn).ToList();

        return Ok(JsonConvert.SerializeObject(userComments));
    }


    [HttpPost]
    [Authorize]
    public ActionResult<Comment> PostComment([FromBody] CommentPost comment)
    {
        if (comment is null)
            return BadRequest("Invalid Comment Input");

        var validationResult = _validator.Validate(comment);
        if (!validationResult.IsValid)
            return BadRequest(validationResult.Errors);

        var authHeader = HttpContext.Request.Headers.Authorization.ToString();

        var tokenPayload = _tokenService.GetTokenPayload(authHeader);
        var userIdIsValid = int.TryParse(tokenPayload["Id"].ToString(), out var userid);

        if (!userIdIsValid)
            return BadRequest("Invalid token");


        var userExists = _dbConnection.Users.Any(u => u.Id == userid);

        if (!userExists)
            return NotFound($"User with id {userid} does not exist");

        var videoExists = _dbConnection.Videos.Any(v => v.Id == comment.VideoId);

        if (!videoExists)
            return NotFound($"Video with id {comment.VideoId} does not exist");

        var newComment = _dbConnection.Comments.Add(
            new()
            {
                Content = comment.Content,
                CreatedOn = DateTime.UtcNow,
                UserId = userid,
                VideoId = comment.VideoId
            }
        );
        _dbConnection.SaveChanges();

        return Ok(JsonConvert.SerializeObject(newComment.Entity));
    }

    [HttpPut]
    [Route("{commentid}")]
    [Authorize]
    public ActionResult<Comment> UpdateComment(int commentid, [FromBody] CommentPut comment)
    {
        if (commentid < 0)
            return BadRequest("Comment Id can not be negative");

        var authHeader = HttpContext.Request.Headers.Authorization.ToString();

        var tokenPayload = _tokenService.GetTokenPayload(authHeader);
        var userIdIsValid = int.TryParse(tokenPayload["Id"].ToString(), out var userid);

        if (!userIdIsValid)
            return BadRequest("Invalid token");

        var existingComment = _dbConnection.Comments.FirstOrDefault(c => c.Id == commentid);

        if (existingComment is null)
            return NotFound("Comment was not found");

        if (existingComment.UserId != userid)
            return Unauthorized();

        if (!(comment.Content is null || comment.Content.Length == 0))
            existingComment.Content = comment.Content;

        _dbConnection.Comments.Update(existingComment);
        _dbConnection.SaveChanges();

        return Ok(JsonConvert.SerializeObject(existingComment));
    }

    [HttpDelete]
    [Route("{commentid}")]
    [Authorize]
    public ActionResult<Comment> DeleteComment(int commentid)
    {

        if (commentid < 0)
            return BadRequest("Comment Id can not be negative");

        var authHeader = HttpContext.Request.Headers.Authorization.ToString();

        var tokenPayload = _tokenService.GetTokenPayload(authHeader);
        var userIdIsValid = int.TryParse(tokenPayload["Id"].ToString(), out var userid);

        if (!userIdIsValid)
            return BadRequest("Invalid token");

        var existingComment = _dbConnection.Comments.FirstOrDefault(c => c.Id == commentid);

        if (existingComment is null)
            return NotFound($"Comment with id {commentid} was not found");

        if (existingComment.UserId != userid)
            return Unauthorized();

        _dbConnection.Comments.Remove(existingComment);
        _dbConnection.SaveChanges();

        return NoContent();
        
    }
}



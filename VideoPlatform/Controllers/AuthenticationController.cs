using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Data.Common;
using VideoPlatform.Models;
using VideoPlatform.Services;
using FluentValidation;
using VideoPlatform.Data;
using DbConnection = VideoPlatform.Data.DbConnection;

namespace VideoPlatform.Controllers;

[ApiController]
[Route("api/users")]
public class AuthenticationController : ControllerBase
{
    public readonly DbConnection _dbConnection;
    public readonly TokenService _tokenService;
    public readonly PasswordService _passwordService;
    private readonly IValidator<UserPost> _userValidator;

    public AuthenticationController(DbConnection dbConnection, TokenService tokenService,
                                    PasswordService passwordService, IValidator<UserPost> uservalidator)
    {
        _dbConnection = dbConnection;
        _tokenService = tokenService;
        _passwordService = passwordService;
        _userValidator = uservalidator;
    }

    [HttpPost]
    [Route("login")]
    public ActionResult<TokenResponse> Login([FromBody] UserCredentials userCredentials)
    {

        if (userCredentials is null)
            return BadRequest("Invalid User credentials");

        var user = _dbConnection.Users.FirstOrDefault(user => user.Username == userCredentials.Username);

        if (user is null)
            return NotFound();

        if (!_passwordService.VerifyPassword(userCredentials.Password, user.Password))
            return Unauthorized();

        var tokenResponse = _tokenService.GenerateToken(user);

        return Ok(tokenResponse);
    }

    [HttpPost]
    [Route("signup")]
    public ActionResult Signup([FromBody] UserPost user)
    {
        if (user is null)
             return BadRequest("Invalid Input");


        var validationResult = _userValidator.Validate(user);
        if (!validationResult.IsValid)
            return BadRequest(validationResult.Errors);

        var userNameExists = _dbConnection.Users.Any(c => c.Username == user.Username);
        if (userNameExists)
            return Conflict("Username already exists");

        user.Password = _passwordService.HashPassword(user.Password);

        var newUser = new User()
        {
            Name = user.Username,
            Surname = user.Surname,
            Username = user.Username,
            Password = user.Password,
            Role = Role.user
        };

        var userEntity = _dbConnection.Users.Add(newUser);
        _dbConnection.SaveChanges();

        return Ok();
    }
}



﻿using Azure.Core;
using Backend.Application.Services.Email;
using Backend.Domain.Entities;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using IdentityModel.Client;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Backend.Application.DTO.UserDTO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

[ApiController]
[Route("api/identity")]
public class UserController : ControllerBase
{
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    private readonly IConfiguration _configuration;
    private readonly IEmailService _emailService;

    public UserController(
        UserManager<User> userManager,
        SignInManager<User> signInManager,
        IConfiguration configuration,
        IEmailService emailService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _configuration = configuration;
        _emailService = emailService;
    }
    
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest model)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var user = new User
        {
            UserName = model.Email,
            Email = model.Email,
            EmailConfirmed = false
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
            return BadRequest(result.Errors);

        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);

        var confirmationLink = Url.Action(nameof(ConfirmEmail), "User",
            new { userId = user.Id, token }, Request.Scheme);

        await _emailService.SendEmailAsync(user.Email, "Confirm your email to your Cupid App Account",
            $"Please confirm your email by clicking this link: {confirmationLink}");

        return Ok($"Confirm your email that we sent to your email address.");
    }

    [HttpGet("confirm-email")]
    public async Task<IActionResult> ConfirmEmail(string userId, string token)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return NotFound("User not found.");

        var result = await _userManager.ConfirmEmailAsync(user, token);
        if (!result.Succeeded)
            return BadRequest("Email confirmation failed.");

        return Ok("Email confirmed successfully.");
    }
    [HttpPost("resend-confirmation-email")]
    public async Task<IActionResult> ResendConfirmationEmail([FromBody] ResendConfirmationEmailRequest model)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null)
            return NotFound("User not found");

        if(await _userManager.IsEmailConfirmedAsync(user))
        {
            return BadRequest("Email is already confirmed");
        }

        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);

        var confirmationLink = Url.Action(nameof(ConfirmEmail), "User",
            new { userId = user.Id, token }, Request.Scheme);

        await _emailService.SendEmailAsync(user.Email, "Resend Confirmation Email for Your Cupid App Account",
            $"Please confirm your email by clicking this link: {confirmationLink}");

        return Ok("Confirmation email has been resent. Please check your inbox.");
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequestDTO model)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

 
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized("Invalid token.");


        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return NotFound("User not found.");

        var result = await _userManager.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);
        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description).ToList();
            return BadRequest(new { errors });
        }

        return Ok("Password changed successfully.");
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest model)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null || !await _userManager.CheckPasswordAsync(user, model.Password))
            return Unauthorized("Invalid email or password.");

        if (!await _userManager.IsEmailConfirmedAsync(user))
            return Unauthorized("Email not confirmed.");

        var accessToken = await GenerateJwtToken(user);
        var refreshToken = GenerateRefreshToken();

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7); 
        await _userManager.UpdateAsync(user);

        return Ok(new
        {
            tokenType = "Bearer",
            accessToken,
            refreshToken,
            expiresIn = 3600 
        });
    }


    [HttpPost("refresh-token")]
    [Authorize]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestDTO request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return Unauthorized("Invalid user ID.");


        if (user.RefreshToken != request.refreshToken || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
            return Unauthorized("Invalid or expired refresh token.");


        var newAccessToken = await GenerateJwtToken(user);


        var newRefreshToken = GenerateRefreshToken();
        user.RefreshToken = newRefreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(2); 
        await _userManager.UpdateAsync(user);

        return Ok(new
        {
            tokenType = "Bearer",
            accessToken = newAccessToken,
            refreshToken = newRefreshToken,
            expiresIn = 3600 
        });
    }


    [HttpPost("change-email")]

    public async Task<IActionResult> ChangeEmail([FromBody] ChangeEmailRequestDTO model)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized("Invalid token.");

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return NotFound("User not found.");

        if (user.Email == model.NewEmail)
            return BadRequest("The new email address is the same as the current email.");

        var token = await _userManager.GenerateChangeEmailTokenAsync(user, model.NewEmail);
        var confirmationLink = Url.Action(nameof(ConfirmChangeEmail), "User",
            new { token, newEmail = model.NewEmail, userId}, Request.Scheme);

        await _emailService.SendEmailAsync(model.NewEmail, "Confirm your new email address. ",
            $"Please confirm your new email by clicking this link: {confirmationLink}");

        return Ok("A confirmation email has been sent to your new email address. ");

    }

    [HttpGet("confirm-change-email")]
    [AllowAnonymous]
    public async Task<IActionResult> ConfirmChangeEmail(string token, string newEmail, string userId)
    {
        if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(newEmail) || string.IsNullOrEmpty(userId))
            return BadRequest("Invalid request. Token, new email, and userId are required.");


        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return NotFound("User not found.");

        var result = await _userManager.ChangeEmailAsync(user, newEmail, token);
        if (!result.Succeeded)
            return BadRequest("Email change confirmation failed.");

        return Ok("Email changed successfully.");
    }

    [HttpPost("upload-profile-picture")]
    [Authorize]
    public async Task<IActionResult> UploadProfilePicture(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file uploaded.");

        if (!file.ContentType.StartsWith("image/"))
            return BadRequest("Invalid file type. Please upload an image.");

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized("Invalid token.");

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return NotFound("User not found.");

        byte[] resizedImage;
        using (var memoryStream = new MemoryStream())
        {
            await file.CopyToAsync(memoryStream);


            memoryStream.Seek(0, SeekOrigin.Begin);
            using (var image = SixLabors.ImageSharp.Image.Load(memoryStream))
            {

                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Mode = ResizeMode.Max,
                    Size = new SixLabors.ImageSharp.Size(400, 400)
                }));


                using (var outputStream = new MemoryStream())
                {
                    image.Save(outputStream, new JpegEncoder());
                    resizedImage = outputStream.ToArray();
                }
            }
        }

        user.ProfilePicture = resizedImage;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
            return BadRequest(result.Errors);

        return Ok("Profile picture uploaded successfully.");
    }


    [HttpGet("profile-picture")]
    [Authorize]
    public async Task<IActionResult> GetProfilePicture()
    {

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized("Invalid token.");


        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return NotFound("User not found.");


        if (user.ProfilePicture == null || user.ProfilePicture.Length == 0)
            return NotFound("No profile picture found.");


        return File(user.ProfilePicture, "image/jpeg"); 
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        if (string.IsNullOrEmpty(request.Email))
            return BadRequest("Email is required.");

        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
            return BadRequest("User not found.");

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);

        var encodedToken = Uri.EscapeDataString(token);

        var resetLink = $"{Request.Scheme}://{Request.Host}/api/identity/reset-password?email={request.Email}&token={token}";

        await _emailService.SendEmailAsync(request.Email, "Reset Your Password",
            $"Please reset your password by clicking this link: {resetLink}");

        return Ok("We have sent you an email with instructions to reset your password.");
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword([FromQuery] string email, [FromQuery] string token, [FromBody] ResetPasswordRequestDTO request)
    {

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(token) || string.IsNullOrEmpty(request.newPassword))
            return BadRequest("Invalid request. Email, token, and new password are required.");
        var decodedToken = Uri.UnescapeDataString(token);
        token = token.Replace(" ", "+").Replace("\t", "+").Replace("\n", "+").Replace("\r", "+");
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
            return BadRequest("User not found.");

        var result = await _userManager.ResetPasswordAsync(user, token, request.newPassword);
        if (!result.Succeeded)
            return BadRequest(result.Errors.Select(e => e.Description));

        return Ok("Your password has been reset successfully.");
    }

    private string GenerateRefreshToken()
    {
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);

    
        var base64String = Convert.ToBase64String(randomNumber);


        return base64String
            .Replace('+', '-')  
            .Replace('/', '_')  
            .TrimEnd('=');
    }
    private async Task<string> GenerateJwtToken(User user)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, user.UserName),
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Email, user.Email)
        };

        var jwtSettings = _configuration.GetSection("Jwt");
        var secretKey = jwtSettings["Key"];
        var issuer = jwtSettings["Issuer"];
        var audience = jwtSettings["Audience"];

        var signingCredentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            SecurityAlgorithms.HmacSha256);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(1),
            Issuer = issuer,
            Audience = audience,
            SigningCredentials = signingCredentials
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}

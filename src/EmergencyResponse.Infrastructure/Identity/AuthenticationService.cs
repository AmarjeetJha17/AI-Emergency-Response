using EmergencyResponse.Application.DTOs.Authentication;
using EmergencyResponse.Application.Interfaces;
using EmergencyResponse.Application.Exceptions;

using Microsoft.AspNetCore.Identity;

namespace EmergencyResponse.Infrastructure.Identity;

public sealed class AuthenticationService
    : IAuthenticationService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;

    public AuthenticationService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    public async Task<UserResponse> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default)
    {
        var existingUser = await _userManager.FindByEmailAsync(
            request.Email);

        if (existingUser is not null)
        {
            throw new ConflictException(
                "A user with this email already exists.");
        }

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            PhoneNumber = request.PhoneNumber,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(
            user,
            request.Password);

        if (!result.Succeeded)
        {
            var errors = string.Join(
                "; ",
                result.Errors.Select(x => x.Description));

            throw new InvalidOperationException(errors);
        }

        return new UserResponse
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email!,
            PhoneNumber = user.PhoneNumber,
            IsActive = user.IsActive
        };
    }

    public async Task<AuthenticationResponse> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(
            request.Email);

        if (user is null || !user.IsActive)
        {
            throw new UnauthorizedException(
                "Invalid email or password.");
        }

        var result = await _signInManager.CheckPasswordSignInAsync(
            user,
            request.Password,
            lockoutOnFailure: true);

        if (!result.Succeeded)
        {
            throw new UnauthorizedException(
                "Invalid email or password.");
        }

        user.LastLoginAtUtc = DateTime.UtcNow;

        await _userManager.UpdateAsync(user);

        return new AuthenticationResponse
        {
            User = new UserResponse
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email!,
                PhoneNumber = user.PhoneNumber,
                IsActive = user.IsActive
            }
        };
    }
}
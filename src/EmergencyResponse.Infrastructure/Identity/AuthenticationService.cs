using EmergencyResponse.Application.DTOs.Authentication;
using EmergencyResponse.Application.Interfaces;
using EmergencyResponse.Application.Exceptions;
using EmergencyResponse.Application.Common.Roles;

using Microsoft.AspNetCore.Identity;

namespace EmergencyResponse.Infrastructure.Identity;

public sealed class AuthenticationService
    : IAuthenticationService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IJwtTokenService _jwtTokenService;

    public AuthenticationService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IJwtTokenService jwtTokenService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _jwtTokenService = jwtTokenService;
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

        var roleResult = await _userManager.AddToRoleAsync(
            user,
            ApplicationRoles.Citizen);

        if (!roleResult.Succeeded)
        {
            var errors = string.Join(
                "; ",
                roleResult.Errors.Select(x => x.Description));

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

        var roles = await _userManager.GetRolesAsync(user);

        var userInfo = new AuthenticationUserInfo
        {
            Id = user.Id,
            Email = user.Email!,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Roles = roles.ToArray()
        };

        var (token, expiresAtUtc) =
            await _jwtTokenService.GenerateTokenAsync(userInfo);

        user.LastLoginAtUtc = DateTime.UtcNow;

        await _userManager.UpdateAsync(user);

        return new AuthenticationResponse
        {
            AccessToken = token,
            ExpiresAtUtc = expiresAtUtc,

            Roles = roles.ToArray(),

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
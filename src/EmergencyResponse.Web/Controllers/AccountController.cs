using System.Security.Claims;

using EmergencyResponse.Application.DTOs.Authentication;
using EmergencyResponse.Web.Services;
using EmergencyResponse.Application.Common.Authentication;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace EmergencyResponse.Web.Controllers;

public class AccountController : Controller
{
    private readonly IAuthApiClient _authApiClient;

    public AccountController(
        IAuthApiClient authApiClient)
    {
        _authApiClient = authApiClient;
    }

    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(
        RegisterRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(request);
        }

        try
        {
            await _authApiClient.RegisterAsync(
                request,
                cancellationToken);

            return RedirectToAction(nameof(Login));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(
                string.Empty,
                ex.Message);

            return View(request);
        }
    }

    [HttpGet]
    public IActionResult Login()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(request);
        }

        try
        {
            var result = await _authApiClient.LoginAsync(
                request,
                cancellationToken);

            if (result is null)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Login could not be completed.");

                return View(request);
            }

            var claims = new List<Claim>
            {
                new(
                    ClaimTypes.NameIdentifier,
                    result.User.Id),

                new(
                    ClaimTypes.Email,
                    result.User.Email),

                new(
                    ClaimTypes.Name,
                    $"{result.User.FirstName} {result.User.LastName}")
            };

            claims.AddRange(
                result.Roles.Select(role =>
                    new Claim(ClaimTypes.Role, role)));

            claims.Add(
                new Claim(
                    AuthenticationConstants.AccessTokenClaim,
                    result.AccessToken));

            var identity = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme);

            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal);

            return RedirectToAction(
                "Index",
                "Home");
        }
        catch (UnauthorizedAccessException)
        {
            ModelState.AddModelError(
                string.Empty,
                "Invalid email or password.");

            return View(request);
        }
    }

    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(
            CookieAuthenticationDefaults.AuthenticationScheme);

        return RedirectToAction(
            "Index",
            "Home");
    }
}

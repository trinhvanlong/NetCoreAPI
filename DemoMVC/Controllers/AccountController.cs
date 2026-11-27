using System.Text.Encodings.Web;
using DemoMVC.Models;
using DemoMVC.Services.Email;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace DemoMVC.Controllers;

public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IEmailService _emailService;
    private readonly IMemoryCache _cache;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IEmailService emailService,
        IMemoryCache cache,
        ILogger<AccountController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _emailService = emailService;
        _cache = cache;
        _logger = logger;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Register(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View(new RegisterViewModel());
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model, string? returnUrl = null)
    {
        if (!ModelState.IsValid) return View(model);

        var existing = await _userManager.FindByNameAsync(model.UserName);
        if (existing is not null)
        {
            ModelState.AddModelError(nameof(model.UserName), "Tên đăng nhập đã được sử dụng.");
            return View(model);
        }

        var user = new ApplicationUser
        {
            UserName = model.UserName,
            Email = model.Email,
            FullName = model.FullName
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);
            return View(model);
        }

        await _userManager.AddToRoleAsync(user, "Customer");
        await SendConfirmationEmailAsync(user);

        TempData["ThongBao"] = "Đăng ký thành công. Vui lòng kiểm tra email để xác thực tài khoản.";
        return RedirectToAction(nameof(ConfirmEmailSent));
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null) =>
        View(new LoginViewModel { ReturnUrl = returnUrl });

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var user = await _userManager.FindByNameAsync(model.UserNameOrEmail)
            ?? await _userManager.FindByEmailAsync(model.UserNameOrEmail);
        if (user is null)
        {
            ModelState.AddModelError(string.Empty, "Tên đăng nhập/email hoặc mật khẩu không đúng.");
            return View(model);
        }

        var result = await _signInManager.PasswordSignInAsync(
            user,
            model.Password,
            model.RememberMe,
            lockoutOnFailure: true);

        if (result.Succeeded)
            return LocalRedirect(string.IsNullOrWhiteSpace(model.ReturnUrl) ? "/" : model.ReturnUrl);
        if (result.IsLockedOut)
        {
            ModelState.AddModelError(string.Empty, "Tài khoản đã bị khóa tạm thời trong 15 phút.");
            return View(model);
        }
        if (result.IsNotAllowed)
        {
            TempData["ThongBao"] = "Tài khoản chưa được xác thực email.";
            return RedirectToAction(nameof(ConfirmEmailSent));
        }

        ModelState.AddModelError(string.Empty, "Tên đăng nhập/email hoặc mật khẩu không đúng.");
        return View(model);
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult ConfirmEmailSent() => View();

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> ConfirmEmail(string userId, string token)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(token))
        {
            ViewData["ThanhCong"] = false;
            return View();
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            ViewData["ThanhCong"] = false;
            return View();
        }

        var result = await _userManager.ConfirmEmailAsync(user, token);
        ViewData["ThanhCong"] = result.Succeeded;
        if (!result.Succeeded)
            ViewData["ThongBao"] = "Liên kết xác thực không hợp lệ hoặc đã hết hạn.";
        return View();
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult ResendConfirmation() => View(new ForgotPasswordViewModel());

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResendConfirmation(ForgotPasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        if (!AllowEmailRequest("confirm", model.Email))
        {
            TempData["ThongBao"] = "Nếu yêu cầu hợp lệ, email xác thực sẽ được gửi trong thời gian ngắn.";
            return RedirectToAction(nameof(ConfirmEmailSent));
        }

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user is not null && !await _userManager.IsEmailConfirmedAsync(user))
            await SendConfirmationEmailAsync(user);

        TempData["ThongBao"] = "Nếu email tồn tại và chưa xác thực, liên kết xác thực đã được gửi.";
        return RedirectToAction(nameof(ConfirmEmailSent));
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult ForgotPassword() => View(new ForgotPasswordViewModel());

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        if (AllowEmailRequest("reset", model.Email))
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user is not null && await _userManager.IsEmailConfirmedAsync(user))
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var callbackUrl = Url.Action(
                    nameof(ResetPassword),
                    "Account",
                    new { userId = user.Id, token },
                    Request.Scheme);
                await _emailService.SendAsync(
                    user.Email!,
                    "Đặt lại mật khẩu DemoMVC",
                    $"<p>Bạn đã yêu cầu đặt lại mật khẩu.</p><p><a href=\"{HtmlEncoder.Default.Encode(callbackUrl!)}\">Đặt lại mật khẩu</a></p>",
                    HttpContext.RequestAborted);
            }
        }

        TempData["ThongBao"] = "Nếu email tồn tại trong hệ thống, hướng dẫn đặt lại mật khẩu đã được gửi.";
        return RedirectToAction(nameof(ForgotPasswordConfirmation));
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult ForgotPasswordConfirmation() => View();

    [HttpGet]
    [AllowAnonymous]
    public IActionResult ResetPassword(string userId, string token) =>
        View(new ResetPasswordViewModel { UserId = userId, Token = token });

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var user = await _userManager.FindByIdAsync(model.UserId);
        if (user is null)
        {
            TempData["ThongBao"] = "Liên kết đặt lại mật khẩu không hợp lệ hoặc đã hết hạn.";
            return RedirectToAction(nameof(ForgotPasswordConfirmation));
        }

        var result = await _userManager.ResetPasswordAsync(user, model.Token, model.Password);
        if (result.Succeeded)
        {
            await _userManager.UpdateSecurityStampAsync(user);
            TempData["ThongBao"] = "Đặt lại mật khẩu thành công.";
            return RedirectToAction(nameof(Login));
        }

        foreach (var error in result.Errors)
            ModelState.AddModelError(string.Empty, error.Description);
        return View(model);
    }

    [HttpGet]
    [Authorize(Policy = "CustomerAccount")]
    public async Task<IActionResult> Profile()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        return View(new ProfileViewModel
        {
            FullName = user.FullName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            UserName = user.UserName ?? string.Empty
        });
    }

    [HttpPost]
    [Authorize(Policy = "CustomerAccount")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(ProfileViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();

        user.FullName = model.FullName;
        var fullNameResult = await _userManager.UpdateAsync(user);
        if (!fullNameResult.Succeeded)
        {
            foreach (var error in fullNameResult.Errors)
                ModelState.AddModelError(string.Empty, error.Description);
            model.UserName = user.UserName ?? string.Empty;
            return View(model);
        }

        if (!string.Equals(user.Email, model.Email, StringComparison.OrdinalIgnoreCase))
        {
            var emailExists = await _userManager.FindByEmailAsync(model.Email);
            if (emailExists is not null && emailExists.Id != user.Id)
            {
                ModelState.AddModelError(nameof(model.Email), "Email đã được sử dụng.");
                model.UserName = user.UserName ?? string.Empty;
                return View(model);
            }

            await _userManager.SetEmailAsync(user, model.Email);
            await _userManager.SetUserNameAsync(user, user.UserName);
            user.EmailConfirmed = false;
            await _userManager.UpdateAsync(user);
            await SendConfirmationEmailAsync(user);
            TempData["ThongBao"] = "Thông tin đã được cập nhật. Email mới cần xác thực.";
        }
        else
        {
            TempData["ThongBao"] = "Cập nhật thông tin cá nhân thành công.";
        }

        return RedirectToAction(nameof(Profile));
    }

    [HttpGet]
    [Authorize(Policy = "CustomerAccount")]
    public IActionResult ChangePassword() => View(new ChangePasswordViewModel());

    [HttpPost]
    [Authorize(Policy = "CustomerAccount")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();

        var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
        if (result.Succeeded)
        {
            await _signInManager.RefreshSignInAsync(user);
            TempData["ThongBao"] = "Đổi mật khẩu thành công.";
            return RedirectToAction(nameof(Profile));
        }

        foreach (var error in result.Errors)
            ModelState.AddModelError(string.Empty, error.Description);
        return View(model);
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult AccessDenied() => View();

    private async Task SendConfirmationEmailAsync(ApplicationUser user)
    {
        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        var callbackUrl = Url.Action(
            nameof(ConfirmEmail),
            "Account",
            new { userId = user.Id, token },
            Request.Scheme);
        await _emailService.SendAsync(
            user.Email!,
            "Xác thực tài khoản DemoMVC",
            $"<p>Chào {HtmlEncoder.Default.Encode(user.FullName ?? user.UserName ?? "bạn")},</p><p><a href=\"{HtmlEncoder.Default.Encode(callbackUrl!)}\">Xác thực tài khoản</a></p>",
            HttpContext.RequestAborted);
    }

    private bool AllowEmailRequest(string purpose, string email)
    {
        var key = $"email-rate:{purpose}:{email.Trim().ToLowerInvariant()}";
        if (_cache.TryGetValue(key, out DateTimeOffset blockedUntil) && blockedUntil > DateTimeOffset.UtcNow)
            return false;
        _cache.Set(key, DateTimeOffset.UtcNow.AddMinutes(1), TimeSpan.FromMinutes(1));
        return true;
    }
}

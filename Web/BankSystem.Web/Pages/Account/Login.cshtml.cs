namespace BankSystem.Web.Pages.Account
{
    using BankSystem.Models;
    using Common;
    using Microsoft.AspNetCore.Authentication;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;
    using System.ComponentModel.DataAnnotations;
    using System.Security.Claims;
    using System.Threading.Tasks;

    [AllowAnonymous]
    public class LoginModel : BasePageModel
    {
        private const string SendEmailPage = "/Account/SendEmailVerification";

        private readonly ILogger<LoginModel> logger;
        private readonly SignInManager<BankUser> signInManager;

        public LoginModel(
            SignInManager<BankUser> signInManager,
            ILogger<LoginModel> logger)
        {
            this.signInManager = signInManager;
            this.logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string ReturnUrl { get; private set; }

        public async Task<IActionResult> OnGetAsync(string returnUrl = null)
        {
            // Session has expired
            if (returnUrl != null)
            {
                ShowErrorMessage(NotificationMessages.SessionExpired);
            }

            returnUrl ??= Url.Content("~/");

            if (User.Identity.IsAuthenticated)
            {
                return LocalRedirect(returnUrl);
            }

            // Clear the existing external cookie to ensure a clean login process
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            ReturnUrl = returnUrl;

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            if (User.Identity.IsAuthenticated)
            {
                return LocalRedirect(returnUrl);
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            Microsoft.AspNetCore.Identity.SignInResult result = await signInManager.PasswordSignInAsync(Input.Email, Input.Password,
                false, true);

            if (!result.Succeeded)
            {
                if (result.RequiresTwoFactor)
                {
                    return RedirectToPage("./LoginWith2fa", new { ReturnUrl = returnUrl, RememberMe = false });
                }

                if (result.IsLockedOut)
                {
                    ShowErrorMessage(NotificationMessages.LoginLockedOut);
                    return Page();
                }

                if (result.IsNotAllowed)
                {
                    ShowErrorMessage(NotificationMessages.EmailVerificationRequired);
                    return RedirectToPage(SendEmailPage);
                }

                ShowErrorMessage(NotificationMessages.InvalidCredentials);
                return Page();
            }

            BankUser user = await signInManager.UserManager.FindByNameAsync(Input.Email);

            if (!user.TwoFactorEnabled && !Request.Cookies.ContainsKey(GlobalConstants.IgnoreTwoFactorWarningCookie))
            {
                TempData.Add(GlobalConstants.TempDataNoTwoFactorKey, true);
            }
            
            AddClaims(user);

            logger.LogInformation("User logged in.");
            return LocalRedirect(returnUrl);
        }

        private void AddClaims(BankUser user)
        {
            Claim[] claims = new Claim[2]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Email, user.Email)
            };
            User.AddIdentity(new ClaimsIdentity(claims));
        }

        public class InputModel
        {
            [Required]
            [EmailAddress]
            public string Email { get; set; }

            [Required]
            [DataType(DataType.Password)]
            public string Password { get; set; }
        }
    }
}
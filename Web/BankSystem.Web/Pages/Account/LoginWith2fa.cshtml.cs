namespace BankSystem.Web.Pages.Account
{
    using System.ComponentModel.DataAnnotations;
    using System.Threading.Tasks;
    using BankSystem.Models;
    using Common;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;

    [AllowAnonymous]
    public class LoginWith2FaModel : BasePageModel
    {
        private readonly ILogger<LoginWith2FaModel> logger;
        private readonly SignInManager<BankUser> signInManager;

        public LoginWith2FaModel(SignInManager<BankUser> signInManager, ILogger<LoginWith2FaModel> logger)
        {
            this.signInManager = signInManager;
            this.logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public bool RememberMe { get; set; }

        public string ReturnUrl { get; set; }

        public async Task<IActionResult> OnGetAsync(bool rememberMe, string returnUrl = null)
        {
            returnUrl = returnUrl ?? Url.Content("~/");

            var user = await signInManager.GetTwoFactorAuthenticationUserAsync();

            if (user == null)
            {
                return LocalRedirect(returnUrl);
            }

            ReturnUrl = returnUrl;
            RememberMe = rememberMe;

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(bool rememberMe, string returnUrl = null)
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            returnUrl = returnUrl ?? Url.Content("~/");

            var user = await signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null)
            {
                return LocalRedirect(returnUrl);
            }

            string authenticatorCode = Input.TwoFactorCode.Replace(" ", string.Empty)
                .Replace("-", string.Empty);

            var result =
                await signInManager.TwoFactorAuthenticatorSignInAsync(authenticatorCode, rememberMe,
                    Input.RememberMachine);

            if (!result.Succeeded)
            {
                if (result.IsLockedOut)
                {
                    await signInManager.SignOutAsync();

                    ShowErrorMessage(NotificationMessages.LoginLockedOut);
                    return RedirectToPage("./Login");
                }

                logger.LogWarning("Invalid authenticator code entered while logging in");

                ShowErrorMessage(NotificationMessages.TwoFactorAuthenticationCodeInvalid);
                return Page();
            }

            return LocalRedirect(returnUrl);
        }

        public class InputModel
        {
            [Required]
            [StringLength(7, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.",
                MinimumLength = 6)]
            [DataType(DataType.Text)]
            [Display(Name = "Authenticator code")]
            public string TwoFactorCode { get; set; }

            [Display(Name = "Remember this device")]
            public bool RememberMachine { get; set; }
        }
    }
}
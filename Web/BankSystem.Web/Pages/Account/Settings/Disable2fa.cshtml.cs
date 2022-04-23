namespace BankSystem.Web.Pages.Account.Settings
{
    using System.ComponentModel.DataAnnotations;
    using System.Threading.Tasks;
    using BankSystem.Models;
    using Common;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;

    [Authorize]
    public class Disable2FaModel : BasePageModel
    {
        private readonly ILogger<Disable2FaModel> logger;
        private readonly SignInManager<BankUser> signInManager;
        private readonly UserManager<BankUser> userManager;

        public Disable2FaModel(UserManager<BankUser> userManager, ILogger<Disable2FaModel> logger,
            SignInManager<BankUser> signInManager)
        {
            this.userManager = userManager;
            this.logger = logger;
            this.signInManager = signInManager;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public async Task<IActionResult> OnGet()
        {
            var user = await userManager.GetUserAsync(User);
            if (user == null)
            {
                return Forbid();
            }

            if (!await userManager.GetTwoFactorEnabledAsync(user))
            {
                return RedirectToPage("./Index");
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await userManager.GetUserAsync(User);
            if (user == null)
            {
                return Forbid();
            }

            if (!await userManager.GetTwoFactorEnabledAsync(user))
            {
                return RedirectToPage("./Index");
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            var isPasswordCorrect = await userManager.CheckPasswordAsync(user, Input.Password);
            if (!isPasswordCorrect)
            {
                ShowErrorMessage(NotificationMessages.InvalidPassword);
                return Page();
            }

            string verificationCode = Input.Code.Replace(" ", string.Empty)
                .Replace("-", string.Empty);

            bool isTokenValid = await userManager.VerifyTwoFactorTokenAsync(user,
                userManager.Options.Tokens.AuthenticatorTokenProvider, verificationCode);

            if (!isTokenValid)
            {
                ShowErrorMessage(NotificationMessages.TwoFactorAuthenticationCodeInvalid);
                return Page();
            }

            var disable2FaResult = await userManager.SetTwoFactorEnabledAsync(user, false);
            if (!disable2FaResult.Succeeded)
            {
                ShowErrorMessage(NotificationMessages.TwoFactorAuthenticationDisableError);
                return Page();
            }

            await userManager.ResetAuthenticatorKeyAsync(user);

            await signInManager.RefreshSignInAsync(user);

            logger.LogInformation("User has disabled 2fa.");

            ShowSuccessMessage(NotificationMessages.TwoFactorAuthenticationDisabled);
            return RedirectToPage("./Index");
        }

        public class InputModel
        {
            [Required]
            [StringLength(7, ErrorMessage = "The {0} must be 6 digits long",
                MinimumLength = 6)]
            [DataType(DataType.Text)]
            [Display(Name = "Verification Code")]
            public string Code { get; set; }

            [Required]
            [DataType(DataType.Password)]
            public string Password { get; set; }
        }
    }
}
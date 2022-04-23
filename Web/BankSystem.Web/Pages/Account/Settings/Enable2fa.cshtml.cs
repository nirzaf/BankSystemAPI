namespace BankSystem.Web.Pages.Account.Settings
{
    using System.ComponentModel.DataAnnotations;
    using System.Text;
    using System.Text.Encodings.Web;
    using System.Threading.Tasks;
    using BankSystem.Models;
    using Common;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;

    [Authorize]
    public class Enable2FaModel : BasePageModel
    {
        private const string AuthenticatorUriFormat = "otpauth://totp/{0}:{1}?secret={2}&issuer={0}&digits=6";
        private readonly ILogger<Enable2FaModel> logger;
        private readonly SignInManager<BankUser> signInManager;
        private readonly UrlEncoder urlEncoder;
        private readonly UserManager<BankUser> userManager;

        public Enable2FaModel(UserManager<BankUser> userManager, ILogger<Enable2FaModel> logger,
            UrlEncoder urlEncoder, SignInManager<BankUser> signInManager)
        {
            this.userManager = userManager;
            this.logger = logger;
            this.urlEncoder = urlEncoder;
            this.signInManager = signInManager;
        }

        public string SharedKey { get; set; }

        public string AuthenticatorUri { get; set; }

        [BindProperty]
        public InputModel Input { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await userManager.GetUserAsync(User);
            if (user == null)
            {
                return Forbid();
            }

            if (await userManager.GetTwoFactorEnabledAsync(user))
            {
                return RedirectToPage("./Index");
            }

            await LoadSharedKeyAndQrCodeUriAsync(user, true);

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await userManager.GetUserAsync(User);
            if (user == null)
            {
                return Forbid();
            }

            if (await userManager.GetTwoFactorEnabledAsync(user))
            {
                return RedirectToPage("./Index");
            }

            if (!ModelState.IsValid)
            {
                await LoadSharedKeyAndQrCodeUriAsync(user);
                return Page();
            }

            var isPasswordCorrect = await userManager.CheckPasswordAsync(user, Input.Password);
            if (!isPasswordCorrect)
            {
                ShowErrorMessage(NotificationMessages.InvalidPassword);
                await LoadSharedKeyAndQrCodeUriAsync(user);
                return Page();
            }

            string verificationCode = Input.Code.Replace(" ", string.Empty)
                .Replace("-", string.Empty);

            bool isTokenValid = await userManager.VerifyTwoFactorTokenAsync(user,
                userManager.Options.Tokens.AuthenticatorTokenProvider, verificationCode);

            if (!isTokenValid)
            {
                ShowErrorMessage(NotificationMessages.TwoFactorAuthenticationCodeInvalid);
                await LoadSharedKeyAndQrCodeUriAsync(user);
                return Page();
            }

            await userManager.SetTwoFactorEnabledAsync(user, true);

            await signInManager.RefreshSignInAsync(user);

            logger.LogInformation("User has enabled 2FA with an authenticator app.");

            ShowSuccessMessage(NotificationMessages.TwoFactorAuthenticationEnabled);
            return RedirectToPage("./Index");
        }

        private async Task LoadSharedKeyAndQrCodeUriAsync(BankUser user, bool resetKey = false)
        {
            string unformattedKey;
            if (resetKey ||
                string.IsNullOrEmpty(unformattedKey = await userManager.GetAuthenticatorKeyAsync(user)))
            {
                await userManager.ResetAuthenticatorKeyAsync(user);
                unformattedKey = await userManager.GetAuthenticatorKeyAsync(user);

                await signInManager.RefreshSignInAsync(user);
            }

            SharedKey = FormatKey(unformattedKey);

            string email = await userManager.GetEmailAsync(user);
            AuthenticatorUri = GenerateQrCodeUri(email, unformattedKey);
        }

        private string FormatKey(string unformattedKey)
        {
            var result = new StringBuilder();
            var currentPosition = 0;
            while (currentPosition + 4 < unformattedKey.Length)
            {
                result.Append(unformattedKey.Substring(currentPosition, 4)).Append(" ");
                currentPosition += 4;
            }

            if (currentPosition < unformattedKey.Length)
            {
                result.Append(unformattedKey.Substring(currentPosition));
            }

            return result.ToString().ToLowerInvariant();
        }

        private string GenerateQrCodeUri(string email, string unformattedKey)
            => string.Format(
                AuthenticatorUriFormat,
                urlEncoder.Encode(nameof(BankSystem)),
                urlEncoder.Encode(email),
                unformattedKey);

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
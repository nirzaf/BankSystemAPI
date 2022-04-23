namespace BankSystem.Web.Pages.Account
{
    using BankSystem.Models;
    using Common;
    using Common.EmailSender.Interface;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;
    using Models;
    using System.ComponentModel.DataAnnotations;
    using System.Text.Encodings.Web;
    using System.Threading.Tasks;

    [AllowAnonymous]
    public class SendEmailVerificationModel : BasePageModel
    {
        private readonly UserManager<BankUser> userManager;
        private readonly SignInManager<BankUser> signInManager;
        private readonly IEmailSender emailSender;

        public SendEmailVerificationModel(
            UserManager<BankUser> userManager,
            SignInManager<BankUser> signInManager,
            IEmailSender emailSender)
        {
            this.userManager = userManager;
            this.signInManager = signInManager;
            this.emailSender = emailSender;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        [BindProperty]
        public ReCaptchaModel Recaptcha { get; set; }

        public IActionResult OnGet()
            => Page();

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var user = await signInManager.UserManager.FindByNameAsync(Input.Email);
            if (user == null)
            {
                ShowErrorMessage(NotificationMessages.TryAgainLaterError);
                return Page();
            }

            bool isEmailConfirmed = await signInManager.UserManager.IsEmailConfirmedAsync(user);
            if (isEmailConfirmed)
            {
                ShowErrorMessage(NotificationMessages.EmailAlreadyVerified);
                return RedirectToLoginPage();
            }

            var code = await userManager.GenerateEmailConfirmationTokenAsync(user);
            var callbackUrl = Url.Page(
                EmailMessages.EmailConfirmationPage,
                null,
                new { userId = user.Id, code },
                Request.Scheme);
            await emailSender.SendEmailAsync(GlobalConstants.BankSystemEmail, Input.Email,
                EmailMessages.ConfirmEmailSubject,
                string.Format(EmailMessages.EmailConfirmationMessage, HtmlEncoder.Default.Encode(callbackUrl)));

            ShowSuccessMessage(NotificationMessages.EmailVerificationLinkResentSuccessfully);
            return RedirectToHome();
        }

        public class InputModel
        {
            [Required]
            [EmailAddress]
            [Display(Name = "Email")]
            public string Email { get; set; }
        }

        public class ReCaptchaModel : BaseReCaptchaModel
        {
        }
    }
}
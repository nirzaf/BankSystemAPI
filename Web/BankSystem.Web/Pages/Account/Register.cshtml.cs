namespace BankSystem.Web.Pages.Account
{
    using BankSystem.Models;
    using Common;
    using Common.EmailSender.Interface;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;
    using Models;
    using System.ComponentModel.DataAnnotations;
    using System.Text.Encodings.Web;
    using System.Threading.Tasks;

    [AllowAnonymous]
    public class RegisterModel : BasePageModel
    {
        private readonly IEmailSender emailSender;
        private readonly ILogger<RegisterModel> logger;
        private readonly UserManager<BankUser> userManager;

        public RegisterModel(
            UserManager<BankUser> userManager,
            ILogger<RegisterModel> logger, IEmailSender emailSender)
        {
            this.userManager = userManager;
            this.logger = logger;
            this.emailSender = emailSender;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        [BindProperty]
        public ReCaptchaModel Recaptcha { get; set; }

        public string ReturnUrl { get; set; }

        public IActionResult OnGet(string returnUrl = null)
        {
            returnUrl = returnUrl ?? Url.Content("~/");

            if (User.Identity.IsAuthenticated)
            {
                return LocalRedirect(returnUrl);
            }

            ReturnUrl = returnUrl;

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl = returnUrl ?? Url.Content("~/");

            if (User.Identity.IsAuthenticated)
            {
                return LocalRedirect(returnUrl);
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            var user = new BankUser
            {
                UserName = Input.Email,
                Email = Input.Email,
                FullName = Input.FullName
            };

            var result = await userManager.CreateAsync(user, Input.Password);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                return Page();
            }

            logger.LogInformation("User created a new account with password.");

            var code = await userManager.GenerateEmailConfirmationTokenAsync(user);
            var callbackUrl = Url.Page(
                EmailMessages.EmailConfirmationPage,
                null,
                new { userId = user.Id, code },
                Request.Scheme);
            await emailSender.SendEmailAsync(GlobalConstants.BankSystemEmail, Input.Email,
                EmailMessages.ConfirmEmailSubject,
                string.Format(EmailMessages.EmailConfirmationMessage, HtmlEncoder.Default.Encode(callbackUrl)));

            ShowSuccessMessage(NotificationMessages.SuccessfulRegistration);
            return RedirectToLoginPage();
        }

        public class InputModel
        {
            [Required]
            [EmailAddress]
            [Display(Name = "Email")]
            public string Email { get; set; }

            [Required]
            [MaxLength(ModelConstants.User.FullNameMaxLength)]
            [Display(Name = "Full Name")]
            public string FullName { get; set; }

            [Required]
            [StringLength(ModelConstants.User.PasswordMaxLength, MinimumLength = ModelConstants.User.PasswordMinLength)]
            [DataType(DataType.Password)]
            [RegularExpression(ModelConstants.User.PasswordRegex,
                ErrorMessage = ModelConstants.User.PasswordErrorMessage)]
            public string Password { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Confirm password")]
            [Compare(nameof(Password), ErrorMessage = "The password and confirmation password do not match.")]
            public string ConfirmPassword { get; set; }
        }

        public class ReCaptchaModel : BaseReCaptchaModel
        {
        }
    }
}
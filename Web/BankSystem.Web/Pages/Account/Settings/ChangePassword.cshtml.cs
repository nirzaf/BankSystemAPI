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
    public class ChangePasswordModel : BasePageModel
    {
        private readonly ILogger<ChangePasswordModel> logger;
        private readonly SignInManager<BankUser> signInManager;
        private readonly UserManager<BankUser> userManager;

        public ChangePasswordModel(
            UserManager<BankUser> userManager,
            SignInManager<BankUser> signInManager,
            ILogger<ChangePasswordModel> logger)
        {
            this.userManager = userManager;
            this.signInManager = signInManager;
            this.logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await userManager.GetUserAsync(User);
            if (user == null)
            {
                return Forbid();
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var user = await userManager.GetUserAsync(User);
            if (user == null)
            {
                return Forbid();
            }

            var changePasswordResult =
                await userManager.ChangePasswordAsync(user, Input.OldPassword, Input.NewPassword);
            if (!changePasswordResult.Succeeded)
            {
                foreach (var error in changePasswordResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                return Page();
            }

            await signInManager.RefreshSignInAsync(user);
            logger.LogInformation("User changed their password successfully.");

            ShowSuccessMessage(NotificationMessages.PasswordChangeSuccessful);
            return RedirectToPage("./Index");
        }

        public class InputModel
        {
            [Required]
            [DataType(DataType.Password)]
            [Display(Name = "Current password")]
            public string OldPassword { get; set; }

            [Required]
            [DataType(DataType.Password)]
            [Display(Name = "New password")]
            [StringLength(ModelConstants.User.PasswordMaxLength, MinimumLength = ModelConstants.User.PasswordMinLength)]
            [RegularExpression(ModelConstants.User.PasswordRegex,
                ErrorMessage = ModelConstants.User.PasswordErrorMessage)]
            public string NewPassword { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Confirm new password")]
            [Compare(nameof(NewPassword), ErrorMessage = "The new password and confirmation password do not match.")]
            public string ConfirmPassword { get; set; }
        }
    }
}
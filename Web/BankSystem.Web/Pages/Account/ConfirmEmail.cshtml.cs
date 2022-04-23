namespace BankSystem.Web.Pages.Account
{
    using BankSystem.Models;
    using Common;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;
    using System.Threading.Tasks;

    [AllowAnonymous]
    public class ConfirmEmail : BasePageModel
    {
        private readonly UserManager<BankUser> userManager;

        public ConfirmEmail(UserManager<BankUser> userManager)
            => this.userManager = userManager;

        public async Task<IActionResult> OnGetAsync(string userId, string code)
        {
            if (userId == null || code == null)
            {
                ShowErrorMessage(NotificationMessages.TryAgainLaterError);
                return RedirectToHome();
            }

            var user = await userManager.FindByIdAsync(userId);
            if (user == null)
            {
                ShowErrorMessage(NotificationMessages.AccountDoesNotExist);
                return RedirectToHome();
            }

            var result = await userManager.ConfirmEmailAsync(user, code);
            if (!result.Succeeded)
            {
                ShowErrorMessage(NotificationMessages.EmailVerificationFailed);
                return RedirectToHome();
            }

            ShowSuccessMessage(NotificationMessages.SuccessfulEmailVerification);
            return RedirectToLoginPage();
        }
    }
}
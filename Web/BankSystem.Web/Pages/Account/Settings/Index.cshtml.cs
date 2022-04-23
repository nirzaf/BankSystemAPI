namespace BankSystem.Web.Pages.Account.Settings
{
    using System.Threading.Tasks;
    using BankSystem.Models;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;

    [Authorize]
    public class IndexModel : BasePageModel
    {
        private readonly UserManager<BankUser> userManager;

        public IndexModel(
            UserManager<BankUser> userManager)
            => this.userManager = userManager;

        public string Email { get; set; }

        public string FullName { get; set; }

        public bool TwoFactorEnabled { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await userManager.GetUserAsync(User);
            if (user == null)
            {
                return Forbid();
            }

            FullName = user.FullName;
            Email = user.Email;
            TwoFactorEnabled = user.TwoFactorEnabled;

            return Page();
        }
    }
}
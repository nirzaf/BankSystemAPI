namespace BankSystem.Web.Pages
{
    using Common;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;

    public abstract class BasePageModel : PageModel
    {
        public IActionResult RedirectToHome() => RedirectToAction("Index", "Home");

        public IActionResult RedirectToLoginPage()
            => RedirectToPage("/Account/Login");

        protected void ShowErrorMessage(string message)
        {
            TempData[GlobalConstants.TempDataErrorMessageKey] = message;
        }

        protected void ShowSuccessMessage(string message)
        {
            TempData[GlobalConstants.TempDataSuccessMessageKey] = message;
        }
    }
}
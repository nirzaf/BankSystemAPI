namespace BankSystem.Web.Controllers
{
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;

    [AllowAnonymous]
    public class ErrorsController : BaseController
    {
        [Route("error/404")]
        public IActionResult Error404()
            => View();

        [Route("error/403")]
        public IActionResult Error403()
            => View();

        [Route("error/{code:int}")]
        public IActionResult Error(int code)
            => View();
    }
}
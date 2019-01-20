using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VerifyWithAspNetCore2.Models;

namespace VerifyWithAspNetCore2.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult About()
        {
            ViewData["Message"] = "Your application description page.";

            return View();
        }

        public IActionResult Contact()
        {
            ViewData["Message"] = "Your contact page.";

            return View();
        }

        // Integrate with Criipto Verify, part 4:
        //  - reference the named authorization policy as configured in the ConfigureServices stage, and let the middleware handle the authentication flow.
        [Authorize(Policy = "CriiptoVerifyAuthenticatedUser")]
        public IActionResult Login()
        {
            return this.View();
        }

        [Authorize(Policy = "CriiptoVerifyAuthenticatedUser")]
        public IActionResult UserClaims()
        {
            var viewModels =
                this.User.Claims
                    .Select(c => new ClaimViewModel { ClaimType = c.Type, ClaimValue = c.Value })
                    .ToList();
            return View(viewModels);
        }

        public async Task Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignOutAsync("CriiptoVerify");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}

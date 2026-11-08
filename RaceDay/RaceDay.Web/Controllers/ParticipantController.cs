using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers
{
    [RequireRole("Participant")]
    public class ParticipantController : Controller
    {
        private readonly ApiClient _api;

        public ParticipantController(ApiClient api) => _api = api;

        // If the API session has ended, send the user back to login instead of showing an error.
        private IActionResult? SessionExpired(ApiResponse res)
        {
            if (res.StatusCode != 401) return null;
            HttpContext.Session.Clear();
            TempData["Error"] = "Your session has ended. Please log in again.";
            return RedirectToAction("Login", "Account");
        }

        public async Task<IActionResult> MyEvents()
        {
            var res = await _api.GetAsync("api/enrolments/mine");
            if (SessionExpired(res) is { } redirect) return redirect;

            if (!res.IsSuccess)
            {
                TempData["Error"] = res.Message;
                return View(new List<EnrolmentItem>());
            }
            return View(res.As<List<EnrolmentItem>>() ?? new());
        }

        public async Task<IActionResult> MyResults()
        {
            var res = await _api.GetAsync("api/results/mine");
            if (SessionExpired(res) is { } redirect) return redirect;

            if (!res.IsSuccess)
            {
                TempData["Error"] = res.Message;
                return View(new List<ResultItem>());
            }
            return View(res.As<List<ResultItem>>() ?? new());
        }

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var res = await _api.GetAsync("api/users/me");
            if (SessionExpired(res) is { } redirect) return redirect;

            if (!res.IsSuccess)
            {
                TempData["Error"] = res.Message;
                return RedirectToAction("Index", "Home");
            }
            return View(res.As<ProfileViewModel>() ?? new ProfileViewModel());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(ProfileViewModel model)
        {
            var current = await _api.GetAsync("api/users/me");
            if (SessionExpired(current) is { } redirect) return redirect;
            var existing = current.As<ProfileViewModel>();

            // The API replaces the picture URL on every update, so we send the current one back.
            model.Email = existing?.Email ?? string.Empty;
            model.Role = existing?.Role ?? string.Empty;
            model.ProfilePictureUrl = existing?.ProfilePictureUrl;

            if (!ModelState.IsValid) return View(model);

            var res = await _api.PutAsync("api/users/me",
                new { fullName = model.FullName, profilePictureUrl = model.ProfilePictureUrl });

            if (!res.IsSuccess)
            {
                ModelState.AddModelError(string.Empty, res.Message);
                return View(model);
            }

            HttpContext.Session.SetString("FullName", model.FullName);
            TempData["Success"] = "Your profile has been updated.";
            return RedirectToAction(nameof(Profile));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadPicture(IFormFile? file)
        {
            if (file == null || file.Length == 0)
            {
                TempData["Error"] = "Please choose an image first.";
                return RedirectToAction(nameof(Profile));
            }

            var res = await _api.UploadAsync("api/users/me/picture", file);
            if (SessionExpired(res) is { } redirect) return redirect;

            if (res.IsSuccess) TempData["Success"] = "Your profile picture has been updated.";
            else TempData["Error"] = res.Message;

            return RedirectToAction(nameof(Profile));
        }
    }
}
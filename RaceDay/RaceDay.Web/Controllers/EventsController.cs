using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers
{
    public class EventsController : Controller
    {
        private readonly ApiClient _api;

        public EventsController(ApiClient api) => _api = api;

        public async Task<IActionResult> Details(int id)
        {
            var res = await _api.GetAsync($"api/events/{id}");
            var model = res.IsSuccess ? res.As<EventDetailModel>() : null;

            if (model == null)
            {
                TempData["Error"] = "That event could not be found.";
                return RedirectToAction("Index", "Home");
            }

            return View(model);
        }

        [RequireRole("Participant")]
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Enrol(int eventId, int categoryId)
        {
            var res = await _api.PostAsync("api/enrolments", new { categoryId });

            if (res.IsSuccess)
            {
                TempData["Success"] = "You're in! Your entry has been recorded.";
                return RedirectToAction("MyEvents", "Participant");
            }

            TempData["Error"] = res.Message;
            return RedirectToAction(nameof(Details), new { id = eventId });
        }
    }
}
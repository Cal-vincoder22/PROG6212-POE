using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers
{
    [RequireRole("Organiser")]
    public class OrganiserController : Controller
    {
        private readonly ApiClient _api;
        public OrganiserController(ApiClient api) => _api = api;

        // If your Swagger route for an event's enrolments is different, change it here only.
        private static string EnrolmentsRoute(int eventId) => $"api/events/{eventId}/enrolments";

        private IActionResult? Expired(ApiResponse res)
        {
            if (res.StatusCode != 401) return null;
            HttpContext.Session.Clear();
            TempData["Error"] = "Your session has ended. Please log in again.";
            return RedirectToAction("Login", "Account");
        }

        private static object EventPayload(EventFormModel m) => new
        {
            name = m.Name,
            description = m.Description,
            eventDate = m.EventDate,
            location = m.Location,
            distanceKm = m.DistanceKm,
            eventType = m.EventType
        };

        // ---------- Dashboard ----------
        public async Task<IActionResult> Index()
        {
            var res = await _api.GetAsync("api/events/mine");
            if (Expired(res) is { } r) return r;
            if (!res.IsSuccess)
            {
                TempData["Error"] = res.Message;
                return View(new List<OrganiserEventItem>());
            }
            return View(res.As<List<OrganiserEventItem>>() ?? new());
        }

        // ---------- Create / edit ----------
        [HttpGet]
        public IActionResult Create() => View("Form", new EventFormModel());

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(EventFormModel model)
        {
            if (!ModelState.IsValid) return View("Form", model);
            var res = await _api.PostAsync("api/events", EventPayload(model));
            if (Expired(res) is { } r) return r;
            if (!res.IsSuccess)
            {
                ModelState.AddModelError(string.Empty, res.Message);
                return View("Form", model);
            }
            TempData["Success"] = "Event created. Add categories so participants can enter.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var res = await _api.GetAsync($"api/events/{id}");
            var e = res.IsSuccess ? res.As<EventDetailModel>() : null;
            if (e == null)
            {
                TempData["Error"] = "That event could not be found.";
                return RedirectToAction(nameof(Index));
            }
            return View("Form", new EventFormModel
            {
                EventId = e.EventId,
                Name = e.Name,
                Description = e.Description,
                EventDate = e.EventDate,
                Location = e.Location,
                DistanceKm = e.DistanceKm,
                EventType = e.EventType
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, EventFormModel model)
        {
            model.EventId = id;
            if (!ModelState.IsValid) return View("Form", model);
            var res = await _api.PutAsync($"api/events/{id}", EventPayload(model));
            if (Expired(res) is { } r) return r;
            if (!res.IsSuccess)
            {
                ModelState.AddModelError(string.Empty, res.Message);
                return View("Form", model);
            }
            TempData["Success"] = "Event updated.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var res = await _api.DeleteAsync($"api/events/{id}");
            if (Expired(res) is { } r) return r;
            if (res.IsSuccess) TempData["Success"] = "Event deleted.";
            else TempData["Error"] = res.Message;
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadBanner(int id, IFormFile? file)
        {
            if (file == null || file.Length == 0)
            {
                TempData["Error"] = "Please choose an image first.";
                return RedirectToAction(nameof(Index));
            }
            var res = await _api.UploadAsync($"api/events/{id}/banner", file);
            if (Expired(res) is { } r) return r;
            if (res.IsSuccess) TempData["Success"] = "Banner image updated.";
            else TempData["Error"] = res.Message;
            return RedirectToAction(nameof(Index));
        }

        // ---------- Categories ----------
        public async Task<IActionResult> Categories(int id)
        {
            var res = await _api.GetAsync($"api/events/{id}");
            var e = res.IsSuccess ? res.As<EventDetailModel>() : null;
            if (e == null)
            {
                TempData["Error"] = "That event could not be found.";
                return RedirectToAction(nameof(Index));
            }
            return View(e);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AddCategory(int id, CategoryFormModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Please enter a category name.";
                return RedirectToAction(nameof(Categories), new { id });
            }
            var res = await _api.PostAsync($"api/events/{id}/categories", new
            {
                categoryName = model.CategoryName,
                minAge = model.MinAge,
                maxAge = model.MaxAge,
                distanceKm = model.DistanceKm
            });
            if (Expired(res) is { } r) return r;
            if (res.IsSuccess) TempData["Success"] = "Category added.";
            else TempData["Error"] = res.Message;
            return RedirectToAction(nameof(Categories), new { id });
        }

        // ---------- Entries and results ----------
        public async Task<IActionResult> Enrolments(int id)
        {
            var ev = await _api.GetAsync($"api/events/{id}");
            var e = ev.IsSuccess ? ev.As<EventDetailModel>() : null;
            if (e == null)
            {
                TempData["Error"] = "That event could not be found.";
                return RedirectToAction(nameof(Index));
            }
            ViewBag.EventName = e.Name;
            ViewBag.EventId = id;

            var res = await _api.GetAsync(EnrolmentsRoute(id));
            if (Expired(res) is { } r) return r;
            if (!res.IsSuccess)
            {
                TempData["Error"] = res.Message;
                return View(new List<EventEnrolmentItem>());
            }
            return View(res.As<List<EventEnrolmentItem>>() ?? new());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CaptureResult(int eventId, int enrolmentId, string finishTime, int finishPosition)
        {
            if (!TimeSpan.TryParse(finishTime, out var ts) || ts <= TimeSpan.Zero || finishPosition < 1)
            {
                TempData["Error"] = "Enter the finish time as hh:mm:ss and a position of 1 or more.";
                return RedirectToAction(nameof(Enrolments), new { id = eventId });
            }
            var res = await _api.PostAsync($"api/enrolments/{enrolmentId}/result",
                new { finishTime = ts.ToString(@"hh\:mm\:ss"), finishPosition });
            if (Expired(res) is { } r) return r;
            if (res.IsSuccess) TempData["Success"] = "Result saved. The participant can now see it.";
            else TempData["Error"] = res.Message;
            return RedirectToAction(nameof(Enrolments), new { id = eventId });
        }
    }
}
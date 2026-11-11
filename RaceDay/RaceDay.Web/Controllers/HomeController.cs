using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApiClient _api;

        public HomeController(ApiClient api) => _api = api;

        public async Task<IActionResult> Index(string? eventType, bool upcomingOnly = true)
        {
            var url = $"api/events?upcomingOnly={upcomingOnly.ToString().ToLower()}";
            if (!string.IsNullOrWhiteSpace(eventType))
                url += $"&eventType={Uri.EscapeDataString(eventType)}";

            var vm = new HomeViewModel { EventType = eventType, UpcomingOnly = upcomingOnly };
            var res = await _api.GetAsync(url);

            if (res.IsSuccess) vm.Events = res.As<List<EventListItem>>() ?? new();
            else vm.Error = "We couldn't load events right now. Please try again shortly.";

            return View(vm);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error() =>
            View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
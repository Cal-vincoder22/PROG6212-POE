using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Controllers;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;
using Xunit;

namespace RaceDay.Tests
{
    public class EventsControllerTests
    {
        private static EventDto ValidDto(string name = "Spring 10K") => new()
        {
            Name = name,
            Description = "A test event",
            EventDate = DateTime.UtcNow.AddDays(20),
            Location = "Cape Town",
            DistanceKm = 10m,
            EventType = "Run"
        };

        private static Event AddEvent(RaceDayDbContext db, int organiserId, string type, DateTime date, string name = "Extra Event")
        {
            var ev = new Event
            {
                Name = name,
                OrganiserId = organiserId,
                EventDate = date,
                Location = "Durban",
                DistanceKm = 5m,
                EventType = type
            };
            db.Events.Add(ev);
            db.SaveChanges();
            return ev;
        }

        [Fact]
        public async Task GetEvents_NoFilter_ReturnsAllEvents()
        {
            using var db = TestHelper.CreateDb();
            var s = TestHelper.Seed(db);
            AddEvent(db, s.OrganiserId, "Walk", DateTime.UtcNow.AddDays(10));
            var ctrl = new EventsController(db).WithSession(s.ParticipantId, "Participant");

            var result = await ctrl.GetEvents(null, false);

            var ok = Assert.IsType<OkObjectResult>(result);
            var list = Assert.IsAssignableFrom<System.Collections.IEnumerable>(ok.Value);
            Assert.Equal(2, list.Cast<object>().Count());
        }

        [Fact]
        public async Task GetEvents_FilterByType_ReturnsOnlyMatching()
        {
            using var db = TestHelper.CreateDb();
            var s = TestHelper.Seed(db);
            AddEvent(db, s.OrganiserId, "Walk", DateTime.UtcNow.AddDays(10));
            AddEvent(db, s.OrganiserId, "Cycle", DateTime.UtcNow.AddDays(12));
            var ctrl = new EventsController(db).WithSession(s.ParticipantId, "Participant");

            var result = await ctrl.GetEvents("Walk", false);

            var ok = Assert.IsType<OkObjectResult>(result);
            var list = Assert.IsAssignableFrom<System.Collections.IEnumerable>(ok.Value);
            Assert.Single(list.Cast<object>());
        }

        [Fact]
        public async Task GetEvents_UpcomingOnly_ExcludesPastEvents()
        {
            using var db = TestHelper.CreateDb();
            var s = TestHelper.Seed(db);
            AddEvent(db, s.OrganiserId, "Run", DateTime.UtcNow.AddDays(-30), "Old Event");
            var ctrl = new EventsController(db).WithSession(s.ParticipantId, "Participant");

            var result = await ctrl.GetEvents(null, true);

            var ok = Assert.IsType<OkObjectResult>(result);
            var list = Assert.IsAssignableFrom<System.Collections.IEnumerable>(ok.Value);
            Assert.Single(list.Cast<object>());
        }

        [Fact]
        public async Task GetEvent_Existing_ReturnsOk()
        {
            using var db = TestHelper.CreateDb();
            var s = TestHelper.Seed(db);
            var ctrl = new EventsController(db).WithSession(s.ParticipantId, "Participant");

            var result = await ctrl.GetEvent(s.EventId);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task GetEvent_Unknown_ReturnsNotFound()
        {
            using var db = TestHelper.CreateDb();
            var s = TestHelper.Seed(db);
            var ctrl = new EventsController(db).WithSession(s.ParticipantId, "Participant");

            var result = await ctrl.GetEvent(9999);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task CreateEvent_ValidDto_ReturnsCreatedAndSetsOwner()
        {
            using var db = TestHelper.CreateDb();
            var s = TestHelper.Seed(db);
            var ctrl = new EventsController(db).WithSession(s.Organiser2Id, "Organiser");

            var result = await ctrl.CreateEvent(ValidDto("New Race"));

            Assert.IsType<CreatedAtActionResult>(result);
            var saved = db.Events.Single(e => e.Name == "New Race");
            Assert.Equal(s.Organiser2Id, saved.OrganiserId);
        }

        [Fact]
        public async Task UpdateEvent_Owner_ReturnsOkAndChangesName()
        {
            using var db = TestHelper.CreateDb();
            var s = TestHelper.Seed(db);
            var ctrl = new EventsController(db).WithSession(s.OrganiserId, "Organiser");

            var result = await ctrl.UpdateEvent(s.EventId, ValidDto("Renamed Race"));

            Assert.IsType<OkObjectResult>(result);
            Assert.Equal("Renamed Race", db.Events.Find(s.EventId)!.Name);
        }

        [Fact]
        public async Task UpdateEvent_NotOwner_Returns403AndDoesNotChange()
        {
            using var db = TestHelper.CreateDb();
            var s = TestHelper.Seed(db);
            var ctrl = new EventsController(db).WithSession(s.Organiser2Id, "Organiser");

            var result = await ctrl.UpdateEvent(s.EventId, ValidDto("Hijacked"));

            var obj = Assert.IsType<ObjectResult>(result);
            Assert.Equal(403, obj.StatusCode);
            Assert.Equal("Test 10K", db.Events.Find(s.EventId)!.Name);
        }

        [Fact]
        public async Task UpdateEvent_Unknown_ReturnsNotFound()
        {
            using var db = TestHelper.CreateDb();
            var s = TestHelper.Seed(db);
            var ctrl = new EventsController(db).WithSession(s.OrganiserId, "Organiser");

            var result = await ctrl.UpdateEvent(9999, ValidDto());

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task DeleteEvent_Owner_ReturnsNoContent()
        {
            using var db = TestHelper.CreateDb();
            var s = TestHelper.Seed(db);
            var ctrl = new EventsController(db).WithSession(s.OrganiserId, "Organiser");

            var result = await ctrl.DeleteEvent(s.EventId);

            Assert.IsType<NoContentResult>(result);
            Assert.Null(db.Events.Find(s.EventId));
        }

        [Fact]
        public async Task DeleteEvent_NotOwner_Returns403AndKeepsEvent()
        {
            using var db = TestHelper.CreateDb();
            var s = TestHelper.Seed(db);
            var ctrl = new EventsController(db).WithSession(s.Organiser2Id, "Organiser");

            var result = await ctrl.DeleteEvent(s.EventId);

            var obj = Assert.IsType<ObjectResult>(result);
            Assert.Equal(403, obj.StatusCode);
            Assert.NotNull(db.Events.Find(s.EventId));
        }

        [Fact]
        public async Task DeleteEvent_WithEnrolments_ReturnsConflict()
        {
            using var db = TestHelper.CreateDb();
            var s = TestHelper.Seed(db);
            db.Enrolments.Add(new Enrolment { ParticipantId = s.ParticipantId, EventId = s.EventId, CategoryId = s.CategoryId });
            db.SaveChanges();
            var ctrl = new EventsController(db).WithSession(s.OrganiserId, "Organiser");

            var result = await ctrl.DeleteEvent(s.EventId);

            Assert.IsType<ConflictObjectResult>(result);
            Assert.NotNull(db.Events.Find(s.EventId));
        }

        [Fact]
        public async Task DeleteEvent_Unknown_ReturnsNotFound()
        {
            using var db = TestHelper.CreateDb();
            var s = TestHelper.Seed(db);
            var ctrl = new EventsController(db).WithSession(s.OrganiserId, "Organiser");

            var result = await ctrl.DeleteEvent(9999);

            Assert.IsType<NotFoundObjectResult>(result);
        }
    }
}
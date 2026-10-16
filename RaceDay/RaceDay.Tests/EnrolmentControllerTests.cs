using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Controllers;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;
using Xunit;

namespace RaceDay.Tests
{
    public class EnrolmentsControllerTests
    {
        [Fact]
        public async Task Enrol_ValidCategory_ReturnsCreated()
        {
            using var db = TestHelper.CreateDb();
            var s = TestHelper.Seed(db);
            var ctrl = new EnrolmentsController(db).WithSession(s.ParticipantId, "Participant");

            var result = await ctrl.Enrol(new CreateEnrolmentDto { CategoryId = s.CategoryId });

            Assert.IsType<CreatedAtActionResult>(result.Result);
            Assert.Equal(1, db.Enrolments.Count());
        }

        [Fact]
        public async Task Enrol_Duplicate_ReturnsConflict()
        {
            using var db = TestHelper.CreateDb();
            var s = TestHelper.Seed(db);
            var ctrl = new EnrolmentsController(db).WithSession(s.ParticipantId, "Participant");

            await ctrl.Enrol(new CreateEnrolmentDto { CategoryId = s.CategoryId });
            var second = await ctrl.Enrol(new CreateEnrolmentDto { CategoryId = s.CategoryId });

            Assert.IsType<ConflictObjectResult>(second.Result);
            Assert.Equal(1, db.Enrolments.Count());
        }

        [Fact]
        public async Task Enrol_UnknownCategory_ReturnsNotFound()
        {
            using var db = TestHelper.CreateDb();
            var s = TestHelper.Seed(db);
            var ctrl = new EnrolmentsController(db).WithSession(s.ParticipantId, "Participant");

            var result = await ctrl.Enrol(new CreateEnrolmentDto { CategoryId = 9999 });

            Assert.IsType<NotFoundObjectResult>(result.Result);
        }

        [Fact]
        public async Task Enrol_PastEvent_ReturnsBadRequest()
        {
            using var db = TestHelper.CreateDb();
            var s = TestHelper.Seed(db, pastEvent: true);
            var ctrl = new EnrolmentsController(db).WithSession(s.ParticipantId, "Participant");

            var result = await ctrl.Enrol(new CreateEnrolmentDto { CategoryId = s.CategoryId });

            Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Empty(db.Enrolments);
        }

        [Fact]
        public async Task GetMine_ReturnsOnlyOwnEnrolments()
        {
            using var db = TestHelper.CreateDb();
            var s = TestHelper.Seed(db);
            db.Enrolments.Add(new Enrolment { ParticipantId = s.ParticipantId, EventId = s.EventId, CategoryId = s.CategoryId });
            db.Enrolments.Add(new Enrolment { ParticipantId = s.Participant2Id, EventId = s.EventId, CategoryId = s.CategoryId });
            db.SaveChanges();
            var ctrl = new EnrolmentsController(db).WithSession(s.ParticipantId, "Participant");

            var result = await ctrl.GetMine();

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var list = Assert.IsType<List<EnrolmentDto>>(ok.Value);
            Assert.Single(list);
        }

        [Fact]
        public async Task GetForEvent_OwnerOrganiser_ReturnsList()
        {
            using var db = TestHelper.CreateDb();
            var s = TestHelper.Seed(db);
            db.Enrolments.Add(new Enrolment { ParticipantId = s.ParticipantId, EventId = s.EventId, CategoryId = s.CategoryId });
            db.SaveChanges();
            var ctrl = new EnrolmentsController(db).WithSession(s.OrganiserId, "Organiser");

            var result = await ctrl.GetForEvent(s.EventId);

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var list = Assert.IsType<List<EventEnrolmentDto>>(ok.Value);
            Assert.Single(list);
        }

        [Fact]
        public async Task GetForEvent_NotOwner_ReturnsForbid()
        {
            using var db = TestHelper.CreateDb();
            var s = TestHelper.Seed(db);
            var ctrl = new EnrolmentsController(db).WithSession(s.Organiser2Id, "Organiser");

            var result = await ctrl.GetForEvent(s.EventId);

            Assert.IsType<ForbidResult>(result.Result);
        }

        [Fact]
        public async Task GetForEvent_UnknownEvent_ReturnsNotFound()
        {
            using var db = TestHelper.CreateDb();
            var s = TestHelper.Seed(db);
            var ctrl = new EnrolmentsController(db).WithSession(s.OrganiserId, "Organiser");

            var result = await ctrl.GetForEvent(9999);

            Assert.IsType<NotFoundResult>(result.Result);
        }
    }
}
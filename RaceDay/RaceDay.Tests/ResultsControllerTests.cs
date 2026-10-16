using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Controllers;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;
using Xunit;

namespace RaceDay.Tests
{
    public class ResultsControllerTests
    {
        private static CaptureResultDto Valid() =>
            new() { FinishTime = TimeSpan.FromMinutes(105), FinishPosition = 3 };

        private static int AddEnrolment(RaceDay.Api.Data.RaceDayDbContext db, SeedData s, string status = "Confirmed")
        {
            var e = new Enrolment { ParticipantId = s.ParticipantId, EventId = s.EventId, CategoryId = s.CategoryId, Status = status };
            db.Enrolments.Add(e);
            db.SaveChanges();
            return e.EnrolmentId;
        }

        [Fact]
        public async Task Capture_OwnerOrganiser_ReturnsCreated()
        {
            using var db = TestHelper.CreateDb();
            var s = TestHelper.Seed(db);
            var enrolmentId = AddEnrolment(db, s);
            var ctrl = new ResultsController(db).WithSession(s.OrganiserId, "Organiser");

            var result = await ctrl.Capture(enrolmentId, Valid());

            Assert.IsType<CreatedAtActionResult>(result.Result);
            Assert.Single(db.Results);
        }

        [Fact]
        public async Task Capture_NotOwner_ReturnsForbid()
        {
            using var db = TestHelper.CreateDb();
            var s = TestHelper.Seed(db);
            var enrolmentId = AddEnrolment(db, s);
            var ctrl = new ResultsController(db).WithSession(s.Organiser2Id, "Organiser");

            var result = await ctrl.Capture(enrolmentId, Valid());

            Assert.IsType<ForbidResult>(result.Result);
            Assert.Empty(db.Results);
        }

        [Fact]
        public async Task Capture_Duplicate_ReturnsConflict()
        {
            using var db = TestHelper.CreateDb();
            var s = TestHelper.Seed(db);
            var enrolmentId = AddEnrolment(db, s);
            var ctrl = new ResultsController(db).WithSession(s.OrganiserId, "Organiser");

            await ctrl.Capture(enrolmentId, Valid());
            var second = await ctrl.Capture(enrolmentId, Valid());

            Assert.IsType<ConflictObjectResult>(second.Result);
        }

        [Fact]
        public async Task Capture_UnknownEnrolment_ReturnsNotFound()
        {
            using var db = TestHelper.CreateDb();
            var s = TestHelper.Seed(db);
            var ctrl = new ResultsController(db).WithSession(s.OrganiserId, "Organiser");

            var result = await ctrl.Capture(9999, Valid());

            Assert.IsType<NotFoundObjectResult>(result.Result);
        }

        [Fact]
        public async Task Capture_CancelledEnrolment_ReturnsBadRequest()
        {
            using var db = TestHelper.CreateDb();
            var s = TestHelper.Seed(db);
            var enrolmentId = AddEnrolment(db, s, "Cancelled");
            var ctrl = new ResultsController(db).WithSession(s.OrganiserId, "Organiser");

            var result = await ctrl.Capture(enrolmentId, Valid());

            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task Capture_ZeroFinishTime_ReturnsBadRequest()
        {
            using var db = TestHelper.CreateDb();
            var s = TestHelper.Seed(db);
            var enrolmentId = AddEnrolment(db, s);
            var ctrl = new ResultsController(db).WithSession(s.OrganiserId, "Organiser");

            var result = await ctrl.Capture(enrolmentId, new CaptureResultDto { FinishTime = TimeSpan.Zero, FinishPosition = 1 });

            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task Update_OwnerOrganiser_ReturnsOkWithNewValues()
        {
            using var db = TestHelper.CreateDb();
            var s = TestHelper.Seed(db);
            var enrolmentId = AddEnrolment(db, s);
            var ctrl = new ResultsController(db).WithSession(s.OrganiserId, "Organiser");
            await ctrl.Capture(enrolmentId, Valid());
            var resultId = db.Results.Single().ResultId;

            var result = await ctrl.Update(resultId, new CaptureResultDto { FinishTime = TimeSpan.FromMinutes(100), FinishPosition = 1 });

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var dto = Assert.IsType<ResultDto>(ok.Value);
            Assert.Equal(1, dto.FinishPosition);
        }

        [Fact]
        public async Task Update_NotOwner_ReturnsForbid()
        {
            using var db = TestHelper.CreateDb();
            var s = TestHelper.Seed(db);
            var enrolmentId = AddEnrolment(db, s);
            await new ResultsController(db).WithSession(s.OrganiserId, "Organiser").Capture(enrolmentId, Valid());
            var resultId = db.Results.Single().ResultId;
            var other = new ResultsController(db).WithSession(s.Organiser2Id, "Organiser");

            var result = await other.Update(resultId, Valid());

            Assert.IsType<ForbidResult>(result.Result);
        }

        [Fact]
        public async Task GetMine_ReturnsOnlyOwnResults()
        {
            using var db = TestHelper.CreateDb();
            var s = TestHelper.Seed(db);
            var mine = AddEnrolment(db, s);
            var other = new Enrolment { ParticipantId = s.Participant2Id, EventId = s.EventId, CategoryId = s.CategoryId };
            db.Enrolments.Add(other);
            db.SaveChanges();
            var org = new ResultsController(db).WithSession(s.OrganiserId, "Organiser");
            await org.Capture(mine, Valid());
            await org.Capture(other.EnrolmentId, Valid());
            var ctrl = new ResultsController(db).WithSession(s.ParticipantId, "Participant");

            var result = await ctrl.GetMine();

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Single(Assert.IsType<List<ResultDto>>(ok.Value));
        }
    }
}
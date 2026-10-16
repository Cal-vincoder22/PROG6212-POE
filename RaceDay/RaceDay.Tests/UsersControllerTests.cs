using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Controllers;
using RaceDay.Api.Dtos;
using Xunit;

namespace RaceDay.Tests
{
    public class UsersControllerTests
    {
        [Fact]
        public async Task GetMe_ReturnsLoggedInUsersProfile()
        {
            using var db = TestHelper.CreateDb();
            var s = TestHelper.Seed(db);
            var ctrl = new UsersController(db).WithSession(s.ParticipantId, "Participant");

            var result = await ctrl.GetMe();

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var dto = Assert.IsType<UserProfileDto>(ok.Value);
            Assert.Equal("run1@test.com", dto.Email);
            Assert.Equal("Participant", dto.Role);
        }

        [Fact]
        public async Task GetMe_UserNoLongerExists_ReturnsNotFound()
        {
            using var db = TestHelper.CreateDb();
            var ctrl = new UsersController(db).WithSession(9999, "Participant");

            var result = await ctrl.GetMe();

            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task UpdateMe_ChangesNameAndPicture()
        {
            using var db = TestHelper.CreateDb();
            var s = TestHelper.Seed(db);
            var ctrl = new UsersController(db).WithSession(s.ParticipantId, "Participant");

            var result = await ctrl.UpdateMe(new UpdateProfileDto { FullName = "New Name", ProfilePictureUrl = "https://example.com/a.jpg" });

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var dto = Assert.IsType<UserProfileDto>(ok.Value);
            Assert.Equal("New Name", dto.FullName);
            Assert.Equal("New Name", db.Users.Find(s.ParticipantId)!.FullName);
        }
    }
}
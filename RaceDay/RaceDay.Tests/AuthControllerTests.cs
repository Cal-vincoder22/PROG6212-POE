using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Controllers;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;
using Xunit;

namespace RaceDay.Tests
{
    public class AuthControllerTests
    {
        /// <summary>Makes sure the Organiser and Participant roles exist in the test database.</summary>
        private static void EnsureRoles(RaceDayDbContext db)
        {
            if (db.Roles.Any()) return;
            db.Roles.Add(new Role { RoleId = 1, RoleName = "Organiser" });
            db.Roles.Add(new Role { RoleId = 2, RoleName = "Participant" });
            db.SaveChanges();
        }

        private static AuthController CreateController(RaceDayDbContext db)
        {
            var controller = new AuthController(db);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { Session = new FakeSession() }
            };
            return controller;
        }

        private static RegisterDto ValidRegister(string email = "new@test.com", string role = "Participant") => new()
        {
            FullName = "New User",
            Email = email,
            Password = "Password123!",
            Role = role
        };

        [Fact]
        public async Task Register_ValidUser_Returns201AndHashesPassword()
        {
            using var db = TestHelper.CreateDb();
            EnsureRoles(db);
            var ctrl = CreateController(db);

            var result = await ctrl.Register(ValidRegister());

            var obj = Assert.IsType<ObjectResult>(result);
            Assert.Equal(201, obj.StatusCode);
            var saved = db.Users.Single(u => u.Email == "new@test.com");
            Assert.NotEqual("Password123!", saved.PasswordHash);
            Assert.True(BCrypt.Net.BCrypt.Verify("Password123!", saved.PasswordHash));
        }

        [Fact]
        public async Task Register_InvalidRole_ReturnsBadRequest()
        {
            using var db = TestHelper.CreateDb();
            EnsureRoles(db);
            var ctrl = CreateController(db);

            var result = await ctrl.Register(ValidRegister(role: "Admin"));

            Assert.IsType<BadRequestObjectResult>(result);
            Assert.Empty(db.Users);
        }

        [Fact]
        public async Task Register_DuplicateEmail_ReturnsConflict()
        {
            using var db = TestHelper.CreateDb();
            EnsureRoles(db);
            var ctrl = CreateController(db);

            await ctrl.Register(ValidRegister("dup@test.com"));
            var second = await ctrl.Register(ValidRegister("DUP@test.com"));

            Assert.IsType<ConflictObjectResult>(second);
            Assert.Single(db.Users);
        }

        [Fact]
        public async Task Login_ValidCredentials_ReturnsOkAndSetsSession()
        {
            using var db = TestHelper.CreateDb();
            EnsureRoles(db);
            var ctrl = CreateController(db);
            await ctrl.Register(ValidRegister("login@test.com", "Organiser"));

            var result = await ctrl.Login(new LoginDto { Email = "login@test.com", Password = "Password123!" });

            Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(ctrl.HttpContext.Session.GetInt32("UserId"));
            Assert.Equal("Organiser", ctrl.HttpContext.Session.GetString("Role"));
        }

        [Fact]
        public async Task Login_WrongPassword_ReturnsUnauthorized()
        {
            using var db = TestHelper.CreateDb();
            EnsureRoles(db);
            var ctrl = CreateController(db);
            await ctrl.Register(ValidRegister("login@test.com"));

            var result = await ctrl.Login(new LoginDto { Email = "login@test.com", Password = "WrongPass1!" });

            Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Null(ctrl.HttpContext.Session.GetInt32("UserId"));
        }

        [Fact]
        public async Task Login_UnknownEmail_ReturnsUnauthorized()
        {
            using var db = TestHelper.CreateDb();
            EnsureRoles(db);
            var ctrl = CreateController(db);

            var result = await ctrl.Login(new LoginDto { Email = "nobody@test.com", Password = "Password123!" });

            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        [Fact]
        public async Task Logout_ClearsSessionAndReturnsOk()
        {
            using var db = TestHelper.CreateDb();
            EnsureRoles(db);
            var ctrl = CreateController(db);
            await ctrl.Register(ValidRegister("out@test.com"));
            await ctrl.Login(new LoginDto { Email = "out@test.com", Password = "Password123!" });
            Assert.NotNull(ctrl.HttpContext.Session.GetInt32("UserId"));

            var result = ctrl.Logout();

            Assert.IsType<OkObjectResult>(result);
            Assert.Null(ctrl.HttpContext.Session.GetInt32("UserId"));
            Assert.Null(ctrl.HttpContext.Session.GetString("Role"));
        }
    }
}

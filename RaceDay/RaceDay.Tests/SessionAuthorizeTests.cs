using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using RaceDay.Api.Filters;
using Xunit;

namespace RaceDay.Tests
{
    public class SessionAuthorizeTests
    {
        private static AuthorizationFilterContext CreateContext(int? userId, string? role)
        {
            var http = new DefaultHttpContext { Session = new FakeSession() };
            if (userId.HasValue) http.Session.SetInt32("UserId", userId.Value);
            if (role != null) http.Session.SetString("Role", role);

            var actionContext = new ActionContext(http, new RouteData(), new ActionDescriptor());
            return new AuthorizationFilterContext(actionContext, new List<IFilterMetadata>());
        }

        [Fact]
        public void NoSession_Returns401()
        {
            var context = CreateContext(null, null);

            new SessionAuthorizeAttribute("Organiser").OnAuthorization(context);

            Assert.IsType<UnauthorizedObjectResult>(context.Result);
        }

        [Fact]
        public void WrongRole_Returns403()
        {
            var context = CreateContext(1, "Participant");

            new SessionAuthorizeAttribute("Organiser").OnAuthorization(context);

            var result = Assert.IsType<ObjectResult>(context.Result);
            Assert.Equal(403, result.StatusCode);
        }

        [Fact]
        public void CorrectRole_AllowsRequest()
        {
            var context = CreateContext(1, "Organiser");

            new SessionAuthorizeAttribute("Organiser").OnAuthorization(context);

            Assert.Null(context.Result);
        }

        [Fact]
        public void NoRoleRestriction_AnyLoggedInUserAllowed()
        {
            var context = CreateContext(1, "Participant");

            new SessionAuthorizeAttribute().OnAuthorization(context);

            Assert.Null(context.Result);
        }

        [Fact]
        public void MultipleRoles_AllowsEither()
        {
            var context = CreateContext(1, "Participant");

            new SessionAuthorizeAttribute("Organiser", "Participant").OnAuthorization(context);

            Assert.Null(context.Result);
        }
    }
}
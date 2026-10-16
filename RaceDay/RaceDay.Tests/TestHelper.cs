using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Models;

namespace RaceDay.Tests
{
    /// <summary>A simple in-memory session so controllers can read UserId and Role.</summary>
    public class FakeSession : ISession
    {
        private readonly Dictionary<string, byte[]> _store = new();
        public bool IsAvailable => true;
        public string Id { get; } = Guid.NewGuid().ToString();
        public IEnumerable<string> Keys => _store.Keys;
        public void Clear() => _store.Clear();
        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Remove(string key) => _store.Remove(key);
        public void Set(string key, byte[] value) => _store[key] = value;
        public bool TryGetValue(string key, [NotNullWhen(true)] out byte[]? value)
            => _store.TryGetValue(key, out value);
    }

    public class SeedData
    {
        public int OrganiserId { get; set; }
        public int Organiser2Id { get; set; }
        public int ParticipantId { get; set; }
        public int Participant2Id { get; set; }
        public int EventId { get; set; }
        public int CategoryId { get; set; }
    }

    public static class TestHelper
    {
        /// <summary>Creates a fresh, empty in-memory database for one test.</summary>
        public static RaceDayDbContext CreateDb()
        {
            var options = new DbContextOptionsBuilder<RaceDayDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            var db = new RaceDayDbContext(options);
            db.Database.EnsureCreated();
            return db;
        }

        /// <summary>Adds 2 organisers, 2 participants, 1 future event and 1 category.</summary>
        public static SeedData Seed(RaceDayDbContext db, bool pastEvent = false)
        {
            var org = new User { FullName = "Organiser One", Email = "org1@test.com" };
            var org2 = new User { FullName = "Organiser Two", Email = "org2@test.com" };
            var p1 = new User { FullName = "Runner One", Email = "run1@test.com" };
            var p2 = new User { FullName = "Runner Two", Email = "run2@test.com" };
            db.Users.AddRange(org, org2, p1, p2);
            db.SaveChanges();

            var ev = new Event
            {
                Name = "Test 10K",
                OrganiserId = org.UserId,
                EventDate = pastEvent ? DateTime.UtcNow.AddDays(-5) : DateTime.UtcNow.AddDays(30)
            };
            db.Events.Add(ev);
            db.SaveChanges();

            var cat = new Category { EventId = ev.EventId, CategoryName = "Open 10K" };
            db.Categories.Add(cat);
            db.SaveChanges();

            return new SeedData
            {
                OrganiserId = org.UserId,
                Organiser2Id = org2.UserId,
                ParticipantId = p1.UserId,
                Participant2Id = p2.UserId,
                EventId = ev.EventId,
                CategoryId = cat.CategoryId
            };
        }

        /// <summary>Gives a controller a fake logged-in user with the given role.</summary>
        public static T WithSession<T>(this T controller, int userId, string role) where T : ControllerBase
        {
            var http = new DefaultHttpContext { Session = new FakeSession() };
            http.Session.SetInt32("UserId", userId);
            http.Session.SetString("Role", role);
            controller.ControllerContext = new ControllerContext { HttpContext = http };
            return controller;
        }
    }
}
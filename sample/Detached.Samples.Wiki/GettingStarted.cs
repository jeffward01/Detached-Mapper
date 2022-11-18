﻿using Detached.Mappers.EntityFramework;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore; 

public class GettingStarted
{
    public static async Task Main()
    {
        TestDbContext dbContext = new TestDbContext();
        await dbContext.Database.EnsureCreatedAsync();

        dbContext.Map<User>(new User { Id = 1, Name = "mapped from Entity" });
        dbContext.Map<User>(new UserDTO { Id = 2, Name = "mapped from DTO" });
        dbContext.Map<User>(new Dictionary<string, object> { { "Id", 3 }, { "Name", "mapped from Entity" } });

        await dbContext.SaveChangesAsync();

        foreach (User persistedUser in dbContext.Users)
        {
            Console.WriteLine($"Id: {persistedUser.Id}, Name = '{persistedUser.Name}'");
        }
    }

    class TestDbContext : DbContext
    {
        static SqliteConnection _connection = new SqliteConnection($"DataSource=file:{Guid.NewGuid()}?mode=memory&cache=shared");

        static TestDbContext()
        {
            _connection = new SqliteConnection($"DataSource=file:{Guid.NewGuid()}?mode=memory&cache=shared");
            _connection.Open();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(_connection).UseDetached();
        }

        public DbSet<User> Users { get; set; }
    }

    class User
    {
        public int Id { get; set; }

        public string Name { get; set; }
    }

    class UserDTO
    {
        public int Id { get; set; }

        public string Name { get; set; }
    }
}
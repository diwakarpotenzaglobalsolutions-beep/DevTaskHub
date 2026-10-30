using System;
using DevTaskHubBackend.Entities;
using Microsoft.EntityFrameworkCore;

namespace DevTaskHubBackend.Data;

public class MyDbContext:DbContext
{
        public MyDbContext(DbContextOptions<MyDbContext> options):base (options){}

        protected MyDbContext(DbContextOptions options):base(options){}

        public DbSet<RoleDB> Roles{set;get;}

        public DbSet<UserDB> Users{set;get;}

        public DbSet<ProjectsDB> Projects {set;get;}

        public DbSet<TasksDB> Tasks{set;get;}

        public DbSet<ProjectMemberDB> ProjectMembers { get; set; }
}

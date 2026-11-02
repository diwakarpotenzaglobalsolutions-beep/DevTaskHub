using System;
using DevTaskHubBackend.Entities;
using Microsoft.EntityFrameworkCore;

namespace DevTaskHubBackend.Data;

public class MyDbContext:DbContext
{
        public MyDbContext(DbContextOptions<MyDbContext> options):base (options){}

        protected MyDbContext(DbContextOptions options):base(options){}

        public DbSet<Role> Roles{set;get;}

        public DbSet<User> Users{set;get;}

        public DbSet<Projects> Projects {set;get;}

        public DbSet<Tasks> Tasks{set;get;}

        public DbSet<ProjectMember> ProjectMembers { get; set; }
}

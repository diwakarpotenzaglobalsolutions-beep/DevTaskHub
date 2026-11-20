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

        public DbSet<TaskComment> TaskComments { get; set; }

        public DbSet<ActivityLog> ActivityLogs { get; set; }

        public DbSet<Attendance> Attendances { get; set; }

        public DbSet<LeaveTypes> LeaveTypes { get; set; }

        public DbSet<EmployeeLeaveBalances> EmployeeLeaveBalances { get; set; }

        public DbSet<LeaveRequests> LeaveRequests { get; set; }
}

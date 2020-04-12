using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using RazorPagesToDoList.Models;

    public class RazorPagesRecordContext : DbContext
    {
        public RazorPagesRecordContext (DbContextOptions<RazorPagesRecordContext> options)
            : base(options)
        {
        }

        public DbSet<RazorPagesToDoList.Models.Record> Record { get; set; }
    }

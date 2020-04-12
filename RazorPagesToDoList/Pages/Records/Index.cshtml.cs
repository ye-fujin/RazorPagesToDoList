using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RazorPagesToDoList.Models;

namespace RazorPagesToDoList.Pages.Records
{
    public class IndexModel : PageModel
    {
        private readonly RazorPagesRecordContext _context;

        public IndexModel(RazorPagesRecordContext context)
        {
            _context = context;
        }

        public string TitleSort { get; set; }
        public string CreatedDateSort { get; set; }
        public string EditedDateSort { get; set; }
        public string IsDoneSort { get; set; }

        public IList<Record> Record { get;set; }

        public async Task OnGetAsync(string sortOrder)
        {
            TitleSort = String.IsNullOrEmpty(sortOrder) ? "title_desc" : "";
            CreatedDateSort = String.IsNullOrEmpty(sortOrder) ? "createdDate_desc" : "";
            EditedDateSort = String.IsNullOrEmpty(sortOrder) ? "editedDate_desc" : "";
            IsDoneSort = String.IsNullOrEmpty(sortOrder) ? "isDone_desc" : "";

            IQueryable<Record> recordsIQ = from r in _context.Record
                                            select r;

            switch (sortOrder)
            {
                case "title_desc":
                    recordsIQ = recordsIQ.OrderByDescending(r => r.Title);
                    break;
                case "createdDate_desc":
                    recordsIQ = recordsIQ.OrderByDescending(r => r.CreatedDate);
                    break;
                case "editedDate_desc":
                    recordsIQ = recordsIQ.OrderByDescending(r => r.EditedDate);
                    break;
                case "isDone_desc":
                    recordsIQ = recordsIQ.OrderByDescending(r => r.IsDone);
                    break;
                default:
                    recordsIQ = recordsIQ.OrderBy(r => r.Title);
                    break;
            }

            Record = await recordsIQ.AsNoTracking().ToListAsync();
        }
    }
}

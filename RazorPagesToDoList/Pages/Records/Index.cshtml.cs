using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RazorPagesToDoList.Models;
using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml;
using System.IO;

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

        public async void OnGetAsync(string sortOrder)
        {

            TitleSort = String.IsNullOrEmpty(sortOrder) ? "title_desc" : "";
            CreatedDateSort = String.IsNullOrEmpty(sortOrder) ? "createdDate_desc" : "";
            EditedDateSort = String.IsNullOrEmpty(sortOrder) ? "editedDate_desc" : "";
            IsDoneSort = String.IsNullOrEmpty(sortOrder) ? "isDone_desc" : "";
            Record = await GetSortedRecordsIQ(sortOrder).AsNoTracking().ToListAsync();
        }

        private IQueryable<Record> GetSortedRecordsIQ(string sortOrder)
        {
            IQueryable<Record> recordsIQ = from r in _context.Record
                                           select r;

            recordsIQ = sortOrder switch
            {
                "title_desc" => recordsIQ.OrderByDescending(r => r.Title),
                "createdDate_desc" => recordsIQ.OrderByDescending(r => r.CreatedDate),
                "editedDate_desc" => recordsIQ.OrderByDescending(r => r.EditedDate),
                "isDone_desc" => recordsIQ.OrderByDescending(r => r.IsDone),
                _ => recordsIQ.OrderBy(r => r.Title),
            };
            return recordsIQ;
        }

        public async Task<IActionResult> OnPostExportAsExcelAsync()
        {
            var stream = new MemoryStream();
            var records = await _context.Record.AsNoTracking().ToListAsync();

            using (var package = new ExcelPackage(stream))
            {
                var workSheet = package.Workbook.Worksheets.Add("Sheet1");
                string dateformat = "m/d/yy h:mm:ss";
                workSheet.Cells.LoadFromCollection(records, true);
                workSheet.Column(3).Style.Numberformat.Format = dateformat;
                workSheet.Column(4).Style.Numberformat.Format = dateformat;
                package.Save();
            }

            stream.Position = 0;

            string fileName = $"Records-{DateTime.Now:yyyyMMddHHmmssfff}.xlsx";
            return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

    }
}

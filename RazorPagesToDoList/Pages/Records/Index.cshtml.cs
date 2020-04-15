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
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Caching.Memory;
using System.Globalization;

namespace RazorPagesToDoList.Pages.Records
{
    public class IndexModel : PageModel
    {
        private readonly RazorPagesRecordContext _context;
        private readonly IMemoryCache _cache;

        public IndexModel(RazorPagesRecordContext context, IMemoryCache cache)
        {
            _context = context;
            _cache = cache;
        }

        readonly string DateFormat = "dd/MM/yy";

        public string TitleSort { get; set; }
        public string CreatedDateSort { get; set; }
        public string EditedDateSort { get; set; }
        public string IsDoneSort { get; set; }
        
        public IList<Record> Record { get;set; }
        public IList<Record> RecordToImport = new List<Record>();

        private readonly string RecordsToImportCacheKey = "RecordsToImport";

        public bool IsImportMode = false;

        [BindProperty]
        [DataType(DataType.Date)]
        public DateTime ImportFromDate { get; set; }

        [BindProperty]
        [DataType(DataType.Date)]
        public DateTime ImportTillDate { get; set; }

        [BindProperty]
        public BufferedSingleFileUploadDb FileUpload { get; set; }

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
            Console.WriteLine("Exporting as Excel...");
            var memoryStream = new MemoryStream();
            var records = await _context.Record.AsNoTracking().ToListAsync();

            using (var package = new ExcelPackage(memoryStream))
            {
                Console.WriteLine("Processing records...");
                ExcelWorksheet workSheet = package.Workbook.Worksheets.Add("Sheet1");
                workSheet.Cells.LoadFromCollection(records, true);
                workSheet.Column(3).Style.Numberformat.Format = DateFormat;
                workSheet.Column(4).Style.Numberformat.Format = DateFormat;
                package.Save();
            }

            Console.WriteLine("Saving file...");
            memoryStream.Position = 0;
            string fileName = $"Records-{DateTime.Now:yyyyMMddHHmmssfff}.xlsx";
            return File(memoryStream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        public async Task<IActionResult> OnPostImportExcelAsync()
        {
            Console.WriteLine("Importing Excel...");
            var memoryStream = new MemoryStream();
            IsImportMode = true;
            await FileUpload.FormFile.CopyToAsync(memoryStream);

            using (var package = new ExcelPackage(memoryStream))
            {
                Console.WriteLine("Processing uploaded file...");
                ExcelWorksheet workSheet = package.Workbook.Worksheets.FirstOrDefault();

                int rows = workSheet.Dimension.Rows;
                int columns = workSheet.Dimension.Columns;

                IMemoryCache _cache1 = _cache;
                var cacheEntry = _cache.CreateEntry(RecordsToImportCacheKey);
                cacheEntry.Dispose();

                for (int i = 2; i <= rows; i++)
                {
                    var record = new Record();

                    for (int j = 1; j <= columns; j++)
                    {
                        ExcelRange range = workSheet.Cells[i, j];
                        var cellValue = range.Value;

                        if (j == 1)
                        {
                            record.ID = Convert.ToBoolean(cellValue) ? Convert.ToInt32(cellValue) : -1;
                        }

                        if (j == 2)
                        {
                            record.Title = Convert.ToString(cellValue);
                        }

                        if (j == 3)
                        {
                            ConvertRangeToDateTime(record, range, true);
                        }

                        if (j == 4)
                        {
                            ConvertRangeToDateTime(record, range, false);
                        }

                        if (j == 5)
                        {
                            try {
                                bool isDone = Convert.ToBoolean(cellValue);
                                record.IsDone = isDone;
                            }
                            catch
                            {
                                Console.WriteLine($"Failed to convert {cellValue} to boolean");

                                Console.WriteLine("Using false as a fallback value for IsDone");
                                record.IsDone = false;
                            }
                        }
                    }

                    if (record.ID != -1)
                    {
                        RecordToImport.Add(record);
                    }


                    _cache1.Set(RecordsToImportCacheKey, RecordToImport);

                }

                ImportFromDate = DateTime.Now;
                ImportTillDate = DateTime.Now;
            }

            return Page();
        }

        public IActionResult OnPostCancelImportAsync()
        {
            Console.WriteLine("Cancel import");
            IsImportMode = false;

            return RedirectToPage();
        }

        public IActionResult OnPostConfirmImportAsync()
        {
            Console.WriteLine("Confirm import");
            IsImportMode = false;
            List<Record> recordsToImport = _cache.Get<List<Record>>(RecordsToImportCacheKey);

            for (var i = 0; i < recordsToImport.Count; i++)
            {
                if (recordsToImport[i].CreatedDate.CompareTo(ImportFromDate) != -1 && recordsToImport[i].CreatedDate.CompareTo(ImportTillDate) != 1)
                {
                    _context.Record.Add(recordsToImport[i]);
                }
            }

            _context.SaveChanges();

            return RedirectToPage();
        }

        private void ConvertRangeToDateTime(Record record, ExcelRange range, bool isCreatedDate)
        {
            string dateString, dateStringSubStr, format;
            DateTime result;
            CultureInfo provider = CultureInfo.InvariantCulture;
            dateString = range.ToText();
            dateStringSubStr = dateString[0..^1];
            format = DateFormat;
            try
            {
                result = DateTime.ParseExact(dateStringSubStr, format, provider);
                Console.WriteLine("{0} converts to {1}.", dateString, result.ToString());
                if (isCreatedDate)
                {
                    record.CreatedDate = result;
                } else
                {
                    record.EditedDate = result;
                }
            }
            catch (FormatException)
            {
                Console.WriteLine("{0} is not in the correct format.", dateStringSubStr);

                DateTime today = DateTime.Today;
                string dateName = isCreatedDate ? "Created date" : "Edited date";
                Console.WriteLine($"Using today's date as a fallback value for {dateName}");
                
                if (isCreatedDate)
                {
                    record.CreatedDate = today;
                }
                else
                {
                    record.EditedDate = today;
                }
            }
        }

    }
}

public class BufferedSingleFileUploadDb
{
    [Required]
    [Display(Name = "File")]
    public IFormFile FormFile { get; set; }
}

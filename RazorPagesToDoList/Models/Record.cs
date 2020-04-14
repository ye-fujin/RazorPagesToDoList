using System;
using System.ComponentModel.DataAnnotations;
namespace RazorPagesToDoList.Models
{
    public class Record
    {
        public int ID { get; set; }

        public string Title { get; set; }

        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yy}")]
        public DateTime CreatedDate { get; set; }

        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yy}")]
        public DateTime EditedDate { get; set; }

        public bool IsDone { get; set; }
    }
}

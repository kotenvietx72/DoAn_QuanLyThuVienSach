using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoAn_QuanLyThuVienSach.Models
{
    public class Loan
    {
        [Key]
        public int LoanId { get; set; }

        [ForeignKey("Book")]
        public int BookId { get; set; }

        [ForeignKey("Member")]
        public int MemberId { get; set; }

        public DateTime DateBorrowed { get; set; }

        public DateTime DueDate { get; set; }

        public DateTime? DateReturned { get; set; }

        public int Quantity { get; set; }

        public virtual Book Book { get; set; } = null!;

        public virtual Member Member { get; set; } = null!;

        public Loan() {
        }
    }
}

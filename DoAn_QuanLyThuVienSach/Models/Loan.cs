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

        public DateTime DateReturned { get; set; }

        public int Quantity { get; set; }

        public virtual Book Book { get; set; }

        public virtual Member Member { get; set; }

        public Loan() {
            LoanId = 0;
            BookId = 0;
            MemberId = 0;
            DateBorrowed = DateTime.MinValue;
            DueDate = DateTime.MinValue;
            Quantity = 0;
            DateReturned = DateTime.MinValue;
            Book = new Book();
            Member = new Member();
        }
    }
}

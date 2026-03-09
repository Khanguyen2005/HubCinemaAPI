using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace API_Project.Models.Entities
{
    [Table("HoaDonDoAn")]
    public class InvoiceFood
    {
        [Key]
        [Column("MaHDDoAn")]
        public int IdInvoiceFood { get; set; }

        [Column("MaHoaDon")]
        public int IdInvoice { get; set; }

        [Column("MaDoAn")]
        public int IdFood { get; set; }

        [Column("SoLuong")]
        public int Quantity { get; set; }

        [Column("TongTien")]
        public int TotalPrice { get; set; }
    }
}

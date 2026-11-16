using System;

namespace CuahangNongduoc.BusinessObject
{
    public class KhachHang
    {
        public string Id { get; set; }
        public string HoTen { get; set; }
        public string DienThoai { get; set; }
        public string DiaChi { get; set; }
        public bool LoaiKH { get; set; } // false: Khách hàng, true: Đại lý
    }
}
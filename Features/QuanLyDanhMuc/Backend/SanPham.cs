using System;

namespace CuahangNongduoc.BusinessObject
{
    public class SanPham
    {
        public string Id { get; set; }
        public string TenSanPham { get; set; }
        public string IdDonViTinh { get; set; }
        // Giữ lại đối tượng đơn vị tính cho chức năng lấy thông tin lô nhập.
        public DonViTinh DonViTinh { get; set; }
        public int SoLuong { get; set; }
        public decimal DonGiaNhap { get; set; }
        public decimal GiaBanSi { get; set; }
        public decimal GiaBanLe { get; set; }
    }
}
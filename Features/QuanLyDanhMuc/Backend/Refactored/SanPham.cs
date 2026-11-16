using System;

namespace CuahangNongduoc.BusinessObject
{
    public class SanPham
    {
        public string Id { get; set; }
        public string TenSanPham { get; set; }
        public string IdDonViTinh { get; set; }
        public int SoLuong { get; set; }
        public decimal DonGiaNhap { get; set; }
        public decimal GiaBanSi { get; set; }
        public decimal GiaBanLe { get; set; }
    }
}
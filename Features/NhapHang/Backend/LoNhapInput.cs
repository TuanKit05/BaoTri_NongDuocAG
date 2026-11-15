using System;

namespace CuahangNongduoc.BusinessObject
{
    /// <summary>
    /// Du lieu mot lo hang moi duoc nhap vao.
    /// Lop nay chi mang du lieu, khong tu truy cap co so du lieu.
    /// </summary>
    public class LoNhapInput
    {
        public string MaLo;
        public string MaSanPham;
        public long DonGiaNhap;
        public int SoLuong;
        public DateTime NgayNhap;
        public DateTime NgaySanXuat;
        public DateTime NgayHetHan;
    }
}

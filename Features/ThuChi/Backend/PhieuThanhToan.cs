using System;

namespace CuahangNongduoc.BusinessObject
{
    public class PhieuThanhToan : PhieuThuChiCoBan
    {
        public PhieuThanhToan() { }
        public PhieuThanhToan(String id) : base(id) { }

        private DateTime m_NgayThanhToan;
        private KhachHang m_KH;

        public DateTime NgayThanhToan
        {
            get { return m_NgayThanhToan; }
            set { m_NgayThanhToan = value; }
        }

        public KhachHang KhachHang
        {
            get { return m_KH; }
            set { m_KH = value; }
        }
    }
}

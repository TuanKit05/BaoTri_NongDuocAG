using System;

namespace CuahangNongduoc.BusinessObject
{
    // Phan du lieu chung duoc rut len tu PhieuChi va PhieuThanhToan.
    // Giu nguyen ten cac property cong khai de tuong thich cac report RDLC.
    public abstract class PhieuThuChiCoBan
    {
        private String m_Id;
        private long m_TongTien;
        private String m_GhiChu;

        protected PhieuThuChiCoBan() { }

        protected PhieuThuChiCoBan(String id)
        {
            m_Id = id;
        }

        public String Id
        {
            get { return m_Id; }
            set { m_Id = value; }
        }

        public long TongTien
        {
            get { return m_TongTien; }
            set { m_TongTien = value; }
        }

        public String GhiChu
        {
            get { return m_GhiChu; }
            set { m_GhiChu = value; }
        }
    }
}

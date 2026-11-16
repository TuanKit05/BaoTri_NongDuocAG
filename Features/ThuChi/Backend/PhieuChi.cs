using System;

namespace CuahangNongduoc.BusinessObject
{
    public class PhieuChi : PhieuThuChiCoBan
    {
        public PhieuChi() { }
        public PhieuChi(String id) : base(id) { }

        private DateTime m_NgayChi;
        private LyDoChi m_LyDo;

        public DateTime NgayChi
        {
            get { return m_NgayChi; }
            set { m_NgayChi = value; }
        }

        public LyDoChi LyDoChi
        {
            get { return m_LyDo; }
            set { m_LyDo = value; }
        }
    }
}

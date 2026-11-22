using System;
using System.Collections.Generic;
using System.Text;

namespace CuahangNongduoc.BusinessObject
{
    // Dòng dịch vụ phát sinh trên hóa đơn, chỉ lưu trong phihoadon.xml
    public class DichVuPhieuBan
    {
        private string m_Ten = "";
        public string Ten
        {
            get { return m_Ten; }
            set { m_Ten = value; }
        }

        private long m_TienDV;
        public long TienDV
        {
            get { return m_TienDV; }
            set { m_TienDV = value; }
        }
    }

    public class PhieuBan
    {
        private String m_Id;

        public String Id
        {
            get { return m_Id; }
            set { m_Id = value; }
        }

        private KhachHang m_KH;

        public KhachHang KhachHang
        {
            get { return m_KH; }
            set { m_KH = value; }
        }

        private DateTime m_NgayBan;

        public DateTime NgayBan
        {
            get { return m_NgayBan; }
            set { m_NgayBan = value; }
        }
        private long m_TongTien;

        public long TongTien
        {
            get { return m_TongTien; }
            set { m_TongTien = value; }
        }
        private long m_DaTra;

        public long DaTra
        {
            get { return m_DaTra; }
            set { m_DaTra = value; }
        }
        private long m_ConNo;

        public long ConNo
        {
            get { return m_ConNo; }
            set { m_ConNo = value; }
        }

        private IList<ChiTietPhieuBan> m_ChiTiet;

        public IList<ChiTietPhieuBan> ChiTiet
        {
            get { return m_ChiTiet; }
            set { m_ChiTiet = value; }
        }

        // ---- Khoản phát sinh: lưu trong phihoadon.xml, không lưu CSDL ----

        private long m_PhiVanChuyen;

        public long PhiVanChuyen
        {
            get { return m_PhiVanChuyen; }
            set { m_PhiVanChuyen = value; }
        }

        private double m_PhanTramGiam; // 0 đến 100; nhập 10 nghĩa là 10%

        public double PhanTramGiam
        {
            get { return m_PhanTramGiam; }
            set { m_PhanTramGiam = value; }
        }

        private long m_TienGiam; // đã làm tròn đến bậc 100, từ 50 trở lên làm tròn lên

        public long TienGiam
        {
            get { return m_TienGiam; }
            set { m_TienGiam = value; }
        }

        private IList<DichVuPhieuBan> m_DichVu = new List<DichVuPhieuBan>();

        public IList<DichVuPhieuBan> DichVu
        {
            get { return m_DichVu; }
            set { m_DichVu = value; }
        }
    }

    // =====================================================================
    // STRATEGY (refactoring.guru): cách tính tiền giảm giá.
    // Thêm cách giảm mới = thêm một lớp mới, không sửa lớp cũ.
    // =====================================================================
    public interface IChienLuocGiamGia
    {
        long TinhTienGiam(long tienHang);
    }

    // Không giảm giá
    public class KhongGiamGia : IChienLuocGiamGia
    {
        public long TinhTienGiam(long tienHang)
        {
            return 0;
        }
    }

    // Giảm theo phần trăm trên tiền hàng, làm tròn đến bậc 100 đồng (từ 50 đồng trở lên làm tròn lên)
    public class GiamGiaPhanTram : IChienLuocGiamGia
    {
        private double m_PhanTram;

        public GiamGiaPhanTram(double phanTram)
        {
            m_PhanTram = phanTram;
        }

        public double PhanTram
        {
            get { return m_PhanTram; }
        }

        public long TinhTienGiam(long tienHang)
        {
            if (tienHang <= 0 || m_PhanTram <= 0)
                return 0;
            decimal tho = (decimal)tienHang * (decimal)m_PhanTram / 100m;
            return checked((long)(Decimal.Round(tho / 100m, 0, MidpointRounding.AwayFromZero) * 100m));
        }
    }

    // =====================================================================
    // DECORATOR (refactoring.guru): tổng hóa đơn được tính bằng cách bao
    // dần từng khoản (giảm giá, phí vận chuyển, từng dịch vụ) quanh tiền hàng.
    // =====================================================================
    public interface IThanhPhanHoaDon
    {
        long GiaTri();
    }

    // Thành phần gốc: tiền hàng
    public class TienHangGoc : IThanhPhanHoaDon
    {
        private long m_TienHang;

        public TienHangGoc(long tienHang)
        {
            m_TienHang = tienHang;
        }

        public long GiaTri()
        {
            return m_TienHang;
        }
    }

    // Lớp trang trí cơ sở: giữ thành phần bên trong
    public abstract class ThanhPhanHoaDonBoc : IThanhPhanHoaDon
    {
        protected IThanhPhanHoaDon m_BenTrong;

        protected ThanhPhanHoaDonBoc(IThanhPhanHoaDon benTrong)
        {
            m_BenTrong = benTrong;
        }

        public abstract long GiaTri();
    }

    // Trừ tiền giảm giá. Giảm tính trên tiền hàng, không tính trên phí.
    public class GiamGiaBoc : ThanhPhanHoaDonBoc
    {
        private long m_TienHang;
        private IChienLuocGiamGia m_ChienLuoc;

        public GiamGiaBoc(IThanhPhanHoaDon benTrong, long tienHang, IChienLuocGiamGia chienLuoc)
            : base(benTrong)
        {
            m_TienHang = tienHang;
            m_ChienLuoc = chienLuoc;
        }

        public override long GiaTri()
        {
            return m_BenTrong.GiaTri() - m_ChienLuoc.TinhTienGiam(m_TienHang);
        }
    }

    // Cộng phí vận chuyển
    public class PhiVanChuyenBoc : ThanhPhanHoaDonBoc
    {
        private long m_Phi;

        public PhiVanChuyenBoc(IThanhPhanHoaDon benTrong, long phi)
            : base(benTrong)
        {
            m_Phi = phi;
        }

        public override long GiaTri()
        {
            return m_BenTrong.GiaTri() + m_Phi;
        }
    }

    // Cộng một dịch vụ phát sinh
    public class DichVuBoc : ThanhPhanHoaDonBoc
    {
        private long m_TienDV;

        public DichVuBoc(IThanhPhanHoaDon benTrong, long tienDV)
            : base(benTrong)
        {
            m_TienDV = tienDV;
        }

        public override long GiaTri()
        {
            return m_BenTrong.GiaTri() + m_TienDV;
        }
    }

    // Dựng chuỗi Decorator theo thứ tự: tiền hàng -> giảm giá -> phí vận chuyển -> dịch vụ
    public static class DungHoaDon
    {
        public static long TongThanhToan(long tienHang, IChienLuocGiamGia chienLuoc, long phiVanChuyen, IList<DichVuPhieuBan> dichVu)
        {
            IThanhPhanHoaDon p = new TienHangGoc(tienHang);
            p = new GiamGiaBoc(p, tienHang, chienLuoc);
            p = new PhiVanChuyenBoc(p, phiVanChuyen);
            if (dichVu != null)
            {
                foreach (DichVuPhieuBan dv in dichVu)
                    p = new DichVuBoc(p, dv.TienDV);
            }
            long tong = p.GiaTri();
            return tong < 0 ? 0 : tong;
        }
    }

    // =====================================================================
    // OBSERVER (refactoring.guru): giữ dữ liệu hóa đơn đang nhập.
    // Mỗi lần dữ liệu đổi, phát sự kiện DaThayDoi để form (người quan sát)
    // cập nhật ô tổng tiền và tiền giảm.
    // =====================================================================
    public class HoaDonDangLap
    {
        public event EventHandler DaThayDoi;

        private long m_TienHang;
        private long m_PhiVanChuyen;
        private double m_PhanTramGiam;
        private IList<DichVuPhieuBan> m_DichVu = new List<DichVuPhieuBan>();

        public long TienHang
        {
            get { return m_TienHang; }
            set { m_TienHang = value; ThongBao(); }
        }

        public long PhiVanChuyen
        {
            get { return m_PhiVanChuyen; }
            set { m_PhiVanChuyen = value; ThongBao(); }
        }

        public double PhanTramGiam
        {
            get { return m_PhanTramGiam; }
            set { m_PhanTramGiam = value; ThongBao(); }
        }

        public IList<DichVuPhieuBan> DichVu
        {
            get { return m_DichVu; }
        }

        // Thay toàn bộ danh sách dịch vụ
        public void ThayDichVu(IList<DichVuPhieuBan> dichVu)
        {
            m_DichVu = dichVu == null ? new List<DichVuPhieuBan>() : dichVu;
            ThongBao();
        }

        // Nạp phí cùng lúc, chỉ phát sự kiện một lần
        public void DatPhi(long phiVanChuyen, double phanTramGiam, IList<DichVuPhieuBan> dichVu)
        {
            m_PhiVanChuyen = phiVanChuyen;
            m_PhanTramGiam = phanTramGiam;
            m_DichVu = dichVu == null ? new List<DichVuPhieuBan>() : dichVu;
            ThongBao();
        }

        // Strategy được chọn theo % giảm: 0 thì không giảm
        public IChienLuocGiamGia ChienLuoc
        {
            get
            {
                if (m_PhanTramGiam > 0)
                    return new GiamGiaPhanTram(m_PhanTramGiam);
                return new KhongGiamGia();
            }
        }

        public long TienGiam
        {
            get { return ChienLuoc.TinhTienGiam(m_TienHang); }
        }

        public long TongTien
        {
            get { return DungHoaDon.TongThanhToan(m_TienHang, ChienLuoc, m_PhiVanChuyen, m_DichVu); }
        }

        // Tạo đối tượng PhieuBan để ghi phí vào phihoadon.xml
        public PhieuBan ToPhieuBan(String idPhieu)
        {
            PhieuBan ph = new PhieuBan();
            ph.Id = idPhieu;
            ph.PhiVanChuyen = m_PhiVanChuyen;
            ph.PhanTramGiam = m_PhanTramGiam;
            ph.TienGiam = TienGiam;
            ph.DichVu = new List<DichVuPhieuBan>();
            foreach (DichVuPhieuBan dv in m_DichVu)
            {
                DichVuPhieuBan copy = new DichVuPhieuBan();
                copy.Ten = dv.Ten;
                copy.TienDV = dv.TienDV;
                ph.DichVu.Add(copy);
            }
            return ph;
        }

        private void ThongBao()
        {
            EventHandler h = DaThayDoi;
            if (h != null)
                h(this, EventArgs.Empty);
        }
    }
}
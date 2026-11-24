using System;
using System.Collections.Generic;
using System.Text;

namespace CuahangNongduoc.BusinessObject
{
    // Dòng dịch vụ phát sinh trên hóa đơn, lưu trong CHI_TIET_DICH_VU (Ten là MO_TA, TienDV là THANH_TIEN)
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
        public string KenhUuDai="";
        public KetQuaChinhSach ChiTietUuDai;

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

        // Phí và chi tiết ưu đãi lưu trong Access; XML giữ tương thích phiếu cũ.

        private long m_PhiVanChuyen;

        public long PhiVanChuyen
        {
            get { return m_PhiVanChuyen; }
            set { m_PhiVanChuyen = value; }
        }

        private double m_PhanTramGiam; // Mức giảm hiệu lực 0 đến 100 = max(mức tay, mức tự động); nhập 10 nghĩa là 10%

        public double PhanTramGiam
        {
            get { return m_PhanTramGiam; }
            set { m_PhanTramGiam = value; }
        }

        private double m_PhanTramGiamTay; // Mức giảm do nhân viên/quản lý nhập tay, dùng để kiểm tra quyền

        public double PhanTramGiamTay
        {
            get { return m_PhanTramGiamTay; }
            set { m_PhanTramGiamTay = value; }
        }

        private double m_PhanTramTuDong; // Mức tự động đã áp khi lập phiếu; phiếu đã lập giữ nguyên mức này

        public double PhanTramTuDong
        {
            get { return m_PhanTramTuDong; }
            set { m_PhanTramTuDong = value; }
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
        public string Kenh="BAN_LE";
        private long daTra;
        private long uuDaiThanhToanCu;
        private KetQuaChinhSach chiTietKhoa;
        public void DatThanhToan(long paid) { daTra=paid; }
        private long TongPhi { get { long n=m_PhiVanChuyen; foreach(DichVuPhieuBan service in m_DichVu) n=checked(n+service.TienDV); return n; } }
        public KetQuaChinhSach ChiTietUuDai {
            get {
                if(chiTietKhoa!=null) return chiTietKhoa;
                if(m_PhanTramTuDongKhoa.HasValue) return new KetQuaChinhSach { GiamGia=new GiamGiaPhanTram(PhanTramGiamHieuLuc).TinhTienGiam(m_TienHang) };
                KetQuaChinhSach result=ChonUuDai.TinhTheoKenh(m_TienHang,m_TongSoLuong,m_DsUuDai,dongUuDai,Kenh,daTra,TongPhi,m_PhanTramGiamTay);
                // Historical amounts stay independent of later cash receipts.
                result.ThanhToan=Math.Min(uuDaiThanhToanCu,Math.Max(0,m_TienHang-result.TongGiam));
                return result;
            }
        }
        public long SoTienTraDu { get { return TongTien; } }
        public void MoSuaUuDai(IDictionary<string,string> lots)
        {
            if(chiTietKhoa==null || String.IsNullOrEmpty(chiTietKhoa.QuyTacDaLuu)) return;
            IList<UuDaiQuyDinh> savedRules=ChonUuDai.DocQuyTac(chiTietKhoa.QuyTacDaLuu,lots);
            List<UuDaiQuyDinh> editable=new List<UuDaiQuyDinh>();
            foreach(UuDaiQuyDinh rule in savedRules)
                if(rule.Loai!=UuDaiQuyDinh.LoaiChietKhauThanhToan) editable.Add(rule);
            m_DsUuDai=editable;
            chiTietKhoa=null; m_PhanTramTuDongKhoa=null;
        }
        public void KhoaChiTiet(KetQuaChinhSach saved)
        { chiTietKhoa=saved; uuDaiThanhToanCu=saved==null?0:saved.ThanhToan; }


        private long m_TienHang;
        private long m_PhiVanChuyen;
        private double m_PhanTramGiamTay;
        private int m_TongSoLuong;
        private double? m_PhanTramTuDongKhoa; // Phiếu đã lập: giữ mức tự động lúc lập, không tính lại theo ưu đãi mới
        private IList<UuDaiQuyDinh> m_DsUuDai = new List<UuDaiQuyDinh>();
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

        // Mức giảm do người nhập tay (0 nghĩa là không nhập)
        public double PhanTramGiamTay
        {
            get { return m_PhanTramGiamTay; }
            set { m_PhanTramGiamTay = value; ThongBao(); }
        }

        // Mức giảm tự động từ ưu đãi HOA_DON đủ điều kiện, theo tiền hàng và số lượng hiện tại
        public double PhanTramTuDong
        {
            get
            {
                if (m_PhanTramTuDongKhoa.HasValue)
                    return m_PhanTramTuDongKhoa.Value;
                KetQuaChinhSach automatic=ChonUuDai.TinhTheoKenh(m_TienHang,m_TongSoLuong,m_DsUuDai,dongUuDai,Kenh,daTra,TongPhi,0);
                return m_TienHang==0?0:(double)automatic.TongGiam*100/m_TienHang;
            }
        }

        // Mức giảm áp dụng: lấy mức cao hơn giữa mức tay và mức tự động
        public double PhanTramGiamHieuLuc
        {
            get { return Math.Max(m_PhanTramGiamTay, PhanTramTuDong); }
        }

        // Tổng số lượng mua (không tính hàng tặng), dùng để xét điều kiện ưu đãi
        public int TongSoLuong
        {
            get { return m_TongSoLuong; }
            set { m_TongSoLuong = value; ThongBao(); }
        }

        // Nạp danh sách ưu đãi còn hiệu lực
        // Nạp ưu đãi cho phiếu mới: mở khóa mức tự động của phiếu đã lưu trước đó
        private IList<DongTinhUuDai> dongUuDai = new List<DongTinhUuDai>();
        public IList<UuDaiQuyDinh> QuyTacDangDung { get { return m_DsUuDai; } }
        public void CapNhatUuDai(IList<UuDaiQuyDinh> rules)
        {
            m_DsUuDai=rules??new List<UuDaiQuyDinh>();
            m_PhanTramTuDongKhoa=null; chiTietKhoa=null; uuDaiThanhToanCu=0;
        }
        public void DatTienHang(IList<DongTinhUuDai> lines,int quantity,long money)
        {
            dongUuDai=lines; m_TongSoLuong=quantity; m_TienHang=money; ThongBao();
        }
        public void DatDongUuDai(IList<DongTinhUuDai> lines)
        {
            dongUuDai = lines;
        }
        public string SanPhamCuaLo(string lot)
        {
            string product;
            foreach (UuDaiQuyDinh rule in m_DsUuDai)
                if(rule.SanPhamTheoLo != null && rule.SanPhamTheoLo.TryGetValue(lot, out product)) return product;
            return lot;
        }
        public void NapUuDai(IList<UuDaiQuyDinh> dsUuDai)
        {
            m_DsUuDai = dsUuDai ?? new List<UuDaiQuyDinh>();
            m_PhanTramTuDongKhoa = null;
            chiTietKhoa=null; uuDaiThanhToanCu=0; daTra=0;
            ThongBao();
        }

        // Phiếu đã lập: khóa mức tự động đã lưu (null = tính theo ưu đãi hiện hành)
        public void KhoaPhanTramTuDong(double? phanTram)
        {
            m_PhanTramTuDongKhoa = phanTram;
            ThongBao();
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
            m_PhanTramGiamTay = phanTramGiam;
            m_DichVu = dichVu == null ? new List<DichVuPhieuBan>() : dichVu;
            ThongBao();
        }

        // Strategy được chọn theo % giảm: 0 thì không giảm
        public IChienLuocGiamGia ChienLuoc
        {
            get
            {
                double pct = PhanTramGiamHieuLuc;
                if (pct > 0)
                    return new GiamGiaPhanTram(pct);
                return new KhongGiamGia();
            }
        }

        public long TienGiam
        {
            get { return ChiTietUuDai.TongGiam; }
        }

        public long TongTien
        {
            get { return Math.Max(0,checked(m_TienHang-TienGiam+TongPhi)); }
        }

        // Tạo đối tượng PhieuBan để ghi phí vào phihoadon.xml
        public PhieuBan ToPhieuBan(String idPhieu)
        {
            PhieuBan ph = new PhieuBan();
            ph.Id = idPhieu;
            ph.KenhUuDai=Kenh; ph.ChiTietUuDai=ChiTietUuDai;
            ph.DaTra=daTra; ph.TongTien=TongTien; ph.ConNo=TongTien-daTra;
            ph.PhiVanChuyen = m_PhiVanChuyen;
            ph.PhanTramGiam = PhanTramGiamHieuLuc;
            ph.PhanTramGiamTay = m_PhanTramGiamTay;
            ph.PhanTramTuDong = PhanTramTuDong;
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
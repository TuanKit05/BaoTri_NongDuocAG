using System;
using System.Data;
using System.Windows.Forms;
using CuahangNongduoc.Features.BaoCao.Backend;

namespace CuahangNongduoc.Features.BaoCao.Frontend
{
    // YC-BC-02: xem tồn kho từng lô tại một ngày được chọn.
    // Designer cần có: dtNgay, btnXem, btnDong, dgvTonKho, lblTongKet, lblCanhBao.
    public partial class frmTonKhoTaiNgay : Form
    {
        private readonly TonKhoTaiNgayService service = new TonKhoTaiNgayService();

        public frmTonKhoTaiNgay()
        {
            InitializeComponent();
            CuahangNongduoc.ResponsiveLayout.Attach(this);
            this.Load += new EventHandler(frmTonKhoTaiNgay_Load);
            this.btnXem.Click += new EventHandler(btnXem_Click);
            this.btnDong.Click += new EventHandler(btnDong_Click);
        }

        private void frmTonKhoTaiNgay_Load(object sender, EventArgs e)
        {
            dtNgay.Value = DateTime.Today;
            lblCanhBao.Text = "";
            lblTongKet.Text = "";
        }

        private void btnXem_Click(object sender, EventArgs e)
        {
            try
            {
                AccessControl.DemandCurrent("BaoCaoTonKho");
                KetQuaTonKho kq = service.LayTonKhoTaiNgay(dtNgay.Value.Date);
                dgvTonKho.DataSource = ChuyenSangBang(kq);
                lblTongKet.Text = string.Format(
                    "Ngày {0:dd/MM/yyyy} | Tổng SL tồn: {1:N0} | Tổng giá trị (giá nhập): {2:N0} đ",
                    kq.Ngay, kq.TongSoLuong, kq.TongGiaTri);
                if (kq.DuDuLieu)
                {
                    lblCanhBao.Text = "";
                }
                else
                {
                    lblCanhBao.Text = "Cảnh báo: sổ biến động kho mở từ "
                        + (kq.NgayMoSo.HasValue ? kq.NgayMoSo.Value.ToString("dd/MM/yyyy") : "(chưa mở sổ)")
                        + ". Số liệu ngày trước đó không chính xác vì thiếu lịch sử biến động.";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi tải tồn kho: " + ex.Message, "Lỗi",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static DataTable ChuyenSangBang(KetQuaTonKho kq)
        {
            DataTable tbl = new DataTable();
            tbl.Columns.Add("MA_LO", typeof(string));
            tbl.Columns.Add("MA_SP", typeof(string));
            tbl.Columns.Add("TEN_SAN_PHAM", typeof(string));
            tbl.Columns.Add("TEN_DON_VI", typeof(string));
            tbl.Columns.Add("SO_LUONG_TAI_NGAY", typeof(long));
            tbl.Columns.Add("DON_GIA_NHAP", typeof(decimal));
            tbl.Columns.Add("GIA_TRI", typeof(decimal));
            tbl.Columns.Add("NGAY_NHAP", typeof(DateTime));
            tbl.Columns.Add("NGAY_HET_HAN", typeof(DateTime));

            foreach (TonKhoLo lo in kq.DanhSach)
            {
                DataRow r = tbl.NewRow();
                r["MA_LO"] = lo.MaLo;
                r["MA_SP"] = lo.MaSp;
                r["TEN_SAN_PHAM"] = lo.TenSanPham;
                r["TEN_DON_VI"] = lo.TenDonVi;
                r["SO_LUONG_TAI_NGAY"] = lo.SoLuongTaiNgay;
                r["DON_GIA_NHAP"] = lo.DonGiaNhap;
                r["GIA_TRI"] = lo.GiaTriTaiNgay;
                r["NGAY_NHAP"] = lo.NgayNhap;
                if (lo.NgayHetHan.HasValue)
                    r["NGAY_HET_HAN"] = lo.NgayHetHan.Value;
                tbl.Rows.Add(r);
            }
            return tbl;
        }

        private void btnDong_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}

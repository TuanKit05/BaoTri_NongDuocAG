using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using CuahangNongduoc.Controller;
using CuahangNongduoc.BusinessObject;

namespace CuahangNongduoc
{
    public partial class frmBanLe : Form
    {
        private const string TieuDeThongBao = "Phiếu bán lẻ";
        internal string MaPhieuHienTai { get { return txtMaPhieu.Text; } }

        SanPhamController ctrlSanPham = new SanPhamController();
        KhachHangController ctrlKhachHang = new KhachHangController();
        MaSanPhamController ctrlMaSanPham = new MaSanPhamController();
        PhieuBanController ctrlPhieuBan = new PhieuBanController();
        ChiTietPhieuBanController ctrlChiTiet = new ChiTietPhieuBanController();
        Controll status = Controll.Normal;

        // OBSERVER (refactoring.guru): mô hình hóa đơn đang nhập.
        // Form là người quan sát: mỗi lần mô hình đổi, HoaDon_DaThayDoi cập nhật ô tổng và tiền giảm.
        HoaDonDangLap hoaDon = new HoaDonDangLap();
        bool dangNapPhi = false;
        private bool dangNapDuLieu = true;
        private BindingSource nguonPhieu;
        private PhieuBanController controllerTruocKhiThem;
        private readonly ToolStripButton toolHuy = new ToolStripButton("Hủy");

        public frmBanLe()
        {
            InitializeComponent();
            chkHangTang.Checked=false; chkHangTang.Enabled=false;
            hoaDon.Kenh="BAN_LE";
            numDaTra.ValueChanged += delegate { if(!dangNapDuLieu && !dangNapPhi && status!=Controll.Normal) hoaDon.DatThanhToan((long)numDaTra.Value); };
            ToolStripButton payFull=new ToolStripButton("Trả đủ"); payFull.Name="toolTraDu";
            bindingNavigator.Items.Add(payFull);
            payFull.Click += delegate {
                if(status==Controll.Normal) return;
                DongBoPhi(); DongBoTienHang();
                numDaTra.Value=hoaDon.SoTienTraDu;
                hoaDon.DatThanhToan((long)numDaTra.Value);
            };
            Label detailDiscount=new Label { Name="lblChiTietUuDai",Location=new System.Drawing.Point(16,132),
                Size=new System.Drawing.Size(365,45),AutoSize=false,Text="Giảm giá: 0 | CK thương mại: 0" };
            grpPhi.Controls.Add(detailDiscount);
            InvoiceServiceEditor.Attach(dgvDichVu, DongBoPhi);
            SaleLotDisplay.Attach(dgvDanhsachSP, colMaSanPham);
            toolHuy.Name = "toolHuy";
            bindingNavigator.Items.Insert(bindingNavigator.Items.IndexOf(toolLuu) + 1, toolHuy);
            toolHuy.Click += delegate { HuyThayDoi(); };
            Shown += delegate { Allow(status != Controll.Normal); };
            FormClosing += delegate (object sender, FormClosingEventArgs e) {
                if (status == Controll.Normal) return;
                DialogResult answer = MessageBox.Show("Bạn có muốn lưu thay đổi trước khi đóng?", TieuDeThongBao,
                    MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                if (answer == DialogResult.Cancel) e.Cancel = true;
                else if (answer == DialogResult.Yes)
                {
                    if (!Luu()) e.Cancel = true;
                    else status = Controll.Normal;
                }
                else HuyThayDoi();
            };
            numThanhTien.ReadOnly = true;
            numThanhTien.DecimalPlaces = 0;
            numThanhTien.Maximum = Int64.MaxValue;
            numConNo.ReadOnly = true;
            dgvDanhsachSP.CellEndEdit += delegate (object sender, DataGridViewCellEventArgs e) {
                if (dangNapDuLieu || status == Controll.Normal || e.RowIndex < 0) return;
                DataRowView row = dgvDanhsachSP.Rows[e.RowIndex].DataBoundItem as DataRowView;
                if (row == null) return;
                try
                {
                    row["THANH_TIEN"] = TienTe.ThanhTien(Convert.ToDecimal(row["DON_GIA"]), Convert.ToDecimal(row["SO_LUONG"]));
                    row.EndEdit();
                    DongBoUuDaiVaHangTang();
                }
                catch (Exception ex) { MessageBox.Show(ErrorLog.UserMessage(ex, "Kh\u00f4ng th\u1ec3 ho\u00e0n t\u1ea5t thao t\u00e1c"), TieuDeThongBao, MessageBoxButtons.OK, MessageBoxIcon.Warning); }
            };

            this.FormClosed += delegate { NgatTheoDoiPhieu(); };
            this.Disposed += delegate { NgatTheoDoiPhieu(); };
            CuahangNongduoc.ResponsiveLayout.Attach(this);
            this.Load += delegate { SecuredUi.ApplyForm(this); };
            this.Shown += delegate {
                if (!AccessControl.Can("ThietLapUuDai"))
                {
                    numPhanTramGiam.Enabled = false;
                }
            };

            cmbLoaiChietKhau.SelectedIndex = 0;
            cmbLoaiGiamGia.SelectedIndex = 0;

            status = Controll.AddNew;
            hoaDon.DaThayDoi += new EventHandler(HoaDon_DaThayDoi);
        }


        private decimal tienHangGoc = 0m;
        private decimal tienChietKhau = 0m;
        private decimal tienGiamGia = 0m;

        private void TinhLaiHoaDon()
        {
            // Cơ chế chiết khấu/giảm giá cũ không còn tính tổng: chỉ đồng bộ tiền hàng vào HoaDonDangLap
            if (dangNapDuLieu || status == Controll.Normal) return;
            DongBoTienHang();
            return;

            if (dangNapDuLieu || status == Controll.Normal) return;
            if (cmbLoaiChietKhau.SelectedIndex < 0 ||
                cmbLoaiGiamGia.SelectedIndex < 0)
                return;

            IChienLuocUuDai chienLuocChietKhau =
                cmbLoaiChietKhau.SelectedIndex == 0
                ? (IChienLuocUuDai)new UuDaiPhanTram()
                : new UuDaiCoDinh();

            IChienLuocUuDai chienLuocGiamGia =
                cmbLoaiGiamGia.SelectedIndex == 0
                ? (IChienLuocUuDai)new UuDaiPhanTram()
                : new UuDaiCoDinh();

            TinhTienHoaDon boTinh = new TinhTienHoaDon();

            tienHangGoc = TinhTienHangTuDanhSach();
            KetQuaUuDai ketQua = boTinh.Tinh(
                tienHangGoc,
                chienLuocChietKhau,
                numGiaTriChietKhau.Value,
                chienLuocGiamGia,
                numGiaTriGiamGia.Value
            );
            tienChietKhau = ketQua.TienChietKhau;
            tienGiamGia = ketQua.TienGiamGia;


            lblTienChietKhau.Text =
                ketQua.TienChietKhau.ToString("#,##0") + " đ";

            lblTienGiamGia.Text =
                ketQua.TienGiamGia.ToString("#,##0") + " đ";

            if (ketQua.TongThanhToan > numTongTien.Maximum)
            {
                MessageBox.Show(
                    "Tổng thanh toán vượt giới hạn cho phép.", TieuDeThongBao, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            numTongTien.Value = Math.Max(0m, hoaDon.TongTien - tienChietKhau - tienGiamGia);
            CapNhatCongNo();

        }


        private decimal TinhTienHangTuDanhSach()
        {
            decimal tong = 0m;

            foreach (DataGridViewRow row in dgvDanhsachSP.Rows)
            {
                if (row.IsNewRow)
                    continue;

                object giaTri = row.Cells["colThanhTien"].Value;

                if (giaTri != null && giaTri != DBNull.Value)
                {
                    tong += Convert.ToDecimal(giaTri);
                }
            }

            return tong;
        }


        private void UuDai_ValueChanged(object sender, EventArgs e)
        {
            TinhLaiHoaDon();
        }


        public frmBanLe(PhieuBanController ctrlPB)
            : this()
        {
            this.ctrlPhieuBan = ctrlPB;
            status = Controll.Normal;
        }

        private void frmNhapHang_Load(object sender, EventArgs e)
        {
            dangNapDuLieu = true;
            try
            {
                hoaDon.NapUuDai(PhieuBanController.DanhSachUuDaiHieuLuc("BAN_LE"));
                ctrlSanPham.HienthiAutoComboBox(cmbSanPham);
                ctrlMaSanPham.HienThiDataGridViewComboBox(colMaSanPham);
                cmbSanPham.SelectedIndexChanged -= cmbSanPham_SelectedIndexChanged;
                cmbSanPham.SelectedIndexChanged += cmbSanPham_SelectedIndexChanged;
                ctrlKhachHang.HienthiAutoComboBox(cmbKhachHang, false);
                ctrlPhieuBan.HienthiPhieuBan(bindingNavigator, cmbKhachHang, txtMaPhieu,
                    dtNgayLapPhieu, numTongTien, numDaTra, numConNo);
                NgatTheoDoiPhieu();
                nguonPhieu = bindingNavigator.BindingSource;
                if (nguonPhieu != null) nguonPhieu.CurrentChanged += BindingSource_CurrentChanged;
                if (status == Controll.AddNew)
                {
                    txtMaPhieu.Text = IdGenerator.Next("PHIEU_BAN");
                    // A new invoice must not load the detail rows of an existing ID.
                    ctrlChiTiet.HienThiChiTiet(dgvDanhsachSP, "-1");
                    cmbKhachHang.SelectedIndex = -1;
                }
                else this.Allow(false);
            }
            finally { dangNapDuLieu = false; }
            if (status == Controll.AddNew)
            {
                cmbSanPham_SelectedIndexChanged(cmbSanPham, EventArgs.Empty);
                DongBoTienHang();
            }
            else BindingSource_CurrentChanged(nguonPhieu, EventArgs.Empty);
        }

        private void GanNguonPhieu()
        {
            NgatTheoDoiPhieu();
            ctrlPhieuBan.HienthiPhieuBan(bindingNavigator, cmbKhachHang, txtMaPhieu,
                dtNgayLapPhieu, numTongTien, numDaTra, numConNo);
            nguonPhieu = bindingNavigator.BindingSource;
            nguonPhieu.CurrentChanged += BindingSource_CurrentChanged;
        }

        private void HuyThayDoi()
        {
            dangNapDuLieu = true;
            try
            {
                dgvDanhsachSP.CancelEdit();
                if (nguonPhieu != null) nguonPhieu.CancelEdit();
                if (controllerTruocKhiThem != null)
                {
                    ctrlPhieuBan = controllerTruocKhiThem;
                    controllerTruocKhiThem = null;
                }
                ctrlPhieuBan.HuyThayDoi();
                status = Controll.Normal;
                GanNguonPhieu();
            }
            finally { dangNapDuLieu = false; }
            BindingSource_CurrentChanged(this, EventArgs.Empty);
            Allow(false);
        }

        private void NgatTheoDoiPhieu()
        {
            if (nguonPhieu != null) nguonPhieu.CurrentChanged -= BindingSource_CurrentChanged;
            nguonPhieu = null;
        }

        void BindingSource_CurrentChanged(object sender, EventArgs e)
        {
            if (dangNapDuLieu || IsDisposed || Disposing || status != Controll.Normal) return;
            DataRowView current = nguonPhieu == null ? null : nguonPhieu.Current as DataRowView;
            string id = current == null ? "-1" : Convert.ToString(current["ID"]);
            dangNapDuLieu = true;
            try
            {
                ctrlPhieuBan.HienThiThongTinPhieu(cmbKhachHang, txtMaPhieu, dtNgayLapPhieu,
                    numTongTien, numDaTra, numConNo);
                ctrlChiTiet.HienThiChiTiet(dgvDanhsachSP, id);
                HienThiPhi(id);
            }
            finally { dangNapDuLieu = false; }
            Allow(false);
        }

        void cmbSanPham_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (dangNapDuLieu || cmbSanPham.SelectedValue == null ||
                cmbSanPham.SelectedValue == DBNull.Value || cmbSanPham.SelectedValue is DataRowView) return;
            MaSanPhamController ctrlMSP = new MaSanPhamController();
            cmbMaSanPham.SelectedIndexChanged -= cmbMaSanPham_SelectedIndexChanged;
            try { ctrlMSP.HienThiAutoComboBox(Convert.ToString(cmbSanPham.SelectedValue), cmbMaSanPham); }
            finally { cmbMaSanPham.SelectedIndexChanged += cmbMaSanPham_SelectedIndexChanged; }
            cmbMaSanPham_SelectedIndexChanged(cmbMaSanPham, EventArgs.Empty);
        }

        void cmbMaSanPham_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (dangNapDuLieu) return;
            if (cmbMaSanPham.SelectedValue == null || cmbMaSanPham.SelectedValue == DBNull.Value ||
                cmbMaSanPham.SelectedValue is DataRowView)
            {
                txtGiaNhap.Clear(); txtGiaBanSi.Clear(); txtGiaBanLe.Clear(); txtGiaBQGQ.Clear();
                numDonGia.Value = 0;
                return;
            }
            MaSanPhamController ctrl = new MaSanPhamController();
            MaSanPham masp = ctrl.LayMaSanPham(Convert.ToString(cmbMaSanPham.SelectedValue));
            if (masp == null || masp.SanPham == null) return;
            numDonGia.Value = masp.SanPham.GiaBanLe;
            txtGiaNhap.Text = masp.GiaNhap.ToString("#,###0");
            txtGiaBanSi.Text = masp.SanPham.GiaBanSi.ToString("#,###0");
            txtGiaBanLe.Text = masp.SanPham.GiaBanLe.ToString("#,###0");
            txtGiaBQGQ.Text = AccessControl.Can("XemGiaVon") ? WarehouseCost.Preview(masp.Id).ToString("#,###0") : "";
            label15.Text = "Giá vốn xuất:";
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            if(status==Controll.Normal || dangNapDuLieu) return;
            try {
                dgvDanhsachSP.EndEdit();
                BindingSource source=dgvDanhsachSP.DataSource as BindingSource; if(source!=null) source.EndEdit();
                numDonGia.Value=Decimal.Round(numDonGia.Value,0,MidpointRounding.AwayFromZero);
                ctrlMaSanPham.HienThiDataGridViewComboBox(colMaSanPham);
                IList<UuDaiQuyDinh> rules=ctrlChiTiet.ThemSanPhamVaUuDai(SaleInputValidation.Selection(cmbKhachHang),
                    SaleInputValidation.Selection(cmbSanPham),SaleInputValidation.Selection(cmbMaSanPham),txtMaPhieu.Text,
                    numSoLuong.Value,numDonGia.Value,chkHangTang.Checked,"BAN_LE",
                    status==Controll.AddNew?null:hoaDon.QuyTacDangDung);
                if(status==Controll.AddNew) hoaDon.CapNhatUuDai(rules);
                DongBoTienHang();
            } catch(Exception ex) {
                MessageBox.Show(ErrorLog.UserMessage(ex,"Không thể thêm sản phẩm và áp dụng ưu đãi"),TieuDeThongBao,MessageBoxButtons.OK,MessageBoxIcon.Warning);
            }
        }

        private void numDonGia_ValueChanged(object sender, EventArgs e)
        {
            if (dangNapDuLieu) return;
            try
            {
                numDonGia.Value = Decimal.Round(numDonGia.Value, 0, MidpointRounding.AwayFromZero);
                numThanhTien.Value = TienTe.ThanhTien(numDonGia.Value, numSoLuong.Value);
            }
            catch (OverflowException)
            {
                // Adding a line validates the amount and displays the error.
                numThanhTien.Value = 0;
            }
        }

        private void numTongTien_ValueChanged(object sender, EventArgs e)
        {
            CapNhatCongNo();
        }

        private void CapNhatCongNo()
        {
            if (dangNapDuLieu || status == Controll.Normal) return;
            decimal conNo = numTongTien.Value - numDaTra.Value;

            if (conNo < 0m)
                conNo = 0m;

            numConNo.Value = conNo;
        }

        // ---------- Phí phát sinh và giảm giá (dùng HoaDonDangLap) ----------

        // KHUYEN_MAI: mua đủ MUA_SL thì tặng TANG_SL cùng mặt hàng. Thêm dòng tặng còn thiếu (đơn giá 0, trừ tồn như hàng tặng).
        void DongBoUuDaiVaHangTang()
        {
            if(dangNapDuLieu || status==Controll.Normal) return;
            ctrlMaSanPham.HienThiDataGridViewComboBox(colMaSanPham);
            IList<UuDaiQuyDinh> rules=ctrlChiTiet.DongBoHangTang(txtMaPhieu.Text,"BAN_LE",
                status==Controll.AddNew?null:hoaDon.QuyTacDangDung);
            if(status==Controll.AddNew) hoaDon.CapNhatUuDai(rules);
            DongBoTienHang();
        }

        static bool LaHangTang(DataRowView rv)
        {
            object o = rv["LA_HANG_TANG"];
            return o != null && o != DBNull.Value && Convert.ToBoolean(o);
        }

        // Đọc tiền hàng và số lượng mua từ lưới chi tiết vào mô hình
        void DongBoTienHang()
        {
            List<DongTinhUuDai> lines=new List<DongTinhUuDai>();
            long money=0; int quantity=0;
            DataTable details=ctrlChiTiet.NewRow().Table;
            foreach(DataRow row in details.Rows) {
                if(row.RowState==DataRowState.Deleted || Convert.ToString(row["ID_PHIEU_BAN"])!=txtMaPhieu.Text) continue;
                bool gift=row["LA_HANG_TANG"]!=DBNull.Value && Convert.ToBoolean(row["LA_HANG_TANG"]);
                if(row["SO_LUONG"]==DBNull.Value || row["DON_GIA"]==DBNull.Value) continue;
                int count=Convert.ToInt32(row["SO_LUONG"]);
                long amount=gift?0:checked((long)TienTe.ThanhTien(Convert.ToDecimal(row["DON_GIA"]),count));
                if(row["THANH_TIEN"]==DBNull.Value || Convert.ToDecimal(row["THANH_TIEN"])!=amount) row["THANH_TIEN"]=(decimal)amount;
                if(gift) continue;
                money=checked(money+amount); quantity=checked(quantity+count);
                string lot=Convert.ToString(row["ID_MA_SAN_PHAM"]);
                lines.Add(new DongTinhUuDai { Lo=lot,SanPham=hoaDon.SanPhamCuaLo(lot),Tien=amount,SoLuong=count });
            }
            hoaDon.DatTienHang(lines,quantity,money);
        }

        void HienThiChiTietUuDai(KetQuaChinhSach result)
        {
            Control[] labels=Controls.Find("lblChiTietUuDai",true);
            if(labels.Length>0) labels[0].Text=result==null?"Phiếu cũ: giữ nguyên tổng giảm đã lưu.":
                "Giảm giá: "+result.GiamGia.ToString("N0")+" đ | CK thương mại: "+result.ThuongMai.ToString("N0")+
                " đ"+(result.ThanhToan>0?" | CK thanh toán cũ đã lưu: "+result.ThanhToan.ToString("N0")+" đ":"");
        }
        void DongBoPhi()
        {
            if (dangNapPhi)
                return;
            hoaDon.DatPhi((long)numPhiVanChuyen.Value, (double)numPhanTramGiam.Value, DocDichVu());
        }

        // Người quan sát: cập nhật ô tổng tiền và tiền giảm mỗi khi mô hình đổi
        void HoaDon_DaThayDoi(object sender, EventArgs e)
        {
            if (dangNapDuLieu || status == Controll.Normal) return;
            decimal total = hoaDon.TongTien;
            if (total < numTongTien.Minimum || total > numTongTien.Maximum)
                throw new OverflowException("Tong thanh toan vuot gioi han.");
            numTongTien.Value = total;
            lblTienGiam.Text = hoaDon.TienGiam.ToString("#,###0");
            HienThiChiTietUuDai(hoaDon.ChiTietUuDai);
        }

        IList<DichVuPhieuBan> DocDichVu()
        {
            return InvoiceServiceEditor.Read(dgvDichVu, false);
        }

        void HienThiPhi(String idPhieu)
        {
            PhieuBan phi = PhieuBanController.DocPhi(idPhieu);
            DataRowView header = nguonPhieu == null ? null : nguonPhieu.Current as DataRowView;
            cmbLoaiChietKhau.SelectedIndex = cmbLoaiGiamGia.SelectedIndex = 1;
            numGiaTriChietKhau.Value = header == null || header["TIEN_CHIET_KHAU"] == DBNull.Value ? 0 : Convert.ToDecimal(header["TIEN_CHIET_KHAU"]);
            numGiaTriGiamGia.Value = header == null || header["TIEN_GIAM_GIA"] == DBNull.Value ? 0 : Convert.ToDecimal(header["TIEN_GIAM_GIA"]);
            dangNapPhi = true;
            numPhiVanChuyen.Value = phi.PhiVanChuyen;
            numPhanTramGiam.Value = (decimal)phi.PhanTramGiamTay;
            hoaDon.KhoaPhanTramTuDong(phi.PhanTramTuDong);
            hoaDon.KhoaChiTiet(phi.ChiTietUuDai);
            dgvDichVu.Rows.Clear();
            foreach (DichVuPhieuBan dv in phi.DichVu)
            {
                dgvDichVu.Rows.Add(dv.Ten, dv.TienDV);
            }
            hoaDon.DatPhi(phi.PhiVanChuyen, phi.PhanTramGiamTay, phi.DichVu);
            hoaDon.DatThanhToan((long)numDaTra.Value);
            DongBoTienHang();
            dangNapPhi = false;
            lblTienGiam.Text = phi.TienGiam.ToString("#,###0");
            HienThiChiTietUuDai(phi.ChiTietUuDai);
            if (status != Controll.Normal)
            {
                DongBoPhi();
                DongBoTienHang();
            }
        }

        private void numPhiVanChuyen_ValueChanged(object sender, EventArgs e)
        {
            DongBoPhi();
        }

        private void numPhanTramGiam_ValueChanged(object sender, EventArgs e)
        {
            DongBoPhi();
        }

        // Cột "Tên dịch vụ" là ô chọn từ danh mục DICH_VU đang sử dụng.
        // DocDichVu() vẫn đọc r.Cells["colDichVuTen"] nên không cần sửa.
        private void dgvDichVu_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            DongBoPhi();
        }

        private void dgvDichVu_UserDeletedRow(object sender, DataGridViewRowEventArgs e)
        {
            DongBoPhi();
        }

        // ---------- Lưu / sửa / xóa ----------

        private void toolLuu_Click(object sender, EventArgs e)
        {
            bindingNavigatorPositionItem.Focus();
            if (this.Luu())
            {
                controllerTruocKhiThem = null;
                status = Controll.Normal;
                dangNapDuLieu = true;
                try { string savedId = txtMaPhieu.Text; GanNguonPhieu(); int position = nguonPhieu.Find("ID", savedId); if (position >= 0) nguonPhieu.Position = position; }
                finally { dangNapDuLieu = false; }
                BindingSource_CurrentChanged(this, EventArgs.Empty);
                this.Allow(false);
            }
        }

        // Trả về true nếu lưu thành công
        private bool KiemTraThanhToan()
        {
            string error = SaleInputValidation.CheckPayment(numTongTien.Value, numDaTra.Value);
            if (error.Length > 0)
            {
                MessageBox.Show(error, TieuDeThongBao, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                numDaTra.Focus();
                numDaTra.Select(0, numDaTra.Text.Length);
                return false;
            }
            numConNo.Value = numTongTien.Value - numDaTra.Value;
            return true;
        }

        bool Luu()
        {
            if (status == Controll.Normal) return true;
            try { return LuuThayDoi(); }
            catch (Exception ex) { System.Diagnostics.Trace.TraceError(ex.ToString()); MessageBox.Show(ErrorLog.UserMessage(ex, "Kh\u00f4ng th\u1ec3 ho\u00e0n t\u1ea5t thao t\u00e1c"), TieuDeThongBao, MessageBoxButtons.OK, MessageBoxIcon.Warning); return false; }
        }

        bool LuuThayDoi()
        {
            InvoiceServiceEditor.Read(dgvDichVu, true);
            hoaDon.DatThanhToan((long)numDaTra.Value);
            dgvDanhsachSP.EndEdit();
            string error = SaleInputValidation.CheckHeader(cmbKhachHang, dgvDanhsachSP);
            if (error.Length > 0)
            {
                MessageBox.Show(error, TieuDeThongBao, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (status != Controll.Normal)
            {
                BindingSource details = dgvDanhsachSP.DataSource as BindingSource;
                if (details != null) details.EndEdit();
                foreach (DataGridViewRow view in dgvDanhsachSP.Rows)
                {
                    DataRowView item = view.DataBoundItem as DataRowView;
                    if (item != null && item.Row.RowState != DataRowState.Unchanged)
                        item["THANH_TIEN"] = TienTe.ThanhTien(Convert.ToDecimal(item["DON_GIA"]), Convert.ToDecimal(item["SO_LUONG"]));
                }
                DongBoUuDaiVaHangTang();
                DongBoPhi();
                DongBoTienHang();
            }
            if (status == Controll.AddNew)
            {
                return ThemMoi();
            }
            if (!AccessControl.Can("SuaPhieuBan"))
            { MessageBox.Show("Bạn chỉ được xem phiếu đã lập. Chỉ quản lý được sửa phiếu.", TieuDeThongBao); return false; }
            if (!KiemTraThanhToan()) return false;
            try
            {
                CapNhat();
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ErrorLog.UserMessage(ex, "Kh\u00f4ng th\u1ec3 ho\u00e0n t\u1ea5t thao t\u00e1c"), TieuDeThongBao, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
        }

        // Sửa phiếu đã lưu: phiếu, chi tiết, tồn kho và phí cùng một giao dịch
        void CapNhat()
        {
            AccessControl.DemandCurrent("SuaPhieuBan");
            dgvDanhsachSP.EndEdit();
            BindingSource details = dgvDanhsachSP.DataSource as BindingSource;
            if (details != null) details.EndEdit();
            DongBoPhi();
            DongBoTienHang();
            DataRowView header = bindingNavigator.BindingSource.Current as DataRowView;
            if (header == null) throw new InvalidOperationException("Khong tim thay phieu ban.");
            header["ID_KHACH_HANG"] = cmbKhachHang.SelectedValue;
            header["NGAY_BAN"] = dtNgayLapPhieu.Value.Date;
            header["TONG_TIEN"] = numTongTien.Value;
            header["TIEN_CHIET_KHAU"] = 0m;
            header["TIEN_GIAM_GIA"] = hoaDon.TienGiam;
            header["DA_TRA"] = numDaTra.Value;
            header["CON_NO"] = numTongTien.Value - numDaTra.Value;
            header.EndEdit();
            ctrlPhieuBan.CapNhatPhieuVaChiTiet(ctrlChiTiet, hoaDon.ToPhieuBan(txtMaPhieu.Text));
        }

        // TEMPLATE METHOD: lập hóa đơn mới qua LapHoaDonMoi (kiểm tra -> lưu -> thông báo)
        bool ThemMoi()
        {
            if (!AccessControl.Can("ThietLapUuDai") && (numGiaTriChietKhau.Value != 0 || numGiaTriGiamGia.Value != 0))
                throw new UnauthorizedAccessException("Chỉ quản lý được thiết lập mức ưu đãi.");
            DongBoPhi();
            DongBoTienHang();
            if (!KiemTraThanhToan()) return false;

            DataRow row = ctrlPhieuBan.NewRow();
            row["ID"] = txtMaPhieu.Text;
            row["ID_KHACH_HANG"] = cmbKhachHang.SelectedValue;
            row["NGAY_BAN"] = dtNgayLapPhieu.Value.Date;
            row["TONG_TIEN"] = numTongTien.Value;
            row["DA_TRA"] = numDaTra.Value;
            row["CON_NO"] = numConNo.Value;
            row["TIEN_CHIET_KHAU"] = 0m; // Chiết khấu cũ đã bỏ; mọi ưu đãi gom vào TIEN_GIAM_GIA
            row["TIEN_GIAM_GIA"] = hoaDon.TienGiam; // Tiền giảm đúng với tổng đang lưu (tay + tự động)
            ctrlPhieuBan.Add(row);

            LapHoaDonMoi lap = new LapHoaDonMoi(ctrlPhieuBan, ctrlChiTiet, hoaDon.ToPhieuBan(txtMaPhieu.Text));
            bool daLuu;
            try
            {
                daLuu = lap.ThucHien();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError(ex.ToString());
                // Transaction đã rollback; giữ nguyên dữ liệu đang nhập để người dùng lưu lại
                MessageBox.Show("Lưu phiếu bán thất bại. Chi tiết: " + ErrorLog.UserMessage(ex, "Kh\u00f4ng th\u1ec3 ho\u00e0n t\u1ea5t thao t\u00e1c"), TieuDeThongBao, MessageBoxButtons.OK, MessageBoxIcon.Error);
                daLuu = false;
            }

            if (!daLuu)
            {
                // Dòng chưa được lưu: bỏ khỏi DataTable. Nếu trùng mã thì điền mã gợi ý vào ô mã phiếu.
                row.Delete();
                if (lap.MaGoiY != null)
                    txtMaPhieu.Text = lap.MaGoiY;
                return false;
            }

            return true;
        }

        private void toolLuu_Them_Click(object sender, EventArgs e)
        {
            if (status != Controll.Normal) return;
            controllerTruocKhiThem = ctrlPhieuBan;
            ctrlPhieuBan = new PhieuBanController();
            hoaDon.NapUuDai(PhieuBanController.DanhSachUuDaiHieuLuc("BAN_LE"));
            dangNapDuLieu = true;
            status = Controll.AddNew;
            try
            {
                GanNguonPhieu();
                txtMaPhieu.Text = IdGenerator.Next("PHIEU_BAN");
                ctrlChiTiet.HienThiChiTiet(dgvDanhsachSP, "-1");
                cmbKhachHang.SelectedIndex = -1;
                dtNgayLapPhieu.Value = DateTime.Today;
                numDaTra.Value = numConNo.Value = numTongTien.Value = 0;
                dangNapPhi = true;
                numPhiVanChuyen.Value = numPhanTramGiam.Value = 0;
                dgvDichVu.Rows.Clear();
                numGiaTriChietKhau.Value = numGiaTriGiamGia.Value = 0;
                cmbLoaiChietKhau.SelectedIndex = cmbLoaiGiamGia.SelectedIndex = 0;
            }
            finally { dangNapPhi = false; dangNapDuLieu = false; }
            DongBoPhi();
            DongBoTienHang();
            Allow(true);
        }

        private void btnRemove_Click(object sender, EventArgs e)
        {
            if (status == Controll.Normal || dgvDanhsachSP.CurrentRow == null || dgvDanhsachSP.CurrentRow.IsNewRow) return;
            BindingSource bs = dgvDanhsachSP.DataSource as BindingSource;

            if (bs == null || bs.Current == null)
                return;

            if (MessageBox.Show(
                "Bạn có chắc chắn xóa không?", TieuDeThongBao,
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            DataRowView row = bs.Current as DataRowView;

            if (row == null)
                return;

            bs.RemoveCurrent();

            DongBoUuDaiVaHangTang();
        }


        private void dgvDanhsachSP_UserDeletingRow(object sender, DataGridViewRowCancelEventArgs e)
        {
            if (status == Controll.Normal) { e.Cancel = true; return; }
            if (MessageBox.Show(
                "Bạn có chắc chắn xóa không?", TieuDeThongBao,
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question) == DialogResult.No)
            {
                e.Cancel = true;
                return;
            }

            DataRowView row = e.Row.DataBoundItem as DataRowView;

            if (row != null)
            {
            }
        }

        private void dgvDanhsachSP_UserDeletedRow(object sender, DataGridViewRowEventArgs e)
        {
            try { DongBoUuDaiVaHangTang(); } catch(Exception ex) { MessageBox.Show(ex.Message,TieuDeThongBao); }
        }

        private void toolLuuIn_Click(object sender, EventArgs e)
        {
            if (status != Controll.Normal)
            {
                MessageBox.Show("Vui lòng lưu lại Phiếu bán hiện tại!", TieuDeThongBao, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                String ma_phieu = txtMaPhieu.Text;
                PhieuBanController ctrlPB = new PhieuBanController();
                CuahangNongduoc.BusinessObject.PhieuBan ph = ctrlPB.LayPhieuBan(ma_phieu);
                frmInPhieuBan InPhieuBan = new frmInPhieuBan(ph);
                InPhieuBan.Show();
            }
        }

        private void dgvDanhsachSP_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            e.Cancel = true;
        }

        private void bindingNavigatorDeleteItem_Click(object sender, EventArgs e)
        {
        }

        private void toolChinhSua_Click(object sender, EventArgs e)
        {
            if (status != Controll.Normal || nguonPhieu == null || nguonPhieu.Current == null) return;
            if (!AccessControl.Can("SuaPhieuBan"))
            { MessageBox.Show("Chỉ quản lý được sửa phiếu đã lập.", TieuDeThongBao); return; }
            hoaDon.MoSuaUuDai(PhieuBanController.SanPhamTheoLo());
            status = Controll.Edit;
            DongBoPhi();
            DongBoTienHang();
            this.Allow(true);
        }

        void Allow(bool val)
        {
            txtMaPhieu.ReadOnly = true;
            txtMaPhieu.Enabled = false;
            dtNgayLapPhieu.Enabled = val;
            // Ô tổng tiền tính tự động, không sửa tay
            numTongTien.Enabled = false;
            btnAdd.Enabled = val;
            btnRemove.Enabled = val;
            dgvDanhsachSP.Enabled = true;
            dgvDanhsachSP.ReadOnly = !val;
            colSoLuong.ReadOnly = !val;
            colDonGia.ReadOnly = !val || !AccessControl.Can("SuaGiaBan");
            colMaSanPham.ReadOnly = !val;
            colMaPhieuBan.ReadOnly = true;
            colThanhTien.ReadOnly = true;
            toolLuu.Enabled = val;
            toolHuy.Enabled = val;
            bool hasInvoice = nguonPhieu != null && nguonPhieu.Current != null;
            toolThem.Enabled = !val && AccessControl.Can("BanHang");
            toolChinhSua.Enabled = !val && hasInvoice && AccessControl.Can("SuaPhieuBan");
            toolXoa.Enabled = !val && hasInvoice && AccessControl.Can("XoaPhieu");
            toolIn.Enabled = !val && hasInvoice;
            dgvDanhsachSP.AllowUserToDeleteRows = val;
            cmbSanPham.Enabled = numSoLuong.Enabled = val;
            bool automaticLots = WarehouseSettings.Load() == WarehouseSettings.FirstInFirstOut;
            cmbMaSanPham.Enabled = val && !automaticLots;
            colMaSanPham.ReadOnly = !val || automaticLots;
            numDonGia.Enabled = val && AccessControl.Can("SuaGiaBan");
            foreach (ToolStripItem item in new ToolStripItem[] { bindingNavigatorMoveFirstItem,
                bindingNavigatorMovePreviousItem, bindingNavigatorMoveNextItem, bindingNavigatorMoveLastItem,
                bindingNavigatorPositionItem }) item.Enabled = !val;
            numPhiVanChuyen.Enabled = val && (status == Controll.AddNew || AccessControl.Can("SuaPhieuBan"));
            numPhanTramGiam.Enabled = val && status == Controll.AddNew && AccessControl.Can("ThietLapUuDai");
            dgvDichVu.Enabled = val && (status == Controll.AddNew || AccessControl.Can("SuaPhieuBan"));
            cmbKhachHang.Enabled = val;
            numDaTra.Enabled = val;
            cmbLoaiChietKhau.Enabled = val && AccessControl.Can("ThietLapUuDai");
            numGiaTriChietKhau.Enabled = val && AccessControl.Can("ThietLapUuDai");

            cmbLoaiGiamGia.Enabled = val && AccessControl.Can("ThietLapUuDai");
            numGiaTriGiamGia.Enabled = val && AccessControl.Can("ThietLapUuDai");
        }

        private void toolThoat_Click(object sender, EventArgs e)
        {
            Close();
        }

        // TEMPLATE METHOD: xóa hóa đơn qua XoaHoaDon (không áp giới hạn bản dùng thử)
        private void toolXoa_Click(object sender, EventArgs e)
        {
            if (status != Controll.Normal || !AccessControl.Can("XoaPhieu") || nguonPhieu == null) return;
            DataRowView view = (DataRowView)bindingNavigator.BindingSource.Current;
            if (view == null)
                return;
            if (MessageBox.Show("Bạn có chắc chắn xóa không?", TieuDeThongBao, MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            String maPhieu = view["ID"].ToString();
            try
            {
                ThaoTacPhieuBan xoa = new XoaHoaDon(ctrlPhieuBan, maPhieu);
                if (!xoa.ThucHien())
                    return;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Xóa phiếu bán thất bại. Chi tiết: " + ErrorLog.UserMessage(ex, "Kh\u00f4ng th\u1ec3 ho\u00e0n t\u1ea5t thao t\u00e1c"), TieuDeThongBao, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            bindingNavigator.BindingSource.RemoveCurrent();
        }

        private void btnThemDaiLy_Click(object sender, EventArgs e)
        {
            frmKhachHang KhachHang = new frmKhachHang();
            KhachHang.ShowDialog();
            ctrlKhachHang.HienthiAutoComboBox(cmbKhachHang, false);
        }

        private void btnThemSanPham_Click(object sender, EventArgs e)
        {
            frmSanPham SanPham = new frmSanPham();
            SanPham.ShowDialog();
            ctrlSanPham.HienthiAutoComboBox(cmbSanPham);
        }
    }
}
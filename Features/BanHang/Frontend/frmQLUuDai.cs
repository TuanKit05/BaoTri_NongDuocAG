using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using CuahangNongduoc.BusinessObject;

namespace CuahangNongduoc
{
    public static class UuDaiSettings
    {
        public static void DemandAdmin()
        {
            AccessControl.DemandCurrent("ThietLapUuDai");
            if (!AccountRoles.IsAdmin(AppSession.CurrentUser.Role)) throw new UnauthorizedAccessException("Chỉ admin được cấu hình ưu đãi.");
        }
        public static DataTable Load()
        {
            DemandAdmin();
            using (OleDbConnection c = new OleDbConnection(DataService.m_ConnectString)) {
                c.Open(); return AccountRepository.ReadTable(c, null, "SELECT * FROM UU_DAI ORDER BY TU_NGAY DESC, ID DESC");
            }
        }
        public static string Save(string id, string type, decimal rate, decimal minimumMoney, int minimumQuantity,
            string lot, int buy, int gift, DateTime start, DateTime? end)
        {
            DemandAdmin();
            if (type != UuDaiQuyDinh.LoaiGiamPhanTram && type != UuDaiQuyDinh.LoaiChietKhau && type != UuDaiQuyDinh.LoaiKhuyenMai)
                throw new ArgumentException("Chọn loại ưu đãi hợp lệ.");
            bool promotion = type == UuDaiQuyDinh.LoaiKhuyenMai;
            if (!promotion && (rate <= 0 || rate > 100)) throw new ArgumentException("Tỷ lệ phải lớn hơn 0 và không vượt quá 100%.");
            if (minimumMoney < 0 || minimumQuantity < 0) throw new ArgumentException("Điều kiện tối thiểu không được âm.");
            if (promotion && (buy <= 0 || gift <= 0)) throw new ArgumentException("Số lượng mua và tặng phải lớn hơn 0.");
            if (end.HasValue && end.Value.Date < start.Date) throw new ArgumentException("Ngày kết thúc không được trước ngày bắt đầu.");
            lot = promotion ? (lot ?? "").Trim() : "";
            if (lot.Length > 50) throw new ArgumentException("Mã lô tối đa 50 ký tự.");
            string conditions = promotion ? "MUA_SL=" + buy + ";TANG_SL=" + gift :
                "TOI_THIEU_TIEN=" + minimumMoney.ToString(CultureInfo.InvariantCulture) + ";TOI_THIEU_SL=" + minimumQuantity;
            bool adding = String.IsNullOrEmpty(id);
            using (OleDbConnection c = new OleDbConnection(DataService.m_ConnectString)) {
                c.Open(); IdGenerator.EnsureSchema(c);
                if (adding) id = IdGenerator.Next("UU_DAI");
                using (OleDbTransaction tr = c.BeginTransaction()) {
                    try {
                        if (lot.Length > 0 && Convert.ToInt32(AccountRepository.Scalar(c, tr, "SELECT COUNT(*) FROM MA_SAN_PHAM WHERE ID=?", lot)) == 0)
                            throw new ArgumentException("Mã lô không tồn tại.");
                        object expires = end.HasValue ? (object)end.Value.Date : DBNull.Value;
                        if (adding) AccountRepository.Execute(c, tr,
                            "INSERT INTO UU_DAI (ID,LOAI,PHAM_VI,GIA_TRI,MA_SAN_PHAM,DIEU_KIEN,TU_NGAY,DEN_NGAY,NGUOI_THIET_LAP) VALUES (?,?,?,?,?,?,?,?,?)",
                            id, type, "HOA_DON", promotion ? 0d : (double)rate, lot.Length == 0 ? (object)DBNull.Value : lot,
                            conditions, start.Date, expires, AppSession.CurrentUser.Id);
                        else if (AccountRepository.Execute(c, tr,
                            "UPDATE UU_DAI SET LOAI=?,PHAM_VI=?,GIA_TRI=?,MA_SAN_PHAM=?,DIEU_KIEN=?,TU_NGAY=?,DEN_NGAY=?,NGUOI_THIET_LAP=? WHERE ID=?",
                            type, "HOA_DON", promotion ? 0d : (double)rate, lot.Length == 0 ? (object)DBNull.Value : lot,
                            conditions, start.Date, expires, AppSession.CurrentUser.Id, id) != 1) throw new InvalidOperationException("Ưu đãi không còn tồn tại.");
                        AccountRepository.Audit(c, tr, adding ? "THEM_UU_DAI" : "SUA_UU_DAI", id, type + "; " + rate + "%; " + conditions);
                        tr.Commit();
                    } catch { tr.Rollback(); throw; }
                }
            }
            return id;
        }
        public static void Delete(string id)
        {
            DemandAdmin();
            if (String.IsNullOrEmpty(id)) throw new ArgumentException("Chọn ưu đãi cần xóa.");
            using (OleDbConnection c = new OleDbConnection(DataService.m_ConnectString)) {
                c.Open(); using (OleDbTransaction tr = c.BeginTransaction()) {
                    try {
                        if (AccountRepository.Execute(c, tr, "DELETE FROM UU_DAI WHERE ID=?", id) != 1) throw new InvalidOperationException("Ưu đãi không còn tồn tại.");
                        AccountRepository.Audit(c, tr, "XOA_UU_DAI", id, "Xóa cấu hình; giữ nguyên phiếu đã lập."); tr.Commit();
                    } catch { tr.Rollback(); throw; }
                }
            }
        }
    }
    public partial class frmQLUuDai : Form
    {
        private readonly BindingSource nguon = new BindingSource();
        private string idDangChon;
        private bool dangNap;
        private readonly NumericUpDown numToiThieuTien = Number(1000000000000m);
        private readonly NumericUpDown numToiThieuSL = Number(Int32.MaxValue);
        private readonly NumericUpDown numMua = Number(Int32.MaxValue);
        private readonly NumericUpDown numTang = Number(Int32.MaxValue);
        private readonly Button btnSua = new Button();
        private readonly Button btnHuy = new Button();
        private readonly TextBox txtMa = new TextBox();
        private readonly ComboBox cmbSanPhamTang = new ComboBox();
        private TableLayoutPanel fields;
        private static NumericUpDown Number(decimal max) { return new NumericUpDown { Maximum = max, ThousandsSeparator = true }; }
        public frmQLUuDai()
        {
            InitializeComponent(); BuildLayout();
            Load += delegate { try { TaiLai(); } catch (Exception e) { MessageBox.Show(this,e.Message,Text); Close(); } };
            btnThem.Click += delegate { BeginEdit(true); };
            btnSua.Click += delegate { if (idDangChon != null) BeginEdit(false); };
            btnHuy.Click += delegate { ShowCurrent(); };
            btnLuu.Click += delegate { try { LuuCauHinh(); MessageBox.Show(this,"Đã lưu cấu hình ưu đãi.",Text); } catch(Exception e) { MessageBox.Show(this,e.Message,Text); } };
            btnXoa.Click += delegate {
                if (idDangChon == null || MessageBox.Show(this,"Xóa cấu hình này? Phiếu đã lập vẫn giữ nguyên.",Text,MessageBoxButtons.YesNo) != DialogResult.Yes) return;
                try { UuDaiSettings.Delete(idDangChon); TaiLai(); } catch(Exception e) { MessageBox.Show(this,e.Message,Text); }
            };
            btnDong.Click += delegate { Close(); };
            nguon.CurrentChanged += delegate { if (!dangNap) ShowCurrent(); };
            cmbLoai.SelectedValueChanged += delegate { TypeChanged(); };
            chkKhongHetHan.CheckedChanged += delegate { dtDenNgay.Enabled = !chkKhongHetHan.Checked; };
            Disposed += delegate { nguon.Dispose(); };
        }
        private void BuildLayout()
        {
            SuspendLayout(); Controls.Clear(); panel1.Dispose(); panel2.Controls.Clear(); panel2.Dispose();
            Font = new Font("Segoe UI",9F); AutoSize = false; AutoScaleMode = AutoScaleMode.None;
            Text = "Cấu hình giảm giá và chiết khấu"; StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(900,660); MinimumSize = new Size(790,650);
            TableLayoutPanel layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(12), ColumnCount = 1, RowCount = 4 };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute,295)); layout.RowStyles.Add(new RowStyle(SizeType.Absolute,48));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute,60)); layout.RowStyles.Add(new RowStyle(SizeType.Percent,100));
            fields = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 7 };
            fields.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,140)); fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50));
            fields.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,140)); fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50));
            for(int r=0;r<7;r++) fields.RowStyles.Add(new RowStyle(SizeType.Percent,100F/7));
            txtMa.ReadOnly = true; txtMa.Enabled = false;
            cmbLoai.DropDownStyle = ComboBoxStyle.DropDownList; cmbLoai.DisplayMember = "Value"; cmbLoai.ValueMember = "Key";
            cmbLoai.DataSource = new List<KeyValuePair<string,string>> {
                new KeyValuePair<string,string>(UuDaiQuyDinh.LoaiGiamPhanTram,"Giảm giá (%)"),
                new KeyValuePair<string,string>(UuDaiQuyDinh.LoaiChietKhau,"Chiết khấu (%)"),
                new KeyValuePair<string,string>(UuDaiQuyDinh.LoaiKhuyenMai,"Mua hàng tặng hàng") };
            numGiaTri.Maximum=100; numGiaTri.DecimalPlaces=2;
            cmbSanPhamTang.DropDownStyle = ComboBoxStyle.DropDownList; cmbSanPhamTang.DisplayMember = "Text"; cmbSanPhamTang.ValueMember = "Value";
            AddField("Mã ưu đãi",txtMa,0,0); AddField("Loại ưu đãi",cmbLoai,2,0);
            AddField("Tỷ lệ (%)",numGiaTri,0,1); AddField("Từ ngày",dtTuNgay,2,1);
            AddField("Tiền hàng tối thiểu",numToiThieuTien,0,2); AddField("Đến ngày",dtDenNgay,2,2);
            AddField("Số lượng tối thiểu",numToiThieuSL,0,3); AddField("",chkKhongHetHan,2,3); chkKhongHetHan.Text="Không hết hạn";
            AddField("Số lượng mua",numMua,0,4); AddField("Số lượng tặng",numTang,2,4); AddField("Sản phẩm tặng",cmbSanPhamTang,0,5);
            Label scope = new Label { Text="Áp dụng cho phiếu bán sỉ và bán lẻ mới. Để trống mã lô: mọi lô.", Dock=DockStyle.Fill, AutoSize=false };
            fields.Controls.Add(scope,2,5); fields.SetColumnSpan(scope,2);
            Label hint = new Label { Text="Giảm giá và chiết khấu tính trên tiền hàng; mỗi loại lấy mức cao nhất đủ điều kiện. Tổng tỷ lệ tối đa 100%.", Dock=DockStyle.Fill, AutoSize=false };
            fields.Controls.Add(hint,0,6); fields.SetColumnSpan(hint,4);
            FlowLayoutPanel buttons = new FlowLayoutPanel { Dock=DockStyle.Fill, WrapContents=false };
            Button[] all={btnThem,btnSua,btnLuu,btnHuy,btnXoa,btnDong}; string[] captions={"Thêm","Sửa","Lưu","Hủy","Xóa","Đóng"};
            for(int i=0;i<all.Length;i++) { all[i].BackColor=SystemColors.Control; all[i].UseVisualStyleBackColor=true; all[i].Text=captions[i]; all[i].AutoSize=false; all[i].Size=new Size(90,32); buttons.Controls.Add(all[i]); }
            Label note = new Label { AutoSize=false, Dock=DockStyle.Fill, Text="Cấu hình được lưu trong Access và ghi nhật ký thao tác. Chỉ admin được thêm, sửa, xóa.\nPhiếu đã lập giữ mức ưu đãi đã lưu. Tiền chiết khấu và giảm giá tự động được cộng vào tổng tiền giảm của phiếu." };
            dgvUuDai.Dock=DockStyle.Fill; dgvUuDai.AutoGenerateColumns=false; dgvUuDai.Columns.Clear();
            string[] names={"ID","LOAI","GIA_TRI","DIEU_KIEN","TU_NGAY","DEN_NGAY","NGUOI_THIET_LAP"};
            string[] titles={"Mã ưu đãi","Loại ưu đãi","Tỷ lệ (%)","Điều kiện","Từ ngày","Đến ngày","ID admin"};
            for(int i=0;i<names.Length;i++) {
                DataGridViewTextBoxColumn col=new DataGridViewTextBoxColumn { Name=names[i],DataPropertyName=names[i],HeaderText=titles[i],MinimumWidth=65 };
                if(names[i].EndsWith("NGAY")) col.DefaultCellStyle.Format="dd/MM/yyyy"; dgvUuDai.Columns.Add(col);
            }
            dgvUuDai.CellFormatting += delegate(object sender,DataGridViewCellFormattingEventArgs e) {
                if(e.ColumnIndex==1 && e.Value!=null) { string t=Convert.ToString(e.Value); e.Value=t=="GIAM_PHAN_TRAM"?"Giảm giá":t=="CHIET_KHAU"?"Chiết khấu":t=="KHUYEN_MAI"?"Mua tặng":t; }
            };
            dgvUuDai.ReadOnly=true; dgvUuDai.MultiSelect=false; dgvUuDai.SelectionMode=DataGridViewSelectionMode.FullRowSelect; dgvUuDai.DataSource=nguon;
            layout.Controls.Add(fields,0,0); layout.Controls.Add(buttons,0,1); layout.Controls.Add(note,0,2); layout.Controls.Add(dgvUuDai,0,3);
            Controls.Add(layout); ResumeLayout(true); SetEditing(false);
        }
        private void AddField(string title,Control input,int column,int row)
        {
            fields.Controls.Add(new Label { Text=title,Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft },column,row);
            input.Dock=DockStyle.Fill; input.Margin=new Padding(3,7,8,3); fields.Controls.Add(input,column+1,row);
        }
        private void SetEditing(bool editing)
        {
            fields.Enabled=editing; dgvUuDai.Enabled=!editing; btnThem.Enabled=!editing;
            btnSua.Enabled=btnXoa.Enabled=!editing && idDangChon!=null; btnLuu.Enabled=btnHuy.Enabled=editing; TypeChanged();
        }
        private void TypeChanged()
        {
            bool promotion=Convert.ToString(cmbLoai.SelectedValue)==UuDaiQuyDinh.LoaiKhuyenMai;
            numGiaTri.Enabled=numToiThieuTien.Enabled=numToiThieuSL.Enabled=!promotion;
            numMua.Enabled=numTang.Enabled=cmbSanPhamTang.Enabled=promotion; dtDenNgay.Enabled=!chkKhongHetHan.Checked;
            if(promotion && cmbSanPhamTang.Items.Count==0) LoadGiftProducts();
        }
        private void LoadGiftProducts()
        {
            cmbSanPhamTang.Items.Clear();
            cmbSanPhamTang.Items.Add(new { Text = "(Không chọn)", Value = "" });
            try {
                using (OleDbConnection c = new OleDbConnection(DataService.m_ConnectString)) {
                    c.Open();
                    using (OleDbCommand cmd = new OleDbCommand(
                        "SELECT TOP 50 ID, TEN_SAN_PHAM FROM SAN_PHAM WHERE NGAY_HET_HAN IS NOT NULL ORDER BY NGAY_HET_HAN ASC, ID ASC", c)) {
                        using (OleDbDataReader dr = cmd.ExecuteReader()) {
                            while (dr.Read()) {
                                string id = Convert.ToString(dr[0]);
                                string name = Convert.ToString(dr[1]);
                                cmbSanPhamTang.Items.Add(new { Text = name + " (" + id + ")", Value = id });
                            }
                        }
                    }
                }
            } catch { }
            cmbSanPhamTang.SelectedIndex=0;
        }
        private void TaiLai()
        {
            dangNap=true; try { nguon.DataSource=UuDaiSettings.Load(); } finally { dangNap=false; } ShowCurrent();
        }
        private void ShowCurrent()
        {
            DataRowView row=nguon.Current as DataRowView; idDangChon=row==null?null:Convert.ToString(row["ID"]);
            if(row!=null) {
                UuDaiQuyDinh rule=UuDaiQuyDinh.TuDataRow(row.Row); txtMa.Text=idDangChon; cmbLoai.SelectedValue=rule.Loai;
                numGiaTri.Value=Math.Max(0,Math.Min(100,rule.GiaTri)); numToiThieuTien.Value=Math.Max(0,Math.Min(numToiThieuTien.Maximum,rule.TienToiThieu));
                numToiThieuSL.Value=Math.Max(0,rule.SoLuongToiThieu); numMua.Value=Math.Max(0,rule.MuaSl); numTang.Value=Math.Max(0,rule.TangSl);
                cmbSanPhamTang.SelectedValue=rule.MaSanPham; if(cmbSanPhamTang.SelectedValue==null || Convert.ToString(cmbSanPhamTang.SelectedValue)!=rule.MaSanPham) cmbSanPhamTang.SelectedIndex=0;
                dtTuNgay.Value=row["TU_NGAY"]==DBNull.Value?DateTime.Today:Convert.ToDateTime(row["TU_NGAY"]);
                chkKhongHetHan.Checked=row["DEN_NGAY"]==DBNull.Value; dtDenNgay.Value=chkKhongHetHan.Checked?DateTime.Today.AddMonths(1):Convert.ToDateTime(row["DEN_NGAY"]);
            } else txtMa.Clear(); SetEditing(false);
        }
        internal void BeginEdit(bool adding)
        {
            UuDaiSettings.DemandAdmin();
            if(adding) {
                idDangChon=null; txtMa.Text="Tự động khi lưu"; cmbLoai.SelectedIndex=0; numGiaTri.Value=0;
                numToiThieuTien.Value=numToiThieuSL.Value=numMua.Value=numTang.Value=0; cmbSanPhamTang.SelectedIndex=0;
                dtTuNgay.Value=DateTime.Today; dtDenNgay.Value=DateTime.Today.AddMonths(1); chkKhongHetHan.Checked=false;
            } SetEditing(true);
        }
        internal void LuuCauHinh()
        {
            string id=UuDaiSettings.Save(idDangChon,Convert.ToString(cmbLoai.SelectedValue),numGiaTri.Value,numToiThieuTien.Value,
                Decimal.ToInt32(numToiThieuSL.Value),Convert.ToString(cmbSanPhamTang.SelectedValue ?? ""),Decimal.ToInt32(numMua.Value),Decimal.ToInt32(numTang.Value),
                dtTuNgay.Value,chkKhongHetHan.Checked?(DateTime?)null:dtDenNgay.Value);
            TaiLai(); nguon.Position=nguon.Find("ID",id); ShowCurrent();
        }
    }
}

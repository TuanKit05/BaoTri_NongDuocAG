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
                c.Open(); return AccountRepository.ReadTable(c, null, "SELECT * FROM UU_DAI WHERE LOAI <> 'CK_THANH_TOAN' ORDER BY TU_NGAY DESC, ID DESC");
            }
        }
        public static string Save(string id, string type, decimal rate, decimal minimumMoney, int minimumQuantity,
            string lot, int buy, int gift, DateTime start, DateTime? end)
        {
            return Save(id,type,rate,minimumMoney,minimumQuantity,lot,buy,gift,start,end,
                type == UuDaiQuyDinh.LoaiKhuyenMai && !String.IsNullOrEmpty(lot) ? "LO_HANG" : "HOA_DON");
        }
        public static string Save(string id, string type, decimal rate, decimal minimumMoney, int minimumQuantity,
            string lot, int buy, int gift, DateTime start, DateTime? end, string scope)
        {
            return Save(id,type,rate,minimumMoney,minimumQuantity,lot,buy,gift,start,end,scope,null);
        }
        public static string Save(string id, string type, decimal rate, decimal minimumMoney, int minimumQuantity,
            string lot, int buy, int gift, DateTime start, DateTime? end, string scope, string giftProduct)
        {
            return Save(id,type,rate,minimumMoney,minimumQuantity,lot,buy,gift,start,end,scope,giftProduct,"TAT_CA");
        }
        public static string Save(string id,string type,decimal rate,decimal minimumMoney,int minimumQuantity,
            string lot,int buy,int gift,DateTime start,DateTime? end,string scope,string giftProduct,string channel)
        {
            DemandAdmin();
            if (scope != "HOA_DON" && scope != "SAN_PHAM" && scope != "LO_HANG")
                throw new ArgumentException("Phạm vi áp dụng không hợp lệ.");
            if (type != UuDaiQuyDinh.LoaiGiamPhanTram && type != UuDaiQuyDinh.LoaiChietKhau && type != UuDaiQuyDinh.LoaiKhuyenMai)
                throw new ArgumentException("Chọn loại ưu đãi hợp lệ.");
            if(channel!="TAT_CA" && channel!="BAN_SI" && channel!="BAN_LE") throw new ArgumentException("Kênh áp dụng không hợp lệ.");
            bool promotion = type == UuDaiQuyDinh.LoaiKhuyenMai;
            if (!promotion && (rate <= 0 || rate > 100)) throw new ArgumentException("Tỷ lệ phải lớn hơn 0 và không vượt quá 100%.");
            if (minimumMoney < 0 || minimumQuantity < 0) throw new ArgumentException("Điều kiện tối thiểu không được âm.");
            if (promotion && (buy <= 0 || gift <= 0)) throw new ArgumentException("Số lượng mua và tặng phải lớn hơn 0.");
            if (end.HasValue && end.Value.Date < start.Date) throw new ArgumentException("Ngày kết thúc không được trước ngày bắt đầu.");
            lot = scope == "HOA_DON" ? "" : (lot ?? "").Trim();
            if (scope != "HOA_DON" && lot.Length == 0) throw new ArgumentException("Vui lòng chọn sản phẩm áp dụng.");
            if (scope == "LO_HANG" && !promotion) throw new ArgumentException("Phạm vi lô cũ chỉ dùng cho mua tặng.");
            string storedScope = scope == "LO_HANG" ? "HOA_DON" : scope;
            if (lot.Length > 50) throw new ArgumentException("Mã sản phẩm hoặc mã lô tối đa 50 ký tự.");
            string conditions = promotion ? "MUA_SL=" + buy + ";TANG_SL=" + gift :
                "TOI_THIEU_TIEN=" + minimumMoney.ToString(CultureInfo.InvariantCulture) + ";TOI_THIEU_SL=" + minimumQuantity;
            if (promotion && giftProduct != null) {
                giftProduct=giftProduct.Trim();
                if(giftProduct.Length==0) throw new ArgumentException("Vui lòng chọn sản phẩm tặng.");
                if(giftProduct.Length>50) throw new ArgumentException("Mã sản phẩm tặng tối đa 50 ký tự.");
                conditions+=";TANG_SP="+Uri.EscapeDataString(giftProduct);
                if(conditions.Length>255) throw new ArgumentException("Mã sản phẩm tặng quá dài để lưu cấu hình.");
            }
            conditions+=";KENH="+channel;
            if(conditions.Length>255) throw new ArgumentException("Nội dung cấu hình quá dài.");
            bool adding = String.IsNullOrEmpty(id);
            using (OleDbConnection c = new OleDbConnection(DataService.m_ConnectString)) {
                c.Open(); IdGenerator.EnsureSchema(c);
                if (adding) id = IdGenerator.Next("UU_DAI");
                using (OleDbTransaction tr = c.BeginTransaction()) {
                    try {
                        if (lot.Length > 0 && Convert.ToInt32(AccountRepository.Scalar(c, tr, "SELECT COUNT(*) FROM " + (scope == "SAN_PHAM" ? "SAN_PHAM" : "MA_SAN_PHAM") + " WHERE ID=?", lot)) == 0)
                            throw new ArgumentException("Sản phẩm hoặc lô đã chọn không còn tồn tại.");
                        if(promotion && !String.IsNullOrEmpty(giftProduct) &&
                            Convert.ToInt32(AccountRepository.Scalar(c,tr,"SELECT COUNT(*) FROM SAN_PHAM WHERE ID=?",giftProduct))==0)
                            throw new ArgumentException("Sản phẩm tặng không còn tồn tại.");
                        object expires = end.HasValue ? (object)end.Value.Date : DBNull.Value;
                        if (adding) AccountRepository.Execute(c, tr,
                            "INSERT INTO UU_DAI (ID,LOAI,PHAM_VI,GIA_TRI,MA_SAN_PHAM,DIEU_KIEN,TU_NGAY,DEN_NGAY,NGUOI_THIET_LAP) VALUES (?,?,?,?,?,?,?,?,?)",
                            id, type, storedScope, promotion ? 0d : (double)rate, lot.Length == 0 ? (object)DBNull.Value : lot,
                            conditions, start.Date, expires, AppSession.CurrentUser.Id);
                        else if (AccountRepository.Execute(c, tr,
                            "UPDATE UU_DAI SET LOAI=?,PHAM_VI=?,GIA_TRI=?,MA_SAN_PHAM=?,DIEU_KIEN=?,TU_NGAY=?,DEN_NGAY=?,NGUOI_THIET_LAP=? WHERE ID=?",
                            type, storedScope, promotion ? 0d : (double)rate, lot.Length == 0 ? (object)DBNull.Value : lot,
                            conditions, start.Date, expires, AppSession.CurrentUser.Id, id) != 1) throw new InvalidOperationException("Ưu đãi không còn tồn tại.");
                        AccountRepository.Audit(c, tr, adding ? "THEM_UU_DAI" : "SUA_UU_DAI", id, type + "; " + rate + "%; " + conditions + "; Phạm vi=" + scope + "; Sản phẩm=" + lot);
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
        private readonly ComboBox cmbKenh = new ComboBox();
        private readonly ComboBox cmbQuaTang = new ComboBox();
        private bool legacyGift;
        private readonly ComboBox cmbPhamVi = new ComboBox();
        private bool loadingScope;
        private readonly Dictionary<string,string> productNames = new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string,string> lotProducts = new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
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
            cmbLoai.SelectedValueChanged += delegate {
                if(fields.Enabled && !dangNap && Convert.ToString(cmbLoai.SelectedValue)==UuDaiQuyDinh.LoaiKhuyenMai)
                    cmbPhamVi.SelectedValue="SAN_PHAM";
                string kind=Convert.ToString(cmbLoai.SelectedValue);
                if(fields.Enabled && !dangNap && kind=="CHIET_KHAU") cmbKenh.SelectedValue="BAN_SI";
                TypeChanged();
            };
            cmbPhamVi.SelectedIndexChanged += delegate { if(!loadingScope) LoadGiftProducts(); TypeChanged(); };
            chkKhongHetHan.CheckedChanged += delegate { dtDenNgay.Enabled = !chkKhongHetHan.Checked; };
            Disposed += delegate { nguon.Dispose(); };
        }
        private void BuildLayout()
        {
            SuspendLayout(); Controls.Clear(); panel1.Dispose(); panel2.Controls.Clear(); panel2.Dispose();
            Font = new Font("Segoe UI",9F); AutoSize = false; AutoScaleMode = AutoScaleMode.None;
            Text = "Cấu hình giảm giá và chiết khấu"; StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(1080,700); MinimumSize = new Size(940,670);
            TableLayoutPanel layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(12), ColumnCount = 1, RowCount = 4 };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute,380)); layout.RowStyles.Add(new RowStyle(SizeType.Absolute,48));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute,60)); layout.RowStyles.Add(new RowStyle(SizeType.Percent,100));
            fields = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 9 };
            fields.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,140)); fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50));
            fields.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,140)); fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50));
            for(int r=0;r<9;r++) fields.RowStyles.Add(new RowStyle(SizeType.Percent,100F/9));
            txtMa.ReadOnly = true; txtMa.Enabled = false;
            cmbLoai.DropDownStyle = ComboBoxStyle.DropDownList; cmbLoai.DisplayMember = "Value"; cmbLoai.ValueMember = "Key";
            cmbLoai.DataSource = new List<KeyValuePair<string,string>> {
                new KeyValuePair<string,string>(UuDaiQuyDinh.LoaiGiamPhanTram,"Giảm giá (%)"),
                new KeyValuePair<string,string>(UuDaiQuyDinh.LoaiChietKhau,"Chiết khấu thương mại (%)"),
                new KeyValuePair<string,string>(UuDaiQuyDinh.LoaiKhuyenMai,"Mua hàng tặng hàng") };
            numGiaTri.Maximum=100; numGiaTri.DecimalPlaces=2;
            cmbSanPhamTang.DropDownStyle = ComboBoxStyle.DropDownList; cmbSanPhamTang.DisplayMember = "Text"; cmbSanPhamTang.ValueMember = "Value";
            AddField("Mã ưu đãi",txtMa,0,0); AddField("Loại ưu đãi",cmbLoai,2,0);
            AddField("Tỷ lệ (%)",numGiaTri,0,1); AddField("Từ ngày",dtTuNgay,2,1);
            AddField("Tiền hàng tối thiểu",numToiThieuTien,0,2); AddField("Đến ngày",dtDenNgay,2,2);
            AddField("Số lượng tối thiểu",numToiThieuSL,0,3); AddField("",chkKhongHetHan,2,3); chkKhongHetHan.Text="Không hết hạn";
            AddField("Số lượng mua",numMua,0,4); AddField("Số lượng tặng",numTang,2,4);
            cmbPhamVi.Name="cmbPhamVi"; cmbSanPhamTang.Name="cmbSanPhamApDung";
            cmbPhamVi.DropDownStyle=ComboBoxStyle.DropDownList;
            cmbPhamVi.DisplayMember="Value"; cmbPhamVi.ValueMember="Key";
            SetScopeChoices(false);
            AddField("Phạm vi áp dụng",cmbPhamVi,0,5);
            AddField("Sản phẩm áp dụng",cmbSanPhamTang,2,5);
            cmbQuaTang.Name="cmbSanPhamTang";
            cmbQuaTang.DropDownStyle=ComboBoxStyle.DropDownList;
            cmbQuaTang.DisplayMember="Text"; cmbQuaTang.ValueMember="Value";
            AddField("Sản phẩm tặng",cmbQuaTang,0,6);
            Label giftHint=new Label { Text="Mua đủ số lượng đã đặt để nhận sản phẩm tặng bên trái. Có thể chọn cùng hoặc khác sản phẩm mua.",Dock=DockStyle.Fill };
            fields.Controls.Add(giftHint,2,6); fields.SetColumnSpan(giftHint,2);
            cmbKenh.Name="cmbKenh"; cmbKenh.DropDownStyle=ComboBoxStyle.DropDownList;
            cmbKenh.DisplayMember="Value"; cmbKenh.ValueMember="Key";
            cmbKenh.DataSource=new List<KeyValuePair<string,string>> {
                new KeyValuePair<string,string>("TAT_CA","Tất cả kênh"),
                new KeyValuePair<string,string>("BAN_SI","Bán sỉ / đại lý"),
                new KeyValuePair<string,string>("BAN_LE","Bán lẻ / nông dân") };
            AddField("Kênh áp dụng",cmbKenh,0,7);
            Label paymentHint=new Label { Text="Chiết khấu áp dụng theo tiền hàng hoặc số lượng mua, không phụ thuộc số tiền đã trả.",Dock=DockStyle.Fill };
            fields.Controls.Add(paymentHint,2,7); fields.SetColumnSpan(paymentHint,2);
            Label hint=new Label { Text="Cùng phần hàng: lấy mức có lợi hơn giữa giảm giá và chiết khấu thương mại. Phí vận chuyển và dịch vụ không được chiết khấu.",Dock=DockStyle.Fill };
            fields.Controls.Add(hint,0,8); fields.SetColumnSpan(hint,4);
            FlowLayoutPanel buttons = new FlowLayoutPanel { Dock=DockStyle.Fill, WrapContents=false };
            Button[] all={btnThem,btnSua,btnLuu,btnHuy,btnXoa,btnDong}; string[] captions={"Thêm","Sửa","Lưu","Hủy","Xóa","Đóng"};
            for(int i=0;i<all.Length;i++) { all[i].BackColor=SystemColors.Control; all[i].UseVisualStyleBackColor=true; all[i].Text=captions[i]; all[i].AutoSize=false; all[i].Size=new Size(90,32); buttons.Controls.Add(all[i]); }
            Label note = new Label { AutoSize=false, Dock=DockStyle.Fill, Text="Cấu hình được lưu trong Access và ghi nhật ký thao tác. Chỉ admin được thêm, sửa, xóa.\nPhiếu đã lập giữ mức ưu đãi đã lưu. Tiền chiết khấu và giảm giá tự động được cộng vào tổng tiền giảm của phiếu." };
            dgvUuDai.Dock=DockStyle.Fill; dgvUuDai.AutoGenerateColumns=false; dgvUuDai.Columns.Clear();
            string[] names={"ID","LOAI","GIA_TRI","KENH_HIEN_THI","PHAM_VI_HIEN_THI","SAN_PHAM_HIEN_THI","SAN_PHAM_TANG_HIEN_THI","DIEU_KIEN_HIEN_THI","TU_NGAY","DEN_NGAY","NGUOI_THIET_LAP"};
            string[] titles={"Mã ưu đãi","Loại ưu đãi","Tỷ lệ (%)","Kênh áp dụng","Phạm vi áp dụng","Sản phẩm mua / áp dụng","Sản phẩm tặng","Điều kiện","Từ ngày","Đến ngày","ID admin"};
            for(int i=0;i<names.Length;i++) {
                DataGridViewTextBoxColumn col=new DataGridViewTextBoxColumn { Name=names[i],DataPropertyName=names[i],HeaderText=titles[i],MinimumWidth=65 };
                if(names[i].EndsWith("NGAY")) col.DefaultCellStyle.Format="dd/MM/yyyy"; dgvUuDai.Columns.Add(col);
            }
            dgvUuDai.CellFormatting += delegate(object sender,DataGridViewCellFormattingEventArgs e) {
                if(e.ColumnIndex==1 && e.Value!=null) { string t=Convert.ToString(e.Value); e.Value=t=="GIAM_PHAN_TRAM"?"Giảm giá":t=="CHIET_KHAU"?"CK thương mại":t=="KHUYEN_MAI"?"Mua tặng":t; }
            };
            dgvUuDai.AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill;
            dgvUuDai.Columns["SAN_PHAM_HIEN_THI"].MinimumWidth=180;
            dgvUuDai.Columns["DIEU_KIEN_HIEN_THI"].MinimumWidth=160;
            dgvUuDai.Columns["SAN_PHAM_TANG_HIEN_THI"].MinimumWidth=180;
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
            Control productLabel=fields.GetControlFromPosition(2,5);
            if(productLabel!=null) productLabel.Text=promotion?"Sản phẩm mua":"Sản phẩm áp dụng";
            numGiaTri.Enabled=numToiThieuTien.Enabled=numToiThieuSL.Enabled=!promotion;
            numMua.Enabled=numTang.Enabled=cmbQuaTang.Enabled=promotion;
            cmbSanPhamTang.Enabled=Convert.ToString(cmbPhamVi.SelectedValue)!="HOA_DON"; dtDenNgay.Enabled=!chkKhongHetHan.Checked;

        }
        private void SetScopeChoices(bool legacy)
        {
            loadingScope=true;
            try {
                List<KeyValuePair<string,string>> choices=new List<KeyValuePair<string,string>> {
                    new KeyValuePair<string,string>("HOA_DON","Toàn cửa hàng"),
                    new KeyValuePair<string,string>("SAN_PHAM","Sản phẩm cụ thể") };
                if(legacy) choices.Add(new KeyValuePair<string,string>("LO_HANG","Lô hàng (cấu hình cũ)"));
                cmbPhamVi.DataSource=choices;
            } finally { loadingScope=false; }
        }
        private void LoadGiftProducts()
        {
            string scope=Convert.ToString(cmbPhamVi.SelectedValue);
            DataTable choices=new DataTable(); choices.Columns.Add("Value",typeof(string)); choices.Columns.Add("Text",typeof(string));
            choices.Rows.Add("",scope=="HOA_DON"?"Tất cả sản phẩm":"-- Chọn sản phẩm --");
            if(scope=="SAN_PHAM") foreach(KeyValuePair<string,string> pair in productNames)
                choices.Rows.Add(pair.Key,pair.Value+" ("+pair.Key+")");
            if(scope=="LO_HANG") foreach(KeyValuePair<string,string> pair in lotProducts) {
                string name; productNames.TryGetValue(pair.Value,out name);
                choices.Rows.Add(pair.Key,(name??pair.Value)+" - Lô "+pair.Key);
            }
            cmbSanPhamTang.DataSource=choices; cmbSanPhamTang.SelectedIndex=0;
        }
        private void TaiLai()
        {
            dangNap=true;
            try {
                productNames.Clear(); lotProducts.Clear();
                using(OleDbConnection c=new OleDbConnection(DataService.m_ConnectString)) {
                    c.Open();
                    DataTable products=AccountRepository.ReadTable(c,null,"SELECT ID,TEN_SAN_PHAM FROM SAN_PHAM ORDER BY TEN_SAN_PHAM");
                    foreach(DataRow product in products.Rows) productNames[Convert.ToString(product["ID"])]=Convert.ToString(product["TEN_SAN_PHAM"]);
                    DataTable lots=AccountRepository.ReadTable(c,null,"SELECT ID,ID_SAN_PHAM FROM MA_SAN_PHAM");
                    foreach(DataRow lot in lots.Rows) lotProducts[Convert.ToString(lot["ID"])]=Convert.ToString(lot["ID_SAN_PHAM"]);
                }
                DataTable gifts=new DataTable(); gifts.Columns.Add("Value",typeof(string)); gifts.Columns.Add("Text",typeof(string));
                gifts.Rows.Add("","-- Chọn sản phẩm tặng --");
                foreach(KeyValuePair<string,string> product in productNames) gifts.Rows.Add(product.Key,product.Value+" ("+product.Key+")");
                cmbQuaTang.DataSource=gifts;
                DataTable table=UuDaiSettings.Load();
                table.Columns.Add("PHAM_VI_HIEN_THI",typeof(string)); table.Columns.Add("SAN_PHAM_HIEN_THI",typeof(string));
                table.Columns.Add("DIEU_KIEN_HIEN_THI",typeof(string));
                table.Columns.Add("SAN_PHAM_TANG_HIEN_THI",typeof(string));
                table.Columns.Add("KENH_HIEN_THI",typeof(string));
                foreach(DataRow row in table.Rows) {
                    UuDaiQuyDinh rule=UuDaiQuyDinh.TuDataRow(row);
                    row["KENH_HIEN_THI"]=rule.Kenh=="BAN_SI"?"Bán sỉ":rule.Kenh=="BAN_LE"?"Bán lẻ":"Tất cả";
                    bool product=rule.PhamVi=="SAN_PHAM", legacy=!product && rule.MaSanPham.Length>0;
                    row["PHAM_VI_HIEN_THI"]=product?"Sản phẩm cụ thể":legacy?"Lô hàng (cấu hình cũ)":"Toàn cửa hàng";
                    string id=rule.MaSanPham, name;
                    if(legacy) { string productId; if(lotProducts.TryGetValue(id,out productId)) id=productId; }
                    productNames.TryGetValue(id,out name);
                    row["SAN_PHAM_HIEN_THI"]=product||legacy?(name??"Sản phẩm không còn tồn tại")+" ("+id+")"+(legacy?" - Lô "+rule.MaSanPham:""):"Tất cả sản phẩm";
                    string giftName;
                    productNames.TryGetValue(rule.SanPhamTang,out giftName);
                    row["SAN_PHAM_TANG_HIEN_THI"]=rule.Loai!=UuDaiQuyDinh.LoaiKhuyenMai?"":
                        rule.SanPhamTang.Length==0?"Cùng sản phẩm mua (cấu hình cũ)":
                        (giftName??"Sản phẩm không còn tồn tại")+" ("+rule.SanPhamTang+")";
                    row["DIEU_KIEN_HIEN_THI"]=rule.Loai==UuDaiQuyDinh.LoaiKhuyenMai?
                        "Mua "+rule.MuaSl+" tặng "+rule.TangSl+(rule.SanPhamTang.Length==0?" (từng lô, cấu hình cũ)":""):
                        "Tối thiểu "+rule.TienToiThieu.ToString("N0")+" đ; "+rule.SoLuongToiThieu+" sản phẩm";
                }
                nguon.DataSource=table;
            } finally { dangNap=false; }
            ShowCurrent();
        }
        private void ShowCurrent()
        {
            DataRowView row=nguon.Current as DataRowView; idDangChon=row==null?null:Convert.ToString(row["ID"]);
            if(row!=null) {
                UuDaiQuyDinh rule=UuDaiQuyDinh.TuDataRow(row.Row); txtMa.Text=idDangChon; cmbLoai.SelectedValue=rule.Loai;
                cmbKenh.SelectedValue=rule.Kenh;
                legacyGift=rule.Loai==UuDaiQuyDinh.LoaiKhuyenMai && rule.SanPhamTang.Length==0;
                cmbQuaTang.SelectedValue=rule.SanPhamTang;
                if(cmbQuaTang.SelectedIndex<0) cmbQuaTang.SelectedIndex=0;
                numGiaTri.Value=Math.Max(0,Math.Min(100,rule.GiaTri)); numToiThieuTien.Value=Math.Max(0,Math.Min(numToiThieuTien.Maximum,rule.TienToiThieu));
                numToiThieuSL.Value=Math.Max(0,rule.SoLuongToiThieu); numMua.Value=Math.Max(0,rule.MuaSl); numTang.Value=Math.Max(0,rule.TangSl);
                SetScopeChoices(rule.PhamVi!="SAN_PHAM" && rule.MaSanPham.Length>0);
                loadingScope=true;
                try { cmbPhamVi.SelectedValue=rule.PhamVi=="SAN_PHAM"?"SAN_PHAM":rule.MaSanPham.Length>0?"LO_HANG":"HOA_DON"; }
                finally { loadingScope=false; }
                LoadGiftProducts();
                if (cmbSanPhamTang.Items.Count > 0)
                {
                    cmbSanPhamTang.SelectedValue = rule.MaSanPham;
                    if (Convert.ToString(cmbSanPhamTang.SelectedValue) != rule.MaSanPham)
                        cmbSanPhamTang.SelectedIndex = 0;
                }
                dtTuNgay.Value=row["TU_NGAY"]==DBNull.Value?DateTime.Today:Convert.ToDateTime(row["TU_NGAY"]);
                chkKhongHetHan.Checked=row["DEN_NGAY"]==DBNull.Value; dtDenNgay.Value=chkKhongHetHan.Checked?DateTime.Today.AddMonths(1):Convert.ToDateTime(row["DEN_NGAY"]);
            } else txtMa.Clear(); SetEditing(false);
        }
        internal void BeginEdit(bool adding)
        {
            UuDaiSettings.DemandAdmin();
            if(adding) {
                cmbKenh.SelectedValue="TAT_CA";
                legacyGift=false; if(cmbQuaTang.Items.Count>0) cmbQuaTang.SelectedIndex=0;
                SetScopeChoices(false); cmbPhamVi.SelectedValue="HOA_DON"; LoadGiftProducts();
                idDangChon=null; txtMa.Text="Tự động khi lưu"; cmbLoai.SelectedIndex=0; numGiaTri.Value=0;
                numToiThieuTien.Value=numToiThieuSL.Value=numMua.Value=numTang.Value=0; if(cmbSanPhamTang.Items.Count>0) cmbSanPhamTang.SelectedIndex=0;
                dtTuNgay.Value=DateTime.Today; dtDenNgay.Value=DateTime.Today.AddMonths(1); chkKhongHetHan.Checked=false;
            } SetEditing(true);
        }
        internal void LuuCauHinh()
        {
            string id=UuDaiSettings.Save(idDangChon,Convert.ToString(cmbLoai.SelectedValue),numGiaTri.Value,numToiThieuTien.Value,
                Decimal.ToInt32(numToiThieuSL.Value),Convert.ToString(cmbSanPhamTang.SelectedValue ?? ""),Decimal.ToInt32(numMua.Value),Decimal.ToInt32(numTang.Value),
                dtTuNgay.Value,chkKhongHetHan.Checked?(DateTime?)null:dtDenNgay.Value,Convert.ToString(cmbPhamVi.SelectedValue),
                legacyGift && Convert.ToString(cmbQuaTang.SelectedValue).Length==0 ? null : Convert.ToString(cmbQuaTang.SelectedValue),Convert.ToString(cmbKenh.SelectedValue));
            TaiLai(); nguon.Position=nguon.Find("ID",id); ShowCurrent();
        }
    }
}

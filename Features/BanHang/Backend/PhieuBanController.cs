using System;
using System.Collections.Generic;
using System.Text;
using CuahangNongduoc.DataLayer;
using CuahangNongduoc.BusinessObject;
using System.Windows.Forms;
using System.Data;
using System.IO;
using System.Xml;

namespace CuahangNongduoc.Controller
{
    public class PhieuBanController
    {
        PhieuBanFactory factory = new PhieuBanFactory();

        BindingSource bs = new BindingSource();

        public PhieuBanController()
        {
            bs.DataSource = factory.NapPhieuBan("-1");
        }

        public void TaiPhieuBan(string id)
        {
            bs.DataSource = factory.NapPhieuBan(id);
        }

        public DataRow NewRow()
        {
            return factory.NewRow();
        }

        public void Add(DataRow row)
        {
            factory.Add(row);
        }

        public void HuyThayDoi()
        {
            bs.CancelEdit();
            factory.HuyThayDoi();
            bs.ResetBindings(false);
        }

        public void Update()
        {
            bs.MoveNext();
            factory.Save();
        }

        /// <summary>
        /// Lưu phiếu bán và chi tiết (kèm trừ tồn) trong CÙNG một transaction.
        /// Được gọi bởi các thao tác trong khung ThaoTacPhieuBan.
        /// </summary>
        public void LuuPhieuVaChiTiet(ChiTietPhieuBanController ctrlChiTiet)
        {
            LuuPhieuVaChiTiet(ctrlChiTiet, null);
        }

        public void LuuPhieuVaChiTiet(ChiTietPhieuBanController ctrlChiTiet, PhieuBan phi)
        {
            if (ctrlChiTiet == null || (factory.CoPhieuMoi() && !ctrlChiTiet.CoDongMoi()))
                throw new InvalidOperationException("Phieu ban phai co it nhat mot dong hang.");

            AccessControl.DemandCurrent("BanHang");
            bs.EndEdit();
            factory.KiemTraHangTang(ctrlChiTiet.NewRow().Table);
            Dictionary<string, long> configuredDiscounts = factory.KiemTraUuDaiTuDong(ctrlChiTiet.NewRow().Table, phi);
            if (phi != null)
            {
                factory.DatChiTietUuDai(phi);
                factory.DatTienGiamGia(phi.Id, phi.TienGiam);
                factory.DatTienVanChuyen(phi.Id, phi.PhiVanChuyen);
            }
            using (System.Data.OleDb.OleDbConnection conn = new System.Data.OleDb.OleDbConnection(DataService.m_ConnectString))
            {
                conn.Open();
                using (System.Data.OleDb.OleDbTransaction tr = conn.BeginTransaction())
                {
                    try
                    {
                        AccessControl.SaleTransaction = tr;
                        AccessControl.SaleAutomaticDiscounts = configuredDiscounts;
                        factory.SaveTrongGiaoDich(conn, tr);
                        if (phi != null) factory.LuuDichVuTrongGiaoDich(conn, tr, phi.Id, phi.DichVu);
                        ctrlChiTiet.SaveTrongGiaoDich(conn, tr);
                        tr.Commit();
                        AccessControl.SaleTransaction = null;
                        AccessControl.SaleAutomaticDiscounts = null;
                    }
                    catch
                    {
                        AccessControl.SaleTransaction = null;
                        AccessControl.SaleAutomaticDiscounts = null;
                        ErrorLog.Rollback(tr, "Rollback Access");
                        throw;
                    }
                }
            }
            factory.ChapNhanThayDoi();
            ctrlChiTiet.ChapNhanThayDoi();
        }

        /// <summary>
        /// Cập nhật phiếu đã lập: phiếu, chi tiết, đồng bộ tồn và phí XML.
        /// Tất cả ghi xuống CSDL nằm trong CÙNG một transaction. Nếu lỗi thì rollback
        /// CSDL và khôi phục phí XML về trạng thái cũ.
        /// </summary>
        public void CapNhatPhieuVaChiTiet(ChiTietPhieuBanController ctrlChiTiet, PhieuBan phiMoi)
        {
            AccessControl.DemandCurrent("SuaPhieuBan");
            // Chỉ mức giảm nhập tay mới cần quyền; mức tự động do quản lý thiết lập nên không chặn nhân viên
            if (!AccessControl.Can("ThietLapUuDai") && phiMoi.PhanTramGiamTay != 0)
                throw new UnauthorizedAccessException("Nhân viên không được tự nhập mức ưu đãi.");

            KiemTraKyChotCuaPhieu(phiMoi.Id);
            PhieuBan phiCu = DocPhi(phiMoi.Id);
            GhiPhiKhongKiemTra(phiMoi);
            try
            {
                using (System.Data.OleDb.OleDbConnection conn = new System.Data.OleDb.OleDbConnection(DataService.m_ConnectString))
                {
                    conn.Open();
                    using (System.Data.OleDb.OleDbTransaction tr = conn.BeginTransaction())
                    {
                        try
                        {
                            AccessControl.SaleTransaction = tr;
                            factory.DatChiTietUuDai(phiMoi);
                            factory.DatTienVanChuyen(phiMoi.Id, phiMoi.PhiVanChuyen);
                            factory.DatTienGiamGia(phiMoi.Id, phiMoi.TienGiam);
                            factory.SaveTrongGiaoDich(conn, tr);
                            factory.LuuDichVuTrongGiaoDich(conn, tr, phiMoi.Id, phiMoi.DichVu);
                            ctrlChiTiet.SaveTrongGiaoDich(conn, tr);
                            tr.Commit();
                        }
                        catch
                        {
                            tr.Rollback();
                            throw;
                        }
                        finally
                        {
                            AccessControl.SaleTransaction = null;
                        }
                    }
                }
            }
            catch
            {
                // CSDL không ghi được: đưa phí XML về trạng thái cũ để hai nơi khớp nhau
                try { GhiPhiKhongKiemTra(phiCu); }
                catch { /* giữ nguyên lỗi gốc của CSDL */ }
                throw;
            }
            factory.ChapNhanThayDoi();
            ctrlChiTiet.ChapNhanThayDoi();
        }

        /// <summary>
        /// Kiểm tra dữ liệu phiếu trước khi lưu. Trả về nội dung lỗi, hoặc null nếu hợp lệ.
        /// </summary>
        // Kỳ (tháng/năm) đã chốt công nợ: có dòng DU_NO_KH với DA_CHOT = true (YC khóa kỳ)
        public static bool KyDaChot(int thang, int nam)
        {
            DataService ds = new DataService();
            System.Data.OleDb.OleDbCommand cmd = new System.Data.OleDb.OleDbCommand(
                "SELECT COUNT(*) FROM DU_NO_KH WHERE THANG = @t AND NAM = @n AND DA_CHOT = TRUE");
            cmd.Parameters.Add("t", System.Data.OleDb.OleDbType.Integer).Value = thang;
            cmd.Parameters.Add("n", System.Data.OleDb.OleDbType.Integer).Value = nam;
            object o = ds.ExecuteScalar(cmd);
            return o != null && Convert.ToInt32(o) > 0;
        }

        // Chặn sửa hoặc xóa phiếu thuộc kỳ đã chốt. Phiếu chưa có trong CSDL thì không kiểm tra.
        public static void KiemTraKyChotCuaPhieu(String idPhieu)
        {
            DataService ds = new DataService();
            System.Data.OleDb.OleDbCommand cmd = new System.Data.OleDb.OleDbCommand("SELECT NGAY_BAN FROM PHIEU_BAN WHERE ID = @id");
            cmd.Parameters.Add("id", System.Data.OleDb.OleDbType.VarChar, 50).Value = idPhieu;
            object o = ds.ExecuteScalar(cmd);
            if (o == null || o == DBNull.Value)
                return;
            DateTime ngay = Convert.ToDateTime(o);
            if (KyDaChot(ngay.Month, ngay.Year))
                throw new InvalidOperationException("Kỳ " + ngay.Month + "/" + ngay.Year + " đã chốt công nợ. Không sửa hoặc xóa phiếu của kỳ này.");
        }

        // Đặt phí vận chuyển vào phiếu trong bộ nhớ (cột TIEN_VAN_CHUYEN của CSDL) trước khi lưu giao dịch
        public void DatTienVanChuyen(String id, long phiVanChuyen)
        {
            factory.DatTienVanChuyen(id, phiVanChuyen);
        }

        public static String KiemTraTruocKhiLuu(object khachHang, int soDongHang, decimal tongTien, decimal daTra)
        {
            bool coKhachHang = khachHang != null && khachHang != DBNull.Value
                && !(khachHang is DataRowView)
                && !String.IsNullOrEmpty(Convert.ToString(khachHang));
            if (!coKhachHang)
                return "Vui lòng chọn khách hàng.";
            if (soDongHang <= 0)
                return "Phiếu chưa có dòng hàng nào. Vui lòng chọn mã sản phẩm, lô và số lượng.";
            if (daTra < 0)
                return "Số tiền đã trả không được âm.";
            if (daTra > tongTien)
                return "Số tiền đã trả không được lớn hơn tổng tiền của phiếu.";
            return null;
        }

        /// <summary>
        /// Xóa phiếu bán: hoàn tồn, xóa chi tiết và xóa phiếu trong CÙNG một transaction.
        /// Không áp giới hạn bản dùng thử. Sau khi commit thì xóa mục phí trong phihoadon.xml.
        /// </summary>
        public void XoaPhieuBan(String id)
        {
            AccessControl.DemandCurrent("XoaPhieu");
            KiemTraKyChotCuaPhieu(id);
            ChiTietPhieuBanController ctrlChiTiet = new ChiTietPhieuBanController();
            using (System.Data.OleDb.OleDbConnection conn = new System.Data.OleDb.OleDbConnection(DataService.m_ConnectString))
            {
                conn.Open();
                using (System.Data.OleDb.OleDbTransaction tr = conn.BeginTransaction())
                {
                    try
                    {
                        ctrlChiTiet.XoaTheoPhieuTrongGiaoDich(conn, tr, id);
                        factory.XoaDichVuTrongGiaoDich(conn, tr, id);
                        factory.XoaPhieuTrongGiaoDich(conn, tr, id);
                        tr.Commit();
                    }
                    catch
                    {
                        ErrorLog.Rollback(tr, "Rollback Access");
                        throw;
                    }
                }
            }
            XoaPhi(id);
        }

        // Ưu đãi hiệu lực trong ngày, đã chuyển sang đối tượng để HoaDonDangLap dùng
        public static IDictionary<string,string> SanPhamTheoLo()
        {
            Dictionary<string,string> result = new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
            using (System.Data.OleDb.OleDbConnection c = new System.Data.OleDb.OleDbConnection(DataService.m_ConnectString)) {
                c.Open();
                using (System.Data.OleDb.OleDbCommand cmd = new System.Data.OleDb.OleDbCommand("SELECT ID,ID_SAN_PHAM FROM MA_SAN_PHAM",c))
                using (System.Data.OleDb.OleDbDataReader r = cmd.ExecuteReader())
                    while(r.Read()) result[Convert.ToString(r[0])] = Convert.ToString(r[1]);
            }
            return result;
        }

        public static string KenhKhachHang(string customer)
        {
            using(System.Data.OleDb.OleDbConnection c=new System.Data.OleDb.OleDbConnection(DataService.m_ConnectString)) {
                c.Open(); object value=AccountRepository.Scalar(c,null,"SELECT LOAI_KH FROM KHACH_HANG WHERE ID=?",customer);
                if(value==null || value==DBNull.Value) throw new InvalidOperationException("Khách hàng không tồn tại hoặc chưa xác định kênh bán.");
                return Convert.ToBoolean(value)?"BAN_SI":"BAN_LE";
            }
        }
        public static IList<UuDaiQuyDinh> DanhSachUuDaiHieuLuc(string channel)
        {
            List<UuDaiQuyDinh> selected=new List<UuDaiQuyDinh>();
            foreach(UuDaiQuyDinh rule in DanhSachUuDaiHieuLuc()) if(rule.DungKenh(channel)) selected.Add(rule);
            return selected;
        }
        public static IList<UuDaiQuyDinh> DanhSachUuDaiHieuLuc()
        {
            List<UuDaiQuyDinh> ds = new List<UuDaiQuyDinh>();
            DataTable tbl = PhieuBanFactory.LayUuDaiHieuLuc(DateTime.Today);
            IDictionary<string,string> products = SanPhamTheoLo();
            foreach (DataRow row in tbl.Rows) {
                UuDaiQuyDinh rule = UuDaiQuyDinh.TuDataRow(row);
                rule.SanPhamTheoLo = products;
                ds.Add(rule);
            }
            return ds;
        }

        // Dịch vụ của phiếu trong CHI_TIET_DICH_VU (Ten lấy từ MO_TA, TienDV lấy từ THANH_TIEN)
        static List<DichVuPhieuBan> LayDichVuDb(String id)
        {
            List<DichVuPhieuBan> ds = new List<DichVuPhieuBan>();
            using (System.Data.OleDb.OleDbConnection conn = new System.Data.OleDb.OleDbConnection(DataService.m_ConnectString))
            {
                conn.Open();
                System.Data.OleDb.OleDbCommand cmd = new System.Data.OleDb.OleDbCommand(
                    "SELECT MO_TA, THANH_TIEN FROM CHI_TIET_DICH_VU WHERE ID_PHIEU_BAN = ?", conn);
                cmd.Parameters.Add("id", System.Data.OleDb.OleDbType.VarWChar, 50).Value = id;
                using (System.Data.OleDb.OleDbDataReader r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        DichVuPhieuBan dv = new DichVuPhieuBan();
                        dv.Ten = r["MO_TA"] == DBNull.Value ? "" : Convert.ToString(r["MO_TA"]);
                        dv.TienDV = r["THANH_TIEN"] == DBNull.Value ? 0 : Convert.ToInt64(r["THANH_TIEN"]);
                        ds.Add(dv);
                    }
                }
            }
            return ds;
        }

        // ---------- Lưu trữ phí và giảm giá trong phihoadon.xml (không đổi CSDL) ----------

        static XmlNode TimNodePhieu(XmlNode goc, String id)
        {
            foreach (XmlNode n in goc.ChildNodes)
            {
                if (n.Name == "Phieu" && n.Attributes["Id"] != null && n.Attributes["Id"].Value == id)
                    return n;
            }
            return null;
        }

        static void LuuAnToan(XmlDocument doc, String path)
        {
            // Ghi file tạm rồi thay thế để không bị ghi dở khi lỗi giữa chừng
            String tam = path + ".tmp";
            doc.Save(tam);
            if (File.Exists(path))
                File.Replace(tam, path, null);
            else
                File.Move(tam, path);
        }

        static void ThemThuocTinh(XmlDocument doc, XmlElement cha, String ten, String giaTri)
        {
            XmlElement con = doc.CreateElement(ten);
            con.InnerText = giaTri;
            cha.AppendChild(con);
        }

        public static void GhiPhi(PhieuBan phi)
        {
            AccessControl.DemandCurrent("BanHang");
            if (!AccessControl.Can("ThietLapUuDai") && phi.PhanTramGiamTay != 0)
                throw new UnauthorizedAccessException("Nhân viên không được tự nhập mức ưu đãi.");
            GhiPhiKhongKiemTra(phi);
        }

        // Ghi phí không kiểm tra quyền. Chỉ dùng nội bộ, kể cả khi khôi phục dữ liệu cũ.
        static void GhiPhiKhongKiemTra(PhieuBan phi)
        {
            String path = ThamSo.LayDuongDanPhiHoaDon();
            XmlDocument doc = new XmlDocument();
            if (File.Exists(path))
                doc.Load(path);
            else
                doc.LoadXml("<PhiHoaDon />");

            XmlElement goc = doc.DocumentElement;
            XmlNode cu = TimNodePhieu(goc, phi.Id);
            if (cu != null)
                goc.RemoveChild(cu);

            XmlElement el = doc.CreateElement("Phieu");
            el.SetAttribute("Id", phi.Id);
            ThemThuocTinh(doc, el, "PhiVanChuyen", phi.PhiVanChuyen.ToString());
            ThemThuocTinh(doc, el, "PhanTramGiam", phi.PhanTramGiam.ToString(System.Globalization.CultureInfo.InvariantCulture));
            ThemThuocTinh(doc, el, "PhanTramGiamTay", phi.PhanTramGiamTay.ToString(System.Globalization.CultureInfo.InvariantCulture));
            ThemThuocTinh(doc, el, "PhanTramTuDong", phi.PhanTramTuDong.ToString(System.Globalization.CultureInfo.InvariantCulture));
            ThemThuocTinh(doc, el, "TienGiam", phi.TienGiam.ToString());
            // Dịch vụ không ghi XML nữa: lưu trong CHI_TIET_DICH_VU, cùng giao dịch với phiếu
            goc.AppendChild(el);
            LuuAnToan(doc, path);
        }

        // Phí vận chuyển đọc từ CSDL (PHIEU_BAN.TIEN_VAN_CHUYEN). Trả về null nếu CSDL chưa có giá trị.
        static long? DocPhiVanChuyenDb(String id)
        {
            DataService ds = new DataService();
            System.Data.OleDb.OleDbCommand cmd = new System.Data.OleDb.OleDbCommand("SELECT TIEN_VAN_CHUYEN FROM PHIEU_BAN WHERE ID = @id");
            cmd.Parameters.Add("id", System.Data.OleDb.OleDbType.VarChar, 50).Value = id;
            object o = ds.ExecuteScalar(cmd);
            if (o == null || o == DBNull.Value)
                return null;
            return Convert.ToInt64(o);
        }

        private static void DocChiTietUuDaiDb(PhieuBan invoice)
        {
            using(System.Data.OleDb.OleDbConnection c=new System.Data.OleDb.OleDbConnection(DataService.m_ConnectString)) {
                c.Open(); DataTable table=AccountRepository.ReadTable(c,null,"SELECT * FROM PHIEU_BAN WHERE ID=?",invoice.Id);
                if(table.Rows.Count==0 || !table.Columns.Contains("UU_DAI_PHIEN_BAN")) return;
                DataRow row=table.Rows[0]; if(row["UU_DAI_PHIEN_BAN"]==DBNull.Value || Convert.ToInt32(row["UU_DAI_PHIEN_BAN"])<1) return;
                invoice.ChiTietUuDai=new KetQuaChinhSach {
                    GiamGia=row["TIEN_GIAM_RIENG"]==DBNull.Value?0:Convert.ToInt64(row["TIEN_GIAM_RIENG"]),
                    ThuongMai=row["TIEN_CK_THUONG_MAI"]==DBNull.Value?0:Convert.ToInt64(row["TIEN_CK_THUONG_MAI"]),
                    ThanhToan=row["TIEN_CK_THANH_TOAN"]==DBNull.Value?0:Convert.ToInt64(row["TIEN_CK_THANH_TOAN"]),
                    QuyTacDaLuu=Convert.ToString(row["CHINH_SACH_UU_DAI"]) };
                invoice.KenhUuDai=Convert.ToString(row["KENH_UU_DAI"]);
                invoice.PhanTramGiamTay=row["PHAN_TRAM_GIAM_TAY"]==DBNull.Value?0:Convert.ToDouble(row["PHAN_TRAM_GIAM_TAY"]);
                invoice.TienGiam=invoice.ChiTietUuDai.TongGiam;
            }
        }

        public static PhieuBan DocPhi(String id)
        {
            PhieuBan ph = new PhieuBan();
            ph.Id = id;
            long? phiDb = DocPhiVanChuyenDb(id);
            if (phiDb.HasValue)
                ph.PhiVanChuyen = phiDb.Value;
            // Dịch vụ lấy từ CHI_TIET_DICH_VU; phiếu cũ còn dịch vụ trong XML thì được nối thêm ở dưới
            ph.DichVu = LayDichVuDb(id);
            DocChiTietUuDaiDb(ph);
            bool coPhanTramTay = false;
            bool coPhanTramTuDong = false;
            String path = ThamSo.LayDuongDanPhiHoaDon();
            if (!File.Exists(path))
                return ph;

            XmlDocument doc = new XmlDocument();
            doc.Load(path);
            XmlNode node = TimNodePhieu(doc.DocumentElement, id);
            if (node == null)
                return ph;

            foreach (XmlNode con in node.ChildNodes)
            {
                long so;
                if (con.Name == "PhiVanChuyen")
                {
                    // Phiếu cũ chưa có giá trị trong CSDL thì dùng giá trị trong XML
                    if (!phiDb.HasValue)
                        ph.PhiVanChuyen = long.TryParse(con.InnerText, out so) ? so : 0;
                }
                else if (con.Name == "PhanTramGiamTay")
                {
                    double tay;
                    ph.PhanTramGiamTay = double.TryParse(con.InnerText, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out tay) ? tay : 0;
                    coPhanTramTay = true;
                }
                else if (con.Name == "PhanTramTuDong")
                {
                    double tuDong;
                    ph.PhanTramTuDong = double.TryParse(con.InnerText, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out tuDong) ? tuDong : 0;
                    coPhanTramTuDong = true;
                }
                else if (con.Name == "PhanTramGiam")
                {
                    double pct;
                    ph.PhanTramGiam = double.TryParse(con.InnerText, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out pct) ? pct : 0;
                }
                else if (con.Name == "TienGiam")
                {
                    ph.TienGiam = long.TryParse(con.InnerText, out so) ? so : 0;
                }
                else if (con.Name == "DichVu")
                {
                    DichVuPhieuBan dv = new DichVuPhieuBan();
                    dv.Ten = con.Attributes["Ten"] != null ? con.Attributes["Ten"].Value : "";
                    dv.TienDV = (con.Attributes["TienDV"] != null && long.TryParse(con.Attributes["TienDV"].Value, out so)) ? so : 0;
                    ph.DichVu.Add(dv);
                }
            }
            // Phiếu cũ chưa có mức tay trong XML: mức giảm đã lưu chính là mức tay
            if (!coPhanTramTay)
                ph.PhanTramGiamTay = ph.PhanTramGiam;
            // Phiếu lưu trước khi có mục PhanTramTuDong: mức hiệu lực lớn hơn mức tay thì phần chênh lệch là mức tự động
            if (!coPhanTramTuDong)
                ph.PhanTramTuDong = Math.Max(0, ph.PhanTramGiam - ph.PhanTramGiamTay);
            DocChiTietUuDaiDb(ph);
            return ph;
        }

        public static void XoaPhi(String id)
        {
            String path = ThamSo.LayDuongDanPhiHoaDon();
            if (!File.Exists(path))
                return;
            XmlDocument doc = new XmlDocument();
            doc.Load(path);
            XmlNode cu = TimNodePhieu(doc.DocumentElement, id);
            if (cu == null)
                return;
            doc.DocumentElement.RemoveChild(cu);
            LuuAnToan(doc, path);
        }

        public void HienthiPhieuBanLe(BindingNavigator bn, DataGridView dg)
        {
            bs.DataSource = factory.DanhsachPhieuBanLe();
            bn.BindingSource = bs;
            dg.DataSource = bs;
        }

        public void HienthiPhieuBanSi(BindingNavigator bn, DataGridView dg)
        {
            bs.DataSource = factory.DanhsachPhieuBanSi();
            bn.BindingSource = bs;
            dg.DataSource = bs;
        }

        public void HienthiPhieuBan(BindingNavigator bn, ComboBox cmb, TextBox txt, DateTimePicker dt,
            NumericUpDown numTongTien, NumericUpDown numDatra, NumericUpDown numConNo)
        {
            bn.BindingSource = bs;
            // Header fields are copied explicitly: an empty list must not push DBNull into Value bindings.
            foreach (Control control in new Control[] { cmb, txt, dt, numTongTien, numDatra, numConNo })
                control.DataBindings.Clear();
            HienThiThongTinPhieu(cmb, txt, dt, numTongTien, numDatra, numConNo);
        }

        public void HienThiThongTinPhieu(ComboBox cmb, TextBox txt, DateTimePicker dt,
            NumericUpDown numTongTien, NumericUpDown numDatra, NumericUpDown numConNo)
        {
            DataRowView current = bs.Current as DataRowView;
            txt.Text = current == null ? String.Empty : Convert.ToString(current["ID"]);
            cmb.SelectedIndex = -1;
            if (current != null) cmb.SelectedValue = current["ID_KHACH_HANG"];
            dt.Value = current == null || current["NGAY_BAN"] == DBNull.Value ? DateTime.Today : Convert.ToDateTime(current["NGAY_BAN"]);
            DisplayAmount(numTongTien, current, "TONG_TIEN");
            DisplayAmount(numDatra, current, "DA_TRA");
            DisplayAmount(numConNo, current, "CON_NO");
        }

        private static void DisplayAmount(NumericUpDown input, DataRowView row, string column)
        {
            decimal value = row == null || row[column] == DBNull.Value ? 0 : Convert.ToDecimal(row[column]);
            input.Value = Math.Max(input.Minimum, Math.Min(input.Maximum, value));
        }

        public PhieuBan LayPhieuBan(String id)
        {
            DataTable tbl = factory.LayPhieuBan(id);
            PhieuBan ph = null;
            if (tbl.Rows.Count > 0)
            {
                ph = new PhieuBan();
                ph.Id = Convert.ToString(tbl.Rows[0]["ID"]);

                ph.NgayBan = Convert.ToDateTime(tbl.Rows[0]["NGAY_BAN"]);
                ph.TongTien = Convert.ToInt64(tbl.Rows[0]["TONG_TIEN"]);
                ph.DaTra = Convert.ToInt64(tbl.Rows[0]["DA_TRA"]);
                ph.ConNo = Convert.ToInt64(tbl.Rows[0]["CON_NO"]);
                KhachHangController ctrlKH = new KhachHangController();
                ph.KhachHang = ctrlKH.LayKhachHang(Convert.ToString(tbl.Rows[0]["ID_KHACH_HANG"]));
                ChiTietPhieuBanController ctrl = new ChiTietPhieuBanController();
                ph.ChiTiet = ctrl.ChiTietPhieuBan(ph.Id);
                PhieuBan fees = DocPhi(ph.Id);
                ph.PhiVanChuyen = fees.PhiVanChuyen;
                ph.DichVu = fees.DichVu;
                ph.KenhUuDai=fees.KenhUuDai; ph.ChiTietUuDai=fees.ChiTietUuDai;
                ph.TienGiam = tbl.Columns.Contains("TIEN_GIAM_GIA") && tbl.Rows[0]["TIEN_GIAM_GIA"] != DBNull.Value
                    ? Convert.ToInt64(tbl.Rows[0]["TIEN_GIAM_GIA"]) : fees.TienGiam;
                if (tbl.Columns.Contains("TIEN_CHIET_KHAU") && tbl.Rows[0]["TIEN_CHIET_KHAU"] != DBNull.Value)
                    ph.TienGiam = checked(ph.TienGiam + Convert.ToInt64(tbl.Rows[0]["TIEN_CHIET_KHAU"]));
            }
            return ph;
        }

        public void TimPhieuBan(String maKH, DateTime dt)
        {
            factory.TimPhieuBan(maKH, dt);
        }
    }

    // =====================================================================
    // TEMPLATE METHOD (refactoring.guru): khung chung cho mọi thao tác trên
    // phiếu bán: KiemTra -> LuuDuLieu -> HauXuLy. Khung ThucHien() cố định,
    // lớp con chỉ viết các bước của mình.
    // =====================================================================
    public abstract class ThaoTacPhieuBan
    {
        protected readonly PhieuBanController ctrl;

        protected ThaoTacPhieuBan(PhieuBanController ctrl)
        {
            this.ctrl = ctrl;
        }

        // Khung cố định: không cho lớp con ghi đè
        public bool ThucHien()
        {
            if (!KiemTra())
                return false;
            LuuDuLieu();
            HauXuLy();
            return true;
        }

        // Trả về false nếu không được phép thực hiện (đã thông báo lý do)
        protected abstract bool KiemTra();

        // Ném exception nếu lỗi; giao dịch đã được rollback ở tầng dưới
        protected abstract void LuuDuLieu();

        protected virtual void HauXuLy()
        {
        }
    }

    // Lập hóa đơn mới: kiểm tra giới hạn bản dùng thử và trùng mã, ghi phí XML,
    // lưu phiếu + chi tiết + trừ tồn trong một transaction.
    public class LapHoaDonMoi : ThaoTacPhieuBan
    {
        private readonly ChiTietPhieuBanController ctrlChiTiet;
        private readonly PhieuBan phi;

        public LapHoaDonMoi(PhieuBanController ctrl, ChiTietPhieuBanController ctrlChiTiet, PhieuBan phi)
            : base(ctrl)
        {
            this.ctrlChiTiet = ctrlChiTiet;
            this.phi = phi;
        }

        // Mã gợi ý khi trùng mã phiếu; null nếu không trùng. Form dùng mã này để điền lại ô mã phiếu.
        public String MaGoiY { get; private set; }

        protected override bool KiemTra()
        {
            MaGoiY = null;
            // Kiểm tra trùng mã phiếu và gợi ý mã trống kế tiếp.
            if (new PhieuBanController().LayPhieuBan(phi.Id) != null)
            {
                long so;
                long.TryParse(phi.Id, out so);
                PhieuBanController kiem = new PhieuBanController();
                long goiY = Math.Max(so + 1, ThamSo.LayMaPhieuBan());
                while (kiem.LayPhieuBan(goiY.ToString()) != null)
                    goiY++;
                MaGoiY = goiY.ToString();

                MessageBox.Show(
                    "Mã phiếu bán " + phi.Id + " đã tồn tại. Mã gợi ý: " + MaGoiY + ".\nMã phiếu đã được điền lại, hãy bấm Lưu để lưu phiếu với mã này.",
                    "Phiếu bán",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return false;
            }

            return true;
        }

        protected override void LuuDuLieu()
        {
            // Ghi phí trước. Nếu CSDL lỗi thì gỡ mục phí vừa ghi để không còn phí mồ côi.
            PhieuBanController.GhiPhi(phi);
            try
            {
                ctrl.DatTienVanChuyen(phi.Id, phi.PhiVanChuyen);
                ctrl.LuuPhieuVaChiTiet(ctrlChiTiet, phi);
            }
            catch
            {
                try { PhieuBanController.XoaPhi(phi.Id); }
                catch { /* giữ nguyên lỗi gốc của CSDL */ }
                throw;
            }
        }
    }

    // Xóa hóa đơn: không áp giới hạn bản dùng thử. Hoàn tồn, xóa chi tiết, xóa phiếu và mục phí.
    public class XoaHoaDon : ThaoTacPhieuBan
    {
        private readonly String maPhieu;

        public XoaHoaDon(PhieuBanController ctrl, String maPhieu)
            : base(ctrl)
        {
            this.maPhieu = maPhieu;
        }

        protected override bool KiemTra()
        {
            return !String.IsNullOrEmpty(maPhieu);
        }

        protected override void LuuDuLieu()
        {
            ctrl.XoaPhieuBan(maPhieu);
        }
    }
}

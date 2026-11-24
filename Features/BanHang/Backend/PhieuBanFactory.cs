using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Data.OleDb;

namespace CuahangNongduoc.DataLayer
{
    public class PhieuBanFactory
    {
        DataService m_Ds = new DataService();

        public DataTable TimPhieuBan(String idKh, DateTime dt)
        {
            OleDbCommand cmd = new OleDbCommand("SELECT * FROM PHIEU_BAN WHERE NGAY_BAN = @ngay AND ID_KHACH_HANG=@kh");
            cmd.Parameters.Add("ngay", OleDbType.Date).Value = dt;
            cmd.Parameters.Add("kh", OleDbType.VarChar).Value = idKh;

            m_Ds.Load(cmd);

            return m_Ds;
        }

        public DataTable DanhsachPhieuBanLe()
        {
            OleDbCommand cmd = new OleDbCommand("SELECT PB.* FROM PHIEU_BAN PB INNER JOIN KHACH_HANG KH ON PB.ID_KHACH_HANG=KH.ID WHERE KH.LOAI_KH=FALSE");
            m_Ds.Load(cmd);

            return m_Ds;
        }
        public DataTable DanhsachPhieuBanSi()
        {
            OleDbCommand cmd = new OleDbCommand("SELECT PB.* FROM PHIEU_BAN PB INNER JOIN KHACH_HANG KH ON PB.ID_KHACH_HANG=KH.ID WHERE KH.LOAI_KH=TRUE");
            m_Ds.Load(cmd);

            return m_Ds;
        }


        internal DataTable NapPhieuBan(String id)
        {
            OleDbCommand cmd = new OleDbCommand("SELECT * FROM PHIEU_BAN WHERE ID=?");
            cmd.Parameters.Add("id", OleDbType.VarChar, 50).Value = id;
            m_Ds.Load(cmd);
            return m_Ds;
        }

        public DataTable LayPhieuBan(String id)
        {
            OleDbCommand cmd = new OleDbCommand("SELECT * FROM PHIEU_BAN WHERE ID = @id");
            cmd.Parameters.Add("id", OleDbType.VarChar, 50).Value = id;
            DataService receipt = new DataService();
            receipt.Load(cmd);
            return receipt;
        }


        public DataTable LayChiTietPhieuBan(String idPhieuBan)
        {
            OleDbCommand cmd = new OleDbCommand("SELECT * FROM CHI_TIET_PHIEU_BAN WHERE ID_PHIEU_BAN = @id");
            cmd.Parameters.Add("id", OleDbType.VarChar, 50).Value = idPhieuBan;
            m_Ds.Load(cmd);
            return m_Ds;
        }

        public static long LayConNo(String kh, int thang, int nam)
        {
            DataService ds = new DataService();
            OleDbCommand cmd = new OleDbCommand("SELECT SUM(CON_NO) FROM PHIEU_BAN WHERE ID_KHACH_HANG = @kh AND MONTH(NGAY_BAN)=@thang AND YEAR(NGAY_BAN)= @nam");
            cmd.Parameters.Add("kh", OleDbType.VarChar, 50).Value = kh;
            cmd.Parameters.Add("thang", OleDbType.Integer).Value = thang;
            cmd.Parameters.Add("nam", OleDbType.Integer).Value = nam;

            object obj = ds.ExecuteScalar(cmd);
            if (obj == null)
                return 0;
            else
                return Convert.ToInt64(obj);
        }

        public static int LaySoPhieu()
        {
            DataService ds = new DataService();
            OleDbCommand cmd = new OleDbCommand("SELECT COUNT(*) FROM PHIEU_BAN");

            object obj = ds.ExecuteScalar(cmd);
            if (obj == null)
                return 0;
            else
                return Convert.ToInt32(obj);
        }

        public DataRow NewRow()
        {
            return m_Ds.NewRow();
        }
        public void Add(DataRow row)
        {
            m_Ds.Rows.Add(row);
        }
        public bool Save()
        {

            return m_Ds.ExecuteNoneQuery() > 0;
        }

        /// <summary>
        /// Ghi phiếu bán trong transaction do người gọi quản lý.
        /// Gọi ChapNhanThayDoi() sau khi người gọi commit thành công.
        /// </summary>
        public bool CoPhieuMoi()
        {
            return m_Ds.GetChanges(DataRowState.Added) != null;
        }

        // YC-BH-04/05: hàng tặng chỉ được lưu khi khớp chương trình KHUYEN_MAI hiện hành
        // và không vượt số lượng được hưởng theo số lượng mua cùng phiếu.
        internal void KiemTraHangTang(DataTable details)
        {
            if(details==null) return;
            IList<CuahangNongduoc.BusinessObject.UuDaiQuyDinh> rules=
                CuahangNongduoc.Controller.PhieuBanController.DanhSachUuDaiHieuLuc();
            IDictionary<string,string> products=CuahangNongduoc.Controller.PhieuBanController.SanPhamTheoLo();
            Dictionary<string,Dictionary<string,int>> invoices=new Dictionary<string,Dictionary<string,int>>();
            foreach(DataRow line in details.Rows) {
                if(line.RowState==DataRowState.Deleted || line["LA_HANG_TANG"]==DBNull.Value || !Convert.ToBoolean(line["LA_HANG_TANG"])) continue;
                string invoice=Convert.ToString(line["ID_PHIEU_BAN"]),lot=Convert.ToString(line["ID_MA_SAN_PHAM"]),product;
                products.TryGetValue(lot,out product);
                Dictionary<string,int> budgets;
                if(!invoices.TryGetValue(invoice,out budgets)) {
                    string customer="";
                    foreach(DataRow header in m_Ds.Rows) if(header.RowState!=DataRowState.Deleted && Convert.ToString(header["ID"])==invoice) customer=Convert.ToString(header["ID_KHACH_HANG"]);
                    string channel=CuahangNongduoc.Controller.PhieuBanController.KenhKhachHang(customer);
                    List<CuahangNongduoc.BusinessObject.UuDaiQuyDinh> selected=new List<CuahangNongduoc.BusinessObject.UuDaiQuyDinh>();
                    foreach(CuahangNongduoc.BusinessObject.UuDaiQuyDinh rule in rules) if(rule.DungKenh(channel)) selected.Add(rule);
                    budgets=CuahangNongduoc.BusinessObject.ChonKhuyenMai.QuyenTang(selected,details,invoice);
                    invoices[invoice]=budgets;
                }
                int quantity=Convert.ToInt32(line["SO_LUONG"]);
                if(quantity<=0 || CuahangNongduoc.BusinessObject.ChonKhuyenMai.TruHangTang(budgets,lot,product??"",quantity)>0)
                    throw new InvalidOperationException("Sản phẩm tặng hoặc số lượng tặng không đúng ưu đãi đang có hiệu lực (lô "+lot+").");
            }
        }

        internal Dictionary<string, long> KiemTraUuDaiTuDong(DataTable details,
            CuahangNongduoc.BusinessObject.PhieuBan snapshot)
        {
            Dictionary<string, long> allowed = new Dictionary<string, long>(StringComparer.Ordinal);

            IList<CuahangNongduoc.BusinessObject.UuDaiQuyDinh> rules =
                CuahangNongduoc.Controller.PhieuBanController.DanhSachUuDaiHieuLuc();
            foreach (DataRow header in m_Ds.Rows)
            {
                if (header.RowState != DataRowState.Added) continue;
                string id = Convert.ToString(header["ID"]);
                decimal money = 0; int quantity = 0;
                System.Collections.Generic.List<CuahangNongduoc.BusinessObject.DongTinhUuDai> lines =
                    new System.Collections.Generic.List<CuahangNongduoc.BusinessObject.DongTinhUuDai>();
                foreach (DataRow line in details.Rows)
                {
                    if (line.RowState == DataRowState.Deleted || Convert.ToString(line["ID_PHIEU_BAN"]) != id) continue;
                    bool gift = line["LA_HANG_TANG"] != DBNull.Value && Convert.ToBoolean(line["LA_HANG_TANG"]);
                    if (gift) continue;
                    money = checked(money + CuahangNongduoc.BusinessObject.TienTe.ThanhTien(
                        Convert.ToDecimal(line["DON_GIA"]), Convert.ToDecimal(line["SO_LUONG"])));
                    quantity = checked(quantity + Convert.ToInt32(line["SO_LUONG"]));
                    string lot = Convert.ToString(line["ID_MA_SAN_PHAM"]), product = lot;
                    foreach (CuahangNongduoc.BusinessObject.UuDaiQuyDinh rule in rules)
                        if(rule.SanPhamTheoLo != null && rule.SanPhamTheoLo.TryGetValue(lot,out product)) break;
                    lines.Add(new CuahangNongduoc.BusinessObject.DongTinhUuDai { Lo=lot, SanPham=product,
                        Tien=CuahangNongduoc.BusinessObject.TienTe.ThanhTien(Convert.ToDecimal(line["DON_GIA"]),Convert.ToDecimal(line["SO_LUONG"])),
                        SoLuong=Convert.ToInt32(line["SO_LUONG"]) });
                }
                long subtotal = checked((long)Decimal.Round(money, 0, MidpointRounding.AwayFromZero));
                string channel=CuahangNongduoc.Controller.PhieuBanController.KenhKhachHang(Convert.ToString(header["ID_KHACH_HANG"]));
                long paid=Convert.ToInt64(header["DA_TRA"]);
                double manual=snapshot==null?0:snapshot.PhanTramGiamTay;
                if(manual!=0) AccessControl.DemandCurrent("ThietLapUuDai");
                long fees=snapshot==null?0:snapshot.PhiVanChuyen;
                if(snapshot!=null && snapshot.DichVu!=null) foreach(CuahangNongduoc.BusinessObject.DichVuPhieuBan service in snapshot.DichVu) fees=checked(fees+service.TienDV);
                CuahangNongduoc.BusinessObject.KetQuaChinhSach expected=CuahangNongduoc.BusinessObject.ChonUuDai.TinhTheoKenh(subtotal,quantity,rules,lines,channel,paid,fees,manual);
                long total=checked(subtotal-expected.TongGiam+fees);
                if(snapshot==null || snapshot.Id!=id || snapshot.KenhUuDai!=channel || snapshot.ChiTietUuDai==null ||
                    snapshot.ChiTietUuDai.GiamGia!=expected.GiamGia || snapshot.ChiTietUuDai.ThuongMai!=expected.ThuongMai ||
                    snapshot.ChiTietUuDai.ThanhToan!=expected.ThanhToan || snapshot.TienGiam!=expected.TongGiam ||
                    Convert.ToInt64(header["TONG_TIEN"])!=total)
                    throw new InvalidOperationException("Ưu đãi hoặc thanh toán đã thay đổi. Vui lòng kiểm tra và lưu lại phiếu.");
                snapshot.ChiTietUuDai=expected;
                allowed[id]=expected.TongGiam;
            }
            return allowed;
        }

        public bool SaveTrongGiaoDich(OleDbConnection conn, OleDbTransaction tr)
        {
            if (!AccessControl.Can("SuaPhieuBan") && m_Ds.GetChanges(DataRowState.Added) == null)
                throw new UnauthorizedAccessException("Chỉ quản lý được sửa phiếu đã lập.");
            AccessControl.ValidateRows(m_Ds, m_Ds.Command.CommandText);
            foreach (DataRow row in m_Ds.Rows)
            {
                if (row.RowState != DataRowState.Added && row.RowState != DataRowState.Modified) continue;
                string customerId = Convert.ToString(row["ID_KHACH_HANG"]).Trim();
                if (customerId.Length == 0 || Convert.ToInt32(AccountRepository.Scalar(conn, tr,
                    "SELECT COUNT(*) FROM KHACH_HANG WHERE ID=?", customerId)) != 1)
                    throw new InvalidOperationException("Vui long chon khach hang hoac dai ly hop le.");
            }

            OleDbCommand cmdSelect = m_Ds.Command;
            cmdSelect.Connection = conn;
            cmdSelect.Transaction = tr;

            OleDbDataAdapter adapter = new OleDbDataAdapter();
            adapter.SelectCommand = cmdSelect;
            adapter.AcceptChangesDuringUpdate = false;
            OleDbCommandBuilder builder = new OleDbCommandBuilder(adapter);
            adapter.RowUpdating += delegate (object sender, OleDbRowUpdatingEventArgs e)
            {
                // CommandBuilder skips rows whose values match their original values.
                if (e.Command == null) return;
                e.Command.Connection = conn;
                e.Command.Transaction = tr;
            };

            string auditChanges = AccessControl.DescribeChanges(m_Ds);
            int changed = adapter.Update(m_Ds);
            if (changed > 0) AccountRepository.Audit(conn, tr, "LAP_SUA_PHIEU_BAN", "PHIEU_BAN", auditChanges);
            return changed > 0;
        }

        /// <summary>
        /// Xóa phiếu trong transaction do người gọi quản lý. Ném lỗi nếu không xóa đúng một dòng.
        /// </summary>
        public void XoaPhieuTrongGiaoDich(OleDbConnection conn, OleDbTransaction tr, String idPhieu)
        {
            AccessControl.DemandCurrent("XoaPhieu");
            AccountRepository.Audit(conn, tr, "XOA_PHIEU_BAN", idPhieu, "Hoàn tồn và xóa chi tiết cùng giao dịch");
            OleDbCommand cmd = new OleDbCommand("DELETE FROM PHIEU_BAN WHERE ID = @id", conn, tr);
            cmd.Parameters.Add("id", OleDbType.VarChar, 50).Value = idPhieu;
            if (cmd.ExecuteNonQuery() != 1)
                throw new InvalidOperationException("Không xóa được phiếu bán " + idPhieu);
        }

        // Ghi phí vận chuyển vào cột TIEN_VAN_CHUYEN của phiếu đang nằm trong bộ nhớ (trước khi lưu giao dịch).
        // Ném lỗi nếu không tìm thấy phiếu, để không lưu thiếu phí.
        // Ghi tiền giảm (tay + tự động) vào phiếu đang sửa, để báo cáo ưu đãi khớp với tổng đã lưu
        public void DatChiTietUuDai(CuahangNongduoc.BusinessObject.PhieuBan snapshot)
        {
            if(snapshot==null || snapshot.ChiTietUuDai==null) return;
            foreach(DataRow row in m_Ds.Rows) if(row.RowState!=DataRowState.Deleted && Convert.ToString(row["ID"])==snapshot.Id) {
                row["TIEN_GIAM_RIENG"]=snapshot.ChiTietUuDai.GiamGia;
                row["TIEN_CK_THUONG_MAI"]=snapshot.ChiTietUuDai.ThuongMai;
                row["TIEN_CK_THANH_TOAN"]=snapshot.ChiTietUuDai.ThanhToan;
                row["KENH_UU_DAI"]=snapshot.KenhUuDai; row["CHINH_SACH_UU_DAI"]=snapshot.ChiTietUuDai.QuyTacDaLuu;
                row["UU_DAI_PHIEN_BAN"]=1; row["PHAN_TRAM_GIAM_TAY"]=snapshot.PhanTramGiamTay;
                return;
            }
            throw new InvalidOperationException("Không tìm thấy phiếu để lưu chi tiết ưu đãi.");
        }

        public void DatTienGiamGia(String id, long tienGiam)
        {
            foreach (DataRow row in m_Ds.Rows)
            {
                if (row.RowState == DataRowState.Deleted)
                    continue;
                if (String.Equals(Convert.ToString(row["ID"]), id, StringComparison.Ordinal))
                {
                    row["TIEN_GIAM_GIA"] = tienGiam;
                    return;
                }
            }
            throw new InvalidOperationException("Không tìm thấy phiếu " + id + " để ghi tiền giảm.");
        }

        public void DatTienVanChuyen(String id, long phiVanChuyen)
        {
            foreach (DataRow row in m_Ds.Rows)
            {
                if (row.RowState == DataRowState.Deleted)
                    continue;
                if (String.Equals(Convert.ToString(row["ID"]), id, StringComparison.Ordinal))
                {
                    row["TIEN_VAN_CHUYEN"] = phiVanChuyen;
                    return;
                }
            }
            throw new InvalidOperationException("Không tìm thấy phiếu " + id + " để ghi phí vận chuyển.");
        }

        public void HuyThayDoi() { m_Ds.RejectChanges(); }

        public void ChapNhanThayDoi()
        {
            m_Ds.AcceptChanges();
        }

        // Ưu đãi còn hiệu lực trong ngày: TU_NGAY <= ngày và (DEN_NGAY >= ngày hoặc chưa có DEN_NGAY)
        public static DataTable LayUuDaiHieuLuc(DateTime ngay)
        {
            // A fresh reader sees settings saved by the admin connection immediately.
            using (OleDbConnection connection = new OleDbConnection(DataService.m_ConnectString))
            using (OleDbCommand cmd = new OleDbCommand("SELECT * FROM UU_DAI WHERE LOAI <> 'CK_THANH_TOAN' AND TU_NGAY <= ? AND (DEN_NGAY >= ? OR DEN_NGAY IS NULL) ORDER BY TU_NGAY DESC, ID DESC", connection))
            {
                cmd.Parameters.Add("tuNgay", OleDbType.Date).Value = ngay.Date;
                cmd.Parameters.Add("denNgay", OleDbType.Date).Value = ngay.Date;
                using (OleDbDataAdapter adapter = new OleDbDataAdapter(cmd))
                {
                    DataTable table = new DataTable();
                    adapter.Fill(table);
                    return table;
                }
            }
        }

        // Ghi dịch vụ của phiếu vào CHI_TIET_DICH_VU trong cùng giao dịch: xóa dòng cũ rồi ghi lại.
        // Dịch vụ chọn từ danh mục hoặc nhập tự do đều được lưu cùng giao dịch với phiếu.
        public void LuuDichVuTrongGiaoDich(OleDbConnection conn, OleDbTransaction tr, String idPhieu,
            System.Collections.Generic.IList<CuahangNongduoc.BusinessObject.DichVuPhieuBan> dichVu)
        {
            AccessControl.DemandCurrent("BanHang");
            XoaDichVuTrongGiaoDich(conn, tr, idPhieu);
            if (dichVu == null)
                return;
            Dictionary<string, long> charges = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
            foreach (CuahangNongduoc.BusinessObject.DichVuPhieuBan dv in dichVu)
            {
                if (dv == null) throw new InvalidOperationException("Dòng dịch vụ không hợp lệ.");
                String moTa = dv.Ten == null ? "" : dv.Ten.Trim();
                if (moTa.Length == 0 || moTa.Length > 100 || dv.TienDV <= 0)
                    throw new InvalidOperationException("Dịch vụ cần tên (tối đa 100 ký tự) và số tiền lớn hơn 0.");
                long previous;
                charges.TryGetValue(moTa, out previous);
                charges[moTa] = checked(previous + dv.TienDV);
            }
            foreach (KeyValuePair<string, long> charge in charges)
            {
                string moTa = charge.Key;
                object maDichVu = TimMaDichVu(conn, tr, moTa, charge.Value);
                OleDbCommand cmd = new OleDbCommand(
                    "INSERT INTO CHI_TIET_DICH_VU (ID_PHIEU_BAN, ID_DICH_VU, MO_TA, SO_LUONG, DON_GIA, THANH_TIEN) VALUES (?, ?, ?, ?, ?, ?)",
                    conn, tr);
                cmd.Parameters.Add("idPhieu", OleDbType.VarChar, 50).Value = idPhieu;
                cmd.Parameters.AddWithValue("idDichVu", maDichVu);
                cmd.Parameters.Add("moTa", OleDbType.VarWChar, 255).Value = moTa;
                cmd.Parameters.Add("soLuong", OleDbType.Integer).Value = 1;
                cmd.Parameters.Add("donGia", OleDbType.Currency).Value = (decimal)charge.Value;
                cmd.Parameters.Add("thanhTien", OleDbType.Currency).Value = (decimal)charge.Value;
                cmd.ExecuteNonQuery();
            }
        }

        // Xóa toàn bộ dịch vụ của một phiếu trong giao dịch do người gọi quản lý
        public void XoaDichVuTrongGiaoDich(OleDbConnection conn, OleDbTransaction tr, String idPhieu)
        {
            OleDbCommand cmd = new OleDbCommand("DELETE FROM CHI_TIET_DICH_VU WHERE ID_PHIEU_BAN = ?", conn, tr);
            cmd.Parameters.Add("idPhieu", OleDbType.VarChar, 50).Value = idPhieu;
            cmd.ExecuteNonQuery();
        }

        // Tìm mã dịch vụ trong danh mục DICH_VU đang sử dụng theo tên
        static object TimMaDichVu(OleDbConnection conn, OleDbTransaction tr, String tenDichVu, long amount)
        {
            if (tenDichVu.Length == 0)
                throw new InvalidOperationException("Dịch vụ chưa có tên. Hãy nhập tên dịch vụ hoặc xóa dòng đó.");
            OleDbCommand cmd = new OleDbCommand("SELECT ID FROM DICH_VU WHERE TEN_DICH_VU = ? AND DANG_SU_DUNG = TRUE", conn, tr);
            cmd.Parameters.Add("ten", OleDbType.VarWChar, 255).Value = tenDichVu;
            object ma = cmd.ExecuteScalar();
            if (ma != null && ma != DBNull.Value) return ma;
            CuahangNongduoc.DataLayer.DichVuFactory.DemandManage();
            // Ad hoc services satisfy the existing foreign key without becoming selectable catalog items.
            // Allocate the shared service counter in this same transaction; rollback also removes the ad hoc item.
            string id;
            do
            {
                if (AccountRepository.Execute(conn, tr,
                    "UPDATE BO_DEM_MA SET GIA_TRI=GIA_TRI+1 WHERE TEN_BANG='DICH_VU'") != 1)
                    throw new InvalidOperationException("Chưa khởi tạo bộ đếm dịch vụ. Hãy khởi động lại ứng dụng.");
                id = Convert.ToString(AccountRepository.Scalar(conn, tr,
                    "SELECT GIA_TRI FROM BO_DEM_MA WHERE TEN_BANG='DICH_VU'"), System.Globalization.CultureInfo.InvariantCulture);
            } while (Convert.ToInt32(AccountRepository.Scalar(conn, tr, "SELECT COUNT(*) FROM DICH_VU WHERE ID=?", id)) != 0);
            AccountRepository.Execute(conn, tr,
                "INSERT INTO DICH_VU (ID,TEN_DICH_VU,DON_GIA,DANG_SU_DUNG) VALUES (?,?,?,False)", id, tenDichVu, (decimal)amount);
            return id;
        }
    }
}

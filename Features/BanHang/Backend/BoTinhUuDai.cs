using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;

namespace CuahangNongduoc.BusinessObject
{
    public class BoTinhUuDai
    {
        private IChienLuocUuDai chienLuoc;

        public BoTinhUuDai(IChienLuocUuDai chienLuoc)
        {
            if (chienLuoc == null)
                throw new ArgumentNullException("chienLuoc");

            this.chienLuoc = chienLuoc;
        }

        public void ThayDoiChienLuoc(IChienLuocUuDai chienLuocMoi)
        {
            if (chienLuocMoi == null)
                throw new ArgumentNullException("chienLuocMoi");

            this.chienLuoc = chienLuocMoi;
        }

        public decimal TinhUuDai(decimal soTienGoc, decimal giaTri)
        {
            return chienLuoc.TinhUuDai(soTienGoc, giaTri);
        }
    }

    // Quy tắc ưu đãi đọc từ bảng UU_DAI.
    // PHAM_VI = HOA_DON: toàn cửa hàng; SAN_PHAM: MA_SAN_PHAM chứa ID của bảng SAN_PHAM.
    // KHUYEN_MAI: mua X sản phẩm áp dụng, tặng Y sản phẩm TANG_SP; cấu hình cũ giữ cách tặng cùng lô.
    // DIEU_KIEN dạng "KHOA=GIA_TRI;KHOA=GIA_TRI", ví dụ "TOI_THIEU_TIEN=500000;TOI_THIEU_SL=10".
    public class UuDaiQuyDinh
    {
        public const String LoaiGiamPhanTram = "GIAM_PHAN_TRAM";
        public const String LoaiChietKhau = "CHIET_KHAU";
        public const String LoaiChietKhauThanhToan = "CK_THANH_TOAN";
        public string Id = "", Kenh = "TAT_CA";
        public bool DungKenh(string channel) { return Kenh == "TAT_CA" || Kenh == channel; }
        public const String LoaiKhuyenMai = "KHUYEN_MAI";
        public const String PhamViHoaDon = "HOA_DON";
        public const String PhamViSanPham = "SAN_PHAM";
        public IDictionary<string, string> SanPhamTheoLo;

        public bool ApDungChoLo(string lo)
        {
            if (PhamVi == PhamViSanPham) {
                string product;
                return SanPhamTheoLo != null && SanPhamTheoLo.TryGetValue(lo, out product) &&
                    String.Equals(product, MaSanPham, StringComparison.OrdinalIgnoreCase);
            }
            return PhamVi == PhamViHoaDon && (MaSanPham.Length == 0 ||
                String.Equals(MaSanPham, lo, StringComparison.OrdinalIgnoreCase));
        }

        public String Loai = "";
        public String PhamVi = "";
        public decimal GiaTri;
        public decimal TienToiThieu;
        public int SoLuongToiThieu;
        public int MuaSl;
        public int TangSl;
        public string SanPhamTang = "";
        public String MaSanPham = ""; // ID sản phẩm khi SAN_PHAM; ID lô cho cấu hình mua tặng cũ, trống = toàn cửa hàng.

        public static UuDaiQuyDinh TuDataRow(DataRow row)
        {
            UuDaiQuyDinh ud = new UuDaiQuyDinh();
            ud.Id = DocChuoiCot(row,"ID");
            ud.Loai = DocChuoiCot(row, "LOAI").Trim().ToUpperInvariant();
            ud.PhamVi = DocChuoiCot(row, "PHAM_VI").Trim().ToUpperInvariant();
            ud.GiaTri = DocSoCot(row, "GIA_TRI");
            ud.MaSanPham = DocChuoiCot(row, "MA_SAN_PHAM").Trim();

            Dictionary<String, String> dieuKien = PhanTichDieuKien(DocChuoiCot(row, "DIEU_KIEN"));
            if(ud.Loai==LoaiChietKhau || ud.Loai==LoaiChietKhauThanhToan) ud.Kenh="BAN_SI";
            string channel; if(dieuKien.TryGetValue("KENH",out channel)) ud.Kenh=channel;
            ud.TienToiThieu = LaySo(dieuKien, "TOI_THIEU_TIEN");
            ud.SoLuongToiThieu = (int)LaySo(dieuKien, "TOI_THIEU_SL");
            ud.MuaSl = (int)LaySo(dieuKien, "MUA_SL");
            ud.TangSl = (int)LaySo(dieuKien, "TANG_SL");
            string giftProduct;
            if(dieuKien.TryGetValue("TANG_SP",out giftProduct)) ud.SanPhamTang=Uri.UnescapeDataString(giftProduct);
            return ud;
        }

        private static String DocChuoiCot(DataRow row, String ten)
        {
            if (!row.Table.Columns.Contains(ten))
                return "";
            object o = row[ten];
            return o == null || o == DBNull.Value ? "" : Convert.ToString(o);
        }

        private static decimal DocSoCot(DataRow row, String ten)
        {
            if (!row.Table.Columns.Contains(ten))
                return 0m;
            object o = row[ten];
            return o == null || o == DBNull.Value ? 0m : Convert.ToDecimal(o, CultureInfo.InvariantCulture);
        }

        private static Dictionary<String, String> PhanTichDieuKien(String dieuKien)
        {
            Dictionary<String, String> d = new Dictionary<String, String>(StringComparer.OrdinalIgnoreCase);
            if (dieuKien == null || dieuKien.Trim().Length == 0)
                return d;
            foreach (String cap in dieuKien.Split(';'))
            {
                int dau = cap.IndexOf('=');
                if (dau <= 0)
                    continue;
                d[cap.Substring(0, dau).Trim()] = cap.Substring(dau + 1).Trim();
            }
            return d;
        }

        private static decimal LaySo(Dictionary<String, String> d, String khoa)
        {
            String s;
            decimal v;
            if (d.TryGetValue(khoa, out s) &&
                decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out v))
                return v;
            return 0m;
        }
    }

    // Mua tặng (KHUYEN_MAI): mua MUA_SL tặng TANG_SL cùng mặt hàng.
    public static class ChonKhuyenMai
    {
        // Quy tắc đầu tiên áp được cho mặt hàng ma. Null nếu không có.
        public static UuDaiQuyDinh TimQuyTac(IEnumerable<UuDaiQuyDinh> dsUuDai, String ma)
        {
            if (dsUuDai == null)
                return null;
            foreach (UuDaiQuyDinh ud in dsUuDai)
            {
                if (ud.Loai != UuDaiQuyDinh.LoaiKhuyenMai || ud.MuaSl <= 0 || ud.TangSl <= 0)
                    continue;
                if (!ud.ApDungChoLo(ma))
                    continue;
                return ud;
            }
            return null;
        }

        // Gift budgets are shared across all qualifying paid lines on this invoice.
        public static Dictionary<string,int> QuyenTang(IList<UuDaiQuyDinh> rules, DataTable details, string invoice)
        {
            Dictionary<string,int> result=new Dictionary<string,int>(StringComparer.OrdinalIgnoreCase);
            Dictionary<UuDaiQuyDinh,int> purchased=new Dictionary<UuDaiQuyDinh,int>();
            Dictionary<string,int> legacy=new Dictionary<string,int>(StringComparer.OrdinalIgnoreCase);
            foreach(DataRow line in details.Rows) {
                if(line.RowState==DataRowState.Deleted || Convert.ToString(line["ID_PHIEU_BAN"])!=invoice ||
                    (line["LA_HANG_TANG"]!=DBNull.Value && Convert.ToBoolean(line["LA_HANG_TANG"]))) continue;
                string lot=Convert.ToString(line["ID_MA_SAN_PHAM"]);
                UuDaiQuyDinh rule=TimQuyTac(rules,lot);
                if(rule==null) continue;
                int quantity=Convert.ToInt32(line["SO_LUONG"]), previous;
                if(rule.SanPhamTang.Length>0) {
                    purchased.TryGetValue(rule,out previous); purchased[rule]=checked(previous+quantity);
                } else {
                    legacy.TryGetValue(lot,out previous); legacy[lot]=checked(previous+quantity);
                }
            }
            foreach(KeyValuePair<UuDaiQuyDinh,int> item in purchased) {
                string key="SP:"+item.Key.SanPhamTang; int previous;
                result.TryGetValue(key,out previous);
                result[key]=checked(previous+SoHangTangCanThem(item.Key,item.Value,0));
            }
            foreach(KeyValuePair<string,int> item in legacy)
                result["LO:"+item.Key]=SoHangTangCanThem(TimQuyTac(rules,item.Key),item.Value,0);
            return result;
        }

        public static int TruHangTang(Dictionary<string,int> budgets, string lot, string product, int quantity)
        {
            foreach(string key in new string[] { "LO:"+lot,"SP:"+product }) {
                int allowed;
                if(!budgets.TryGetValue(key,out allowed) || allowed<=0) continue;
                int take=Math.Min(allowed,quantity); budgets[key]=allowed-take; quantity-=take;
                if(quantity==0) break;
            }
            return quantity;
        }

        // Số hàng tặng còn thiếu so với số đã mua (đã tặng rồi thì không tặng thêm)
        public static int SoHangTangCanThem(UuDaiQuyDinh quyTac, int daMua, int daTang)
        {
            int can = checked((daMua / quyTac.MuaSl) * quyTac.TangSl);
            return can > daTang ? can - daTang : 0;
        }
    }

    // Chọn ưu đãi tự động cho hóa đơn. Đây là quy tắc nghiệp vụ, không phụ thuộc giao diện.
    public sealed class DongTinhUuDai
    {
        public string Lo, SanPham;
        public decimal Tien;
        public int SoLuong;
    }

    public sealed class KetQuaChinhSach
    {
        public long GiamGia, ThuongMai, ThanhToan;
        public string ChinhSach="", QuyTacDaLuu="";
        public long TongGiam { get { return checked(GiamGia+ThuongMai+ThanhToan); } }
    }

    public static class ChonUuDai
    {
        public static KetQuaChinhSach TinhTheoKenh(long total,int quantity,IEnumerable<UuDaiQuyDinh> rules,
            IList<DongTinhUuDai> lines,string channel,long paid,long fees,double manual)
        {
            KetQuaChinhSach result=new KetQuaChinhSach();
            if(total<=0) return result;
            List<DongTinhUuDai> working=new List<DongTinhUuDai>();
            if(lines!=null) working.AddRange(lines);
            if(working.Count==0) working.Add(new DongTinhUuDai { Lo="",SanPham="",Tien=total,SoLuong=quantity });
            List<UuDaiQuyDinh> applicable=new List<UuDaiQuyDinh>();
            if(rules!=null) foreach(UuDaiQuyDinh rule in rules) if(rule.DungKenh(channel) && rule.Loai!=UuDaiQuyDinh.LoaiChietKhauThanhToan) applicable.Add(rule);
            decimal reductions=0,trade=0;
            List<string> policies=new List<string>();
            foreach(DongTinhUuDai line in working) {
                decimal productMoney=0; int productQuantity=0;
                foreach(DongTinhUuDai other in working) if(String.Equals(line.SanPham,other.SanPham,StringComparison.OrdinalIgnoreCase)) {
                    productMoney+=other.Tien; productQuantity=checked(productQuantity+other.SoLuong);
                }
                UuDaiQuyDinh discount=null,commercial=null;
                foreach(UuDaiQuyDinh rule in applicable) {
                    bool global=rule.PhamVi==UuDaiQuyDinh.PhamViHoaDon && rule.MaSanPham.Length==0;
                    if(!global && !(rule.PhamVi==UuDaiQuyDinh.PhamViSanPham && rule.ApDungChoLo(line.Lo))) continue;
                    if((global?total:productMoney)<rule.TienToiThieu || (global?quantity:productQuantity)<rule.SoLuongToiThieu) continue;
                    if(rule.Loai==UuDaiQuyDinh.LoaiGiamPhanTram && (discount==null || rule.GiaTri>discount.GiaTri)) discount=rule;
                    if(rule.Loai==UuDaiQuyDinh.LoaiChietKhau && (commercial==null || rule.GiaTri>commercial.GiaTri)) commercial=rule;
                }
                decimal discountRate=Math.Min(100m,Math.Max((decimal)manual,discount==null?0:discount.GiaTri));
                decimal tradeRate=Math.Min(100m,commercial==null?0:commercial.GiaTri);
                decimal winner=Math.Max(discountRate,tradeRate);
                decimal baseReduction=line.Tien*winner/100m;
                if(tradeRate>discountRate) { trade+=baseReduction; AddPolicy(policies,commercial); }
                else { reductions+=baseReduction; if(discount!=null && discount.GiaTri>=(decimal)manual) AddPolicy(policies,discount); }
            }
            result.GiamGia=Money(reductions); result.ThuongMai=Math.Min(total-result.GiamGia,Money(trade));
            if(manual>0) policies.Add("GIAM_TAY/"+manual.ToString(CultureInfo.InvariantCulture)+"%");
            result.ChinhSach=String.Join("; ",policies.ToArray());
            result.QuyTacDaLuu=LuuQuyTac(applicable,result.ChinhSach);
            return result;
        }
        public static string LuuQuyTac(IEnumerable<UuDaiQuyDinh> rules,string applied)
        {
            System.Xml.XmlDocument doc=new System.Xml.XmlDocument();
            System.Xml.XmlElement root=doc.CreateElement("ChinhSach"); root.SetAttribute("ApDung",applied); doc.AppendChild(root);
            foreach(UuDaiQuyDinh rule in rules) {
                System.Xml.XmlElement node=doc.CreateElement("QuyTac"); root.AppendChild(node);
                node.SetAttribute("ID",rule.Id); node.SetAttribute("LOAI",rule.Loai); node.SetAttribute("PHAM_VI",rule.PhamVi);
                node.SetAttribute("MA_SAN_PHAM",rule.MaSanPham); node.SetAttribute("GIA_TRI",rule.GiaTri.ToString(CultureInfo.InvariantCulture));
                node.SetAttribute("DIEU_KIEN","KENH="+rule.Kenh+";TOI_THIEU_TIEN="+rule.TienToiThieu.ToString(CultureInfo.InvariantCulture)+
                    ";TOI_THIEU_SL="+rule.SoLuongToiThieu+";MUA_SL="+rule.MuaSl+";TANG_SL="+rule.TangSl+";TANG_SP="+Uri.EscapeDataString(rule.SanPhamTang));
            }
            return doc.OuterXml;
        }
        public static IList<UuDaiQuyDinh> DocQuyTac(string xml,IDictionary<string,string> lots)
        {
            System.Xml.XmlDocument doc=new System.Xml.XmlDocument(); doc.XmlResolver=null; doc.LoadXml(xml);
            DataTable table=new DataTable();
            foreach(string name in new string[] { "ID","LOAI","PHAM_VI","MA_SAN_PHAM","GIA_TRI","DIEU_KIEN" }) table.Columns.Add(name);
            List<UuDaiQuyDinh> rules=new List<UuDaiQuyDinh>();
            foreach(System.Xml.XmlElement node in doc.DocumentElement.SelectNodes("QuyTac")) {
                DataRow row=table.NewRow(); foreach(DataColumn col in table.Columns) row[col]=node.GetAttribute(col.ColumnName);
                UuDaiQuyDinh rule=UuDaiQuyDinh.TuDataRow(row); rule.SanPhamTheoLo=lots; rules.Add(rule);
            }
            return rules;
        }
        private static long Money(decimal value) { return checked((long)Decimal.Round(value,0,MidpointRounding.AwayFromZero)); }
        private static void AddPolicy(List<string> policies,UuDaiQuyDinh rule)
        {
            if(rule==null) return;
            string text=rule.Id+"/"+rule.Loai+"/"+rule.GiaTri.ToString(CultureInfo.InvariantCulture)+"%/"+rule.Kenh+"/"+
                rule.PhamVi+"/"+rule.MaSanPham+"/MIN="+rule.TienToiThieu.ToString(CultureInfo.InvariantCulture)+","+rule.SoLuongToiThieu;
            if(!policies.Contains(text)) policies.Add(text);
        }

        // Tính tiền giảm đúng phạm vi, quy đổi về tỷ lệ hóa đơn để lưu cùng phiếu.
        public static double PhanTramTheoSanPham(long total, int quantity,
            IEnumerable<UuDaiQuyDinh> rules, IList<DongTinhUuDai> lines)
        {
            if (total <= 0 || rules == null) return 0;
            if (lines == null || lines.Count == 0) return PhanTramGiamHoaDon(total, quantity, rules);
            decimal reduction = 0;
            foreach (DongTinhUuDai line in lines) {
                decimal productMoney = 0; int productQuantity = 0;
                // Cộng các lô cùng sản phẩm khi xét điều kiện giảm giá và chiết khấu.
                foreach (DongTinhUuDai other in lines) {
                    if (String.Equals(other.SanPham, line.SanPham, StringComparison.OrdinalIgnoreCase)) {
                        productMoney += other.Tien; productQuantity = checked(productQuantity + other.SoLuong);
                    }
                }
                decimal discount = 0, commission = 0;
                foreach (UuDaiQuyDinh rule in rules) {
                    bool global = rule.PhamVi == UuDaiQuyDinh.PhamViHoaDon && rule.MaSanPham.Length == 0;
                    bool product = rule.PhamVi == UuDaiQuyDinh.PhamViSanPham && rule.ApDungChoLo(line.Lo);
                    if (!global && !product) continue;
                    if ((global ? total : productMoney) < rule.TienToiThieu ||
                        (global ? quantity : productQuantity) < rule.SoLuongToiThieu) continue;
                    if (rule.Loai == UuDaiQuyDinh.LoaiGiamPhanTram) discount = Math.Max(discount, rule.GiaTri);
                    if (rule.Loai == UuDaiQuyDinh.LoaiChietKhau) commission = Math.Max(commission, rule.GiaTri);
                }
                reduction += line.Tien * Math.Min(100m, Math.Max(0m, discount + commission)) / 100m;
            }
            return (double)(reduction * 100m / total);
        }

        // Tương thích lời gọi cũ chỉ có tổng tiền và số lượng hóa đơn.
        public static double PhanTramGiamHoaDon(long tienHang, int tongSoLuong, IEnumerable<UuDaiQuyDinh> dsUuDai)
        {
            if (dsUuDai == null)
                return 0;
            double cao = 0;
            double chietKhau = 0;
            foreach (UuDaiQuyDinh ud in dsUuDai)
            {
                if ((ud.Loai != UuDaiQuyDinh.LoaiGiamPhanTram && ud.Loai != UuDaiQuyDinh.LoaiChietKhau) || ud.PhamVi != UuDaiQuyDinh.PhamViHoaDon)
                    continue;
                if (tienHang < ud.TienToiThieu || tongSoLuong < ud.SoLuongToiThieu)
                    continue;
                double phanTram = (double)ud.GiaTri;
                if (ud.Loai == UuDaiQuyDinh.LoaiChietKhau)
                    chietKhau = Math.Max(chietKhau, phanTram);
                else if (phanTram > cao)
                    cao = phanTram;
            }
            return Math.Min(100, cao + chietKhau);
        }
    }
}

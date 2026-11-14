
using System;
using System.Collections.Generic;

namespace CuahangNongduoc
{
    // Đối tượng lưu kết quả một chỉ tiêu báo cáo
    public sealed class KetQuaChiTieu
    {
        private string m_Ten;
        private decimal? m_SoTien;

        public string Ten
        {
            get { return m_Ten; }
            set { m_Ten = value; }
        }

        public decimal? SoTien
        {
            get { return m_SoTien; }
            set { m_SoTien = value; }
        }
    }

    // Service xử lý dữ liệu báo cáo
    public sealed class BaoCaoTongHopService
    {
        public IList<KetQuaChiTieu> LayThongKe(
            DateTime tuNgay,
            DateTime denNgay)
        {
            if (tuNgay.Date > denNgay.Date)
            {
                throw new ArgumentException(
                    "Từ ngày không được lớn hơn đến ngày.");
            }

            // Chưa có dữ liệu các khoản phí từ CSDL.
            // Trả về danh sách rỗng, không tạo số liệu giả.
            return new List<KetQuaChiTieu>();
        }
    }
}

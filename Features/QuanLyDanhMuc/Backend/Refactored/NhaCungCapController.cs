using System.Data;
using System.Windows.Forms;
using CuahangNongduoc.DataLayer;

namespace CuahangNongduoc.Controller
{
    public class NhaCungCapController : BaseController
    {
        private readonly NhaCungCapFactory factory = new NhaCungCapFactory();

        protected override BaseFactory Factory => factory;

        public void TimHoTen(string ten)
        {
            factory.TimHoTen(ten);
        }

        public void TimDiaChi(string diachi)
        {
            factory.TimDiaChi(diachi);
        }

        public override DataRow NewRow()
        {
            return factory.NewRow();
        }
    }
}
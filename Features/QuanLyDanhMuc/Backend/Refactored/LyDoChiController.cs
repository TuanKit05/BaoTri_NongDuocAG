using System.Data;
using System.Windows.Forms;
using CuahangNongduoc.DataLayer;

namespace CuahangNongduoc.Controller
{
    public class LyDoChiController : BaseController
    {
        private readonly LyDoChiFactory factory = new LyDoChiFactory();

        protected override BaseFactory Factory => factory;

        public override DataRow NewRow()
        {
            return factory.NewRow();
        }
    }
}
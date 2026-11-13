using System;
using System.Data;
using System.Windows.Forms;
using CuahangNongduoc.DataLayer;

namespace CuahangNongduoc.Controller
{
    public abstract class BaseController
    {
        /// <label>Hook property: Mỗi controller con bắt buộc cung cấp Factory tương ứng</label>
        protected abstract BaseFactory Factory { get; }

        public virtual DataRow NewRow()
        {
            return Factory.NewRow();
        }

        public virtual void Add(DataRow row)
        {
            Factory.Add(row);
        }

        public virtual bool Save()
        {
            return Factory.Save();
        }

        public virtual void HienthiDataGridview(DataGridView dg, BindingNavigator bn)
        {
            BindingSource bs = new BindingSource();
            DataTable tbl = Factory.DanhSach();
            bs.DataSource = tbl;
            bn.BindingSource = bs;
            dg.DataSource = bs;
        }
    }
}
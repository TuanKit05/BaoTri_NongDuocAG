namespace CuahangNongduoc.Features.BaoCao.Frontend
{
    partial class frmTonKhoTaiNgay
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.Label lblNgay;
        private System.Windows.Forms.DateTimePicker dtNgay;
        private System.Windows.Forms.Button btnXem;
        private System.Windows.Forms.Button btnDong;
        private System.Windows.Forms.DataGridView dgvTonKho;
        private System.Windows.Forms.Label lblTongKet;
        private System.Windows.Forms.Label lblCanhBao;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.lblNgay = new System.Windows.Forms.Label();
            this.dtNgay = new System.Windows.Forms.DateTimePicker();
            this.btnXem = new System.Windows.Forms.Button();
            this.btnDong = new System.Windows.Forms.Button();
            this.dgvTonKho = new System.Windows.Forms.DataGridView();
            this.lblTongKet = new System.Windows.Forms.Label();
            this.lblCanhBao = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.dgvTonKho)).BeginInit();
            this.SuspendLayout();

            // lblNgay
            this.lblNgay.AutoSize = true;
            this.lblNgay.Location = new System.Drawing.Point(12, 15);
            this.lblNgay.Name = "lblNgay";
            this.lblNgay.Text = "Ngày:";

            // dtNgay
            this.dtNgay.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dtNgay.Location = new System.Drawing.Point(60, 12);
            this.dtNgay.Name = "dtNgay";
            this.dtNgay.Size = new System.Drawing.Size(120, 20);

            // btnXem
            this.btnXem.Location = new System.Drawing.Point(195, 10);
            this.btnXem.Name = "btnXem";
            this.btnXem.Size = new System.Drawing.Size(80, 26);
            this.btnXem.Text = "Xem";
            this.btnXem.UseVisualStyleBackColor = true;

            // btnDong
            this.btnDong.Location = new System.Drawing.Point(285, 10);
            this.btnDong.Name = "btnDong";
            this.btnDong.Size = new System.Drawing.Size(80, 26);
            this.btnDong.Text = "Đóng";
            this.btnDong.UseVisualStyleBackColor = true;

            // lblTongKet
            this.lblTongKet.AutoSize = true;
            this.lblTongKet.Location = new System.Drawing.Point(12, 48);
            this.lblTongKet.Name = "lblTongKet";

            // lblCanhBao
            this.lblCanhBao.AutoSize = true;
            this.lblCanhBao.ForeColor = System.Drawing.Color.DarkRed;
            this.lblCanhBao.Location = new System.Drawing.Point(12, 68);
            this.lblCanhBao.Name = "lblCanhBao";

            // dgvTonKho
            this.dgvTonKho.AllowUserToAddRows = false;
            this.dgvTonKho.AllowUserToDeleteRows = false;
            this.dgvTonKho.ReadOnly = true;
            this.dgvTonKho.Location = new System.Drawing.Point(12, 92);
            this.dgvTonKho.Name = "dgvTonKho";
            this.dgvTonKho.Size = new System.Drawing.Size(760, 360);
            this.dgvTonKho.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));

            // frmTonKhoTaiNgay
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(784, 464);
            this.Controls.Add(this.lblCanhBao);
            this.Controls.Add(this.lblTongKet);
            this.Controls.Add(this.dgvTonKho);
            this.Controls.Add(this.btnDong);
            this.Controls.Add(this.btnXem);
            this.Controls.Add(this.dtNgay);
            this.Controls.Add(this.lblNgay);
            this.Name = "frmTonKhoTaiNgay";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Tồn kho tại ngày";
            ((System.ComponentModel.ISupportInitialize)(this.dgvTonKho)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}
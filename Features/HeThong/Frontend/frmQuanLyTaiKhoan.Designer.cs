namespace CuahangNongduoc
{
    partial class frmQuanLyTaiKhoan
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.DataGridView grid;
        private System.Windows.Forms.Label lblUser;
        private System.Windows.Forms.Label lblName;
        private System.Windows.Forms.Label lblRole;
        private System.Windows.Forms.Label lblPassword;
        private System.Windows.Forms.Label lblNote;
        private System.Windows.Forms.TextBox txtUsername;
        private System.Windows.Forms.TextBox txtFullName;
        private System.Windows.Forms.TextBox txtPassword;
        private System.Windows.Forms.ComboBox cboRole;
        private System.Windows.Forms.Button btnCreate;
        private System.Windows.Forms.Button btnRole;
        private System.Windows.Forms.Button btnToggle;
        private System.Windows.Forms.Button btnReset;
        private System.Windows.Forms.Button btnRefresh;
        private System.Windows.Forms.Button btnClose;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.grid = new System.Windows.Forms.DataGridView();
            this.lblUser = new System.Windows.Forms.Label();
            this.lblName = new System.Windows.Forms.Label();
            this.lblRole = new System.Windows.Forms.Label();
            this.lblPassword = new System.Windows.Forms.Label();
            this.lblNote = new System.Windows.Forms.Label();
            this.txtUsername = new System.Windows.Forms.TextBox();
            this.txtFullName = new System.Windows.Forms.TextBox();
            this.txtPassword = new System.Windows.Forms.TextBox();
            this.cboRole = new System.Windows.Forms.ComboBox();
            this.btnCreate = new System.Windows.Forms.Button();
            this.btnRole = new System.Windows.Forms.Button();
            this.btnToggle = new System.Windows.Forms.Button();
            this.btnReset = new System.Windows.Forms.Button();
            this.btnRefresh = new System.Windows.Forms.Button();
            this.btnClose = new System.Windows.Forms.Button();
            this.btnDelete = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.grid)).BeginInit();
            this.SuspendLayout();
            // 
            // grid
            // 
            this.grid.AllowUserToAddRows = false;
            this.grid.AllowUserToDeleteRows = false;
            this.grid.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.grid.ColumnHeadersHeight = 29;
            this.grid.Location = new System.Drawing.Point(10, 10);
            this.grid.MultiSelect = false;
            this.grid.Name = "grid";
            this.grid.ReadOnly = true;
            this.grid.RowHeadersWidth = 51;
            this.grid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.grid.Size = new System.Drawing.Size(780, 265);
            this.grid.TabIndex = 0;
            this.grid.SelectionChanged += new System.EventHandler(this.grid_SelectionChanged);
            // 
            // lblUser
            // 
            this.lblUser.Location = new System.Drawing.Point(15, 292);
            this.lblUser.Name = "lblUser";
            this.lblUser.Size = new System.Drawing.Size(115, 25);
            this.lblUser.TabIndex = 1;
            this.lblUser.Text = "Tên đăng nhập:";
            // 
            // lblName
            // 
            this.lblName.Location = new System.Drawing.Point(15, 327);
            this.lblName.Name = "lblName";
            this.lblName.Size = new System.Drawing.Size(115, 25);
            this.lblName.TabIndex = 2;
            this.lblName.Text = "Họ tên:";
            // 
            // lblRole
            // 
            this.lblRole.Location = new System.Drawing.Point(395, 292);
            this.lblRole.Name = "lblRole";
            this.lblRole.Size = new System.Drawing.Size(85, 25);
            this.lblRole.TabIndex = 3;
            this.lblRole.Text = "Vai trò:";
            // 
            // lblPassword
            // 
            this.lblPassword.Location = new System.Drawing.Point(395, 327);
            this.lblPassword.Name = "lblPassword";
            this.lblPassword.Size = new System.Drawing.Size(90, 25);
            this.lblPassword.TabIndex = 4;
            this.lblPassword.Text = "Mật khẩu tạm:";
            // 
            // lblNote
            // 
            this.lblNote.ForeColor = System.Drawing.Color.DarkSlateGray;
            this.lblNote.Location = new System.Drawing.Point(15, 425);
            this.lblNote.Name = "lblNote";
            this.lblNote.Size = new System.Drawing.Size(540, 28);
            this.lblNote.TabIndex = 5;
            this.lblNote.Text = "Tài khoản mới và mật khẩu đặt lại sẽ phải đổi mật khẩu khi đăng nhập lần tiếp the" +
    "o.";
            // 
            // txtUsername
            // 
            this.txtUsername.Location = new System.Drawing.Point(130, 290);
            this.txtUsername.Name = "txtUsername";
            this.txtUsername.Size = new System.Drawing.Size(220, 22);
            this.txtUsername.TabIndex = 6;
            // 
            // txtFullName
            // 
            this.txtFullName.Location = new System.Drawing.Point(130, 325);
            this.txtFullName.Name = "txtFullName";
            this.txtFullName.Size = new System.Drawing.Size(220, 22);
            this.txtFullName.TabIndex = 7;
            // 
            // txtPassword
            // 
            this.txtPassword.Location = new System.Drawing.Point(485, 325);
            this.txtPassword.Name = "txtPassword";
            this.txtPassword.Size = new System.Drawing.Size(285, 22);
            this.txtPassword.TabIndex = 8;
            this.txtPassword.UseSystemPasswordChar = true;
            // 
            // cboRole
            // 
            this.cboRole.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboRole.Location = new System.Drawing.Point(485, 290);
            this.cboRole.Name = "cboRole";
            this.cboRole.Size = new System.Drawing.Size(285, 24);
            this.cboRole.TabIndex = 9;
            // 
            // btnCreate
            // 
            this.btnCreate.Location = new System.Drawing.Point(15, 375);
            this.btnCreate.Name = "btnCreate";
            this.btnCreate.Size = new System.Drawing.Size(130, 32);
            this.btnCreate.TabIndex = 10;
            this.btnCreate.Text = "Tạo tài khoản";
            this.btnCreate.UseVisualStyleBackColor = true;
            this.btnCreate.Click += new System.EventHandler(this.btnCreate_Click);
            // 
            // btnRole
            // 
            this.btnRole.Location = new System.Drawing.Point(155, 375);
            this.btnRole.Name = "btnRole";
            this.btnRole.Size = new System.Drawing.Size(115, 32);
            this.btnRole.TabIndex = 11;
            this.btnRole.Text = "Lưu vai trò";
            this.btnRole.UseVisualStyleBackColor = true;
            this.btnRole.Click += new System.EventHandler(this.btnRole_Click);
            // 
            // btnToggle
            // 
            this.btnToggle.Location = new System.Drawing.Point(280, 375);
            this.btnToggle.Name = "btnToggle";
            this.btnToggle.Size = new System.Drawing.Size(130, 32);
            this.btnToggle.TabIndex = 12;
            this.btnToggle.Text = "Khóa / Mở khóa";
            this.btnToggle.UseVisualStyleBackColor = true;
            this.btnToggle.Click += new System.EventHandler(this.btnToggle_Click);
            // 
            // btnReset
            // 
            this.btnReset.Location = new System.Drawing.Point(420, 375);
            this.btnReset.Name = "btnReset";
            this.btnReset.Size = new System.Drawing.Size(135, 32);
            this.btnReset.TabIndex = 13;
            this.btnReset.Text = "Đặt lại mật khẩu";
            this.btnReset.UseVisualStyleBackColor = true;
            this.btnReset.Click += new System.EventHandler(this.btnReset_Click);
            // 
            // btnRefresh
            // 
            this.btnRefresh.Location = new System.Drawing.Point(565, 375);
            this.btnRefresh.Name = "btnRefresh";
            this.btnRefresh.Size = new System.Drawing.Size(100, 32);
            this.btnRefresh.TabIndex = 14;
            this.btnRefresh.Text = "Làm mới";
            this.btnRefresh.UseVisualStyleBackColor = true;
            this.btnRefresh.Click += new System.EventHandler(this.btnRefresh_Click);
            // 
            // btnClose
            // 
            this.btnClose.Location = new System.Drawing.Point(695, 417);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(95, 32);
            this.btnClose.TabIndex = 15;
            this.btnClose.Text = "Đóng";
            this.btnClose.UseVisualStyleBackColor = true;
            this.btnClose.Click += new System.EventHandler(this.btnClose_Click);
            // 
            // btnDelete
            // 
            this.btnDelete.Location = new System.Drawing.Point(671, 375);
            this.btnDelete.Name = "btnDelete";
            this.btnDelete.Size = new System.Drawing.Size(119, 32);
            this.btnDelete.TabIndex = 16;
            this.btnDelete.Text = "Xóa tài khoản";
            this.btnDelete.UseVisualStyleBackColor = true;
            this.btnDelete.Visible = false;
            this.btnDelete.Click += new System.EventHandler(this.btnDelete_Click);
            // 
            // frmQuanLyTaiKhoan
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 470);
            this.Controls.Add(this.btnDelete);
            this.Controls.Add(this.btnClose);
            this.Controls.Add(this.btnRefresh);
            this.Controls.Add(this.btnReset);
            this.Controls.Add(this.btnToggle);
            this.Controls.Add(this.btnRole);
            this.Controls.Add(this.btnCreate);
            this.Controls.Add(this.lblNote);
            this.Controls.Add(this.txtPassword);
            this.Controls.Add(this.lblPassword);
            this.Controls.Add(this.cboRole);
            this.Controls.Add(this.lblRole);
            this.Controls.Add(this.txtFullName);
            this.Controls.Add(this.lblName);
            this.Controls.Add(this.txtUsername);
            this.Controls.Add(this.lblUser);
            this.Controls.Add(this.grid);
            this.MinimumSize = new System.Drawing.Size(800, 510);
            this.Name = "frmQuanLyTaiKhoan";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "QUẢN LÝ TÀI KHOẢN";
            this.Load += new System.EventHandler(this.frmQuanLyTaiKhoan_Load);
            ((System.ComponentModel.ISupportInitialize)(this.grid)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        private System.Windows.Forms.Button btnDelete;
    }
}

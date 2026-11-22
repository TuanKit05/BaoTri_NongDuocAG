# Bổ sung cơ sở dữ liệu Access theo yêu cầu và mã nguồn thực tế

## Phạm vi đối chiếu

Nguồn yêu cầu: Thu thập yêu cầu.docx, mục 5 và 6. Mã nguồn đối chiếu: nhánh main tại commit 256370e cùng các bổ sung kho hiện có trong dự án. Không dùng cơ sở dữ liệu của nhánh bán hàng để ghi đè toàn bộ dữ liệu hiện tại.

CSDL đã cập nhật trực tiếp: `D:\NongDuoc_BaoTri\Database\cuahang.dll`. Đây là tệp Access Jet có đuôi .dll. Bản .mdb trong gói giao có cùng nội dung để dễ mở bằng Microsoft Access.

## Các thay đổi đã thực hiện

- Thêm UU_DAI và BIEN_DONG_KHO. Giữ các bảng tài khoản, vai trò, quyền và nhật ký hiện có.
- Bổ sung CAU_HINH_KHO.PHUONG_PHAP_GIA, NGUONG_HET_HAN; giữ cấu hình cũ, chỉ điền mặc định BQGQ và 60 ngày nếu trường còn NULL.
- Bổ sung MA_SAN_PHAM.SO_LUONG_NHAP. Mã lô MA_SAN_PHAM.ID vẫn là mã nhập tay. Các lô cũ chưa ghi số lượng nhập ban đầu được để NULL.
- Thêm GIA_VON, PHUONG_PHAP_GIA, GIAM_GIA, LA_HANG_TANG vào chi tiết bán; giữ GIA_VON_DON_VI và THANH_TIEN_GIA_VON đã có để tương thích hai cách đọc giá vốn.
- Đổi khóa chính chi tiết bán thành ID_PHIEU_BAN + ID_MA_SAN_PHAM + LA_HANG_TANG, cho phép cùng một lô có dòng mua và dòng tặng.
- Thêm DU_NO_KH.DA_CHOT và các cột mô tả ưu đãi còn thiếu trên PHIEU_BAN.
- Đổi các cột tiền Long Integer/Double được xác định theo tên nghiệp vụ sang Currency. Giữ nguyên kiểu của số lượng, mã, tỷ lệ phần trăm và các bộ đếm.
- Thêm các quan hệ còn thiếu: phiếu bán–khách hàng, phiếu nhập–nhà cung cấp, sản phẩm–đơn vị tính, phiếu chi–lý do chi, người thiết lập ưu đãi–tài khoản và biến động kho–lô.
- Thêm chỉ mục phục vụ truy vấn hạn dùng, biến động theo lô/ngày, phiếu theo nhân viên/ngày và dịch vụ theo phiếu.

## Quy đổi tên theo code thực tế

| Nội dung yêu cầu | Tên dùng thực tế | Quyết định |
|---|---|---|
| Nhân viên lập phiếu | TAI_KHOAN và PHIEU_BAN.ID_NHAN_VIEN | Giữ quan hệ hiện có, không tạo NHAN_VIEN trùng danh tính |
| PHI_VAN_CHUYEN | PHIEU_BAN.TIEN_VAN_CHUYEN | Giữ tên code đang dùng |
| DICH_VU_PHIEU_BAN | CHI_TIET_DICH_VU và DICH_VU | Bổ sung trên bảng hiện có, không tạo thêm bảng lưu trùng |
| Tiền giảm hóa đơn | TIEN_GIAM_GIA, TIEN_CHIET_KHAU | Giữ cột đang được form và báo cáo dùng |
| Giá vốn dòng | GIA_VON cùng các cột tương thích | Không tính lại giá vốn phiếu cũ từ giá nhập hiện tại |

## Đối chiếu bảo toàn dữ liệu

| Bảng | Trước | Sau |
|---|---:|---:|
| CHI_TIET_PHIEU_BAN | 11 | 11 |
| DU_NO_KH | 3 | 3 |
| KHACH_HANG | 10 | 10 |
| MA_SAN_PHAM | 20 | 20 |
| NHA_CUNG_CAP | 5 | 5 |
| PHIEU_BAN | 8 | 8 |
| PHIEU_CHI | 2 | 2 |
| PHIEU_NHAP | 17 | 17 |
| PHIEU_THANH_TOAN | 2 | 2 |
| SAN_PHAM | 8 | 8 |

Mã hiện có, tổng tiền/công nợ và tổng số lượng đã được đối chiếu bằng script; không xóa phiếu, lô, khách hàng hoặc tạo dữ liệu giao dịch mẫu trong CSDL thật.

## Kiểm tra đã chạy

- Build dự án thành công.
- Kiểm thử thêm/sửa/xóa bán sỉ và bán lẻ, chênh lệch tồn, hoàn tác thanh toán, nhập hàng và điều kiện xóa phiếu.
- Kiểm thử giá vốn đã lưu, phân quyền ẩn giá và số lượng nhập ban đầu.
- Ghi và đọc lại số tiền 3.000.000.000 bằng Currency trên bản sao rồi rollback.
- Kiểm tra khóa ngoại từ chối khách hàng không tồn tại và kiểm tra khóa chính ba cột.
- Chạy lại migration trên bản sao đã cập nhật thành công; không thêm trùng cấu trúc hoặc thay đổi số liệu.

## Lưu ý về dữ liệu lịch sử và chức năng chưa có

Không tự điền số lượng nhập ban đầu hoặc giá vốn lịch sử chưa được ghi nhận. Không tạo giả lịch sử BIEN_DONG_KHO từ số tồn hiện tại. Báo cáo tồn kho tại ngày trong quá khứ cần lịch sử nhập/xuất/điều chỉnh đầy đủ hoặc số dư đầu kỳ được xác nhận.

Cấu trúc bảng không tự hoàn thành các chức năng ưu đãi, khóa kỳ hoặc sao lưu tự động hằng ngày. Các chức năng chưa có trong main vẫn cần code ghi/đọc tương ứng. Script này tạo bản sao lưu trước mỗi lần nâng cấp, không phải tác vụ sao lưu hằng ngày.

Không bỏ bảng DAI_LY hay đổi tên cột đang được code tham chiếu chỉ vì tài liệu gợi ý dọn dẹp. Không hợp nhất bản ghi giao dịch khác nhau giữa các branch.

## Sao lưu và cách sử dụng

Bản trước cập nhật: `D:\NongDuoc_BaoTri\Database\Backups\cuahang_truoc_bo_sung_20261122_202326_498.mdb`.

Trong gói ZIP: `Database/cuahang.dll` dùng cho ứng dụng; `cuahang_da_bo_sung.mdb` để mở trong Access. Hai file là cùng một ảnh chụp dữ liệu, không cấu hình ứng dụng ghi đồng thời vào cả hai.

Mã hiện tại đã được bổ sung mặc định LA_HANG_TANG=False cho dòng mua mới và điều kiện khóa ba cột khi kiểm tra dòng cũ. Bản patch tương thích đi kèm chỉ áp dụng cho mã main đã đối chiếu; nếu dùng branch khác, cần đối chiếu trước khi áp dụng.

Để nâng cấp CSDL khác, đóng ứng dụng và Access, chạy script bằng PowerShell 32-bit:

```powershell
& "C:\Windows\SysWOW64\WindowsPowerShell\v1.0\powershell.exe" -NoProfile -ExecutionPolicy Bypass -File ".\Bo_sung_Access_theo_yeu_cau.ps1" -DatabasePath "D:\NongDuoc_BaoTri\Database\cuahang.dll"
```

Script làm việc trên bản sao, kiểm tra số liệu, kiểm tra tệp gốc không bị thay đổi trong thời gian nâng cấp rồi mới thay thế. Nếu có bản ghi mồ côi cản tạo quan hệ, script ghi rõ vào log và không xóa dữ liệu để ép tạo khóa.

## Danh sách lệnh đã thực thi

```sql
ALTER TABLE [CAU_HINH_KHO] ADD COLUMN [PHUONG_PHAP_GIA] TEXT(20)
ALTER TABLE [CAU_HINH_KHO] ADD COLUMN [NGUONG_HET_HAN] LONG
UPDATE CAU_HINH_KHO SET PHUONG_PHAP_GIA='BQGQ' WHERE PHUONG_PHAP_GIA IS NULL
UPDATE CAU_HINH_KHO SET NGUONG_HET_HAN=60 WHERE NGUONG_HET_HAN IS NULL
ALTER TABLE [MA_SAN_PHAM] ADD COLUMN [SO_LUONG_NHAP] LONG
ALTER TABLE [CHI_TIET_PHIEU_BAN] ADD COLUMN [GIA_VON] CURRENCY
ALTER TABLE [CHI_TIET_PHIEU_BAN] ADD COLUMN [PHUONG_PHAP_GIA] TEXT(20)
ALTER TABLE [CHI_TIET_PHIEU_BAN] ADD COLUMN [GIAM_GIA] CURRENCY
ALTER TABLE [CHI_TIET_PHIEU_BAN] ADD COLUMN [LA_HANG_TANG] YESNO
UPDATE CHI_TIET_PHIEU_BAN SET LA_HANG_TANG=False WHERE LA_HANG_TANG IS NULL
ALTER TABLE [DU_NO_KH] ADD COLUMN [DA_CHOT] YESNO
UPDATE DU_NO_KH SET DA_CHOT=False WHERE DA_CHOT IS NULL
ALTER TABLE [PHIEU_BAN] ADD COLUMN [ID_UU_DAI] TEXT(50)
ALTER TABLE [PHIEU_BAN] ADD COLUMN [GIAM_GIA_LOAI] TEXT(20)
ALTER TABLE [PHIEU_BAN] ADD COLUMN [GIAM_GIA_GIA_TRI] CURRENCY
CREATE TABLE [UU_DAI] ([ID] TEXT(50) PRIMARY KEY,[LOAI] TEXT(20),[PHAM_VI] TEXT(20),[MA_SAN_PHAM] TEXT(50),[DIEU_KIEN] MEMO,[GIA_TRI] DOUBLE,[TU_NGAY] DATETIME,[DEN_NGAY] DATETIME,[NGUOI_THIET_LAP] TEXT(50))
CREATE TABLE [BIEN_DONG_KHO] ([ID] COUNTER PRIMARY KEY,[NGAY] DATETIME,[MA_LO] TEXT(50),[LOAI] TEXT(20),[SO_LUONG] LONG,[MA_PHIEU] TEXT(50),[ID_TAI_KHOAN] TEXT(50))
ALTER TABLE [CHI_TIET_DICH_VU] ALTER COLUMN [DON_GIA] CURRENCY
ALTER TABLE [CHI_TIET_DICH_VU] ALTER COLUMN [THANH_TIEN] CURRENCY
ALTER TABLE [CHI_TIET_PHIEU_BAN] ALTER COLUMN [DON_GIA] CURRENCY
ALTER TABLE [CHI_TIET_PHIEU_BAN] ALTER COLUMN [GIA_VON_DON_VI] CURRENCY
ALTER TABLE [CHI_TIET_PHIEU_BAN] ALTER COLUMN [THANH_TIEN] CURRENCY
ALTER TABLE [CHI_TIET_PHIEU_BAN] ALTER COLUMN [THANH_TIEN_GIA_VON] CURRENCY
ALTER TABLE [DICH_VU] ALTER COLUMN [DON_GIA] CURRENCY
ALTER TABLE [DU_NO_KH] ALTER COLUMN [CUOI_KY] CURRENCY
ALTER TABLE [DU_NO_KH] ALTER COLUMN [DA_TRA] CURRENCY
ALTER TABLE [DU_NO_KH] ALTER COLUMN [DAU_KY] CURRENCY
ALTER TABLE [DU_NO_KH] ALTER COLUMN [PHAT_SINH] CURRENCY
ALTER TABLE [MA_SAN_PHAM] ALTER COLUMN [DON_GIA_NHAP] CURRENCY
ALTER TABLE [PHIEU_BAN] ALTER COLUMN [CON_NO] CURRENCY
ALTER TABLE [PHIEU_BAN] ALTER COLUMN [DA_TRA] CURRENCY
ALTER TABLE [PHIEU_BAN] ALTER COLUMN [TIEN_CHIET_KHAU] CURRENCY
ALTER TABLE [PHIEU_BAN] ALTER COLUMN [TIEN_GIAM_GIA] CURRENCY
ALTER TABLE [PHIEU_BAN] ALTER COLUMN [TIEN_KHUYEN_MAI] CURRENCY
ALTER TABLE [PHIEU_BAN] ALTER COLUMN [TIEN_VAN_CHUYEN] CURRENCY
ALTER TABLE [PHIEU_BAN] ALTER COLUMN [TONG_TIEN] CURRENCY
ALTER TABLE [PHIEU_CHI] ALTER COLUMN [TONG_TIEN] CURRENCY
ALTER TABLE [PHIEU_NHAP] ALTER COLUMN [CON_NO] CURRENCY
ALTER TABLE [PHIEU_NHAP] ALTER COLUMN [DA_TRA] CURRENCY
ALTER TABLE [PHIEU_NHAP] ALTER COLUMN [TONG_TIEN] CURRENCY
ALTER TABLE [PHIEU_THANH_TOAN] ALTER COLUMN [TONG_TIEN] CURRENCY
ALTER TABLE [SAN_PHAM] ALTER COLUMN [DON_GIA_NHAP] CURRENCY
ALTER TABLE [SAN_PHAM] ALTER COLUMN [GIA_BAN_LE] CURRENCY
ALTER TABLE [SAN_PHAM] ALTER COLUMN [GIA_BAN_SI] CURRENCY
ALTER TABLE CHI_TIET_PHIEU_BAN DROP CONSTRAINT [PrimaryKey]
ALTER TABLE CHI_TIET_PHIEU_BAN ADD CONSTRAINT PK_CHI_TIET_BAN PRIMARY KEY (ID_PHIEU_BAN,ID_MA_SAN_PHAM,LA_HANG_TANG)
ALTER TABLE [PHIEU_BAN] ADD CONSTRAINT [FK_PHIEU_BAN_ID_KHACH_HANG] FOREIGN KEY ([ID_KHACH_HANG]) REFERENCES [KHACH_HANG] ([ID])
ALTER TABLE [PHIEU_NHAP] ADD CONSTRAINT [FK_PHIEU_NHAP_ID_NHA_CUNG_CAP] FOREIGN KEY ([ID_NHA_CUNG_CAP]) REFERENCES [NHA_CUNG_CAP] ([ID])
ALTER TABLE [SAN_PHAM] ADD CONSTRAINT [FK_SAN_PHAM_ID_DON_VI_TINH] FOREIGN KEY ([ID_DON_VI_TINH]) REFERENCES [DON_VI_TINH] ([ID])
ALTER TABLE [PHIEU_CHI] ADD CONSTRAINT [FK_PHIEU_CHI_ID_LY_DO_CHI] FOREIGN KEY ([ID_LY_DO_CHI]) REFERENCES [LY_DO_CHI] ([ID])
ALTER TABLE [UU_DAI] ADD CONSTRAINT [FK_UU_DAI_NGUOI_THIET_LAP] FOREIGN KEY ([NGUOI_THIET_LAP]) REFERENCES [TAI_KHOAN] ([ID])
ALTER TABLE [BIEN_DONG_KHO] ADD CONSTRAINT [FK_BIEN_DONG_KHO_MA_LO] FOREIGN KEY ([MA_LO]) REFERENCES [MA_SAN_PHAM] ([ID])
CREATE INDEX [IX_LO_HAN_DUNG] ON [MA_SAN_PHAM] (NGAY_HET_HAN,SO_LUONG)
CREATE INDEX [IX_BIEN_DONG_LO_NGAY] ON [BIEN_DONG_KHO] (MA_LO,NGAY)
CREATE INDEX [IX_BAN_NV_NGAY] ON [PHIEU_BAN] (ID_NHAN_VIEN,NGAY_BAN)
CREATE INDEX [IX_DICH_VU_PHIEU] ON [CHI_TIET_DICH_VU] (ID_PHIEU_BAN)
```

SHA256 CSDL sau cập nhật: `94c2fc500ff5a0f1c1683442f65b1af5f9730464870bcde3438009a760bcafca`.
# Bổ sung quy tắc một tài khoản – một vai trò

Theo điều chỉnh của người dùng, mỗi tài khoản chỉ được gán một trong bốn vai trò ADMIN, KHO, BANHANG, KETOAN. Chỉ mục duy nhất `UX_TAI_KHOAN_MOT_VAI_TRO` trên `TAI_KHOAN_VAI_TRO.ID_TAI_KHOAN` chặn vai trò thứ hai ngay tại Access. Hệ thống kiểm tra khi tạo/sửa tài khoản và từ chối cấp quyền cho chuỗi nhiều vai trò. Migration dừng nếu phát hiện tài khoản có nhiều vai trò; không tự xóa hoặc tự chọn thay người dùng.


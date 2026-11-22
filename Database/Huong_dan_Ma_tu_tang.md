# Mã tự tăng trong NongDuoc_BaoTri

## Thay đổi đã thực hiện

Mã bản ghi mới được cấp tự động; ngoại lệ MA_SAN_PHAM.ID là số lô thực tế do người dùng nhập tay. Không cho sửa số lô của bản ghi đã lưu trên giao diện. Mã đã tồn tại được giữ nguyên để các phiếu, tồn kho, công nợ và khóa ngoại tiếp tục khớp.

| Đối tượng | Bảng | Cách cấp mã mới |
|---|---|---|
| Sản phẩm | SAN_PHAM | Bộ đếm riêng, mã Text chứa số tăng dần |
| Khách hàng và đại lý | KHACH_HANG | Dùng chung một bộ đếm |
| Nhà cung cấp | NHA_CUNG_CAP | Bộ đếm riêng |
| Lô hàng | MA_SAN_PHAM | Nhập tay số lô thực tế; bắt buộc, không trùng |
| Phiếu bán sỉ và bán lẻ | PHIEU_BAN | Dùng chung một bộ đếm |
| Phiếu nhập | PHIEU_NHAP | Bộ đếm riêng |
| Phiếu chi | PHIEU_CHI | Bộ đếm riêng |
| Phiếu thu/thanh toán | PHIEU_THANH_TOAN | Bộ đếm riêng |
| Tài khoản | TAI_KHOAN | TK + số tăng dần, ví dụ TK000001 |
| Đơn vị tính | DON_VI_TINH | AutoNumber hiện có của Access |
| Lý do chi | LY_DO_CHI | AutoNumber hiện có của Access |
| Nhật ký | NHAT_KY | COUNTER hiện có của Access |

Các mã vai trò ADMIN, KHO, BANHANG, KETOAN và mã quyền là hằng nghiệp vụ, không đổi thành số. Các khóa ngoại nhận mã của bản ghi được tham chiếu, không có bộ đếm riêng. Các bảng chi tiết giữ khóa ghép hiện tại.

## Cơ sở dữ liệu cần bổ sung cho bản hiện tại

Đây là cơ chế cấp số tăng dần ở ứng dụng, không phải chuyển các ID Text thành AutoNumber.

Trước khi chạy phiên bản mới, đóng các cửa sổ đang sử dụng database và sao lưu `Database/cuahang.dll`. Đây là tệp Access mặc dù mang đuôi .dll. Nếu cần mở bằng Access, làm trên một bản sao mang đuôi .mdb; không ghi đè bản gốc khi chưa kiểm tra.

Khi khởi động, `AccountRepository.EnsureSchema()` gọi `IdGenerator.EnsureSchema()` để tự bổ sung:

| Bảng BO_DEM_MA | Kiểu trong Access | Ràng buộc |
|---|---|---|
| TEN_BANG | Short Text (50) | Khóa chính |
| GIA_TRI | Number / Long Integer | Required; lưu số cuối đã cấp |

Nếu muốn tạo bảng thủ công trong SQL View, chạy một lần:

```sql
CREATE TABLE BO_DEM_MA (
    TEN_BANG TEXT(50) NOT NULL PRIMARY KEY,
    GIA_TRI LONG NOT NULL
);
```

Không chạy CREATE TABLE nếu bảng đã tồn tại. Ứng dụng tự tạo các dòng bộ đếm cho 8 bảng dùng mã Text và đồng bộ số bắt đầu với mã số lớn nhất hiện có. Không cần tự nhập giá trị khởi tạo. Các mã cũ không phải số vẫn được giữ nguyên. Với tài khoản, mã TK theo sau bằng chữ số cũng được xét khi đồng bộ.

Cấp số được thực hiện trong một giao dịch riêng, cập nhật bộ đếm có điều kiện và kiểm tra mã đã tồn tại trước khi trả kết quả. Số đã cấp không được tái sử dụng khi hủy hoặc xóa bản ghi. Vì thế số tăng dần nhưng có thể có khoảng trống; đây không phải đánh số liên tục tuyệt đối.

Không chỉnh hoặc giảm GIA_TRI bằng tay. Không tiếp tục dùng các cột đếm cũ trong THAM_SO để sinh mã ở một ứng dụng khác. Các phương thức đọc/ghi THAM_SO cũ còn được giữ để tương thích nhưng các luồng tạo bản ghi đã chuyển sang bộ đếm mới.

## Nếu muốn chuyển hẳn tất cả khóa chính sang AutoNumber của Access

Đây là một đợt chuyển đổi dữ liệu và mã nguồn riêng. Không chỉ đổi kiểu dữ liệu của ID đang dùng.

1. Sao lưu và chuyển đổi trên bản sao; thống kê quan hệ, bản ghi mồ côi và mã trùng.
2. Thêm khóa kỹ thuật mới (ví dụ ID_MOI) kiểu AutoNumber / Long Integer cho từng bảng cha. Giữ ID cũ làm MA_CU hoặc mã nghiệp vụ có chỉ mục Unique.
3. Lập ánh xạ ID cũ → ID_MOI cho từng bảng.
4. Thêm khóa ngoại mới kiểu Number / Long Integer vào bảng con; cập nhật bằng JOIN với bảng ánh xạ. Khóa ngoại không được đặt AutoNumber.
5. Đối chiếu số bản ghi, tổng tiền, công nợ, số lượng từng lô và các liên kết tài khoản. Chỉ bật Enforce Referential Integrity khi không còn bản ghi mồ côi.
6. Sửa câu INSERT để bỏ cột AutoNumber. Sau mỗi INSERT, đọc SELECT @@IDENTITY bằng đúng connection và transaction đó, trước khi INSERT nhật ký hoặc bản ghi khác có AutoNumber. Dùng ID trả về để ghi bảng con.
7. Sửa model, tham số OleDb, binding ComboBox và báo cáo tương ứng; chuyển các khóa ngoại từ chuỗi sang Integer. Chuyển cả liên kết mã phiếu trong phihoadon.xml.
8. Chỉ thay cơ sở dữ liệu đang dùng sau khi kiểm thử và đối chiếu đạt; giữ bản sao trước chuyển đổi để có thể phục hồi.

Đối với bảng chi tiết, có thể giữ khóa ghép (mã phiếu, mã lô). Nếu thêm một ID AutoNumber cho dòng chi tiết, vẫn cần ràng buộc Unique thích hợp để không vô tình cho phép trùng dòng nghiệp vụ.

Tài liệu Microsoft:
- https://support.microsoft.com/en-us/access/create-edit-or-delete-a-relationship
- https://learn.microsoft.com/en-us/dotnet/framework/data/adonet/retrieving-identity-or-autonumber-values
- https://support.microsoft.com/en-us/access/design-considerations-for-updating-data

## Ngoại lệ số lô sản phẩm

MA_SAN_PHAM.ID giữ kiểu Short Text (50), là khóa chính và không chuyển sang AutoNumber. Người dùng nhập số lô khi thêm lô trong phiếu nhập; hệ thống kiểm tra không rỗng và không trùng trong phiếu hoặc trong database. Các bảng tham chiếu ID_MA_SAN_PHAM tiếp tục giữ cùng kiểu Text và cùng kích thước. Dòng BO_DEM_MA có TEN_BANG=MA_SAN_PHAM từ phiên bản trước, nếu có, không còn được sử dụng; có thể giữ lại mà không ảnh hưởng hoạt động.

# Báo cáo Refactoring cho chức năng Thu – Chi

**Hệ thống:** Cửa hàng Nông dược An Giang (dự án `CuahangNongduoc.csproj`).  
**Phạm vi:** `Features/ThuChi/Backend` và `Features/ThuChi/Frontend`; cập nhật `CuahangNongduoc.csproj` để khai báo file mới.  
**Nguồn kỹ thuật:** https://refactoring.guru/refactoring/catalog  
**Chú ý:** đây là **Refactoring Techniques**, KHÔNG dùng tên hay đếm **Design Patterns** (GoF).

## Kết quả và nguyên tắc

- Refactoring.Guru có **66 kỹ thuật** trong **6 nhóm**. Đã áp dụng **22 kỹ thuật** tại nơi có mã nguồn phù hợp; **44 kỹ thuật không áp dụng**, có lý do tương ứng bên dưới.
- Các kỹ thuật đối nghịch như Extract/Inline Class, Pull Up/Push Down, Replace Inheritance with Delegation/Replace Delegation with Inheritance không nên cùng áp lên một cấu trúc chỉ để đủ con số.
- Giữ chữ ký public của các controller cho việc thêm, xóa, lưu, tìm kiếm, in; riêng `Tonghop` đổi thành `TongHop` và vẫn giữ wrapper tên cũ có `[Obsolete]`.
- Các `.Designer.cs`, `.resx`, các câu SQL SELECT/DELETE của ba Factory và các tên tham số RDLC vẫn giữ nguyên. Không đổi lược đồ cơ sở dữ liệu.
- Nguồn nhắm đến **.NET Framework 2.0**, không thêm cú pháp hiện đại.

## 22 kỹ thuật đã áp dụng — vị trí chính xác

| STT | Tên kỹ thuật Refactoring.Guru | Vị trí trong `Features/ThuChi/` | Trước → Sau / tác dụng |
|---:|---|---|---|
| 1 | **Extract Method** | `Backend/PhieuChiController.cs:27`, `Backend/PhieuThanhToanController.cs:26`, `Backend/DuNoKhachHangController.cs:46`, `Backend/DuNoKhachHangController.cs:58` | Tách khối chuyển dòng dữ liệu, tính công nợ, ghi công nợ thành các phương thức riêng. |
| 2 | **Extract Variable** | `Frontend/frmPhieuChi.cs:100`, `Frontend/frmThanhToan.cs:99` | Đặt tên cho mã lý do, mã khách hàng và ngày lọc trước khi truyền vào thao tác tìm kiếm. |
| 3 | **Inline Temp** | `Backend/DuNoKhachHangFactory.cs:15` | Bỏ biến OleDbCommand chỉ dùng một lần trong LoadSchema. |
| 4 | **Replace Temp with Query** | `Backend/DuNoKhachHang.cs:53`, `Backend/DuNoKhachHangController.cs:53` | Thay biến tính số dư cuối kỳ bằng TinhCuoiKy() của đối tượng công nợ. |
| 5 | **Substitute Algorithm** | `Frontend/frmDunoKhachhang.cs:44` | Thay giải thuật kiểm tra năm bằng TryParse + so sánh khoảng, không dựa vào exception thông thường. |
| 6 | **Move Method** | `Backend/ThuChiBinding.cs:35`, `Backend/PhieuChiController.cs:42`, `Backend/PhieuThanhToanController.cs:41` | Chuyển logic binding khỏi PhieuChiController/PhieuThanhToanController sang ThuChiBinding. |
| 7 | **Extract Class** | `Backend/ThuChiBinding.cs:33`, `Backend/ThuChiScalarQuery.cs:7`, `Frontend/ThuChiReportParameters.cs:8` | Tách ThuChiBinding, ThuChiReportParameters, ThuChiScalarQuery để gom trách nhiệm. |
| 8 | **Replace Magic Number with Symbolic Constant** | `Frontend/frmDunoKhachhang.cs:16`, `Frontend/frmDunoKhachhang.cs:17` | Đưa ngưỡng năm 2000 và 9999 thành hằng NamNhoNhat/NamLonNhat. |
| 9 | **Decompose Conditional** | `Frontend/frmDunoKhachhang.cs:41`, `Frontend/frmDunoKhachhang.cs:50` | Tách kiểm tra hợp lệ năm thành hàm NamHopLe(string). |
| 10 | **Consolidate Conditional Expression** | `Frontend/frmDunoKhachhang.cs:44` | Kết hợp parse + hai điều kiện miền giá trị trong biểu thức bool rõ nghĩa. |
| 11 | **Remove Control Flag** | `Frontend/frmDunoKhachhang.cs:41` | Bỏ bool ok và nhánh try/catch điều khiển khi kiểm tra năm. |
| 12 | **Replace Nested Conditional with Guard Clauses** | `Backend/PhieuChiController.cs:20`, `Backend/PhieuThanhToanController.cs:20` | Trả về null sớm khi không tìm thấy phiếu trước khi ánh xạ dòng dữ liệu. |
| 13 | **Rename Method** | `Backend/DuNoKhachHangController.cs:14`, `Frontend/frmDunoKhachhang.cs:61` | Đổi Tonghop thành TongHop cho nhất quán, giữ wrapper Tonghop [Obsolete] để tương thích. |
| 14 | **Separate Query from Modifier** | `Backend/DuNoKhachHangController.cs:46`, `Backend/DuNoKhachHangController.cs:58` | Tách TinhCongNo (đọc + tính) khỏi GhiDuNo (ghi vào DataRow). |
| 15 | **Parameterize Method** | `Backend/ThuChiBinding.cs:36` | Dùng HienThi có tham số tên cột FK và ngày để thay nhiều khối binding giống nhau. |
| 16 | **Preserve Whole Object** | `Frontend/ThuChiReportParameters.cs:11`, `Frontend/frmInPhieuChi.cs:23`, `Frontend/frmInPhieuThanhToan.cs:23` | Truyền PhieuChi/PhieuThanhToan trực tiếp vào Tao(phieu) thay vì chỉ chuyển TongTien. |
| 17 | **Introduce Parameter Object** | `Backend/ThuChiBinding.cs:8`, `Backend/PhieuChiController.cs:43` | Nhóm 7 controls thu/chi trong ThuChiControls. |
| 18 | **Replace Exception with Test** | `Frontend/frmDunoKhachhang.cs:44` | Thay Convert.ToInt32 trong try/catch bằng Int32.TryParse. |
| 19 | **Pull Up Field** | `Backend/PhieuThuChiCoBan.cs:9`, `Backend/PhieuThuChiCoBan.cs:10`, `Backend/PhieuThuChiCoBan.cs:11` | Chuyển các trường ID, TONG_TIEN, GHI_CHU từ hai chứng từ lên PhieuThuChiCoBan. |
| 20 | **Pull Up Method** | `Backend/PhieuThuChiCoBan.cs:20`, `Backend/PhieuThuChiCoBan.cs:26`, `Backend/PhieuThuChiCoBan.cs:32` | Chuyển các getter/setter property Id, TongTien, GhiChu lên PhieuThuChiCoBan. |
| 21 | **Pull Up Constructor Body** | `Backend/PhieuThuChiCoBan.cs:15`, `Backend/PhieuChi.cs:8`, `Backend/PhieuThanhToan.cs:8` | Đưa phép gán Id trong các constructor có tham số về constructor base(id). |
| 22 | **Extract Superclass** | `Backend/PhieuThuChiCoBan.cs:7`, `Backend/PhieuChi.cs:5`, `Backend/PhieuThanhToan.cs:5` | Rút PhieuThuChiCoBan từ PhieuChi và PhieuThanhToan. |

## Đối chiếu đầy đủ 66/66 kỹ thuật trên Refactoring.Guru

Ghi chú: **Áp dụng** chỉ dành cho kỹ thuật có biến đổi mã nguồn thực tế trong bản nộp. **Không áp dụng** nghĩa là kỹ thuật đã được xem xét nhưng không có vấn đề tương ứng hoặc sẽ làm hệ thống phức tạp/sai hợp đồng.

### Composing Methods

| Kỹ thuật | Trạng thái | Lý do hoặc phần áp dụng |
|---|---|---|
| Extract Method | **Áp dụng** | `Backend/PhieuChiController.cs:27` — Tách khối chuyển dòng dữ liệu, tính công nợ, ghi công nợ thành các phương thức riêng. |
| Inline Method | Không áp dụng | Các hàm trung gian hiện có là ranh giới Controller–Factory; không nên bỏ chỉ để đạt số lượng. |
| Extract Variable | **Áp dụng** | `Frontend/frmPhieuChi.cs:100` — Đặt tên cho mã lý do, mã khách hàng và ngày lọc trước khi truyền vào thao tác tìm kiếm. |
| Inline Temp | **Áp dụng** | `Backend/DuNoKhachHangFactory.cs:15` — Bỏ biến OleDbCommand chỉ dùng một lần trong LoadSchema. |
| Replace Temp with Query | **Áp dụng** | `Backend/DuNoKhachHang.cs:53` — Thay biến tính số dư cuối kỳ bằng TinhCuoiKy() của đối tượng công nợ. |
| Split Temporary Variable | Không áp dụng | Không có biến tạm được dùng liên tiếp cho hai ý nghĩa nghiệp vụ khác nhau; các biến lặp đã loại bỏ. |
| Remove Assignments to Parameters | Không áp dụng | Không phát hiện tham số bị gán lại trong phạm vi refactor. |
| Replace Method with Method Object | Không áp dụng | Sau khi tách TinhCongNo/GhiDuNo, không còn phương thức cần object hóa trạng thái cục bộ. |
| Substitute Algorithm | **Áp dụng** | `Frontend/frmDunoKhachhang.cs:44` — Thay giải thuật kiểm tra năm bằng TryParse + so sánh khoảng, không dựa vào exception thông thường. |

### Moving Features between Objects

| Kỹ thuật | Trạng thái | Lý do hoặc phần áp dụng |
|---|---|---|
| Move Method | **Áp dụng** | `Backend/ThuChiBinding.cs:35` — Chuyển logic binding khỏi PhieuChiController/PhieuThanhToanController sang ThuChiBinding. |
| Move Field | Không áp dụng | Thuộc tính chung cần Pull Up Field lên lớp cha thay vì di chuyển ngang sang lớp khác. |
| Extract Class | **Áp dụng** | `Backend/ThuChiBinding.cs:33` — Tách ThuChiBinding, ThuChiReportParameters, ThuChiScalarQuery để gom trách nhiệm. |
| Inline Class | Không áp dụng | Các lớp tách ra có trách nhiệm dùng chung, gộp lại sẽ tái tạo mã lặp. |
| Hide Delegate | Không áp dụng | Controller đã che chi tiết Factory đối với giao diện; thêm tầng ủy quyền mới không cần thiết. |
| Remove Middle Man | Không áp dụng | Xóa lớp Controller để giao diện gọi thẳng DataLayer sẽ vi phạm phân lớp đang có. |
| Introduce Foreign Method | Không áp dụng | Không có phương thức còn thiếu trong thư viện bên ngoài cần bổ sung ở lớp gọi. |
| Introduce Local Extension | Không áp dụng | Không có thư viện đóng cần mở rộng để giải quyết hành vi hiện tại. |

### Organizing Data

| Kỹ thuật | Trạng thái | Lý do hoặc phần áp dụng |
|---|---|---|
| Self Encapsulate Field | Không áp dụng | Các trường lưu trữ mô hình đã có property, không phát hiện truy cập trực tiếp gây vấn đề. |
| Replace Data Value with Object | Không áp dụng | Không có kiểu nguyên thủy đơn lẻ chứa đủ hành vi để buộc tách object mới. |
| Change Value to Reference | Không áp dụng | Không có các value object cần quản lý chung danh tính. |
| Change Reference to Value | Không áp dụng | KhachHang/LyDoChi là thực thể tham chiếu; đổi sang value sẽ có rủi ro ngữ nghĩa. |
| Replace Array with Object | Không áp dụng | Không có mảng chứa các trường dị loại. |
| Duplicate Observed Data | Không áp dụng | Giao diện đã dùng BindingSource/DataTable làm lớp dữ liệu liên kết. |
| Change Unidirectional Association to Bidirectional | Không áp dụng | Không có liên kết cần tạo quan hệ hai chiều. |
| Change Bidirectional Association to Unidirectional | Không áp dụng | Không có quan hệ hai chiều thừa cần loại bỏ. |
| Replace Magic Number with Symbolic Constant | **Áp dụng** | `Frontend/frmDunoKhachhang.cs:16` — Đưa ngưỡng năm 2000 và 9999 thành hằng NamNhoNhat/NamLonNhat. |
| Encapsulate Field | Không áp dụng | Các trường BusinessObject vốn private và truy cập qua property. |
| Encapsulate Collection | Không áp dụng | Không có collection công khai cần đóng gói quyền sửa đổi. |
| Replace Type Code with Class | Không áp dụng | ID_LY_DO_CHI, ID_KHACH_HANG là khóa ngoại DB chứ không phải type code nội bộ. |
| Replace Type Code with Subclasses | Không áp dụng | Không có giá trị type code điều khiển hành vi để tách subclass. |
| Replace Type Code with State/Strategy | Không áp dụng | Không có type code chuyển trạng thái; không đưa Design Pattern vào sai yêu cầu. |
| Replace Subclass with Fields | Không áp dụng | PhieuChi và PhieuThanhToan có thuộc tính/nghiệp vụ riêng, không chỉ khác nhau ở hằng. |

### Simplifying Conditional Expressions

| Kỹ thuật | Trạng thái | Lý do hoặc phần áp dụng |
|---|---|---|
| Decompose Conditional | **Áp dụng** | `Frontend/frmDunoKhachhang.cs:41` — Tách kiểm tra hợp lệ năm thành hàm NamHopLe(string). |
| Consolidate Conditional Expression | **Áp dụng** | `Frontend/frmDunoKhachhang.cs:44` — Kết hợp parse + hai điều kiện miền giá trị trong biểu thức bool rõ nghĩa. |
| Consolidate Duplicate Conditional Fragments | Không áp dụng | Không có mã giống hệt ở cả hai nhánh điều kiện cần chuyển ra ngoài. |
| Remove Control Flag | **Áp dụng** | `Frontend/frmDunoKhachhang.cs:41` — Bỏ bool ok và nhánh try/catch điều khiển khi kiểm tra năm. |
| Replace Nested Conditional with Guard Clauses | **Áp dụng** | `Backend/PhieuChiController.cs:20` — Trả về null sớm khi không tìm thấy phiếu trước khi ánh xạ dòng dữ liệu. |
| Replace Conditional with Polymorphism | Không áp dụng | Không có nhiều nhánh theo phân loại đối tượng đòi hỏi đa hình. |
| Introduce Null Object | Không áp dụng | Các phương thức LayPhieu... đã dùng hợp đồng null; thay bằng Null Object có thể đổi hành vi gọi. |
| Introduce Assertion | Không áp dụng | Chưa có bất biến nghiệp vụ đã được xác thực đủ để assert an toàn. |

### Simplifying Method Calls

| Kỹ thuật | Trạng thái | Lý do hoặc phần áp dụng |
|---|---|---|
| Rename Method | **Áp dụng** | `Backend/DuNoKhachHangController.cs:14` — Đổi Tonghop thành TongHop cho nhất quán, giữ wrapper Tonghop [Obsolete] để tương thích. |
| Add Parameter | Không áp dụng | Không có thiếu tham số thực sự; bảo lưu chữ ký public đang được WinForms gọi. |
| Remove Parameter | Không áp dụng | Các tham số event/control public đều đang có người dùng; không cắt hợp đồng gọi. |
| Separate Query from Modifier | **Áp dụng** | `Backend/DuNoKhachHangController.cs:46` — Tách TinhCongNo (đọc + tính) khỏi GhiDuNo (ghi vào DataRow). |
| Parameterize Method | **Áp dụng** | `Backend/ThuChiBinding.cs:36` — Dùng HienThi có tham số tên cột FK và ngày để thay nhiều khối binding giống nhau. |
| Replace Parameter with Explicit Methods | Không áp dụng | Hiển thị và tìm kiếm đã là các phương thức public riêng, không có method điều khiển bởi cờ. |
| Preserve Whole Object | **Áp dụng** | `Frontend/ThuChiReportParameters.cs:11` — Truyền PhieuChi/PhieuThanhToan trực tiếp vào Tao(phieu) thay vì chỉ chuyển TongTien. |
| Replace Parameter with Method Call | Không áp dụng | Chưa có giá trị dẫn xuất truyền thừa mà method nhận có thể truy cập trực tiếp an toàn. |
| Introduce Parameter Object | **Áp dụng** | `Backend/ThuChiBinding.cs:8` — Nhóm 7 controls thu/chi trong ThuChiControls. |
| Remove Setting Method | Không áp dụng | Thuộc tính mẫu dữ liệu cần setter cho mapping/binding và việc sửa phiếu. |
| Hide Method | Không áp dụng | Không thu hẹp phạm vi hàm public cũ nhằm tránh làm hỏng các nơi gọi ngoài thư mục. |
| Replace Constructor with Factory Method | Không áp dụng | Constructor chứng từ chỉ gán ID; không có lựa chọn khởi tạo phức tạp. |
| Replace Error Code with Exception | Không áp dụng | Hợp đồng Save() bool của tầng DataLayer cần giữ để tránh phá API. |
| Replace Exception with Test | **Áp dụng** | `Frontend/frmDunoKhachhang.cs:44` — Thay Convert.ToInt32 trong try/catch bằng Int32.TryParse. |

### Dealing with Generalization

| Kỹ thuật | Trạng thái | Lý do hoặc phần áp dụng |
|---|---|---|
| Pull Up Field | **Áp dụng** | `Backend/PhieuThuChiCoBan.cs:9` — Chuyển các trường ID, TONG_TIEN, GHI_CHU từ hai chứng từ lên PhieuThuChiCoBan. |
| Pull Up Method | **Áp dụng** | `Backend/PhieuThuChiCoBan.cs:20` — Chuyển các getter/setter property Id, TongTien, GhiChu lên PhieuThuChiCoBan. |
| Pull Up Constructor Body | **Áp dụng** | `Backend/PhieuThuChiCoBan.cs:15` — Đưa phép gán Id trong các constructor có tham số về constructor base(id). |
| Push Down Method | Không áp dụng | Các property chung đều được cả hai loại phiếu sử dụng. |
| Push Down Field | Không áp dụng | Các trường chung đều được cả hai loại phiếu sử dụng. |
| Extract Subclass | Không áp dụng | Hai lớp phiếu riêng đã hiện diện; thêm subclass không mang lại khác biệt nghiệp vụ. |
| Extract Superclass | **Áp dụng** | `Backend/PhieuThuChiCoBan.cs:7` — Rút PhieuThuChiCoBan từ PhieuChi và PhieuThanhToan. |
| Extract Interface | Không áp dụng | Đã có superclass chung cho các property tương đồng; thêm interface rỗng/không sử dụng là dư thừa. |
| Collapse Hierarchy | Không áp dụng | Gộp hai chứng từ lại sẽ mất khác biệt khách hàng/lý do chi/ngày tương ứng. |
| Form Template Method | Không áp dụng | Không có chuỗi bước kế thừa đồng hình để lập template mà không tác động WinForms. |
| Replace Inheritance with Delegation | Không áp dụng | Quan hệ lớp cha ở chứng từ là is-a hợp lý, thay bằng delegation sẽ làm RDLC phức tạp. |
| Replace Delegation with Inheritance | Không áp dụng | Controller ủy quyền Factory theo quan hệ uses-a, không phải is-a. |

## Tệp ảnh hưởng

**Tệp mới (4):**

- `Features/ThuChi/Backend/PhieuThuChiCoBan.cs` — lớp cơ sở chung cho `PhieuChi` và `PhieuThanhToan`.
- `Features/ThuChi/Backend/ThuChiBinding.cs` — đối tượng nhóm controls + một nơi duy nhất cấu hình binding.
- `Features/ThuChi/Backend/ThuChiScalarQuery.cs` — chung thao tác scalar và tham số tháng/năm cho ba Factory.
- `Features/ThuChi/Frontend/ThuChiReportParameters.cs` — tạo cùng bộ tham số in cho 3 mẫu báo cáo.

**Tệp sửa (16):** `CuahangNongduoc.csproj`; 9 tệp Backend có sẵn (`DuNoKhachHang`, `DuNoKhachHangController`, `DuNoKhachHangFactory`, `PhieuChi`, `PhieuChiController`, `PhieuChiFactory`, `PhieuThanhToan`, `PhieuThanhToanController`, `PhieuThanhToanFactory`); 6 tệp Frontend (`frmDunoKhachhang`, `frmInDunoKhachHang`, `frmInPhieuChi`, `frmInPhieuThanhToan`, `frmPhieuChi`, `frmThanhToan`).

## Kiểm tra đã thực hiện

- Kiểm tra nội dung tệp cũ không thuộc phạm vi ThuChi vẫn nguyên vẹn; tất cả file mới có mục `<Compile Include=...>` trong `.csproj`.
- Parse `.csproj` và kiểm tra mỗi nguồn `<Compile>` tồn tại, không trùng đường dẫn.
- Đối chiếu nguyên văn câu SQL của ba Factory trước và sau refactoring: giữ nguyên.
- Kiểm tra các binding tên cột `ID`, `ID_LY_DO_CHI` / `ID_KHACH_HANG`, `NGAY_CHI` / `NGAY_THANH_TOAN`, `TONG_TIEN`, `GHI_CHU`.
- Kiểm tra tham chiếu sự kiện thiết kế WinForms và các cặp dấu ngoặc nhọn trong mã nguồn.
- Có script `KIEM_TRA_REFACTORING_THUCHI.py` để tự chạy lại các bước kiểm tra tĩnh trên máy có Python.

**Giới hạn kiểm thử:** môi trường tạo bản chỉnh sửa không có Visual Studio/.NET Framework 2.0, Microsoft Jet OLEDB và màn hình WinForms. Chưa biên dịch thật hoặc thực hiện kiểm thử tích hợp với tệp `cuahang.dll` (MDB Access). **Không khẳng định hệ thống chạy thành công khi chưa kiểm thử trên Windows.**

## Các bước cần kiểm tra thủ công trên Windows

1. Mở `CuahangNongDuoc.sln` bằng Visual Studio phù hợp với dự án, chọn đúng cấu hình x86 và kiểm tra đủ thư viện ReportViewer/XPExplorerBar.
2. Chạy **Phiếu chi**: tải danh sách, thêm phiếu, lưu, chỉnh sửa, xóa có xác nhận, tìm theo lý do/ngày, in phiếu; so sánh số liệu trước và sau.
3. Chạy **Phiếu thanh toán/thu**: tải danh sách, thêm, lưu, xóa, tìm theo khách hàng/ngày, in phiếu; kiểm tra liên kết khách hàng và thành tiền bằng chữ.
4. Chạy **Tổng hợp dư nợ** qua tháng 1 (kiểm tra năm trước) và tháng khác, in và lưu; kiểm tra công thức `CuoiKy = DauKy + PhatSinh - DaTra` với một khách hàng.
5. Thử năm không hợp lệ (chữ, dưới 2000, trên 9999), năm hợp lệ, trường hợp tìm kiếm không có dữ liệu và danh sách trống.
6. Dùng bản sao dữ liệu Access trước khi tổng hợp để tránh thay đổi dữ liệu thật; `TongHop` vẫn gọi `factory.Clear(...)` như bản gốc.

## Lưu ý nghiệp vụ

- Hành vi `TongHop` **xóa dữ liệu dư nợ của kỳ trước khi ghi lại**, giống nguyên bản. Nếu muốn thêm transaction an toàn hoặc xử lý lỗi DB, đó là nhiệm vụ sửa lỗi/thiết kế tiếp theo, **không phải refactoring giữ nguyên hành vi**.
- Không sửa lỗi nghiệp vụ tiềm ẩn trong điều kiện tìm phiếu của giao diện (ngày được chọn lấy từ control gốc) vì thay đổi như vậy sẽ vượt phạm vi refactoring; nên có test so sánh trước/sau.

---

*Danh mục được đối chiếu với: https://refactoring.guru/refactoring/catalog*

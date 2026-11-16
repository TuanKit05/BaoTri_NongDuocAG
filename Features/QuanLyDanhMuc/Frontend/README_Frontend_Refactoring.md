# Tái cấu trúc Frontend – Quản lý Danh mục
## Áp dụng Extract Superclass + Template Method (Refactoring Guru)

### Cấu trúc mới

```
Frontend/Refactored/
├── BaseDanhMucForm.cs      ← Superclass cho tất cả form danh mục
├── frmDonViTinh.cs
├── frmLyDoChi.cs
├── frmNhaCungCap.cs
├── frmKhachHang.cs
├── frmDaiLy.cs
└── frmSanPham.cs
```

---

### Các kỹ thuật đã dùng

| Kỹ thuật | Áp dụng |
|----------|---------|
| **Extract Superclass** | Tạo `BaseDanhMucForm` |
| **Pull Up Method** | Kéo `LoadData`, `Luu`, `XoaDongHienTai`, `HuyXoaDong`, `Thoat` lên base |
| **Template Method** | Định nghĩa skeleton (Load → Save → Delete → Exit), subclass override khi cần |
| **Hook Method** | `Controller`, `TenDanhMuc`, `LoadData()` |

---

### Lợi ích

1. **Loại bỏ code trùng lặp**  
   - MessageBox xác nhận xóa, logic Lưu, Thoát… chỉ viết 1 lần.

2. **Dễ bảo trì**  
   - Muốn đổi cách xác nhận xóa → chỉ sửa trong `BaseDanhMucForm`.

3. **Mở rộng dễ dàng**  
   - Thêm form danh mục mới chỉ cần:
   ```csharp
   public partial class frmLoaiMoi : BaseDanhMucForm
   {
       protected override BaseController Controller => new LoaiMoiController();
       protected override string TenDanhMuc => "Loại mới";
       // ...
   }
   ```

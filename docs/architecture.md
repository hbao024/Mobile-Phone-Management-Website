# Kiến trúc và ranh giới module

## Stack và mục tiêu

Source hiện dùng ASP.NET MVC 5.2.9, .NET Framework 4.7.2, EF6 6.5.1 Database First và SQL Server. Mục tiêu là một ứng dụng, một bộ EDMX và một DbContext. Trạng thái phê duyệt: D-001, D-002 trong [sổ quyết định](decisions.md).

Nguồn kiểm tra:

- [Project](../DoAnLTWeb/QuanLiDienThoaiDD/QuanLiDienThoaiDD/QuanLiDienThoaiDD.csproj): target framework, namespace và assembly.
- [packages.config](../DoAnLTWeb/QuanLiDienThoaiDD/QuanLiDienThoaiDD/packages.config): phiên bản dependency.
- [Model1.Context.cs](../DoAnLTWeb/QuanLiDienThoaiDD/QuanLiDienThoaiDD/Models/Model1.Context.cs): `QLHANGHOA_PHUKIEN_WEBEntities`, ném `UnintentionalCodeFirstException` khi dùng như Code First.

## Cấu trúc ứng viên hiện tại

```text
DoAnLTWeb/
├── QuanLiDienThoaiDD/
│   ├── QuanLiDienThoaiDD.sln
│   ├── README.md
│   └── QuanLiDienThoaiDD/
│       ├── QuanLiDienThoaiDD.csproj
│       ├── App_Start/
│       ├── Controllers/
│       ├── Areas/Admin/
│       ├── Models/
│       ├── Security/
│       ├── Data/
│       ├── ViewModels/
│       ├── Views/
│       ├── Content/
│       ├── Scripts/
│       └── images/
├── QL_HANGHOA_PHUKIEN_WEB.sql
└── NhiemVu.docx
```

Solution tham chiếu `QuanLiDienThoaiDD/QuanLiDienThoaiDD.csproj`. Việc đưa solution/project về root thuộc D-007; cây trên mô tả vị trí thực tế, không phải một lần di chuyển đã thực hiện.

## Ranh giới chức năng

Nguồn phân công: [NhiemVu.docx](../DoAnLTWeb/NhiemVu.docx). Tài liệu gốc có nhắc Core/EF Core; stack thực tế ở trên là cơ sở kỹ thuật của dự án.

| Phạm vi | Chủ trì theo phân công |
|---|---|
| Nền tảng, đăng nhập/phân quyền, Dashboard và giao diện chung | Hoàng Bảo |
| Danh mục, thương hiệu, nhà cung cấp, sản phẩm, biến thể, hình ảnh và tồn kho | Thành Đạt |
| Giao diện khách, giỏ hàng, đăng ký, đặt hàng và lịch sử mua | Thanh Nhàn |
| Admin đơn hàng, phiếu nhập, khách hàng, báo cáo và kiểm thử tổng thể | Duy Tân |

`Controllers/` và `Views/` chứa phía khách; `Areas/Admin/` chứa quản trị. Bộ model Database First dùng chung. Không tạo model dữ liệu hoặc DbContext riêng cho từng thành viên.

## Ownership file dùng chung

Phân công điều phối dưới đây là nội dung đề xuất liên quan D-009, không tự cấp quyền sửa:

- Hoàng Bảo điều phối solution, project, dependency, config, startup, authentication và bộ EDMX generated.
- Layout/CSS chung: Hoàng Bảo điều phối; Nhàn review phía khách, Tân review phía Admin.
- Đạt chủ trì nội dung contract catalog–inventory; Tân review nhập/xuất/hoàn, Nhàn review checkout và dữ liệu hiển thị.
- Thay đổi file dùng chung phải chỉ rõ chủ module bị ảnh hưởng. Cơ chế ghi tồn phải thống nhất giữa các module.

## Ranh giới tích hợp

Theo D-003, source ứng viên chứa chức năng khách hàng; module Admin từ nguồn Tân sẽ được kế thừa ở gói tích hợp riêng. Không coi các module đó đã được nhập hoặc nghiệm thu. Các ref và SHA nguồn nằm duy nhất trong [HANDOFF](HANDOFF.md).

Các điểm phải hợp nhất ở giai đoạn sau: `.csproj`, config gốc, Account/Dashboard, hai layout, CSS và seed. Không mặc định chọn toàn bộ hành vi của một branch chỉ vì chọn solution của branch đó. D-004 điều chỉnh mức độ thay đổi kiến trúc; InventoryService được bổ sung trong phạm vi D-019, chưa tích hợp các writer của Tân.

Contract chuyên môn nằm tại [catalog-inventory.md](contracts/catalog-inventory.md). Hướng dẫn chạy trong README cũ cần review trước khi sử dụng: config gốc chưa được theo dõi ở ứng viên, startup gọi seed, SQL gốc có thao tác phá hủy. Gói tài liệu không sửa các điểm này và chưa xác minh runtime.

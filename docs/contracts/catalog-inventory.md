# Contract sản phẩm, giá và tồn kho

Đây là mô tả source và nội dung để review, không phải một thay đổi database đã triển khai. Trạng thái phê duyệt của từng đề xuất nằm duy nhất trong [sổ quyết định](../decisions.md), đặc biệt D-005, D-008, D-009 và D-010.

## Hiện trạng từ source

Nguồn: [SQL hiện có](../../DoAnLTWeb/QL_HANGHOA_PHUKIEN_WEB.sql), [EDMX](../../DoAnLTWeb/QuanLiDienThoaiDD/QuanLiDienThoaiDD/Models/Model1.edmx), [checkout](../../DoAnLTWeb/QuanLiDienThoaiDD/QuanLiDienThoaiDD/Controllers/DatHangController.cs) và [giỏ hàng](../../DoAnLTWeb/QuanLiDienThoaiDD/QuanLiDienThoaiDD/Models/CartItem.cs). Đây là bằng chứng tĩnh, không xác nhận database đang chạy.

| Đối tượng | Dữ liệu hiện có |
|---|---|
| `SANPHAM` | Mã sản phẩm, thông tin chung, loại, thương hiệu, NCC mặc định, bảo hành và trạng thái |
| `BIENSANPHAM` | Mã biến thể, FK sản phẩm, SKU duy nhất, màu/RAM/bộ nhớ, giá và ảnh |
| Giá | `GiaBan`, `GiaNhap` dùng `decimal(18,2)`; ràng buộc không âm |
| `TONKHO` | Khóa theo biến thể, `SoLuongTon`, `SoLuongDangDat`, thời điểm cập nhật |
| `GIAODICHKHO` | Loại giao dịch, số lượng, tồn trước/sau và liên kết chứng từ/người thực hiện |
| Chi tiết đơn | Lưu tên, đơn giá, số lượng, giảm giá; thành tiền tính trong SQL |
| Chi tiết phiếu nhập | Lưu đơn giá và số lượng từng biến thể; thành tiền tính trong SQL |

Checkout hiện dùng giá đã lưu trong session và sinh mã theo số bản ghi. EDMX chưa có concurrency token. So sánh nguồn Admin ở FOUNDATION-02A cho thấy xử lý kho có transaction nhưng vẫn đọc rồi gán số tồn; giá nhập được cập nhật từ phiếu nhập. Nguồn Admin chưa nằm trong cây ứng viên; ref bằng chứng được ghi ở HANDOFF để đọc bằng `git show`, không cần checkout.

## Biên quản lý catalog CAT-04

Phạm vi và căn cứ tại D-015 trong [sổ quyết định](../decisions.md). Phần này mô tả quản trị catalog, không thay các đề xuất tồn kho bên dưới.

- Mã sản phẩm/biến thể và sản phẩm cha của biến thể giữ bất biến. SKU sửa được nhưng phải duy nhất toàn bảng, kể cả biến thể ngừng bán; không áp unique tên sản phẩm hoặc tổ hợp màu/RAM/bộ nhớ.
- Khi tạo/chọn mới, chỉ nhận loại, thương hiệu, NCC đang hoạt động. Khi sửa sản phẩm, cho giữ nguyên liên kết cũ đã ngừng sử dụng hoặc chuyển sang bản ghi đang hoạt động; NCC mặc định có thể để trống. Không xóa/sửa bản ghi danh mục hoặc lịch sử chứng từ.
- Sản phẩm và biến thể có trạng thái riêng, không cập nhật dây chuyền; Admin vẫn quản lý được biến thể dưới sản phẩm ngừng kinh doanh. Storefront/checkout chưa lọc đầy đủ trạng thái biến thể; CAT-04 không sửa phía khách.
- Giá nhập/bán nhận giá trị không âm trong decimal(18,2), tối đa 9999999999999999.99 và 2 chữ số thập phân; bảo hành 0–120 tháng, RAM/bộ nhớ tùy chọn nhưng phải từ 1–32767 nếu có. Không tính lại snapshot giá/tên trên đơn hoặc phiếu nhập, không thay giá đã nằm trong session giỏ hàng.
- Chỉ dùng `BIENSANPHAM.HinhAnhUrl` (tùy chọn, 500 ký tự), chưa upload. Storefront hiện ghép `~` với giá trị lưu, nên form nhận `/images/...`; thư mục/tên dùng chữ ASCII/số/_/-, tên ảnh được có dấu chấm. Chặn traversal, URL ngoài, ký tự điều khiển, backslash, dấu nháy và các ký tự không thuộc định dạng này; không truy cập ảnh hoặc xác minh file có tồn tại. Không di chuyển/sửa các ảnh đã có trong repository.
- Tạo biến thể không tạo TONKHO hoặc nhật ký kho; cập nhật biến thể không nhận/ghi tồn, đơn hàng, giỏ hàng hoặc chi tiết nhập hàng. Luồng tồn/nhập của Tân cần được kiểm chứng riêng; không coi biến thể mới đã có tồn khả dụng.

## Vấn đề cần thống nhất

- Thời điểm chốt giá, đọc lại giá và yêu cầu xác nhận khi giá thay đổi.
- Vòng đời `SoLuongDangDat`: source phía khách chưa giữ hàng, còn nguồn Admin có giảm trường này khi xác nhận.
- Điều kiện xuất, hoàn kho và hủy khi đang giao.
- Ý nghĩa `GiaNhap`, tiêu chí biến thể trùng và ngừng bán.
- Quyền sửa giá/điều chỉnh kho và trách nhiệm giữa các module.

## Nội dung đề xuất cho D-008

- Khóa sản phẩm/biến thể bất biến; SKU duy nhất. Không đổi quan hệ biến thể đã có chứng từ tùy ý.
- Server đọc lại giá, trạng thái và số lượng tại checkout; giá session chỉ là giá đã hiển thị. Nhóm cần chọn thời điểm chốt và cách xác nhận giá mới.
- Giữ snapshot tên/giá trên chứng từ; cập nhật catalog không sửa lịch sử.
- Tạo bản ghi tồn bằng 0 cùng transaction với biến thể; điều chỉnh kho ghi chênh lệch và lý do.
- Ưu tiên ngừng hoạt động thay vì xóa dữ liệu đã có tham chiếu.
- Phương án ít thay đổi là chưa giữ hàng khi đặt, chỉ trừ khi xác nhận. Hệ quả: đơn chờ không bảo đảm còn hàng. Nếu chọn giữ hàng, phải xác định thời hạn và giải phóng khi hủy/hết hạn.
- Không tự sửa hoặc đưa `SoLuongDangDat` hiện hữu về 0; cần đối soát trước khi chọn thuật toán.
- Khi đã xuất kho, chỉ hoàn theo điều kiện nghiệp vụ đã chốt, đúng một lần. Hủy khi đang giao cần phân biệt hủy đơn với thực nhận lại hàng.
- Giữ nghĩa giá nhập tham khảo gần nhất là một phương án; không tự coi là giá vốn bình quân.
- Ảnh upload cần kiểm tra định dạng/kích thước, tên do server sinh, nơi lưu không thực thi script; xử lý bù nếu lưu DB thất bại vì filesystem không cùng transaction SQL.

## Nội dung đề xuất cho D-010

- Cân nhắc `rowversion` trên tồn, sản phẩm/biến thể và chứng từ; map concurrency token trong EDMX. Form sửa phải gửi phiên bản đã đọc, không chỉ tải bản mới rồi ghi đè.
- Một transaction bao trọn kiểm tra trạng thái/dòng hàng, cập nhật tồn, nhật ký kho, trạng thái và lịch sử chứng từ.
- Xung đột hoặc thiếu tồn ở một dòng phải rollback toàn nghiệp vụ; xử lý nhiều biến thể theo thứ tự khóa cố định.
- Chặn áp dụng lặp bằng trạng thái có kiểm soát đồng thời, ràng buộc nguồn nhật ký phù hợp và mã request duy nhất cho tạo đơn.
- Sinh mã có bảo đảm duy nhất từ database; không dựa riêng vào `Count()+1` hoặc `MAX+1`.
- Retry có giới hạn, đọc lại dữ liệu và chạy lại toàn transaction; không tự ghi đè dữ liệu cũ.

## Bằng chứng cần có khi triển khai contract

Hai context độc lập tranh đơn vị tồn cuối; xác nhận lặp; xác nhận đồng thời với hủy; nhập đồng thời với bán; nhiều dòng thiếu một dòng; lỗi trước commit; form sửa cũ; giá đổi trong giỏ; truy xuất chứng từ sau ngừng bán. Kiểm tra phải đối soát cả số tồn, trạng thái và nhật ký. Danh sách này là tiêu chí tương lai, chưa có kết quả chạy.

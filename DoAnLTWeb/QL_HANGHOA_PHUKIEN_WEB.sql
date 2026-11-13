/*
    QLHANGHOA_PHUKIEN_WEB.sql
    Database cho website bán và quản lý điện thoại di động.

    LƯU Ý:
    - Script này tạo database mới QLHANGHOA_PHUKIEN_WEB để không ghi đè
      database cũ đang được project WPF sử dụng.
    - Có thể đổi tên database và connection string sau khi kiểm thử.
    - MatKhauHash bên dưới là hash mẫu tương thích với System.Web.Helpers.Crypto.
      Khi triển khai thật, cần đổi mật khẩu mặc định và không dùng tài khoản mẫu.
*/

USE master;
GO

IF DB_ID(N'QLHANGHOA_PHUKIEN_WEB') IS NOT NULL
BEGIN
    ALTER DATABASE [QLHANGHOA_PHUKIEN_WEB] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE [QLHANGHOA_PHUKIEN_WEB];
END;
GO

CREATE DATABASE [QLHANGHOA_PHUKIEN_WEB];
GO

USE [QLHANGHOA_PHUKIEN_WEB];
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;
GO

/* ================================================================
   1. TÀI KHOẢN VÀ PHÂN QUYỀN
   ================================================================ */

CREATE TABLE dbo.VAITRO
(
    MaVaiTro       varchar(20)      NOT NULL,
    TenVaiTro      nvarchar(50)     NOT NULL,
    MoTa           nvarchar(250)    NULL,
    CONSTRAINT PK_VAITRO PRIMARY KEY (MaVaiTro),
    CONSTRAINT UQ_VAITRO_TenVaiTro UNIQUE (TenVaiTro)
);
GO

CREATE TABLE dbo.TAIKHOAN
(
    MaTaiKhoan     int IDENTITY(1,1) NOT NULL,
    TenDangNhap    nvarchar(100)    NOT NULL,
    Email          varchar(255)     NOT NULL,
    MatKhauHash    nvarchar(500)    NOT NULL,
    MaVaiTro       varchar(20)      NOT NULL,
    TrangThai      bit              NOT NULL CONSTRAINT DF_TAIKHOAN_TrangThai DEFAULT (1),
    NgayTao        datetime2(0)     NOT NULL CONSTRAINT DF_TAIKHOAN_NgayTao DEFAULT (SYSDATETIME()),
    LanDangNhapCuoi datetime2(0)    NULL,
    CONSTRAINT PK_TAIKHOAN PRIMARY KEY (MaTaiKhoan),
    CONSTRAINT UQ_TAIKHOAN_TenDangNhap UNIQUE (TenDangNhap),
    CONSTRAINT UQ_TAIKHOAN_Email UNIQUE (Email),
    CONSTRAINT FK_TAIKHOAN_VAITRO FOREIGN KEY (MaVaiTro)
        REFERENCES dbo.VAITRO(MaVaiTro)
);
GO

CREATE TABLE dbo.NHANVIEN
(
    MaNV           varchar(10)      NOT NULL,
    TenNV          nvarchar(100)    NOT NULL,
    NamSinh        smallint         NULL,
    DienThoai      varchar(15)      NULL,
    Email          varchar(255)     NULL,
    HinhAnh        nvarchar(500)    NULL,
    GioiTinh       nvarchar(10)     NULL,
    MaTaiKhoan     int              NULL,
    TrangThai      bit              NOT NULL CONSTRAINT DF_NHANVIEN_TrangThai DEFAULT (1),
    CONSTRAINT PK_NHANVIEN PRIMARY KEY (MaNV),
    CONSTRAINT FK_NHANVIEN_TAIKHOAN FOREIGN KEY (MaTaiKhoan)
        REFERENCES dbo.TAIKHOAN(MaTaiKhoan),
    CONSTRAINT CK_NHANVIEN_NamSinh CHECK (NamSinh IS NULL OR NamSinh BETWEEN 1900 AND 2100)
);
GO

CREATE UNIQUE INDEX UX_NHANVIEN_MaTaiKhoan
    ON dbo.NHANVIEN(MaTaiKhoan)
    WHERE MaTaiKhoan IS NOT NULL;
GO

CREATE UNIQUE INDEX UX_NHANVIEN_Email
    ON dbo.NHANVIEN(Email)
    WHERE Email IS NOT NULL;
GO

CREATE TABLE dbo.KHACHHANG
(
    MaKH           varchar(10)      NOT NULL,
    TenKH          nvarchar(100)    NOT NULL,
    Email          varchar(255)     NULL,
    DienThoai      varchar(15)      NOT NULL,
    DiaChi         nvarchar(300)    NULL,
    NgayThamGia    datetime2(0)     NOT NULL CONSTRAINT DF_KHACHHANG_NgayThamGia DEFAULT (SYSDATETIME()),
    MaTaiKhoan     int              NULL,
    TrangThai      bit              NOT NULL CONSTRAINT DF_KHACHHANG_TrangThai DEFAULT (1),
    CONSTRAINT PK_KHACHHANG PRIMARY KEY (MaKH),
    CONSTRAINT FK_KHACHHANG_TAIKHOAN FOREIGN KEY (MaTaiKhoan)
        REFERENCES dbo.TAIKHOAN(MaTaiKhoan)
);
GO

CREATE UNIQUE INDEX UX_KHACHHANG_Email
    ON dbo.KHACHHANG(Email)
    WHERE Email IS NOT NULL;
GO

CREATE UNIQUE INDEX UX_KHACHHANG_MaTaiKhoan
    ON dbo.KHACHHANG(MaTaiKhoan)
    WHERE MaTaiKhoan IS NOT NULL;
GO

/* ================================================================
   2. DANH MỤC, THƯƠNG HIỆU VÀ SẢN PHẨM
   ================================================================ */

CREATE TABLE dbo.LOAISANPHAM
(
    MaLoai         varchar(10)      NOT NULL,
    TenLoai        nvarchar(100)    NOT NULL,
    MoTa           nvarchar(500)    NULL,
    TrangThai      bit              NOT NULL CONSTRAINT DF_LOAISANPHAM_TrangThai DEFAULT (1),
    CONSTRAINT PK_LOAISANPHAM PRIMARY KEY (MaLoai),
    CONSTRAINT UQ_LOAISANPHAM_TenLoai UNIQUE (TenLoai)
);
GO

CREATE TABLE dbo.THUONGHIEU
(
    MaThuongHieu   varchar(10)      NOT NULL,
    TenThuongHieu  nvarchar(100)    NOT NULL,
    MoTa           nvarchar(500)    NULL,
    LogoUrl        nvarchar(500)    NULL,
    TrangThai      bit              NOT NULL CONSTRAINT DF_THUONGHIEU_TrangThai DEFAULT (1),
    CONSTRAINT PK_THUONGHIEU PRIMARY KEY (MaThuongHieu),
    CONSTRAINT UQ_THUONGHIEU_Ten UNIQUE (TenThuongHieu)
);
GO

CREATE TABLE dbo.NHACUNGCAP
(
    MaNCC          varchar(10)      NOT NULL,
    TenNCC         nvarchar(150)    NOT NULL,
    DiaChi         nvarchar(300)    NULL,
    DienThoai      varchar(15)      NULL,
    Email          varchar(255)     NULL,
    TrangThai      bit              NOT NULL CONSTRAINT DF_NHACUNGCAP_TrangThai DEFAULT (1),
    CONSTRAINT PK_NHACUNGCAP PRIMARY KEY (MaNCC)
);
GO

CREATE TABLE dbo.SANPHAM
(
    MaSanPham      varchar(20)      NOT NULL,
    TenSanPham     nvarchar(200)    NOT NULL,
    MaLoai         varchar(10)      NOT NULL,
    MaThuongHieu   varchar(10)      NOT NULL,
    MaNCCMacDinh   varchar(10)      NULL,
    MoTa           nvarchar(max)    NULL,
    BaoHanhThang   smallint         NOT NULL CONSTRAINT DF_SANPHAM_BaoHanh DEFAULT (12),
    TrangThai      bit              NOT NULL CONSTRAINT DF_SANPHAM_TrangThai DEFAULT (1),
    NgayTao        datetime2(0)     NOT NULL CONSTRAINT DF_SANPHAM_NgayTao DEFAULT (SYSDATETIME()),
    NgayCapNhat    datetime2(0)     NOT NULL CONSTRAINT DF_SANPHAM_NgayCapNhat DEFAULT (SYSDATETIME()),
    CONSTRAINT PK_SANPHAM PRIMARY KEY (MaSanPham),
    CONSTRAINT FK_SANPHAM_LOAI FOREIGN KEY (MaLoai)
        REFERENCES dbo.LOAISANPHAM(MaLoai),
    CONSTRAINT FK_SANPHAM_THUONGHIEU FOREIGN KEY (MaThuongHieu)
        REFERENCES dbo.THUONGHIEU(MaThuongHieu),
    CONSTRAINT FK_SANPHAM_NCC FOREIGN KEY (MaNCCMacDinh)
        REFERENCES dbo.NHACUNGCAP(MaNCC),
    CONSTRAINT CK_SANPHAM_BaoHanh CHECK (BaoHanhThang BETWEEN 0 AND 120)
);
GO

CREATE TABLE dbo.BIENSANPHAM
(
    MaBienSanPham  varchar(30)      NOT NULL,
    MaSanPham      varchar(20)      NOT NULL,
    SKU            varchar(50)      NOT NULL,
    MauSac         nvarchar(50)     NULL,
    RAMGB          smallint         NULL,
    BoNhoGB        smallint         NULL,
    GiaNhap        decimal(18,2)    NOT NULL,
    GiaBan         decimal(18,2)    NOT NULL,
    HinhAnhUrl     nvarchar(500)    NULL,
    TrangThai      bit              NOT NULL CONSTRAINT DF_BIENSANPHAM_TrangThai DEFAULT (1),
    CONSTRAINT PK_BIENSANPHAM PRIMARY KEY (MaBienSanPham),
    CONSTRAINT UQ_BIENSANPHAM_SKU UNIQUE (SKU),
    CONSTRAINT FK_BIENSANPHAM_SANPHAM FOREIGN KEY (MaSanPham)
        REFERENCES dbo.SANPHAM(MaSanPham),
    CONSTRAINT CK_BIENSANPHAM_GiaNhap CHECK (GiaNhap >= 0),
    CONSTRAINT CK_BIENSANPHAM_GiaBan CHECK (GiaBan >= 0),
    CONSTRAINT CK_BIENSANPHAM_RAM CHECK (RAMGB IS NULL OR RAMGB > 0),
    CONSTRAINT CK_BIENSANPHAM_BoNho CHECK (BoNhoGB IS NULL OR BoNhoGB > 0)
);
GO

/* ================================================================
   3. NHẬP HÀNG, ĐƠN HÀNG VÀ TỒN KHO
   ================================================================ */

CREATE TABLE dbo.TONKHO
(
    MaBienSanPham  varchar(30)      NOT NULL,
    SoLuongTon     int              NOT NULL CONSTRAINT DF_TONKHO_SoLuongTon DEFAULT (0),
    SoLuongDangDat int              NOT NULL CONSTRAINT DF_TONKHO_SoLuongDangDat DEFAULT (0),
    NgayCapNhat    datetime2(0)     NOT NULL CONSTRAINT DF_TONKHO_NgayCapNhat DEFAULT (SYSDATETIME()),
    CONSTRAINT PK_TONKHO PRIMARY KEY (MaBienSanPham),
    CONSTRAINT FK_TONKHO_BIENSANPHAM FOREIGN KEY (MaBienSanPham)
        REFERENCES dbo.BIENSANPHAM(MaBienSanPham),
    CONSTRAINT CK_TONKHO_SoLuongTon CHECK (SoLuongTon >= 0),
    CONSTRAINT CK_TONKHO_SoLuongDangDat CHECK (SoLuongDangDat >= 0)
);
GO

CREATE TABLE dbo.PHIEUNHAP
(
    MaPhieuNhap    varchar(20)      NOT NULL,
    MaNCC          varchar(10)      NOT NULL,
    MaNV           varchar(10)      NOT NULL,
    NgayNhap       datetime2(0)     NOT NULL CONSTRAINT DF_PHIEUNHAP_NgayNhap DEFAULT (SYSDATETIME()),
    TrangThai      varchar(20)      NOT NULL CONSTRAINT DF_PHIEUNHAP_TrangThai DEFAULT ('DA_NHAP'),
    TongTien       decimal(18,2)    NOT NULL CONSTRAINT DF_PHIEUNHAP_TongTien DEFAULT (0),
    GhiChu         nvarchar(500)    NULL,
    CONSTRAINT PK_PHIEUNHAP PRIMARY KEY (MaPhieuNhap),
    CONSTRAINT FK_PHIEUNHAP_NCC FOREIGN KEY (MaNCC)
        REFERENCES dbo.NHACUNGCAP(MaNCC),
    CONSTRAINT FK_PHIEUNHAP_NHANVIEN FOREIGN KEY (MaNV)
        REFERENCES dbo.NHANVIEN(MaNV),
    CONSTRAINT CK_PHIEUNHAP_TrangThai CHECK (TrangThai IN ('CHO_XU_LY', 'DA_NHAP', 'DA_HUY')),
    CONSTRAINT CK_PHIEUNHAP_TongTien CHECK (TongTien >= 0)
);
GO

CREATE TABLE dbo.CHITIETPHIEUNHAP
(
    MaPhieuNhap    varchar(20)      NOT NULL,
    MaBienSanPham  varchar(30)      NOT NULL,
    DonGia         decimal(18,2)    NOT NULL,
    SoLuong        int              NOT NULL,
    ThanhTien AS CONVERT(decimal(18,2), DonGia * SoLuong) PERSISTED,
    CONSTRAINT PK_CHITIETPHIEUNHAP PRIMARY KEY (MaPhieuNhap, MaBienSanPham),
    CONSTRAINT FK_CT_PHIEUNHAP_PHIEU FOREIGN KEY (MaPhieuNhap)
        REFERENCES dbo.PHIEUNHAP(MaPhieuNhap),
    CONSTRAINT FK_CT_PHIEUNHAP_BIENSANPHAM FOREIGN KEY (MaBienSanPham)
        REFERENCES dbo.BIENSANPHAM(MaBienSanPham),
    CONSTRAINT CK_CT_PHIEUNHAP_DonGia CHECK (DonGia >= 0),
    CONSTRAINT CK_CT_PHIEUNHAP_SoLuong CHECK (SoLuong > 0)
);
GO

CREATE TABLE dbo.DONHANG
(
    MaDonHang             varchar(20)      NOT NULL,
    MaKH                  varchar(10)      NULL,
    MaNVXuLy              varchar(10)      NULL,
    NgayDat               datetime2(0)     NOT NULL CONSTRAINT DF_DONHANG_NgayDat DEFAULT (SYSDATETIME()),
    NgayCapNhat           datetime2(0)     NOT NULL CONSTRAINT DF_DONHANG_NgayCapNhat DEFAULT (SYSDATETIME()),
    TrangThaiDonHang      varchar(20)      NOT NULL CONSTRAINT DF_DONHANG_TrangThai DEFAULT ('CHO_XAC_NHAN'),
    PhuongThucThanhToan   varchar(20)      NOT NULL CONSTRAINT DF_DONHANG_PTTT DEFAULT ('COD'),
    TrangThaiThanhToan    varchar(20)      NOT NULL CONSTRAINT DF_DONHANG_TTTT DEFAULT ('CHUA_THANH_TOAN'),
    TenNguoiNhan          nvarchar(100)    NOT NULL,
    SoDienThoaiNhan       varchar(15)      NOT NULL,
    DiaChiGiaoHang        nvarchar(300)    NOT NULL,
    TamTinh               decimal(18,2)    NOT NULL CONSTRAINT DF_DONHANG_TamTinh DEFAULT (0),
    PhiVanChuyen           decimal(18,2)    NOT NULL CONSTRAINT DF_DONHANG_PhiVanChuyen DEFAULT (0),
    GiamGia                decimal(18,2)    NOT NULL CONSTRAINT DF_DONHANG_GiamGia DEFAULT (0),
    TongTien               decimal(18,2)    NOT NULL CONSTRAINT DF_DONHANG_TongTien DEFAULT (0),
    GhiChu                 nvarchar(500)    NULL,
    LyDoHuy                nvarchar(500)    NULL,
    CONSTRAINT PK_DONHANG PRIMARY KEY (MaDonHang),
    CONSTRAINT FK_DONHANG_KHACHHANG FOREIGN KEY (MaKH)
        REFERENCES dbo.KHACHHANG(MaKH),
    CONSTRAINT FK_DONHANG_NHANVIEN FOREIGN KEY (MaNVXuLy)
        REFERENCES dbo.NHANVIEN(MaNV),
    CONSTRAINT CK_DONHANG_TrangThai CHECK (TrangThaiDonHang IN ('CHO_XAC_NHAN', 'DA_XAC_NHAN', 'DANG_GIAO', 'DA_GIAO', 'DA_HUY')),
    CONSTRAINT CK_DONHANG_PTTT CHECK (PhuongThucThanhToan IN ('COD', 'CHUYEN_KHOAN')),
    CONSTRAINT CK_DONHANG_TTTT CHECK (TrangThaiThanhToan IN ('CHUA_THANH_TOAN', 'DA_THANH_TOAN', 'HOAN_TIEN')),
    CONSTRAINT CK_DONHANG_Tien CHECK (TamTinh >= 0 AND PhiVanChuyen >= 0 AND GiamGia >= 0 AND TongTien >= 0)
);
GO

CREATE TABLE dbo.CHITIETDONHANG
(
    MaDonHang       varchar(20)      NOT NULL,
    MaBienSanPham   varchar(30)      NOT NULL,
    TenSanPham      nvarchar(200)    NOT NULL,
    DonGia          decimal(18,2)    NOT NULL,
    SoLuong         int              NOT NULL,
    GiamGia         decimal(18,2)    NOT NULL CONSTRAINT DF_CT_DONHANG_GiamGia DEFAULT (0),
    ThanhTien AS CONVERT(decimal(18,2), (DonGia * SoLuong) - GiamGia) PERSISTED,
    CONSTRAINT PK_CHITIETDONHANG PRIMARY KEY (MaDonHang, MaBienSanPham),
    CONSTRAINT FK_CT_DONHANG_DONHANG FOREIGN KEY (MaDonHang)
        REFERENCES dbo.DONHANG(MaDonHang),
    CONSTRAINT FK_CT_DONHANG_BIENSANPHAM FOREIGN KEY (MaBienSanPham)
        REFERENCES dbo.BIENSANPHAM(MaBienSanPham),
    CONSTRAINT CK_CT_DONHANG_DonGia CHECK (DonGia >= 0),
    CONSTRAINT CK_CT_DONHANG_SoLuong CHECK (SoLuong > 0),
    CONSTRAINT CK_CT_DONHANG_GiamGia CHECK (GiamGia >= 0 AND GiamGia <= DonGia * SoLuong)
);
GO

CREATE TABLE dbo.GIAODICHKHO
(
    MaGiaoDich     bigint IDENTITY(1,1) NOT NULL,
    MaBienSanPham  varchar(30)      NOT NULL,
    LoaiGiaoDich   varchar(25)      NOT NULL,
    SoLuong        int              NOT NULL,
    SoLuongTruoc   int              NOT NULL,
    SoLuongSau     int              NOT NULL,
    MaPhieuNhap    varchar(20)      NULL,
    MaDonHang      varchar(20)      NULL,
    MaNV           varchar(10)      NULL,
    NgayGiaoDich   datetime2(0)     NOT NULL CONSTRAINT DF_GIAODICHKHO_Ngay DEFAULT (SYSDATETIME()),
    GhiChu         nvarchar(500)    NULL,
    CONSTRAINT PK_GIAODICHKHO PRIMARY KEY (MaGiaoDich),
    CONSTRAINT FK_GIAODICHKHO_BIENSANPHAM FOREIGN KEY (MaBienSanPham)
        REFERENCES dbo.BIENSANPHAM(MaBienSanPham),
    CONSTRAINT FK_GIAODICHKHO_PHIEUNHAP FOREIGN KEY (MaPhieuNhap)
        REFERENCES dbo.PHIEUNHAP(MaPhieuNhap),
    CONSTRAINT FK_GIAODICHKHO_DONHANG FOREIGN KEY (MaDonHang)
        REFERENCES dbo.DONHANG(MaDonHang),
    CONSTRAINT FK_GIAODICHKHO_NHANVIEN FOREIGN KEY (MaNV)
        REFERENCES dbo.NHANVIEN(MaNV),
    CONSTRAINT CK_GIAODICHKHO_Loai CHECK (LoaiGiaoDich IN ('NHAP', 'BAN', 'TRA_HANG', 'DIEU_CHINH_TANG', 'DIEU_CHINH_GIAM')),
    CONSTRAINT CK_GIAODICHKHO_SoLuong CHECK (SoLuong > 0),
    CONSTRAINT CK_GIAODICHKHO_Ton CHECK (SoLuongTruoc >= 0 AND SoLuongSau >= 0)
);
GO

/* ================================================================
   4. GIỎ HÀNG, LỊCH SỬ ĐƠN HÀNG VÀ ĐÁNH GIÁ
   ================================================================ */

CREATE TABLE dbo.GIOHANG
(
    MaGioHang      int IDENTITY(1,1) NOT NULL,
    MaTaiKhoan     int              NOT NULL,
    NgayTao        datetime2(0)     NOT NULL CONSTRAINT DF_GIOHANG_NgayTao DEFAULT (SYSDATETIME()),
    NgayCapNhat    datetime2(0)     NOT NULL CONSTRAINT DF_GIOHANG_NgayCapNhat DEFAULT (SYSDATETIME()),
    CONSTRAINT PK_GIOHANG PRIMARY KEY (MaGioHang),
    CONSTRAINT UQ_GIOHANG_TaiKhoan UNIQUE (MaTaiKhoan),
    CONSTRAINT FK_GIOHANG_TAIKHOAN FOREIGN KEY (MaTaiKhoan)
        REFERENCES dbo.TAIKHOAN(MaTaiKhoan)
);
GO

CREATE TABLE dbo.CHITIETGIOHANG
(
    MaGioHang      int              NOT NULL,
    MaBienSanPham  varchar(30)      NOT NULL,
    SoLuong        int              NOT NULL,
    NgayCapNhat    datetime2(0)     NOT NULL CONSTRAINT DF_CT_GIOHANG_NgayCapNhat DEFAULT (SYSDATETIME()),
    CONSTRAINT PK_CHITIETGIOHANG PRIMARY KEY (MaGioHang, MaBienSanPham),
    CONSTRAINT FK_CT_GIOHANG_GIOHANG FOREIGN KEY (MaGioHang)
        REFERENCES dbo.GIOHANG(MaGioHang) ON DELETE CASCADE,
    CONSTRAINT FK_CT_GIOHANG_BIENSANPHAM FOREIGN KEY (MaBienSanPham)
        REFERENCES dbo.BIENSANPHAM(MaBienSanPham),
    CONSTRAINT CK_CT_GIOHANG_SoLuong CHECK (SoLuong > 0)
);
GO

CREATE TABLE dbo.LICHSUTRANGTHAIDONHANG
(
    MaLichSu       bigint IDENTITY(1,1) NOT NULL,
    MaDonHang      varchar(20)      NOT NULL,
    TrangThaiCu    varchar(20)      NULL,
    TrangThaiMoi   varchar(20)      NOT NULL,
    MaTaiKhoan     int              NULL,
    ThoiGian       datetime2(0)     NOT NULL CONSTRAINT DF_LICHSUDONHANG_ThoiGian DEFAULT (SYSDATETIME()),
    GhiChu         nvarchar(500)    NULL,
    CONSTRAINT PK_LICHSUTRANGTHAI PRIMARY KEY (MaLichSu),
    CONSTRAINT FK_LICHSU_DONHANG FOREIGN KEY (MaDonHang)
        REFERENCES dbo.DONHANG(MaDonHang),
    CONSTRAINT FK_LICHSU_TAIKHOAN FOREIGN KEY (MaTaiKhoan)
        REFERENCES dbo.TAIKHOAN(MaTaiKhoan)
);
GO

CREATE TABLE dbo.DANHGIA
(
    MaDanhGia      bigint IDENTITY(1,1) NOT NULL,
    MaDonHang      varchar(20)      NOT NULL,
    MaKH           varchar(10)      NOT NULL,
    MaBienSanPham  varchar(30)      NOT NULL,
    SoSao          tinyint          NOT NULL,
    NoiDung        nvarchar(1000)   NULL,
    NgayDanhGia    datetime2(0)     NOT NULL CONSTRAINT DF_DANHGIA_Ngay DEFAULT (SYSDATETIME()),
    TrangThai      bit              NOT NULL CONSTRAINT DF_DANHGIA_TrangThai DEFAULT (1),
    CONSTRAINT PK_DANHGIA PRIMARY KEY (MaDanhGia),
    CONSTRAINT UQ_DANHGIA_DonHangSanPham UNIQUE (MaDonHang, MaKH, MaBienSanPham),
    CONSTRAINT FK_DANHGIA_DONHANG FOREIGN KEY (MaDonHang)
        REFERENCES dbo.DONHANG(MaDonHang),
    CONSTRAINT FK_DANHGIA_KHACHHANG FOREIGN KEY (MaKH)
        REFERENCES dbo.KHACHHANG(MaKH),
    CONSTRAINT FK_DANHGIA_BIENSANPHAM FOREIGN KEY (MaBienSanPham)
        REFERENCES dbo.BIENSANPHAM(MaBienSanPham),
    CONSTRAINT CK_DANHGIA_SoSao CHECK (SoSao BETWEEN 1 AND 5)
);
GO

/* ================================================================
   5. INDEX PHỤC VỤ TÌM KIẾM VÀ BÁO CÁO
   ================================================================ */

CREATE INDEX IX_SANPHAM_MaLoai ON dbo.SANPHAM(MaLoai);
CREATE INDEX IX_SANPHAM_MaThuongHieu ON dbo.SANPHAM(MaThuongHieu);
CREATE INDEX IX_SANPHAM_Ten ON dbo.SANPHAM(TenSanPham);
CREATE INDEX IX_BIENSANPHAM_MaSanPham ON dbo.BIENSANPHAM(MaSanPham);
CREATE INDEX IX_TAIKHOAN_VaiTroTrangThai ON dbo.TAIKHOAN(MaVaiTro, TrangThai);
CREATE INDEX IX_TONKHO_SoLuongTon ON dbo.TONKHO(SoLuongTon);
CREATE INDEX IX_DONHANG_TrangThaiNgayDat ON dbo.DONHANG(TrangThaiDonHang, NgayDat);
CREATE INDEX IX_DONHANG_MaKH ON dbo.DONHANG(MaKH);
CREATE INDEX IX_CHITIETDONHANG_BienSanPham ON dbo.CHITIETDONHANG(MaBienSanPham);
CREATE INDEX IX_CHITIETPHIEUNHAP_BienSanPham ON dbo.CHITIETPHIEUNHAP(MaBienSanPham);
CREATE INDEX IX_GIAODICHKHO_Ngay ON dbo.GIAODICHKHO(NgayGiaoDich);
CREATE INDEX IX_GIAODICHKHO_SanPham ON dbo.GIAODICHKHO(MaBienSanPham, NgayGiaoDich);
CREATE INDEX IX_LICHSU_DonHangThoiGian ON dbo.LICHSUTRANGTHAIDONHANG(MaDonHang, ThoiGian);
CREATE INDEX IX_DANHGIA_BienSanPhamTrangThai ON dbo.DANHGIA(MaBienSanPham, TrangThai);
GO

/* ================================================================
   6. DỮ LIỆU MẪU
   ================================================================ */

INSERT dbo.VAITRO (MaVaiTro, TenVaiTro, MoTa)
VALUES
    ('ADMIN',     N'Quản trị viên', N'Toàn quyền hệ thống'),
    ('NHANVIEN',  N'Nhân viên',     N'Xử lý sản phẩm, nhập hàng và đơn hàng'),
    ('KHACHHANG', N'Khách hàng',    N'Mua hàng và theo dõi đơn hàng');
GO

INSERT dbo.TAIKHOAN (TenDangNhap, Email, MatKhauHash, MaVaiTro)
VALUES
    ('admin',    'admin@mobilehub.local',    N'AJJJlitmvhjOzIgxJFoo1vuXdAOk1Bn3pKeFZ6YhZLOGAcZ7PIiSDjWtF3XnqyDnvA==',    'ADMIN'),
    ('nhanvien', 'nhanvien@mobilehub.local', N'APAa/RzxPrGOL43jooDLxux6rYmNz6qvTlCJKYwUAOCsf0fJzXrUV5WjndbzmE/Cjw==',    'NHANVIEN'),
    ('khachhang','khachhang@mobilehub.local',N'AJgimPWmZu+LZ4Gp9BwyyltFZe92aUiCwA4OZHcUJ8E9DG2SJQFBurmsFBwMRm1YiA==', 'KHACHHANG');
GO

INSERT dbo.NHANVIEN (MaNV, TenNV, NamSinh, DienThoai, Email, HinhAnh, GioiTinh, MaTaiKhoan)
SELECT 'NV001', N'Nguyễn Quản Trị', 1995, '0901111111', 'admin@mobilehub.local', N'/images/nv01.png', N'Nam', MaTaiKhoan
FROM dbo.TAIKHOAN WHERE TenDangNhap = 'admin'
UNION ALL
SELECT 'NV002', N'Trần Nhân Viên', 1998, '0902222222', 'nhanvien@mobilehub.local', N'/images/nv02.png', N'Nam', MaTaiKhoan
FROM dbo.TAIKHOAN WHERE TenDangNhap = 'nhanvien';
GO

INSERT dbo.KHACHHANG (MaKH, TenKH, Email, DienThoai, DiaChi, MaTaiKhoan)
SELECT 'KH001', N'Hoàng Gia Bảo', 'khachhang@mobilehub.local', '0987654321', N'TP. Hồ Chí Minh', MaTaiKhoan
FROM dbo.TAIKHOAN WHERE TenDangNhap = 'khachhang';

INSERT dbo.KHACHHANG (MaKH, TenKH, Email, DienThoai, DiaChi)
VALUES
    ('KH002', N'Nguyễn Văn An', 'an@example.com', '0911223344', N'Quận 1, TP. Hồ Chí Minh'),
    ('KH003', N'Trần Minh Anh', 'anh@example.com', '0909887766', N'Quận Hải Châu, Đà Nẵng');
GO

INSERT dbo.LOAISANPHAM (MaLoai, TenLoai, MoTa)
VALUES
    ('DT01', N'Điện thoại thông minh', N'Điện thoại Android và iPhone'),
    ('PK01', N'Phụ kiện điện thoại', N'Cáp, sạc, tai nghe và phụ kiện');

INSERT dbo.THUONGHIEU (MaThuongHieu, TenThuongHieu, MoTa)
VALUES
    ('TH01', N'Apple',   N'iPhone và các sản phẩm Apple'),
    ('TH02', N'Samsung', N'Điện thoại Samsung Galaxy'),
    ('TH03', N'Xiaomi',  N'Điện thoại Xiaomi'),
    ('TH04', N'OPPO',    N'Điện thoại OPPO'),
    ('TH05', N'Anker',   N'Phụ kiện Anker');

INSERT dbo.NHACUNGCAP (MaNCC, TenNCC, DiaChi, DienThoai, Email)
VALUES
    ('NCC01', N'Công ty Apple Việt Nam', N'TP. Hồ Chí Minh', '0812345678', 'apple@ncc.local'),
    ('NCC02', N'Tổng kho Samsung Việt Nam', N'TP. Hồ Chí Minh', '0898765432', 'samsung@ncc.local'),
    ('NCC03', N'Nhà phân phối Xiaomi', N'Hà Nội', '0905556677', 'xiaomi@ncc.local'),
    ('NCC04', N'Nhà phân phối OPPO', N'Đà Nẵng', '0934889900', 'oppo@ncc.local'),
    ('NCC05', N'Nhà phân phối phụ kiện Anker', N'TP. Hồ Chí Minh', '0911223344', 'anker@ncc.local');
GO

INSERT dbo.SANPHAM (MaSanPham, TenSanPham, MaLoai, MaThuongHieu, MaNCCMacDinh, MoTa, BaoHanhThang)
VALUES
    ('SP001', N'iPhone 15',             'DT01', 'TH01', 'NCC01', N'iPhone 15 chính hãng, hiệu năng cao, camera chất lượng.', 12),
    ('SP002', N'Samsung Galaxy S24',    'DT01', 'TH02', 'NCC02', N'Flagship Samsung Galaxy S24.', 12),
    ('SP003', N'Xiaomi Redmi Note 13',  'DT01', 'TH03', 'NCC03', N'Smartphone Xiaomi tầm trung.', 12),
    ('SP004', N'OPPO Reno 11',          'DT01', 'TH04', 'NCC04', N'Smartphone OPPO thiết kế hiện đại.', 12),
    ('SP005', N'Cáp sạc nhanh Anker',   'PK01', 'TH05', 'NCC05', N'Cáp sạc nhanh USB-C Anker.', 6);
GO

INSERT dbo.BIENSANPHAM
    (MaBienSanPham, MaSanPham, SKU, MauSac, RAMGB, BoNhoGB, GiaNhap, GiaBan, HinhAnhUrl)
VALUES
    ('SP001-128-BLK', 'SP001', 'IPH15-128-BLK', N'Đen',  6, 128, 18000000, 20990000, N'/images/iphone15-black.png'),
    ('SP001-256-BLU', 'SP001', 'IPH15-256-BLU', N'Xanh', 6, 256, 21000000, 23990000, N'/images/iphone15-blue.png'),
    ('SP002-256-BLK', 'SP002', 'SGS24-256-BLK', N'Đen',  8, 256, 15000000, 17990000, N'/images/galaxy-s24-black.png'),
    ('SP003-128-BLU', 'SP003', 'RN13-128-BLU',  N'Xanh', 8, 128,  6500000,  7990000, N'/images/redmi-note13-blue.png'),
    ('SP004-256-GRN', 'SP004', 'RNO11-256-GRN',  N'Xanh', 8, 256,  9000000, 10990000, N'/images/oppo-reno11-green.png'),
    ('SP005-09-BLK',  'SP005', 'ANK-CABLE-09',   N'Đen',  NULL, NULL,  180000,   290000, N'/images/anker-cable.png');
GO

INSERT dbo.TONKHO (MaBienSanPham, SoLuongTon)
VALUES
    ('SP001-128-BLK', 4),
    ('SP001-256-BLU', 3),
    ('SP002-256-BLK', 5),
    ('SP003-128-BLU', 8),
    ('SP004-256-GRN', 6),
    ('SP005-09-BLK', 50);
GO

INSERT dbo.PHIEUNHAP (MaPhieuNhap, MaNCC, MaNV, NgayNhap, TrangThai, TongTien, GhiChu)
VALUES
    ('PN0001', 'NCC01', 'NV002', '2026-10-01', 'DA_NHAP', 343000000, N'Nhập lô điện thoại và phụ kiện mẫu');
GO

INSERT dbo.CHITIETPHIEUNHAP (MaPhieuNhap, MaBienSanPham, DonGia, SoLuong)
VALUES
    ('PN0001', 'SP001-128-BLK', 18000000, 5),
    ('PN0001', 'SP001-256-BLU', 21000000, 3),
    ('PN0001', 'SP002-256-BLK', 15000000, 5),
    ('PN0001', 'SP003-128-BLU',  6500000, 8),
    ('PN0001', 'SP004-256-GRN',  9000000, 6),
    ('PN0001', 'SP005-09-BLK',   180000, 50);
GO

INSERT dbo.DONHANG
    (MaDonHang, MaKH, MaNVXuLy, NgayDat, NgayCapNhat, TrangThaiDonHang,
     PhuongThucThanhToan, TrangThaiThanhToan, TenNguoiNhan, SoDienThoaiNhan,
     DiaChiGiaoHang, TamTinh, PhiVanChuyen, GiamGia, TongTien, GhiChu)
VALUES
    ('DH0001', 'KH001', 'NV001', '2026-10-05', '2026-10-06', 'DA_GIAO',
     'COD', 'DA_THANH_TOAN', N'Hoàng Gia Bảo', '0987654321',
     N'TP. Hồ Chí Minh', 20990000, 0, 0, 20990000, N'Đơn hàng mẫu đã giao thành công');
GO

INSERT dbo.CHITIETDONHANG (MaDonHang, MaBienSanPham, TenSanPham, DonGia, SoLuong, GiamGia)
VALUES
    ('DH0001', 'SP001-128-BLK', N'iPhone 15 - 128GB - Đen', 20990000, 1, 0);
GO

INSERT dbo.GIAODICHKHO
    (MaBienSanPham, LoaiGiaoDich, SoLuong, SoLuongTruoc, SoLuongSau, MaPhieuNhap, MaNV, GhiChu)
VALUES
    ('SP001-128-BLK', 'NHAP', 5, 0, 5, 'PN0001', 'NV002', N'Nhập hàng mẫu'),
    ('SP001-256-BLU', 'NHAP', 3, 0, 3, 'PN0001', 'NV002', N'Nhập hàng mẫu'),
    ('SP002-256-BLK', 'NHAP', 5, 0, 5, 'PN0001', 'NV002', N'Nhập hàng mẫu'),
    ('SP003-128-BLU', 'NHAP', 8, 0, 8, 'PN0001', 'NV002', N'Nhập hàng mẫu'),
    ('SP004-256-GRN', 'NHAP', 6, 0, 6, 'PN0001', 'NV002', N'Nhập hàng mẫu'),
    ('SP005-09-BLK',  'NHAP', 50, 0, 50, 'PN0001', 'NV002', N'Nhập hàng mẫu'),
    ('SP001-128-BLK', 'BAN', 1, 5, 4, NULL, 'NV001', N'Bán theo đơn DH0001');
GO

INSERT dbo.LICHSUTRANGTHAIDONHANG (MaDonHang, TrangThaiCu, TrangThaiMoi, MaTaiKhoan, GhiChu)
SELECT 'DH0001', NULL, 'CHO_XAC_NHAN', MaTaiKhoan, N'Tạo đơn hàng mẫu'
FROM dbo.TAIKHOAN WHERE TenDangNhap = 'khachhang'
UNION ALL
SELECT 'DH0001', 'CHO_XAC_NHAN', 'DA_XAC_NHAN', MaTaiKhoan, N'Nhân viên xác nhận đơn'
FROM dbo.TAIKHOAN WHERE TenDangNhap = 'admin'
UNION ALL
SELECT 'DH0001', 'DA_XAC_NHAN', 'DANG_GIAO', MaTaiKhoan, N'Bàn giao cho đơn vị vận chuyển'
FROM dbo.TAIKHOAN WHERE TenDangNhap = 'admin'
UNION ALL
SELECT 'DH0001', 'DANG_GIAO', 'DA_GIAO', MaTaiKhoan, N'Giao hàng thành công'
FROM dbo.TAIKHOAN WHERE TenDangNhap = 'admin';
GO

INSERT dbo.GIOHANG (MaTaiKhoan)
SELECT MaTaiKhoan FROM dbo.TAIKHOAN WHERE TenDangNhap = 'khachhang';
GO

INSERT dbo.DANHGIA (MaDonHang, MaKH, MaBienSanPham, SoSao, NoiDung)
VALUES
    ('DH0001', 'KH001', 'SP001-128-BLK', 5, N'Sản phẩm chính hãng, giao hàng nhanh.');
GO

/* ================================================================
   7. KIỂM TRA NHANH SAU KHI KHỞI TẠO
   ================================================================ */

SELECT TABLE_NAME AS TenBang
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_TYPE = 'BASE TABLE'
ORDER BY TABLE_NAME;
GO

SELECT
    sp.MaSanPham,
    sp.TenSanPham,
    th.TenThuongHieu,
    bsp.SKU,
    bsp.GiaBan,
    tk.SoLuongTon
FROM dbo.SANPHAM sp
INNER JOIN dbo.THUONGHIEU th ON th.MaThuongHieu = sp.MaThuongHieu
INNER JOIN dbo.BIENSANPHAM bsp ON bsp.MaSanPham = sp.MaSanPham
INNER JOIN dbo.TONKHO tk ON tk.MaBienSanPham = bsp.MaBienSanPham
ORDER BY sp.MaSanPham, bsp.SKU;
GO

SELECT
    dh.MaDonHang,
    kh.TenKH,
    dh.TrangThaiDonHang,
    dh.TrangThaiThanhToan,
    dh.TongTien
FROM dbo.DONHANG dh
LEFT JOIN dbo.KHACHHANG kh ON kh.MaKH = dh.MaKH
ORDER BY dh.NgayDat DESC;
GO

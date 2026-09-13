# 🎖️ HỆ THỐNG QUẢN LÝ HỌC VIÊN QUÂN SỰ (QL_HocVien)
### *Military Academy Cadet Management System - Dual-Theme Tactical Edition*

<p align="center">
  <img src="Resources/app_icon.png" alt="Logo QL_HocVien" width="120" height="120" />
</p>

<p align="center">
  <a href="https://github.com/NguyenTan33/QL_HocVien/releases/latest"><img src="https://img.shields.io/badge/Release-v1.4.4-blue.svg?style=for-the-badge&logo=github" alt="Latest Release" /></a>
  <a href="https://dotnet.microsoft.com/download/dotnet/8.0"><img src="https://img.shields.io/badge/.NET-8.0%20WPF-512BD4.svg?style=for-the-badge&logo=dotnet" alt=".NET 8.0 WPF" /></a>
  <a href="https://www.zetetic.net/sqlcipher/"><img src="https://img.shields.io/badge/Security-SQLCipher%20AES--256-green.svg?style=for-the-badge&logo=lock" alt="SQLCipher AES-256" /></a>
  <a href="https://github.com/NguyenTan33/QL_HocVien/actions"><img src="https://img.shields.io/badge/Tests-126%2F126%20Passed-brightgreen.svg?style=for-the-badge&logo=checkmarx" alt="Tests 126 Passed" /></a>
  <a href="https://www.microsoft.com/windows"><img src="https://img.shields.io/badge/Platform-Windows%20x64-0078D6.svg?style=for-the-badge&logo=windows" alt="Platform Windows" /></a>
  <a href="LICENSE"><img src="https://img.shields.io/badge/License-Internal%20Military-critical.svg?style=for-the-badge" alt="License" /></a>
</p>

---

## 📌 MỤC LỤC
- [Tổng Quan Hệ Thống](#-tổng-quan-hệ-thống)
- [Tính Năng Đột Phá & Trọng Tâm](#-tính-năng-đột-phá--trọng-tâm)
- [Cơ Cấu Đơn Vị Quân Đội Đa Cấp (7 Cấp Độ)](#-cơ-cấu-đơn-vị-quân-đội-đa-cấp-7-cấp-độ)
- [Quản Lý Khóa Học & Niên Khóa Đào Tạo](#-quản-lý-khóa-học--niên-khóa-đào-tạo)
- [Engine Giao Diện Kép (Dual-Theme Engine v1.4)](#-engine-giao-diện-kép-dual-theme-engine-v14)
  - [1. Chế Độ Tác Chiến (Combat Tactical Mode)](#1-chế-độ-tác-chiến-combat-tactical-mode)
  - [2. Chế Độ Hành Chính (Administrative Mode)](#2-chế-độ-hành-chính-administrative-mode)
- [Tiêu Chuẩn Quân Sự & Nghiệp Vụ Đào Tạo](#-tiêu-chuẩn-quân-sự--nghiệp-vụ-đào-tạo)
  - [Chuẩn Thể Lực TT 32/2009/TTLT-BQP-BVHTTDL](#chuẩn-thể-lực-tt-322009ttlt-bqp-bvhttdl)
  - [Hệ Thống Điểm Tín Chỉ, Xếp Loại & Thao Tác Hàng Loạt](#hệ-thống-điểm-tín-chỉ-xếp-loại--thao-tác-hàng-loạt)
- [Kiến Trúc Bảo Mật Cấp Quân Sự](#-kiến-trúc-bảo-mật-cấp-quân-sự)
- [Hướng Dẫn Cài Đặt & Sử Dụng](#-hướng-dẫn-cài-đặt--sử-dụng)
  - [Tải và Chạy Ứng Dụng (Portable 1 File)](#tải-và-chạy-ứng-dụng-portable-1-file)
  - [Tài Khoản Đăng Nhập Mặc Định](#tài-khoản-đăng-nhập-mặc-định)
  - [Khóa Bảo Mật Cấp 2 (Security Gate Passkeys)](#khóa-bảo-mật-cấp-2-security-gate-passkeys)
- [Hướng Dẫn Biên Dịch Từ Mã Nguồn (Developer Guide)](#-hướng-dẫn-biên-dịch-từ-mã-nguồn-developer-guide)
- [Cấu Trúc Thư Mục Dự Án](#-cấu-trúc-thư-mục-dự-án)
- [Ngăn Xếp Công Nghệ (Tech Stack)](#-ngăn-xếp-công-nghệ-tech-stack)
- [Nhật Ký Cập Nhật (Changelog)](#-nhật-ký-cập-nhật-changelog)
- [Tác Giả & Bản Quyền](#-tác-giả--bản-quyền)

---

## 📖 TỔNG QUAN HỆ THỐNG

**QL_HocVien** là hệ thống giải pháp phần mềm chuyên biệt chuẩn hóa công tác quản trị học viên, cán bộ quản lý, cơ cấu tổ chức đơn vị quân đội và theo dõi toàn diện kết quả huấn luyện - đào tạo tại các học viện, trường sĩ quan quân đội. Phần mềm được xây dựng trên nền tảng **.NET 8.0 WPF** hiện đại, tuân thủ nghiêm ngặt các quy chuẩn văn thư quốc phòng và an toàn thông tin quân sự:

- **100% Offline-First & Self-Contained:** Vận hành hoàn toàn độc lập, không yêu cầu kết nối Internet khi tác nghiệp thực địa thao trường; toàn bộ thư viện, biểu tượng và engine đồ họa được đóng gói trong một file thực thi duy nhất (`Single-file executable`).
- **Quản lý Khóa học & Niên khóa (Cohorts):** Tự động bóc tách và phân tích Khóa từ mã học viên dạng quân đội (`DH.075.299` $\rightarrow$ Khóa K75), liên kết chặt chẽ Khóa học - Lớp học - Quân số biên chế.
- **Cây Tổ chức Đơn vị Đa cấp (7 Levels):** Quản lý mô hình tổ chức quân sự phân tầng từ cấp Khóa đào tạo $\rightarrow$ Trung đoàn $\rightarrow$ Tiểu đoàn $\rightarrow$ Đại đội $\rightarrow$ Trung đội $\rightarrow$ Tiểu đội $\rightarrow$ Nhóm/Tổ chiến đấu với khả năng đếm quân số đệ quy tức thời.
- **Chuẩn hóa Thông tư liên tịch 32/2009/TTLT-BQP-BVHTTDL:** Tự động hóa đánh giá và xếp loại tiêu chuẩn rèn luyện thể lực cán bộ, chiến sĩ, học viên theo độ tuổi và giới tính.
- **Quy chế Đào tạo Tín chỉ Quân sự:** Tính toán điểm trung bình học kỳ (TBM), điểm tích lũy thang 10 & thang 4, hỗ trợ thi nâng điểm, học lại, cảnh báo học vụ và xét tốt nghiệp sĩ quan.
- **Engine Giao diện Kép Độc lập (Dual-Theme Engine v1.4):** Chuyển đổi mượt mà giữa Chế độ Hành chính (sang trọng, chuẩn mực công sở) và Chế độ Tác chiến (Dark Tactical dã chiến với dải tranh nghệ thuật cờ sao vàng, chiến sĩ hành quân, tháp canh gác trên 11 trang chức năng và background chìm trang trọng dưới Quân hiệu).

---

## ⚡ TÍNH NĂNG ĐỘT PHÁ & TRỌNG TÂM

| # | Phân hệ nghiệp vụ | Chi tiết kỹ thuật & Tính năng nổi bật |
|:---:|---|---|
| **1** | 🌓 **Dual-Theme Engine v1.4** | Chuyển đổi linh hoạt giữa **Chế độ Hành chính** (White/Corporate Blue chuẩn nhận diện công vụ) và **Chế độ Tác chiến** (Dark Military Canvas, viền Neon Tactical, tích hợp tranh dã chiến hào hùng `flag_soldier_banner.png` trên 11 màn hình và background chìm dưới Quân hiệu sidebar). |
| **2** | 🎓 **Quản Lý Khóa Học & Niên Khóa** | Quản lý niên khóa (K75, K26...), năm nhập ngũ, thời gian đào tạo. Thuật toán thông minh tự động nhận diện khóa từ mã quân nhân (`DH.075.xxx` $\rightarrow$ `K75`) và phân nhóm lớp biên chế. |
| **3** | 🌲 **Cây Đơn Vị Đa Cấp (7 Tầng)** | Cấu trúc phân cấp toàn diện: Cấp 0 (Khóa) $\rightarrow$ Cấp 1 (Trung đoàn) $\rightarrow$ Cấp 2 (Tiểu đoàn) $\rightarrow$ Cấp 3 (Đại đội) $\rightarrow$ Cấp 4 (Trung đội) $\rightarrow$ Cấp 5 (Tiểu đội) $\rightarrow$ Cấp 6 (Nhóm/Tổ). Tự động thống kê quân số động. |
| **4** | 🎖️ **Thể Lực Quân Sự TT 32** | Tự động chấm điểm 4 môn tiêu chuẩn: Chạy 100m, Co tay xà đơn (hoặc chống đẩy), Bơi ếch 100m, Hành quân mang vác nặng / Chạy vũ trang 3000m. Xếp loại: Giỏi, Khá, Đạt, Không Đạt. |
| **5** | 📊 **TBM Tín Chỉ & Thao Tác Hàng Loạt** | Tính TBM học kỳ, TBM tích lũy thang 10 $\leftrightarrow$ thang 4 $\leftrightarrow$ điểm chữ (A, B, C, D, F). Tích hợp cột **CheckBox chọn nhiều** và **Chọn tất cả** tại Header để xử lý/xóa dữ liệu hàng loạt. |
| **6** | 📅 **Interactive Military Timeline** | Quản lý lịch huấn luyện, diễn tập tác chiến, bắn đạn thật, báo động hành quân, sinh hoạt chi bộ theo dòng thời gian tương tác trực quan với bộ lọc đa cấp theo đơn vị và phân loại sự kiện. |
| **7** | 🛡️ **Bảo Mật Đa Tầng SQLCipher** | Toàn bộ cơ sở dữ liệu SQLite được mã hóa **AES-256 bit CBC**. Khóa mã hóa sinh động kết hợp bảo vệ phần cứng máy tính chủ (`Windows DPAPI` + CPU & Motherboard Binding). |
| **8** | 🔐 **Security Gate Passkey** | Cơ chế khóa bảo mật cấp 2 bắt buộc khi can thiệp vào các tác vụ trọng yếu (sửa bảng điểm chính thức, xóa học viên, phê duyệt danh sách tốt nghiệp, phân quyền tài khoản). |
| **9** | 🛑 **Anti Brute-Force Lockout** | Tự động kích hoạt cơ chế khóa tài khoản lũy tiến theo cấp số nhân ($2^n \times 60s$) khi phát hiện đăng nhập sai liên tiếp vượt ngưỡng cảnh báo (ngăn chặn tấn công vét cạn mật khẩu). |
| **10** | 📑 **Excel 14 Sheets ClosedXML** | Hỗ trợ nhập (Import) và xuất (Export) báo cáo mẫu biểu quân sự 14 Sheets chuẩn quy định, bảo toàn định dạng cột, tiêu đề, công thức và màu sắc. |
| **11** | 🔄 **Auto-Update GitHub Release** | Tích hợp cơ chế kiểm tra phiên bản mới từ GitHub Releases qua API `version.json`, tự động thông báo Changelog và tải bản cập nhật an toàn. |

---

## 🌲 CƠ CẤU ĐƠN VỊ QUÂN ĐỘI ĐA CẤP (7 CẤP ĐỘ)

Hệ thống triển khai dịch vụ `UnitHierarchyService` và mô hình `UnitTreeNode` để chuẩn hóa tổ chức đơn vị quân đội theo 7 cấp độ nghiêm ngặt:

```
[Level 0] Khóa Học / Đợt Đào Tạo (Academic Cohort - VD: Khóa K75)
   └── [Level 1] Trung Đoàn / Hệ Quản Lý (Regiment / Military Branch)
          └── [Level 2] Tiểu Đoàn Quản Lý (Battalion - VD: Tiểu đoàn 1)
                 └── [Level 3] Đại Đội Học Viên (Company - VD: Đại đội 10)
                        └── [Level 4] Trung Đội (Platoon - VD: Trung đội 1)
                               └── [Level 5] Tiểu Đội (Squad - VD: Tiểu đội 1)
                                      └── [Level 6] Nhóm / Tổ Chiến Đấu (Fireteam / Section)
```

- **Thống kê đệ quy quân số:** Tự động tính toán tổng quân số từ cấp Tổ chiến đấu, Tiểu đội dồn lên Trung đội, Đại đội, Tiểu đoàn và toàn Khóa học theo thời gian thực.
- **Chuyển đổi biên chế an toàn:** Khi thay đổi tên hoặc điều chuyển đơn vị cấp trên, hệ thống tự động cập nhật liên kết của toàn bộ đơn vị và học viên cấp dưới, bảo toàn toàn vẹn dữ liệu.

---

## 🎓 QUẢN LÝ KHÓA HỌC & NIÊN KHÓA ĐÀO TẠO

Module **Quản Lý Khóa Học** (`CohortManagementViewModel` & `CohortManagementView`) giúp kiểm soát toàn bộ vòng đời học viên từ khi nhập ngũ đến khi tốt nghiệp ra trường:

- **Bóc tách mã quân nhân thông minh:**
  $$\text{Mã học viên: } \texttt{DH.075.299} \implies \text{Tự động phân loại vào: } \mathbf{Khóa\ K75}$$
- **Phân loại hệ đào tạo:** Bác sĩ Quân y, Dược sĩ, Sĩ quan Chỉ huy Tham mưu, Kỹ sư Quân sự.
- **Theo dõi tiến độ niên khóa:** Tự động đối chiếu năm bắt đầu và năm kết thúc đào tạo để cảnh báo các khóa sắp thi tốt nghiệp hoặc cần hoàn thành chỉ tiêu huấn luyện thể lực.
- **Bộ lọc tích hợp:** Lọc học viên, bảng điểm và kết quả rèn luyện đồng thời theo Khóa học và Lớp biên chế trực tiếp trên giao diện tác nghiệp.

---

## 🎨 ENGINE GIAO DIỆN KÉP (DUAL-THEME ENGINE v1.4)

### 1. Chế Độ Tác Chiến (Combat Tactical Mode)
*Được thiết kế chuyên biệt cho tác nghiệp ban đêm, môi trường dã chiến và trung tâm chỉ huy tác chiến, giảm thiểu độ chói mắt và tăng cường tính nhận diện quân đội hào hùng:*

- **Dải Tranh Nghệ Thuật Dã Chiến 11 Màn Hình:** Hình ảnh cờ đỏ sao vàng tung bay, đoàn quân bộ binh hành quân, tháp canh gác kiên cố và biểu ngữ **"QUYẾT THẮNG"** (`flag_soldier_banner.png`) được đồng bộ trên toàn bộ 11 phân hệ:
  1. *Bảng điều khiển tổng quan (Dashboard)*
  2. *Danh mục quân sự tập trung (Catalog Management)*
  3. *Quản lý hồ sơ học viên (Cadet Management)*
  4. *Quản lý khóa học & niên khóa (Cohort Management)*
  5. *Bảng điểm môn học tín chỉ (Credit Subject Scores)*
  6. *Rèn luyện thể lực quân sự (Physical Fitness TT32)*
  7. *Thi đua, khen thưởng & kỷ luật (Merit & Discipline)*
  8. *Lịch công tác & Dòng thời gian huấn luyện (Timeline)*
  9. *Quản lý kỳ thi & đợt đánh giá (Exam Management)*
  10. *Quản lý cán bộ quản lý (Officer Management)*
  11. *Cài đặt hệ thống & Bảo mật (System Settings)*
- **Lớp Nền Sidebar Chìm Trang Trọng:** Tại sidebar điều hướng bên trái, bức tranh nghệ thuật quân đội dã chiến đóng vai trò là **lớp nền Background chìm** phía dưới khối Quân hiệu Quân đội nhân dân Việt Nam và khẩu hiệu *"★ VÌ TỔ QUỐC XÃ HỘI CHỦ NGHĨA ★"*, tạo hiệu ứng chiều sâu sắc sảo và trang trọng.
- **Hộp Thông Tin & Thẻ Dữ Liệu Tương Phản Cao:** Đường viền Neon Tactical xanh lục quân đội (`#1E3A2F`), hiệu ứng đổ bóng phát sáng tinh tế, văn bản hiển thị rõ nét trong điều kiện thiếu sáng.

<table>
  <tr>
    <td width="50%">
      <p align="center"><b>🎯 Dashboard Chỉ Huy Tác Chiến</b></p>
      <img src="docs/screenshots/combat_dashboard.png" alt="Combat Dashboard" />
      <p align="center"><i>Theo dõi quân số SSCĐ, cảnh báo học vụ và tiến độ rèn luyện dã chiến</i></p>
    </td>
    <td width="50%">
      <p align="center"><b>⏱️ Lịch Huấn Luyện & Báo Động Tác Chiến</b></p>
      <img src="docs/screenshots/combat_timeline.png" alt="Combat Timeline" />
      <p align="center"><i>Timeline dã chiến theo dõi diễn tập, hành quân cơ động, bắn đạn thật</i></p>
    </td>
  </tr>
</table>

---

### 2. Chế Độ Hành Chính (Administrative Mode)
*Dành riêng cho môi trường cơ quan văn phòng, hội trường, phòng giao ban học vụ và in ấn báo cáo văn thư quốc phòng:*

- **Giao Diện Thanh Lịch, Sáng Sủa:** Sử dụng gam màu trắng tinh khôi kết hợp xanh công vụ (Corporate Navy Blue), tạo cảm giác nhẹ nhàng, tập trung cao độ khi xử lý khối lượng dữ liệu lớn.
- **Cách Ly Tuyệt Đối 100% Đồ Họa Dã Chiến:** Hoàn toàn không hiển thị dải tranh hay watermark nền, giữ nguyên vẹn sự trang nghiêm, ngăn nắp và chuyên nghiệp chuẩn mực công sở nhà nước.

<table>
  <tr>
    <td width="50%">
      <p align="center"><b>📊 Bảng Điều Khiển Tổng Quan (Admin Dashboard)</b></p>
      <img src="docs/screenshots/admin_dashboard.png" alt="Admin Dashboard" />
      <p align="center"><i>Phân bố học lực, thống kê quân số toàn trường và biểu đồ trực quan</i></p>
    </td>
    <td width="50%">
      <p align="center"><b>🏅 Bảng Điểm Tín Chỉ & Xếp Loại Học Lực</b></p>
      <img src="docs/screenshots/admin_credit.png" alt="Admin Credit" />
      <p align="center"><i>Tính TBM tích lũy, quy đổi điểm chữ và hỗ trợ thao tác chọn xóa hàng loạt</i></p>
    </td>
  </tr>
  <tr>
    <td width="50%">
      <p align="center"><b>📅 Dòng Thời Gian Huấn Luyện (Admin Timeline)</b></p>
      <img src="docs/screenshots/admin_timeline.png" alt="Admin Timeline" />
      <p align="center"><i>Lịch công tác tuần, kế hoạch giảng dạy lý thuyết và thực hành thao trường</i></p>
    </td>
    <td width="50%">
      <p align="center"><b>🏃 Rèn Luyện Thể Lực (Thông Tư 32)</b></p>
      <img src="docs/screenshots/admin_academic.png" alt="Admin Academic" />
      <p align="center"><i>Tra chuẩn 4 môn chiến sĩ khỏe tự động theo lứa tuổi và phân loại kết quả</i></p>
    </td>
  </tr>
</table>

---

## 🎖️ TIÊU CHUẨN QUÂN SỰ & NGHIỆP VỤ ĐÀO TẠO

### Chuẩn Thể Lực TT 32/2009/TTLT-BQP-BVHTTDL
Hệ thống tích hợp thuật toán tự động tra cứu bảng chuẩn và đánh giá kết quả kiểm tra thể lực định kỳ của quân nhân:
- **Nội dung 1: Chạy 100m** — Đánh giá tốc độ và sức bật của học viên.
- **Nội dung 2: Co tay xà đơn / Chống đẩy** — Đánh giá sức bền nhóm cơ thân trên và cơ tay.
- **Nội dung 3: Bơi ếch 100m** — Kiểm tra kỹ năng vận động dưới nước trong điều kiện trang bị nhẹ.
- **Nội dung 4: Chạy vũ trang 3000m / Hành quân mang vác nặng** — Đánh giá sức bền dẻo dai và ý chí chiến đấu.

```
Tiêu chuẩn phân nhóm tuổi quân nhân:
├── Nhóm 1: Dưới 27 tuổi
├── Nhóm 2: Từ 27 đến 35 tuổi
├── Nhóm 3: Từ 36 đến 45 tuổi
└── Nhóm 4: Trên 45 tuổi
```

### Hệ Thống Điểm Tín Chỉ, Xếp Loại & Thao Tác Hàng Loạt
Tuân thủ Quy chế đào tạo đại học, cao đẳng hệ chính quy theo học chế tín chỉ quân sự:
- **Điểm học phần:** Kết hợp Điểm chuyên cần (10%), Điểm kiểm tra thường xuyên (30%) và Điểm thi kết thúc học phần (60%).
- **Điểm trung bình chung tích lũy (TBM):**

$$\text{TBM} = \frac{\sum_{i=1}^{n} (\text{Điểm}_{i} \times \text{Số tín chỉ}_{i})}{\sum_{i=1}^{n} \text{Số tín chỉ}_{i}}$$

- **Bảng quy đổi thang điểm:**
  - **A (8.5 - 10.0)** $\rightarrow$ 4.0 (Xuất sắc / Giỏi)
  - **B (7.0 - 8.4)** $\rightarrow$ 3.0 (Khá)
  - **C (5.5 - 6.9)** $\rightarrow$ 2.0 (Trung bình)
  - **D (4.0 - 5.4)** $\rightarrow$ 1.0 (Trung bình yếu)
  - **F (< 4.0)** $\rightarrow$ 0.0 (Không đạt — Phải học lại)
- **Thao tác hàng loạt (Batch Actions):** Hỗ trợ cột `CheckBox` chọn từng dòng và `CheckBox` "Chọn tất cả" tại Header giúp xóa danh sách điểm, cập nhật trạng thái học vụ cho hàng trăm học viên cùng lúc chỉ với 1 click.

---

## 🔒 KIẾN TRÚC BẢO MẬT CẤP QUÂN SỰ

```
                          ┌───────────────────────────┐
                          │   XÁC THỰC NGƯỜI DÙNG     │
                          │   BCrypt (Work Factor 11) │
                          └─────────────┬─────────────┘
                                        │
                                        ▼
                          ┌───────────────────────────┐
                          │    SECURITY GATE CẤP 2    │
                          │   Passkey phê duyệt điểm  │
                          └─────────────┬─────────────┘
                                        │
                                        ▼
    ┌───────────────────────────────────────────────────────────────────┐
    │                 CSDL SQLite ĐƯỢC MÃ HÓA TOÀN PHẦN                 │
    │                 SQLCipher v2.1.8 — Thuật toán AES-256             │
    │  Dynamic Key: DPAPI Protected Data + Hardware CPU Binding String  │
    └───────────────────────────────────────────────────────────────────┘
```

1. **Mã hóa CSDL AES-256 bit:** Toàn bộ file dữ liệu được bảo vệ bằng SQLCipher. Nếu tệp tin bị sao chép trái phép ra khỏi thiết bị được cấp phép, dữ liệu hoàn toàn không thể giải mã.
2. **Khóa liên kết thiết bị cứng (Hardware Binding):** Chuỗi khóa giải mã được dẫn xuất từ định danh bo mạch chủ và vi xử lý thông qua `System.Management`, kết hợp bảo vệ qua `Windows DPAPI`.
3. **Mã hóa mật khẩu BCrypt:** Chống lại các cuộc tấn công Rainbow Table và GPU Cracking với muối ngẫu nhiên (salt) độ an toàn cao.
4. **Cơ chế chống Brute-force:** Khi người dùng nhập sai mật khẩu 5 lần, tài khoản bị tạm khóa 60 giây. Sau mỗi lần tiếp theo, thời gian khóa tăng gấp đôi ($2^n \times 60s$).

---

## 🚀 HƯỚNG DẪN CÀI ĐẶT & SỬ DỤNG

### Tải và Chạy Ứng Dụng (Portable 1 File)
1. Truy cập trang [GitHub Releases](https://github.com/NguyenTan33/QL_HocVien/releases/latest).
2. Tải về tệp thực thi duy nhất **`QL_HocVien.exe`**.
3. Chạy trực tiếp mà **không cần cài đặt thêm .NET SDK hoặc .NET Runtime** (ứng dụng đã tích hợp sẵn runtime).

### Tài Khoản Đăng Nhập Mặc Định
Sau khi khởi tạo CSDL lần đầu, hệ thống tạo sẵn các tài khoản sau:

| Phân Quyền | Tên Đăng Nhập | Mật Khẩu Mặc Định | Vai Trò & Giới Hạn Quyền |
|---|---|---|---|
| **Chỉ Huy / Quản Trị Viên** | `admin` | `Admin@123` | Toàn quyền hệ thống, cơ cấu đơn vị, khóa học, quản trị người dùng, sao lưu & phục hồi CSDL. |
| **Cán Bộ Quản Lý** | `canbo01` | `Canbo@123` | Quản lý hồ sơ học viên, nhập điểm tín chỉ, chấm điểm thể lực, lập lịch công tác tuần. |

> ⚠️ **Khuyến nghị bảo mật:** Đổi mật khẩu ngay sau lần đăng nhập đầu tiên tại menu *Thông tin cá nhân*.

### Khóa Bảo Mật Cấp 2 (Security Gate Passkeys)
Khi thực hiện các thao tác quan trọng (như sửa điểm đã chốt sổ hoặc xóa học viên), hệ thống sẽ yêu cầu nhập Passkey xác thực. Các mã Passkey mặc định được cấp:
- `QD-2026-HQ88-K01A`
- `QD-2026-TL32-B02C`
- `QD-2026-QS99-D03E`

---

## 🛠️ HƯỚNG DẪN BIÊN DỊCH TỪ MÃ NGUỒN (DEVELOPER GUIDE)

### Yêu Cầu Môi Trường
- **Hệ điều hành:** Windows 10/11 x64 (Bản build 19041 trở lên)
- **SDK:** [.NET 8.0 SDK (x64)](https://dotnet.microsoft.com/download/dotnet/8.0)
- **Công cụ khuyến nghị:** Visual Studio 2022 (với gói *.NET Desktop Development*) hoặc JetBrains Rider / VS Code.

### Các Bước Biên Dịch & Chạy

1. **Clone mã nguồn từ GitHub:**
   ```bash
   git clone https://github.com/NguyenTan33/QL_HocVien.git
   cd QL_HocVien
   ```

2. **Khôi phục các gói thư viện NuGet:**
   ```bash
   dotnet restore
   ```

3. **Chạy toàn bộ 126 bài kiểm thử đơn vị (Unit Tests):**
   ```bash
   dotnet test QL_HocVien.Tests/QL_HocVien.Tests.csproj
   ```
   *Kết quả mong đợi: `Test Run Successful. Total tests: 126, Passed: 126, Failed: 0, Skipped: 0`*

4. **Khởi chạy ứng dụng ở chế độ Debug:**
   ```bash
   dotnet run --project QL_HocVien.csproj
   ```

5. **Đóng gói xuất bản file duy nhất (Self-Contained Single File):**
   ```bash
   dotnet publish QL_HocVien.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -o ./publish
   ```
   *File thực thi hoàn chỉnh `QL_HocVien.exe` sẽ được tạo trong thư mục `./publish`.*

---

## 📂 CẤU TRÚC THƯ MỤC DỰ ÁN

```
QL_HocVien/
├── Assets/                    # Biểu tượng, icon SVG và tài nguyên hình ảnh
│   └── Images/                # Dải tranh dã chiến flag_soldier_banner.png, logo QĐNDVN...
├── Commands/                  # RelayCommand & AsyncRelayCommand triển khai MVVM
├── Data/                      # SQLite DbContext & Cấu hình mã hóa SQLCipher AES-256
├── docs/                      # Tài liệu hệ thống & hình ảnh chụp màn hình
│   └── screenshots/           # Ảnh chụp giao diện thực tế (Admin & Combat Mode)
├── Helpers/                   # Bộ chuyển đổi màu sắc, Converter định dạng dữ liệu XAML
├── Models/                    # Thực thể dữ liệu (HocVien, CanBo, AcademicCohort, UnitTreeNode...)
├── Resources/                 # Biểu tượng ứng dụng (.ico, .png), hình nền đăng nhập
├── Services/                  # Nghiệp vụ: Bảo mật, Khóa học, Cây đơn vị, Tính điểm, Excel
│   ├── CohortService.cs       # Quản lý khóa học & tự động nhận diện khóa từ mã học viên
│   ├── UnitHierarchyService.cs# Quản trị cơ cấu cây đơn vị quân sự 7 cấp độ
│   ├── ExcelExportService.cs  # Xử lý xuất báo cáo 14 sheets ClosedXML
│   ├── SecurityGateService.cs # Quản lý khóa Passkey cấp 2
│   ├── UpdateService.cs       # Kiểm tra cập nhật GitHub Releases qua version.json
│   └── ...
├── ViewModels/                # Trình điều khiển ViewModels (CommunityToolkit.Mvvm)
│   ├── DashboardViewModel.cs  # Thống kê phân tích số liệu trực quan
│   ├── CohortManagementViewModel.cs # Điều khiển quản lý khóa học & niên khóa
│   ├── CreditScoreViewModel.cs# Xử lý tính điểm tín chỉ, chọn nhiều & xếp loại
│   ├── TimelineViewModel.cs   # Quản lý dòng thời gian huấn luyện tương tác
│   └── ...
├── Views/                     # Giao diện XAML (Window, UserControl, Dialog)
│   ├── CohortManagementView.xaml # Màn hình quản lý khóa học đào tạo
│   ├── CreditSubjectManagementView.xaml # Màn hình điểm tín chỉ & checkbox chọn nhiều
│   └── ...
├── QL_HocVien.Tests/          # Bộ 126 Unit Tests kiểm thử tự động toàn diện (xUnit)
├── appsettings.json           # Cấu hình chuỗi kết nối và tham số ứng dụng
├── version.json               # Phiên bản phát hành v1.4.4 & liên kết cập nhật
└── QL_HocVien.csproj          # Tệp cấu hình dự án .NET 8.0 WPF
```

---

## 💻 NGĂN XẾP CÔNG NGHỆ (TECH STACK)

| Lớp | Công Nghệ / Thư Viện | Phiên Bản | Mục Đích Sử Dụng |
|---|---|:---:|---|
| **Framework** | `.NET 8.0 Windows (WPF)` | `8.0` | Ứng dụng Desktop hiệu năng cao, giao diện mượt mà, tự tích hợp runtime |
| **Kiến Trúc UI** | `MVVM Pattern` | - | Phân tách rạch ròi giữa View, ViewModel và Model |
| **MVVM Toolkit** | `CommunityToolkit.Mvvm` | `8.4.2` | Triển khai `ObservableObject`, `RelayCommand` gọn gàng, bất đồng bộ |
| **Cơ Sở Dữ Liệu** | `SQLite with SQLCipher` | `2.1.8` | Lưu trữ dữ liệu an toàn, mã hóa toàn phần AES-256 bit cấp độ file |
| **ORM** | `Entity Framework Core` | `8.0.8` | Quản lý truy vấn dữ liệu theo phương pháp Code-First / DbContext |
| **Mã Hóa Mật Khẩu** | `BCrypt.Net-Next` | `4.2.0` | Băm mật khẩu người dùng với salt an toàn cao |
| **Bảo Vệ Khóa** | `DPAPI & System.Management` | `8.0.0` | Sinh khóa mã hóa CSDL ràng buộc phần cứng CPU & Motherboard |
| **Báo Cáo Excel** | `ClosedXML` | `0.105.1` | Đọc / Ghi file Excel 14 Sheets nhanh chóng không cần MS Excel |
| **Thư Điện Tử** | `MailKit` | `4.17.0` | Gửi cảnh báo tự động và thông báo báo cáo qua giao thức SMTP |
| **Kiểm Thử Tự Động** | `xUnit & Moq` | `2.9.0` | **126 bài kiểm thử tự động** đảm bảo độ tin cậy và chính xác tuyệt đối |

---

## 📜 NHẬT KÝ CẬP NHẬT (CHANGELOG)

### Phiên Bản v1.4.4 (Bản hiện tại)
- **Tối ưu hóa bảng Học viên:** Tinh gọn Toolbar, tích hợp thao tác chọn học viên trực tiếp qua cột CheckBox trong bảng, loại bỏ các nút dư thừa.
- **Background Sidebar Chìm:** Bức tranh nghệ thuật quân đội dã chiến đè chìm tinh tế dưới khối Quân hiệu ở sidebar trái, tạo độ sâu trang trọng.
- **Hoàn thiện Bộ Kiểm Thử:** Đạt chuẩn **126 / 126 Unit Tests Passed (100%)**.

### Phiên Bản v1.4.0
- **Phân hệ Quản Lý Khóa Học (Academic Cohorts):** Thêm tính năng quản lý niên khóa đào tạo và thuật toán tự động nhận diện khóa học từ mã quân nhân (`DH.075.xxx` $\rightarrow$ `K75`).
- **Cơ Cấu Tổ Chức Đơn Vị 7 Tầng:** Tích hợp `UnitHierarchyService` hỗ trợ mô hình phân cấp từ Khóa đào tạo đến Tổ chiến đấu, tính tổng quân số đệ quy.
- **Dual-Theme Engine v1.4:** Tích hợp dải banner tranh nghệ thuật dã chiến cờ sao vàng, chiến sĩ hành quân trên toàn bộ 11 màn hình chức năng ở Chế độ Tác chiến; bảo toàn 100% giao diện sáng sạch ở Chế độ Hành chính.
- **Nâng cấp Bảng Điểm Tín Chỉ:** Thêm cột CheckBox chọn nhiều và chọn tất cả trên Header bảng điểm phục vụ thao tác hàng loạt.

### Phiên Bản v1.3.0
- Giới thiệu **Engine Giao diện Kép Độc lập (Dual-Theme Engine)**: Chế độ Hành chính và Chế độ Tác chiến.
- Tích hợp **Interactive Military Timeline** quản lý dòng thời gian huấn luyện và diễn tập tác chiến.

### Phiên Bản v1.0.0 - v1.2.0
- Khởi tạo kiến trúc lõi: Mã hóa SQLCipher AES-256 bit và Hardware Binding.
- Đánh giá thể lực quân sự tự động theo Thông tư liên tịch 32/2009/TTLT-BQP-BVHTTDL.
- Tính toán điểm trung bình tích lũy tín chỉ quân sự và xuất nhập báo cáo Excel 14 sheets ClosedXML.

---

## 🎖️ TÁC GIẢ & BẢN QUYỀN

- **Tác giả phát triển:** [Nguyễn Tấn (NguyenTan33)](https://github.com/NguyenTan33)
- **Repository chính thức:** [https://github.com/NguyenTan33/QL_HocVien](https://github.com/NguyenTan33/QL_HocVien)
- **Bản quyền phát hành:** © 2026 Dự án Quản lý Học viên Quân sự. Toàn quyền được bảo lưu.

<p align="center">
  <i>⭐ Đừng quên bấm <b>Star</b> trên GitHub nếu dự án hữu ích đối với công tác và học tập của bạn! ⭐</i>
</p>

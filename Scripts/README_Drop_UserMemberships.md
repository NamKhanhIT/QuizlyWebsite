# Hướng dẫn xóa bảng tb_UserMemberships khỏi Database

## Tình huống
Bảng `tb_UserMemberships` đã được xóa khỏi code (models, DbContext), nhưng vẫn còn trong database với các foreign key constraints:
- FK đến `tb_MembershipPlans` (PlanId)
- FK đến `tb_Users` (UserId)

## Các bước thực hiện

### Bước 1: Kiểm tra dữ liệu (QUAN TRỌNG)
Chạy script `Check_UserMemberships_Data.sql` để:
- Xem có dữ liệu trong bảng không
- Nếu có dữ liệu quan trọng, cần migrate sang `tb_UserSubscriptions` trước

```sql
-- Chạy file: Check_UserMemberships_Data.sql
```

### Bước 2: Xóa bảng (2 cách)

#### Cách 1: Dùng SQL Script (Khuyến nghị)
Chạy script `Drop_UserMemberships_Table.sql`:
- Tự động tìm và xóa tất cả FK constraints
- Xóa bảng an toàn

```sql
-- Chạy file: Drop_UserMemberships_Table.sql
```

#### Cách 2: Dùng EF Core Migration (Tự động)
Nếu bạn đang dùng EF Core migrations:

```bash
# Tạo migration mới
dotnet ef migrations add RemoveUserMembershipsTable

# Xem migration được tạo (sẽ tự động xóa FK và bảng)
# Nếu migration đúng, chạy:
dotnet ef database update
```

**Lưu ý**: EF Core sẽ tự động tạo migration để xóa bảng và FK khi bạn đã xóa model khỏi code.

### Bước 3: Verify
Kiểm tra lại:
```sql
-- Kiểm tra bảng đã bị xóa
SELECT * FROM sys.tables WHERE name = 'tb_UserMemberships'
-- Kết quả: Không có rows

-- Kiểm tra FK đã bị xóa
SELECT * FROM sys.foreign_keys 
WHERE parent_object_id = OBJECT_ID('tb_UserMemberships')
-- Kết quả: Không có rows
```

## Lưu ý quan trọng

⚠️ **Nếu có dữ liệu trong `tb_UserMemberships`**:
1. Backup database trước
2. Migrate dữ liệu sang `tb_UserSubscriptions` (nếu cần)
3. Sau đó mới xóa bảng

## Mapping dữ liệu (nếu cần migrate)

Nếu cần migrate từ `tb_UserMemberships` sang `tb_UserSubscriptions`:

```sql
-- Ví dụ: Migrate dữ liệu
INSERT INTO [dbo].[tb_UserSubscriptions] (UserId, PlanType, StartDate, EndDate, IsActive)
SELECT 
    um.UserId,
    CASE 
        WHEN mp.Title LIKE '%Premium%' THEN 'PREMIUM'
        WHEN mp.Title LIKE '%VIP%' THEN 'VIP'
        ELSE 'BASIC'
    END AS PlanType,
    um.StartDate,
    um.EndDate,
    CASE WHEN um.EndDate > GETDATE() THEN 1 ELSE 0 END AS IsActive
FROM [dbo].[tb_UserMemberships] um
INNER JOIN [dbo].[tb_MembershipPlans] mp ON um.PlanId = mp.Id
WHERE NOT EXISTS (
    SELECT 1 FROM [dbo].[tb_UserSubscriptions] us 
    WHERE us.UserId = um.UserId 
    AND us.StartDate = um.StartDate
)
```

## Kết quả mong đợi

Sau khi xóa:
- ✅ Bảng `tb_UserMemberships` không còn trong database
- ✅ Các FK constraints đã được xóa
- ✅ Bảng `tb_MembershipPlans` vẫn còn (đang được dùng trong Admin)
- ✅ Bảng `tb_UserSubscriptions` vẫn hoạt động bình thường

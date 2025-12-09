-- Script kiểm tra dữ liệu trong bảng tb_UserMemberships trước khi xóa
-- Chạy script này để xem có dữ liệu cần migrate không

USE [QuizlyDB]
GO

-- Kiểm tra số lượng records
SELECT COUNT(*) AS TotalRecords
FROM [dbo].[tb_UserMemberships]
GO

-- Xem chi tiết dữ liệu (nếu có)
SELECT 
    um.Id,
    um.UserId,
    u.Username,
    u.Email,
    um.PlanId,
    mp.Title AS PlanTitle,
    mp.Price AS PlanPrice,
    um.StartDate,
    um.EndDate,
    CASE 
        WHEN um.EndDate > GETDATE() THEN 'Active'
        ELSE 'Expired'
    END AS Status
FROM [dbo].[tb_UserMemberships] um
LEFT JOIN [dbo].[tb_Users] u ON um.UserId = u.Id
LEFT JOIN [dbo].[tb_MembershipPlans] mp ON um.PlanId = mp.Id
ORDER BY um.StartDate DESC
GO

-- Nếu có dữ liệu, bạn có thể migrate sang tb_UserSubscriptions bằng cách:
-- 1. Map PlanId sang PlanType (PREMIUM, VIP, etc.)
-- 2. Tạo records mới trong tb_UserSubscriptions
-- 3. Sau đó mới xóa bảng tb_UserMemberships

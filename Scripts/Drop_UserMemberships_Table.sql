-- Script để xóa bảng tb_UserMemberships và các foreign key constraints
-- Chạy script này trong SQL Server Management Studio hoặc Azure Data Studio

USE [QuizlyDB]
GO

-- Bước 1: Kiểm tra xem bảng có tồn tại không
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'tb_UserMemberships')
BEGIN
    PRINT 'Bảng tb_UserMemberships tồn tại. Bắt đầu xóa...'
    
    -- Bước 2: Xóa các Foreign Key Constraints trước
    -- Xóa FK từ tb_UserMemberships đến tb_MembershipPlans
    IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK__tb_UserMe__PlanI__74AE54BC')
    BEGIN
        ALTER TABLE [dbo].[tb_UserMemberships] 
        DROP CONSTRAINT [FK__tb_UserMe__PlanI__74AE54BC]
        PRINT 'Đã xóa FK: FK__tb_UserMe__PlanI__74AE54BC'
    END
    ELSE
    BEGIN
        -- Thử tìm FK với tên khác (có thể SQL Server tự tạo tên khác)
        DECLARE @FKPlanName NVARCHAR(128)
        SELECT @FKPlanName = name 
        FROM sys.foreign_keys 
        WHERE parent_object_id = OBJECT_ID('tb_UserMemberships')
          AND referenced_object_id = OBJECT_ID('tb_MembershipPlans')
        
        IF @FKPlanName IS NOT NULL
        BEGIN
            EXEC('ALTER TABLE [dbo].[tb_UserMemberships] DROP CONSTRAINT [' + @FKPlanName + ']')
            PRINT 'Đã xóa FK: ' + @FKPlanName
        END
    END
    
    -- Xóa FK từ tb_UserMemberships đến tb_Users
    IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK__tb_UserMe__UserI__73BA3083')
    BEGIN
        ALTER TABLE [dbo].[tb_UserMemberships] 
        DROP CONSTRAINT [FK__tb_UserMe__UserI__73BA3083]
        PRINT 'Đã xóa FK: FK__tb_UserMe__UserI__73BA3083'
    END
    ELSE
    BEGIN
        -- Thử tìm FK với tên khác
        DECLARE @FKUserName NVARCHAR(128)
        SELECT @FKUserName = name 
        FROM sys.foreign_keys 
        WHERE parent_object_id = OBJECT_ID('tb_UserMemberships')
          AND referenced_object_id = OBJECT_ID('tb_Users')
        
        IF @FKUserName IS NOT NULL
        BEGIN
            EXEC('ALTER TABLE [dbo].[tb_UserMemberships] DROP CONSTRAINT [' + @FKUserName + ']')
            PRINT 'Đã xóa FK: ' + @FKUserName
        END
    END
    
    -- Bước 3: Xóa tất cả các FK còn lại (nếu có)
    DECLARE @sql NVARCHAR(MAX) = ''
    SELECT @sql = @sql + 'ALTER TABLE [dbo].[tb_UserMemberships] DROP CONSTRAINT [' + name + '];' + CHAR(13)
    FROM sys.foreign_keys 
    WHERE parent_object_id = OBJECT_ID('tb_UserMemberships')
    
    IF @sql <> ''
    BEGIN
        EXEC sp_executesql @sql
        PRINT 'Đã xóa tất cả các FK còn lại'
    END
    
    -- Bước 4: Xóa bảng
    DROP TABLE [dbo].[tb_UserMemberships]
    PRINT 'Đã xóa bảng tb_UserMemberships thành công!'
END
ELSE
BEGIN
    PRINT 'Bảng tb_UserMemberships không tồn tại. Không cần xóa.'
END
GO

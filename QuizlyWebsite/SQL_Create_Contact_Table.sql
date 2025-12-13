-- Tạo bảng tb_Contacts để lưu thông tin liên hệ từ người dùng
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tb_Contacts]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[tb_Contacts](
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [Name] [nvarchar](200) NOT NULL,
        [Email] [nvarchar](200) NOT NULL,
        [Phone] [nvarchar](20) NULL,
        [Subject] [nvarchar](200) NOT NULL,
        [Message] [nvarchar](2000) NOT NULL,
        [CreatedAt] [datetime2](7) NULL DEFAULT (getdate()),
        [IsRead] [bit] NULL DEFAULT 0,
        [ReadAt] [datetime2](7) NULL,
        [Response] [nvarchar](2000) NULL,
        [RespondedAt] [datetime2](7) NULL,
        CONSTRAINT [PK__tb_Conta__3214EC07] PRIMARY KEY CLUSTERED ([Id] ASC)
    ) ON [PRIMARY];
    
    PRINT N'Đã tạo bảng tb_Contacts thành công';
END
ELSE
BEGIN
    PRINT N'Bảng tb_Contacts đã tồn tại';
END
GO


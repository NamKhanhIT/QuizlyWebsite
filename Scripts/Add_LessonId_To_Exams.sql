-- Script to add LessonId column to tb_Exams table
-- This allows linking exams to lessons for lesson quizzes

-- Check if column already exists
IF NOT EXISTS (
    SELECT * FROM sys.columns 
    WHERE object_id = OBJECT_ID(N'[dbo].[tb_Exams]') 
    AND name = 'LessonId'
)
BEGIN
    -- Add LessonId column (nullable to not break existing data)
    ALTER TABLE [dbo].[tb_Exams]
    ADD [LessonId] [int] NULL;

    -- Create index for LessonId
    CREATE INDEX [IX_Exams_LessonId] ON [dbo].[tb_Exams]([LessonId]);

    -- Add foreign key constraint
    ALTER TABLE [dbo].[tb_Exams]
    ADD CONSTRAINT [FK_Exams_Lesson] 
    FOREIGN KEY([LessonId]) REFERENCES [dbo].[tb_Lessons]([Id])
    ON DELETE SET NULL;

    PRINT 'Column LessonId added to tb_Exams successfully.';
    PRINT 'Index IX_Exams_LessonId created.';
    PRINT 'Foreign key FK_Exams_Lesson created.';
END
ELSE
BEGIN
    PRINT 'Column LessonId already exists in tb_Exams.';
END
GO

PRINT '';
PRINT 'Script completed! You can now link exams to lessons.';

using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace QuizlyWebsite.Models;

public partial class QuizlyDbContext : DbContext
{
    public QuizlyDbContext()
    {
    }

    public QuizlyDbContext(DbContextOptions<QuizlyDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<TbBlog> TbBlogs { get; set; }

    public virtual DbSet<TbCategory> TbCategories { get; set; }

    public virtual DbSet<TbCourse> TbCourses { get; set; }

    public virtual DbSet<TbExam> TbExams { get; set; }

    public virtual DbSet<TbExamResult> TbExamResults { get; set; }

    public virtual DbSet<TbExamResultDetail> TbExamResultDetails { get; set; }

    public virtual DbSet<TbExamReview> TbExamReviews { get; set; }

    public virtual DbSet<TbExamSession> TbExamSessions { get; set; }

    public virtual DbSet<TbExamViolation> TbExamViolations { get; set; }

    public virtual DbSet<TbLesson> TbLessons { get; set; }

    public virtual DbSet<TbLessonProgress> TbLessonProgresses { get; set; }

    public virtual DbSet<TbMembershipPlan> TbMembershipPlans { get; set; }

    public virtual DbSet<TbMenu> TbMenus { get; set; }

    public virtual DbSet<TbPayment> TbPayments { get; set; }

    public virtual DbSet<TbPenaltyRule> TbPenaltyRules { get; set; }

    public virtual DbSet<TbQuestion> TbQuestions { get; set; }

    public virtual DbSet<TbSubject> TbSubjects { get; set; }

    public virtual DbSet<TbUser> TbUsers { get; set; }

    public virtual DbSet<TbUserMembership> TbUserMemberships { get; set; }

    public virtual DbSet<TbUserPurchase> TbUserPurchases { get; set; }

    public virtual DbSet<TbUserSubscription> TbUserSubscriptions { get; set; }

    public virtual DbSet<TbUserXp> TbUserXps { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Data Source=.;Initial Catalog=QuizlyDB;Integrated Security=True;TrustServerCertificate=True;");


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.UseCollation("SQL_Latin1_General_CP1_CI_AS");

        modelBuilder.Entity<TbBlog>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__tb_Blogs__3214EC07C97B1085");

            entity.ToTable("tb_Blogs");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.IsPublished).HasDefaultValue(true);
            entity.Property(e => e.Slug).HasMaxLength(300);
            entity.Property(e => e.Summary).HasMaxLength(500);
            entity.Property(e => e.Thumbnail).HasMaxLength(500);
            entity.Property(e => e.Title).HasMaxLength(250);

            entity.HasOne(d => d.Author).WithMany(p => p.TbBlogs)
                .HasForeignKey(d => d.AuthorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__tb_Blogs__Author__02084FDA");
        });

        modelBuilder.Entity<TbCategory>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__tb_Categ__3214EC073494F476");

            entity.ToTable("tb_Categories");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Title).HasMaxLength(100);
        });

        modelBuilder.Entity<TbCourse>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__tb_Cours__3214EC07B80A2664");

            entity.ToTable("tb_Courses");

            entity.HasIndex(e => e.CreatedBy, "IX_Courses_CreatedBy");

            entity.HasIndex(e => e.IsApproved, "IX_Courses_IsApproved");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.FreeLessonCount).HasDefaultValue(2);
            entity.Property(e => e.IsApproved).HasDefaultValue(false);
            entity.Property(e => e.IsPaid).HasDefaultValue(false);
            entity.Property(e => e.Title).HasMaxLength(200);

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.TbCourses)
                .HasForeignKey(d => d.CreatedBy)
                .HasConstraintName("FK_Courses_CreatedBy");
        });

        modelBuilder.Entity<TbExam>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__tb_Exams__3214EC07D8D4493E");

            entity.ToTable("tb_Exams");

            entity.HasIndex(e => e.CreatedBy, "IX_Exams_CreatedBy");

            entity.HasIndex(e => e.IsApproved, "IX_Exams_IsApproved");

            entity.Property(e => e.AvgRating).HasDefaultValue(0.0);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Difficulty).HasMaxLength(20);
            entity.Property(e => e.IsApproved).HasDefaultValue(true);
            entity.Property(e => e.IsPaid).HasDefaultValue(false);
            entity.Property(e => e.IsPremium).HasDefaultValue(false);
            entity.Property(e => e.Price)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(18, 2)");
            entity.Property(e => e.QuestionCount).HasDefaultValue(0);
            entity.Property(e => e.Title).HasMaxLength(200);
            entity.Property(e => e.TotalReviews).HasDefaultValue(0);

            entity.HasOne(d => d.ApprovedByNavigation).WithMany(p => p.TbExamApprovedByNavigations)
                .HasForeignKey(d => d.ApprovedBy)
                .HasConstraintName("FK_Exams_ApprovedBy");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.TbExamCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .HasConstraintName("FK_Exams_CreatedBy");

            entity.HasOne(d => d.Subject).WithMany(p => p.TbExams)
                .HasForeignKey(d => d.SubjectId)
                .HasConstraintName("FK__tb_Exams__Subjec__46E78A0C");
        });

        modelBuilder.Entity<TbExamResult>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__tb_ExamR__3214EC07EB81C309");

            entity.ToTable("tb_ExamResults");

            entity.Property(e => e.Score).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.Status).HasMaxLength(30);

            entity.HasOne(d => d.Exam).WithMany(p => p.TbExamResults)
                .HasForeignKey(d => d.ExamId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__tb_ExamRe__ExamI__5EBF139D");

            entity.HasOne(d => d.Session).WithMany(p => p.TbExamResults)
                .HasForeignKey(d => d.SessionId)
                .HasConstraintName("FK__tb_ExamRe__Sessi__5FB337D6");

            entity.HasOne(d => d.User).WithMany(p => p.TbExamResults)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__tb_ExamRe__UserI__5DCAEF64");
        });

        modelBuilder.Entity<TbExamResultDetail>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__tb_ExamR__3214EC07B0BC4AE6");

            entity.ToTable("tb_ExamResultDetails");

            entity.Property(e => e.SelectedOption)
                .HasMaxLength(1)
                .IsUnicode(false)
                .IsFixedLength();

            entity.HasOne(d => d.ExamResult).WithMany(p => p.TbExamResultDetails)
                .HasForeignKey(d => d.ExamResultId)
                .HasConstraintName("FK__tb_ExamRe__ExamR__6383C8BA");

            entity.HasOne(d => d.Question).WithMany(p => p.TbExamResultDetails)
                .HasForeignKey(d => d.QuestionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__tb_ExamRe__Quest__6477ECF3");
        });

        modelBuilder.Entity<TbExamReview>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__tb_ExamR__3214EC070DF5AD13");

            entity.ToTable("tb_ExamReviews");

            entity.HasIndex(e => new { e.UserId, e.ExamId }, "UQ_UserExamReview").IsUnique();

            entity.Property(e => e.Comment).HasMaxLength(800);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.Exam).WithMany(p => p.TbExamReviews)
                .HasForeignKey(d => d.ExamId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__tb_ExamRe__ExamI__7D439ABD");

            entity.HasOne(d => d.User).WithMany(p => p.TbExamReviews)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__tb_ExamRe__UserI__7C4F7684");
        });

        modelBuilder.Entity<TbExamSession>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__tb_ExamS__3214EC07E568069C");

            entity.ToTable("tb_ExamSessions");

            entity.Property(e => e.AppliedPenalty).HasDefaultValue(false);
            entity.Property(e => e.SessionToken).HasDefaultValueSql("(newid())");
            entity.Property(e => e.StartedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("Ongoing");

            entity.HasOne(d => d.Exam).WithMany(p => p.TbExamSessions)
                .HasForeignKey(d => d.ExamId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__tb_ExamSe__ExamI__534D60F1");

            entity.HasOne(d => d.User).WithMany(p => p.TbExamSessions)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__tb_ExamSe__UserI__52593CB8");
        });

        modelBuilder.Entity<TbExamViolation>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__tb_ExamV__3214EC0754F10A47");

            entity.ToTable("tb_ExamViolations");

            entity.Property(e => e.ViolationTime).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.ViolationType).HasMaxLength(50);

            entity.HasOne(d => d.Session).WithMany(p => p.TbExamViolations)
                .HasForeignKey(d => d.SessionId)
                .HasConstraintName("FK__tb_ExamVi__Sessi__571DF1D5");
        });

        modelBuilder.Entity<TbLesson>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__tb_Lesso__3214EC07358B9D1A");

            entity.ToTable("tb_Lessons");

            entity.HasIndex(e => e.CourseId, "IX_Lessons_CourseId");

            entity.HasIndex(e => e.CreatedBy, "IX_Lessons_CreatedBy");

            entity.HasIndex(e => e.IsApproved, "IX_Lessons_IsApproved");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.IsApproved).HasDefaultValue(false);
            entity.Property(e => e.IsPreview).HasDefaultValue(false);
            entity.Property(e => e.Title).HasMaxLength(200);

            entity.HasOne(d => d.Course).WithMany(p => p.TbLessons)
                .HasForeignKey(d => d.CourseId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__tb_Lesson__Cours__2739D489");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.TbLessons)
                .HasForeignKey(d => d.CreatedBy)
                .HasConstraintName("FK_Lessons_CreatedBy");
        });

        modelBuilder.Entity<TbLessonProgress>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__tb_Lesso__3214EC07546968BB");

            entity.ToTable("tb_LessonProgress");

            entity.HasIndex(e => e.LessonId, "IX_LessonProgress_LessonId");

            entity.HasIndex(e => e.UserId, "IX_LessonProgress_UserId");

            entity.Property(e => e.IsCompleted).HasDefaultValue(false);

            entity.HasOne(d => d.Lesson).WithMany(p => p.TbLessonProgresses)
                .HasForeignKey(d => d.LessonId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__tb_Lesson__Lesso__2BFE89A6");

            entity.HasOne(d => d.User).WithMany(p => p.TbLessonProgresses)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__tb_Lesson__UserI__2B0A656D");
        });

        modelBuilder.Entity<TbMembershipPlan>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__tb_Membe__3214EC078871AAC3");

            entity.ToTable("tb_MembershipPlans");

            entity.Property(e => e.Price).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Title).HasMaxLength(100);
        });

        modelBuilder.Entity<TbMenu>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__tb_Menus__3214EC07744D91CC");

            entity.ToTable("tb_Menus");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Location)
                .HasMaxLength(50)
                .HasDefaultValue("Header");
            entity.Property(e => e.Order).HasDefaultValue(0);
            entity.Property(e => e.Title).HasMaxLength(100);
            entity.Property(e => e.Url).HasMaxLength(500);

            entity.HasOne(d => d.Parent).WithMany(p => p.InverseParent)
                .HasForeignKey(d => d.ParentId)
                .HasConstraintName("FK__tb_Menus__Parent__08B54D69");
        });

        modelBuilder.Entity<TbPayment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__tb_Payme__3214EC070ABB7132");

            entity.ToTable("tb_Payments");

            entity.Property(e => e.Amount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Currency)
                .HasMaxLength(10)
                .HasDefaultValue("VND");
            entity.Property(e => e.Provider).HasMaxLength(50);
            entity.Property(e => e.ProviderTransId).HasMaxLength(150);
            entity.Property(e => e.Status).HasMaxLength(30);

            entity.HasOne(d => d.Exam).WithMany(p => p.TbPayments)
                .HasForeignKey(d => d.ExamId)
                .HasConstraintName("FK__tb_Paymen__ExamI__6A30C649");

            entity.HasOne(d => d.User).WithMany(p => p.TbPayments)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__tb_Paymen__UserI__693CA210");
        });

        modelBuilder.Entity<TbPenaltyRule>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__tb_Penal__3214EC0729694785");

            entity.ToTable("tb_PenaltyRules");

            entity.Property(e => e.Reason).HasMaxLength(100);

            entity.HasOne(d => d.Exam).WithMany(p => p.TbPenaltyRules)
                .HasForeignKey(d => d.ExamId)
                .HasConstraintName("FK__tb_Penalt__ExamI__5AEE82B9");
        });

        modelBuilder.Entity<TbQuestion>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__tb_Quest__3214EC074A8879B1");

            entity.ToTable("tb_Questions");

            entity.Property(e => e.CorrectOption)
                .HasMaxLength(1)
                .IsUnicode(false)
                .IsFixedLength();
            entity.Property(e => e.Marks)
                .HasDefaultValue(1m)
                .HasColumnType("decimal(5, 2)");
            entity.Property(e => e.OptionA).HasMaxLength(500);
            entity.Property(e => e.OptionB).HasMaxLength(500);
            entity.Property(e => e.OptionC).HasMaxLength(500);
            entity.Property(e => e.OptionD).HasMaxLength(500);

            entity.HasOne(d => d.Exam).WithMany(p => p.TbQuestions)
                .HasForeignKey(d => d.ExamId)
                .HasConstraintName("FK__tb_Questi__ExamI__4BAC3F29");
        });

        modelBuilder.Entity<TbSubject>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__tb_Subje__3214EC07D72D3EA1");

            entity.ToTable("tb_Subjects");

            entity.Property(e => e.Title).HasMaxLength(100);

            entity.HasOne(d => d.Category).WithMany(p => p.TbSubjects)
                .HasForeignKey(d => d.CategoryId)
                .HasConstraintName("FK__tb_Subjec__Categ__3F466844");
        });

        modelBuilder.Entity<TbUser>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__tb_Users__3214EC071252BE45");

            entity.ToTable("tb_Users");

            entity.HasIndex(e => e.Email, "UQ__tb_Users__A9D1053486BE8F1D").IsUnique();

            entity.Property(e => e.AvatarUrl).HasMaxLength(500);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.FullName).HasMaxLength(100);
            entity.Property(e => e.PasswordHash).HasMaxLength(255);
            entity.Property(e => e.Role)
                .HasMaxLength(20)
                .HasDefaultValue("User");
            entity.Property(e => e.Username).HasMaxLength(50);
        });

        modelBuilder.Entity<TbUserMembership>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__tb_UserM__3214EC07DA05BD36");

            entity.ToTable("tb_UserMemberships");

            entity.HasOne(d => d.Plan).WithMany(p => p.TbUserMemberships)
                .HasForeignKey(d => d.PlanId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__tb_UserMe__PlanI__74AE54BC");

            entity.HasOne(d => d.User).WithMany(p => p.TbUserMemberships)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__tb_UserMe__UserI__73BA3083");
        });

        modelBuilder.Entity<TbUserPurchase>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__tb_UserP__3214EC0789E53078");

            entity.ToTable("tb_UserPurchases");

            entity.Property(e => e.PurchasedAt).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.Exam).WithMany(p => p.TbUserPurchases)
                .HasForeignKey(d => d.ExamId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__tb_UserPu__ExamI__6EF57B66");

            entity.HasOne(d => d.User).WithMany(p => p.TbUserPurchases)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__tb_UserPu__UserI__6E01572D");
        });

        modelBuilder.Entity<TbUserSubscription>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__tb_UserS__3214EC07CEE246F5");

            entity.ToTable("tb_UserSubscriptions");

            entity.Property(e => e.EndDate).HasColumnType("datetime");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.PlanType).HasMaxLength(30);
            entity.Property(e => e.StartDate).HasColumnType("datetime");
        });

        modelBuilder.Entity<TbUserXp>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__tb_UserX__1788CC4C11314948");

            entity.ToTable("tb_UserXP");

            entity.Property(e => e.UserId).ValueGeneratedNever();
            entity.Property(e => e.Level).HasDefaultValue(1);
            entity.Property(e => e.Xp)
                .HasDefaultValue(0)
                .HasColumnName("XP");

            entity.HasOne(d => d.User).WithOne(p => p.TbUserXp)
                .HasForeignKey<TbUserXp>(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__tb_UserXP__UserI__30C33EC3");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}

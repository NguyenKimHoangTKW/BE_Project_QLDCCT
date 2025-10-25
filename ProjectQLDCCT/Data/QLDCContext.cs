﻿using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using ProjectQLDCCT.Models;

namespace ProjectQLDCCT.Data;

public partial class QLDCContext : DbContext
{
    public QLDCContext()
    {
    }

    public QLDCContext(DbContextOptions<QLDCContext> options)
        : base(options)
    {
    }

    public virtual DbSet<ChildrenRelationshipRatingScale> ChildrenRelationshipRatingScales { get; set; }

    public virtual DbSet<CivilServant> CivilServants { get; set; }

    public virtual DbSet<Class> Classes { get; set; }

    public virtual DbSet<CoreCourseMatrix> CoreCourseMatrices { get; set; }

    public virtual DbSet<Course> Courses { get; set; }

    public virtual DbSet<CourseByKey> CourseByKeys { get; set; }

    public virtual DbSet<CourseLearningOutcome> CourseLearningOutcomes { get; set; }

    public virtual DbSet<Course_Objective> Course_Objectives { get; set; }

    public virtual DbSet<Faculty> Faculties { get; set; }

    public virtual DbSet<FunctionUser> FunctionUsers { get; set; }

    public virtual DbSet<Group_Course> Group_Courses { get; set; }

    public virtual DbSet<IsCourse> IsCourses { get; set; }

    public virtual DbSet<JWTSession> JWTSessions { get; set; }

    public virtual DbSet<KeyYearSemester> KeyYearSemesters { get; set; }

    public virtual DbSet<LogOperation> LogOperations { get; set; }

    public virtual DbSet<RatingScaleMatrix> RatingScaleMatrices { get; set; }

    public virtual DbSet<RelationshipRatingScale> RelationshipRatingScales { get; set; }

    public virtual DbSet<Semester> Semesters { get; set; }

    public virtual DbSet<Student> Students { get; set; }

    public virtual DbSet<Syllabus> Syllabi { get; set; }

    public virtual DbSet<SyllabusApproval> SyllabusApprovals { get; set; }

    public virtual DbSet<SyllabusSection> SyllabusSections { get; set; }

    public virtual DbSet<SyllabusSectionContent> SyllabusSectionContents { get; set; }

    public virtual DbSet<SyllabusTemplate> SyllabusTemplates { get; set; }

    public virtual DbSet<SyllabusTemplateSection> SyllabusTemplateSections { get; set; }

    public virtual DbSet<TeacherBySubject> TeacherBySubjects { get; set; }

    public virtual DbSet<TrainingProgram> TrainingPrograms { get; set; }

    public virtual DbSet<TypeUser> TypeUsers { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserByFaculProgram> UserByFaculPrograms { get; set; }

    public virtual DbSet<Year> Years { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Data Source=.;Initial Catalog=dbQLDC;Integrated Security=True;Trust Server Certificate=True");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ChildrenRelationshipRatingScale>(entity =>
        {
            entity.HasOne(d => d.id_parentRRSNavigation).WithMany(p => p.ChildrenRelationshipRatingScales).HasConstraintName("FK_ChildrenRelationshipRatingScale_RelationshipRatingScale");
        });

        modelBuilder.Entity<CivilServant>(entity =>
        {
            entity.ToTable(tb => tb.HasTrigger("trg_delete_CivilServants"));

            entity.HasOne(d => d.id_yearNavigation).WithMany(p => p.CivilServants).HasConstraintName("FK_CivilServants_Year");
        });

        modelBuilder.Entity<Class>(entity =>
        {
            entity.ToTable("Class", tb => tb.HasTrigger("trg_delete_Class"));

            entity.HasOne(d => d.id_programNavigation).WithMany(p => p.Classes).HasConstraintName("FK_Class_TrainingProgram");
        });

        modelBuilder.Entity<CoreCourseMatrix>(entity =>
        {
            entity.ToTable("CoreCourseMatrix", tb => tb.HasTrigger("trg_delete_CoreCourseMatrix"));

            entity.HasOne(d => d.id_programNavigation).WithMany(p => p.CoreCourseMatrices).HasConstraintName("FK_CoreCourseMatrix_TrainingProgram");
        });

        modelBuilder.Entity<Course>(entity =>
        {
            entity.ToTable("Course", tb => tb.HasTrigger("trg_delete_Course"));

            entity.HasOne(d => d.id_faccultyNavigation).WithMany(p => p.Courses).HasConstraintName("FK_Course_Faculty");

            entity.HasOne(d => d.id_gr_courseNavigation).WithMany(p => p.Courses).HasConstraintName("FK_Course_Group_Course");

            entity.HasOne(d => d.id_isCourseNavigation).WithMany(p => p.Courses).HasConstraintName("FK_Course_IsCourse");
        });

        modelBuilder.Entity<CourseByKey>(entity =>
        {
            entity.HasOne(d => d.id_courseNavigation).WithMany(p => p.CourseByKeys).HasConstraintName("FK_CourseByKey_Course");

            entity.HasOne(d => d.id_key_semesterNavigation).WithMany(p => p.CourseByKeys).HasConstraintName("FK_CourseByKey_KeyYearSemester");

            entity.HasOne(d => d.id_programNavigation).WithMany(p => p.CourseByKeys).HasConstraintName("FK_CourseByKey_TrainingProgram");

            entity.HasOne(d => d.id_semesterNavigation).WithMany(p => p.CourseByKeys).HasConstraintName("FK_CourseByKey_Semester");
        });

        modelBuilder.Entity<Faculty>(entity =>
        {
            entity.ToTable("Faculty", tb => tb.HasTrigger("trg_delete_Faculty"));

            entity.HasOne(d => d.id_yearNavigation).WithMany(p => p.Faculties).HasConstraintName("FK_Faculty_Year");
        });

        modelBuilder.Entity<FunctionUser>(entity =>
        {
            entity.HasOne(d => d.id_type_usersNavigation).WithMany(p => p.FunctionUsers).HasConstraintName("FK_FunctionUsers_TypeUsers");
        });

        modelBuilder.Entity<Group_Course>(entity =>
        {
            entity.ToTable("Group_Course", tb => tb.HasTrigger("trg_delete_Group_Course"));
        });

        modelBuilder.Entity<JWTSession>(entity =>
        {
            entity.HasOne(d => d.id_userNavigation).WithMany(p => p.JWTSessions).HasConstraintName("FK_JWTSessions_Users");
        });

        modelBuilder.Entity<KeyYearSemester>(entity =>
        {
            entity.HasKey(e => e.id_key_year_semester).HasName("PK_KeyYearSemesterr");

            entity.ToTable("KeyYearSemester", tb => tb.HasTrigger("trg_delete_KeyYearSemester"));

            entity.HasOne(d => d.id_facultyNavigation).WithMany(p => p.KeyYearSemesters).HasConstraintName("FK_KeyYearSemester_Faculty");
        });

        modelBuilder.Entity<RatingScaleMatrix>(entity =>
        {
            entity.Property(e => e.description).IsFixedLength();

            entity.HasOne(d => d.id_core_course_matrixNavigation).WithMany(p => p.RatingScaleMatrices).HasConstraintName("FK_RatingScaleMatrix_CoreCourseMatrix");
        });

        modelBuilder.Entity<RelationshipRatingScale>(entity =>
        {
            entity.ToTable("RelationshipRatingScale", tb => tb.HasTrigger("trg_delete_RelationshipRatingScale"));

            entity.HasOne(d => d.id_core_rating_scale_matrixNavigation).WithMany(p => p.RelationshipRatingScales).HasConstraintName("FK_RelationshipRatingScale_CoreCourseMatrix");
        });

        modelBuilder.Entity<Semester>(entity =>
        {
            entity.ToTable("Semester", tb => tb.HasTrigger("trg_delete_Semester"));

            entity.HasOne(d => d.id_facultyNavigation).WithMany(p => p.Semesters).HasConstraintName("FK_Semester_Faculty");
        });

        modelBuilder.Entity<Student>(entity =>
        {
            entity.HasOne(d => d.id_classNavigation).WithMany(p => p.Students).HasConstraintName("FK_Student_Class");
        });

        modelBuilder.Entity<Syllabus>(entity =>
        {
            entity.ToTable("Syllabus", tb => tb.HasTrigger("trg_delete_Syllabus"));

            entity.HasOne(d => d.id_civilSerNavigation).WithMany(p => p.Syllabi).HasConstraintName("FK_Syllabus_CivilServants");

            entity.HasOne(d => d.id_courseNavigation).WithMany(p => p.Syllabi).HasConstraintName("FK_Syllabus_Course");
        });

        modelBuilder.Entity<SyllabusApproval>(entity =>
        {
            entity.HasKey(e => e.id_approval).HasName("PK__Syllabus__065E3F5E31EAE3FC");

            entity.HasOne(d => d.id_syllabusNavigation).WithMany(p => p.SyllabusApprovals)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SyllabusApproval_Syllabus");
        });

        modelBuilder.Entity<SyllabusSection>(entity =>
        {
            entity.HasKey(e => e.id_section).HasName("PK__Syllabus__3A8312DE091036DE");

            entity.ToTable("SyllabusSection", tb => tb.HasTrigger("trg_delete_SyllabusSection"));

            entity.HasOne(d => d.id_syllabusNavigation).WithMany(p => p.SyllabusSections)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SyllabusSection_Syllabus");

            entity.HasOne(d => d.id_template_sectionNavigation).WithMany(p => p.SyllabusSections).HasConstraintName("FK__SyllabusS__id_se__6E01572D");
        });

        modelBuilder.Entity<SyllabusSectionContent>(entity =>
        {
            entity.HasKey(e => e.id_content).HasName("PK__Syllabus__3E703D5FAC580AF0");

            entity.HasOne(d => d.id_sectionNavigation).WithMany(p => p.SyllabusSectionContents)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SyllabusSectionContent_SyllabusSection");
        });

        modelBuilder.Entity<SyllabusTemplate>(entity =>
        {
            entity.HasKey(e => e.id_template).HasName("PK__Syllabus__97B30205FEAAC473");

            entity.ToTable("SyllabusTemplate", tb => tb.HasTrigger("trg_delete_SyllabusTemplate"));

            entity.Property(e => e.is_default).HasDefaultValue(1);

            entity.HasOne(d => d.id_facultyNavigation).WithMany(p => p.SyllabusTemplates).HasConstraintName("FK_SyllabusTemplate_Faculty");
        });

        modelBuilder.Entity<SyllabusTemplateSection>(entity =>
        {
            entity.HasKey(e => e.id_template_section).HasName("PK__Syllabus__83EAD1E85B9F7048");

            entity.ToTable("SyllabusTemplateSection", tb => tb.HasTrigger("trg_delete_SyllabusTemplateSection"));

            entity.Property(e => e.is_required).HasDefaultValue(1);

            entity.HasOne(d => d.id_templateNavigation).WithMany(p => p.SyllabusTemplateSections)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__SyllabusT__id_te__6A30C649");

            entity.HasOne(d => d.is_CLONavigation).WithMany(p => p.SyllabusTemplateSections).HasConstraintName("FK_SyllabusTemplateSection_CourseLearningOutcomes");

            entity.HasOne(d => d.is_CONavigation).WithMany(p => p.SyllabusTemplateSections).HasConstraintName("FK_SyllabusTemplateSection_Course Objectives");

            entity.HasOne(d => d.is_CoreMatrixNavigation).WithMany(p => p.SyllabusTemplateSections).HasConstraintName("FK_SyllabusTemplateSection_CoreCourseMatrix");
        });

        modelBuilder.Entity<TeacherBySubject>(entity =>
        {
            entity.HasOne(d => d.id_courseNavigation).WithMany(p => p.TeacherBySubjects).HasConstraintName("FK_TeacherBySubject_Course");

            entity.HasOne(d => d.id_userNavigation).WithMany(p => p.TeacherBySubjects).HasConstraintName("FK_TeacherBySubject_Users");
        });

        modelBuilder.Entity<TrainingProgram>(entity =>
        {
            entity.HasKey(e => e.id_program).HasName("PK_Program");

            entity.ToTable("TrainingProgram", tb => tb.HasTrigger("trg_delete_TrainingProgram"));

            entity.HasOne(d => d.id_facultyNavigation).WithMany(p => p.TrainingPrograms).HasConstraintName("FK_Program_Faculty");
        });

        modelBuilder.Entity<TypeUser>(entity =>
        {
            entity.ToTable(tb => tb.HasTrigger("trg_delete_TypeUsers"));
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable(tb => tb.HasTrigger("trg_delete_Users"));

            entity.HasOne(d => d.id_type_usersNavigation).WithMany(p => p.Users).HasConstraintName("FK_Users_TypeUsers");
        });

        modelBuilder.Entity<UserByFaculProgram>(entity =>
        {
            entity.HasOne(d => d.id_facultyNavigation).WithMany(p => p.UserByFaculPrograms).HasConstraintName("FK_UserByFaculProgram_Faculty");

            entity.HasOne(d => d.id_programNavigation).WithMany(p => p.UserByFaculPrograms).HasConstraintName("FK_UserByFaculProgram_TrainingProgram");

            entity.HasOne(d => d.id_usersNavigation).WithMany(p => p.UserByFaculPrograms).HasConstraintName("FK_UserByFaculProgram_Users");
        });

        modelBuilder.Entity<Year>(entity =>
        {
            entity.ToTable("Year", tb => tb.HasTrigger("trg_delete_Year"));
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}

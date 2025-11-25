using System;
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

    public virtual DbSet<ApproveUserSyllabus> ApproveUserSyllabi { get; set; }

    public virtual DbSet<CLO_CO_Mapping> CLO_CO_Mappings { get; set; }

    public virtual DbSet<CLO_PI_Mapping> CLO_PI_Mappings { get; set; }

    public virtual DbSet<CLO_PLO_Mapping> CLO_PLO_Mappings { get; set; }

    public virtual DbSet<CivilServant> CivilServants { get; set; }

    public virtual DbSet<Class> Classes { get; set; }

    public virtual DbSet<ContentType> ContentTypes { get; set; }

    public virtual DbSet<ContributionMatrix> ContributionMatrices { get; set; }

    public virtual DbSet<CoreCourseMatrix> CoreCourseMatrices { get; set; }

    public virtual DbSet<Course> Courses { get; set; }

    public virtual DbSet<CourseByKey> CourseByKeys { get; set; }

    public virtual DbSet<CourseLearningOutcome> CourseLearningOutcomes { get; set; }

    public virtual DbSet<CourseObjective> CourseObjectives { get; set; }

    public virtual DbSet<DataBinding> DataBindings { get; set; }

    public virtual DbSet<Faculty> Faculties { get; set; }

    public virtual DbSet<FunctionUser> FunctionUsers { get; set; }

    public virtual DbSet<GroupCourse> GroupCourses { get; set; }

    public virtual DbSet<Group_Course> Group_Courses { get; set; }

    public virtual DbSet<IsCourse> IsCourses { get; set; }

    public virtual DbSet<JWTSession> JWTSessions { get; set; }

    public virtual DbSet<KeyYearSemester> KeyYearSemesters { get; set; }

    public virtual DbSet<LevelContribution> LevelContributions { get; set; }

    public virtual DbSet<LogOperation> LogOperations { get; set; }

    public virtual DbSet<LogStatus> LogStatuses { get; set; }

    public virtual DbSet<Log_Syllabus> Log_Syllabi { get; set; }

    public virtual DbSet<MappingCLOBySyllabus> MappingCLOBySyllabi { get; set; }

    public virtual DbSet<MappingCLObyPI> MappingCLObyPIs { get; set; }

    public virtual DbSet<OpenSyllabusWindowsCourse> OpenSyllabusWindowsCourses { get; set; }

    public virtual DbSet<PerformanceIndicator> PerformanceIndicators { get; set; }

    public virtual DbSet<ProgramLearningOutcome> ProgramLearningOutcomes { get; set; }

    public virtual DbSet<Semester> Semesters { get; set; }

    public virtual DbSet<Student> Students { get; set; }

    public virtual DbSet<Syllabus> Syllabi { get; set; }

    public virtual DbSet<SyllabusApproval> SyllabusApprovals { get; set; }

    public virtual DbSet<SyllabusDraft> SyllabusDrafts { get; set; }

    public virtual DbSet<SyllabusSection> SyllabusSections { get; set; }

    public virtual DbSet<SyllabusSectionContent> SyllabusSectionContents { get; set; }

    public virtual DbSet<SyllabusTemplate> SyllabusTemplates { get; set; }

    public virtual DbSet<SyllabusTemplateSection> SyllabusTemplateSections { get; set; }

    public virtual DbSet<Syllabus_Draft> Syllabus_Drafts { get; set; }

    public virtual DbSet<Syllabus_Draft_Section> Syllabus_Draft_Sections { get; set; }

    public virtual DbSet<TeacherBySubject> TeacherBySubjects { get; set; }

    public virtual DbSet<TrainingProgram> TrainingPrograms { get; set; }

    public virtual DbSet<TypeUser> TypeUsers { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserByFaculProgram> UserByFaculPrograms { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Data Source=.;Initial Catalog=dbQLDC;Integrated Security=True;Trust Server Certificate=True");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ApproveUserSyllabus>(entity =>
        {
            entity.HasKey(e => e.id_ApproveUserSyllabus).HasName("PK_Table_1");

            entity.HasOne(d => d.id_syllabusNavigation).WithMany(p => p.ApproveUserSyllabi).HasConstraintName("FK_ApproveUserSyllabus_Syllabus");

            entity.HasOne(d => d.id_userNavigation).WithMany(p => p.ApproveUserSyllabi).HasConstraintName("FK_ApproveUserSyllabus_Users");
        });

        modelBuilder.Entity<CLO_CO_Mapping>(entity =>
        {
            entity.HasOne(d => d.id_CLONavigation).WithMany(p => p.CLO_CO_Mappings)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CLO_CO_Mapping_CourseLearningOutcomes");

            entity.HasOne(d => d.id_CONavigation).WithMany(p => p.CLO_CO_Mappings)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CLO_CO_Mapping_CourseObjectives");

            entity.HasOne(d => d.id_programNavigation).WithMany(p => p.CLO_CO_Mappings).HasConstraintName("FK_CLO_CO_Mapping_TrainingProgram");
        });

        modelBuilder.Entity<CLO_PI_Mapping>(entity =>
        {
            entity.HasOne(d => d.Id_CLONavigation).WithMany(p => p.CLO_PI_Mappings).HasConstraintName("FK_CLO_PI_Mapping_CourseLearningOutcomes");

            entity.HasOne(d => d.Id_levelNavigation).WithMany(p => p.CLO_PI_Mappings).HasConstraintName("FK_CLO_PI_Mapping_LevelContribution");

            entity.HasOne(d => d.id_PINavigation).WithMany(p => p.CLO_PI_Mappings).HasConstraintName("FK_CLO_PI_Mapping_PerformanceIndicator");
        });

        modelBuilder.Entity<CLO_PLO_Mapping>(entity =>
        {
            entity.HasOne(d => d.Id_LevelNavigation).WithMany(p => p.CLO_PLO_Mappings).HasConstraintName("FK_CLO_PLO_Mapping_LevelContribution");

            entity.HasOne(d => d.id_CLONavigation).WithMany(p => p.CLO_PLO_Mappings)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CLO_PLO_Mapping_CourseLearningOutcomes");

            entity.HasOne(d => d.id_CoreCourseMatrixNavigation).WithMany(p => p.CLO_PLO_Mappings)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CLO_PLO_Mapping_CoreCourseMatrix");

            entity.HasOne(d => d.id_programNavigation).WithMany(p => p.CLO_PLO_Mappings).HasConstraintName("FK_CLO_PLO_Mapping_TrainingProgram");
        });

        modelBuilder.Entity<CivilServant>(entity =>
        {
            entity.ToTable(tb => tb.HasTrigger("trg_delete_CivilServants"));

            entity.HasOne(d => d.id_programNavigation).WithMany(p => p.CivilServants).HasConstraintName("FK_CivilServants_TrainingProgram");
        });

        modelBuilder.Entity<Class>(entity =>
        {
            entity.ToTable("Class", tb => tb.HasTrigger("trg_delete_Class"));

            entity.HasOne(d => d.id_programNavigation).WithMany(p => p.Classes).HasConstraintName("FK_Class_TrainingProgram");
        });

        modelBuilder.Entity<ContentType>(entity =>
        {
            entity.ToTable("ContentType", tb => tb.HasTrigger("trg_delete_ContentType"));
        });

        modelBuilder.Entity<ContributionMatrix>(entity =>
        {
            entity.HasOne(d => d.Id_PINavigation).WithMany(p => p.ContributionMatrices).HasConstraintName("FK_ContributionMatrix_PerformanceIndicator");

            entity.HasOne(d => d.id_courseNavigation).WithMany(p => p.ContributionMatrices).HasConstraintName("FK_ContributionMatrix_Course");

            entity.HasOne(d => d.id_levelcontributonNavigation).WithMany(p => p.ContributionMatrices).HasConstraintName("FK_ContributionMatrix_LevelContribution");
        });

        modelBuilder.Entity<CoreCourseMatrix>(entity =>
        {
            entity.ToTable("CoreCourseMatrix", tb => tb.HasTrigger("trg_delete_CoreCourseMatrix"));

            entity.HasOne(d => d.id_facultyNavigation).WithMany(p => p.CoreCourseMatrices).HasConstraintName("FK_CoreCourseMatrix_Faculty");
        });

        modelBuilder.Entity<Course>(entity =>
        {
            entity.ToTable("Course", tb => tb.HasTrigger("trg_delete_Course"));

            entity.HasOne(d => d.id_gr_courseNavigation).WithMany(p => p.Courses).HasConstraintName("FK_Course_Group_Course");

            entity.HasOne(d => d.id_isCourseNavigation).WithMany(p => p.Courses).HasConstraintName("FK_Course_IsCourse");

            entity.HasOne(d => d.id_key_year_semesterNavigation).WithMany(p => p.Courses).HasConstraintName("FK_Course_KeyYearSemester");

            entity.HasOne(d => d.id_programNavigation).WithMany(p => p.Courses).HasConstraintName("FK_Course_TrainingProgram");

            entity.HasOne(d => d.id_semesterNavigation).WithMany(p => p.Courses).HasConstraintName("FK_Course_Semester");
        });

        modelBuilder.Entity<CourseByKey>(entity =>
        {
            entity.HasOne(d => d.id_courseNavigation).WithMany(p => p.CourseByKeys).HasConstraintName("FK_CourseByKey_Course");

            entity.HasOne(d => d.id_key_semesterNavigation).WithMany(p => p.CourseByKeys).HasConstraintName("FK_CourseByKey_KeyYearSemester");

            entity.HasOne(d => d.id_programNavigation).WithMany(p => p.CourseByKeys).HasConstraintName("FK_CourseByKey_TrainingProgram");

            entity.HasOne(d => d.id_semesterNavigation).WithMany(p => p.CourseByKeys).HasConstraintName("FK_CourseByKey_Semester");
        });

        modelBuilder.Entity<CourseLearningOutcome>(entity =>
        {
            entity.ToTable(tb => tb.HasTrigger("trg_delete_CourseLearningOutcomes"));

            entity.HasOne(d => d.id_facultyNavigation).WithMany(p => p.CourseLearningOutcomes).HasConstraintName("FK_CourseLearningOutcomes_Faculty");

            entity.HasOne(d => d.program).WithMany(p => p.CourseLearningOutcomes).HasConstraintName("FK_CourseLearningOutcomes_TrainingProgram");
        });

        modelBuilder.Entity<CourseObjective>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK_Course Objectives");

            entity.ToTable(tb => tb.HasTrigger("trg_delete_CourseObjectives"));

            entity.HasOne(d => d.id_facultyNavigation).WithMany(p => p.CourseObjectives).HasConstraintName("FK_CourseObjectives_Faculty");
        });

        modelBuilder.Entity<DataBinding>(entity =>
        {
            entity.ToTable("DataBinding", tb => tb.HasTrigger("trg_delete_DataBinding"));
        });

        modelBuilder.Entity<Faculty>(entity =>
        {
            entity.ToTable("Faculty", tb => tb.HasTrigger("trg_delete_Faculty"));
        });

        modelBuilder.Entity<FunctionUser>(entity =>
        {
            entity.HasOne(d => d.id_type_usersNavigation).WithMany(p => p.FunctionUsers).HasConstraintName("FK_FunctionUsers_TypeUsers");
        });

        modelBuilder.Entity<GroupCourse>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK__GroupCou__3213E83FC2C5D75F");
        });

        modelBuilder.Entity<Group_Course>(entity =>
        {
            entity.ToTable("Group_Course", tb => tb.HasTrigger("trg_delete_Group_Course"));
        });

        modelBuilder.Entity<IsCourse>(entity =>
        {
            entity.ToTable("IsCourse", tb => tb.HasTrigger("trg_delete_IsCourse"));
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

        modelBuilder.Entity<LevelContribution>(entity =>
        {
            entity.ToTable("LevelContribution", tb => tb.HasTrigger("trg_delete_LevelContribution"));

            entity.HasOne(d => d.id_facultyNavigation).WithMany(p => p.LevelContributions).HasConstraintName("FK_LevelContribution_Faculty");
        });

        modelBuilder.Entity<LogStatus>(entity =>
        {
            entity.ToTable("LogStatus", tb => tb.HasTrigger("trg_delete_LogStatus"));
        });

        modelBuilder.Entity<Log_Syllabus>(entity =>
        {
            entity.HasOne(d => d.id_syllabusNavigation).WithMany(p => p.Log_Syllabi).HasConstraintName("FK_Log_Syllabus_Syllabus");
        });

        modelBuilder.Entity<MappingCLOBySyllabus>(entity =>
        {
            entity.ToTable("MappingCLOBySyllabus", tb => tb.HasTrigger("trg_delete_MappingCLOBySyllabus"));

            entity.HasOne(d => d.id_syllabusNavigation).WithMany(p => p.MappingCLOBySyllabi).HasConstraintName("FK_MappingCLOBySyllabus_Syllabus");
        });

        modelBuilder.Entity<MappingCLObyPI>(entity =>
        {
            entity.HasOne(d => d.Id_LevelNavigation).WithMany(p => p.MappingCLObyPIs).HasConstraintName("FK_MappingCLObyPI_LevelContribution");

            entity.HasOne(d => d.Id_PINavigation).WithMany(p => p.MappingCLObyPIs).HasConstraintName("FK_MappingCLObyPI_PerformanceIndicator");

            entity.HasOne(d => d.id_CLoMappingNavigation).WithMany(p => p.MappingCLObyPIs).HasConstraintName("FK_MappingCLObyPI_MappingCLOBySyllabus");
        });

        modelBuilder.Entity<OpenSyllabusWindowsCourse>(entity =>
        {
            entity.HasOne(d => d.created_byNavigation).WithMany(p => p.OpenSyllabusWindowsCourses).HasConstraintName("FK_OpenSyllabusWindowsCourse_Users");

            entity.HasOne(d => d.id_courseNavigation).WithMany(p => p.OpenSyllabusWindowsCourses).HasConstraintName("FK_OpenSyllabusWindowsCourse_Course");
        });

        modelBuilder.Entity<PerformanceIndicator>(entity =>
        {
            entity.ToTable("PerformanceIndicator", tb => tb.HasTrigger("trg_delete_PerformanceIndicator"));

            entity.HasOne(d => d.Id_PLONavigation).WithMany(p => p.PerformanceIndicators).HasConstraintName("FK_PerformanceIndicator_ProgramLearningOutcome");
        });

        modelBuilder.Entity<ProgramLearningOutcome>(entity =>
        {
            entity.ToTable("ProgramLearningOutcome", tb => tb.HasTrigger("trg_delete_ProgramLearningOutcome"));
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

            entity.HasOne(d => d.create_byNavigation).WithMany(p => p.Syllabi).HasConstraintName("FK_Syllabus_Users");

            entity.HasOne(d => d.id_statusNavigation).WithMany(p => p.Syllabi).HasConstraintName("FK_Syllabus_LogStatus");

            entity.HasOne(d => d.id_teacherbysubjectNavigation).WithMany(p => p.Syllabi).HasConstraintName("FK_Syllabus_TeacherBySubject");
        });

        modelBuilder.Entity<SyllabusApproval>(entity =>
        {
            entity.HasKey(e => e.id_approval).HasName("PK__Syllabus__065E3F5E31EAE3FC");

            entity.HasOne(d => d.id_syllabusNavigation).WithMany(p => p.SyllabusApprovals)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SyllabusApproval_Syllabus");
        });

        modelBuilder.Entity<SyllabusDraft>(entity =>
        {
            entity.HasOne(d => d.id_syllabusNavigation).WithMany(p => p.SyllabusDrafts).HasConstraintName("FK_SyllabusDrafts_Syllabus");

            entity.HasOne(d => d.id_userNavigation).WithMany(p => p.SyllabusDrafts).HasConstraintName("FK_SyllabusDrafts_Users");
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

            entity.Property(e => e.allow_input).HasDefaultValue(1);

            entity.HasOne(d => d.id_contentTypeNavigation).WithMany(p => p.SyllabusTemplateSections).HasConstraintName("FK_SyllabusTemplateSection_ContentType");

            entity.HasOne(d => d.id_dataBindingNavigation).WithMany(p => p.SyllabusTemplateSections).HasConstraintName("FK_SyllabusTemplateSection_DataBinding");

            entity.HasOne(d => d.id_templateNavigation).WithMany(p => p.SyllabusTemplateSections).HasConstraintName("FK__SyllabusT__id_te__6A30C649");
        });

        modelBuilder.Entity<Syllabus_Draft>(entity =>
        {
            entity.HasOne(d => d.id_syllabusNavigation).WithMany(p => p.Syllabus_Drafts).HasConstraintName("FK_Syllabus_Draft_Syllabus");
        });

        modelBuilder.Entity<TeacherBySubject>(entity =>
        {
            entity.ToTable("TeacherBySubject", tb => tb.HasTrigger("trg_delete_TeacherBySubject"));

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

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}

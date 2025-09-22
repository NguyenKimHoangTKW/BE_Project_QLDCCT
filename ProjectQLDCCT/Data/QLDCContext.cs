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

    public virtual DbSet<AssessmentCLO> AssessmentCLOs { get; set; }

    public virtual DbSet<CLO> CLOs { get; set; }

    public virtual DbSet<CLOStatistic> CLOStatistics { get; set; }

    public virtual DbSet<CivilServant> CivilServants { get; set; }

    public virtual DbSet<Course> Courses { get; set; }

    public virtual DbSet<Deparment> Deparments { get; set; }

    public virtual DbSet<Faculty> Faculties { get; set; }

    public virtual DbSet<FunctionUser> FunctionUsers { get; set; }

    public virtual DbSet<LogOperation> LogOperations { get; set; }

    public virtual DbSet<Syllabus> Syllabi { get; set; }

    public virtual DbSet<SyllabusApproval> SyllabusApprovals { get; set; }

    public virtual DbSet<SyllabusAssessment> SyllabusAssessments { get; set; }

    public virtual DbSet<SyllabusSection> SyllabusSections { get; set; }

    public virtual DbSet<SyllabusSectionContent> SyllabusSectionContents { get; set; }

    public virtual DbSet<SyllabusTemplate> SyllabusTemplates { get; set; }

    public virtual DbSet<SyllabusTemplateSection> SyllabusTemplateSections { get; set; }

    public virtual DbSet<TrainingProgram> TrainingPrograms { get; set; }

    public virtual DbSet<TypeUser> TypeUsers { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<Year> Years { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Data Source=.;Initial Catalog=dbQLDC;Integrated Security=True;Trust Server Certificate=True");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AssessmentCLO>(entity =>
        {
            entity.HasKey(e => e.id_assess_clo).HasName("PK__Assessme__D6EEA2E41C45109A");

            entity.HasOne(d => d.id_assessmentNavigation).WithMany(p => p.AssessmentCLOs)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Assessmen__id_as__7A672E12");

            entity.HasOne(d => d.id_cloNavigation).WithMany(p => p.AssessmentCLOs)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AssessmentCLO_CLO");
        });

        modelBuilder.Entity<CLO>(entity =>
        {
            entity.HasKey(e => e.id_clo).HasName("PK__CLO__D69619113293FAD2");

            entity.HasOne(d => d.id_syllabusNavigation).WithMany(p => p.CLOs)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__CLO__id_syllabus__73BA3083");
        });

        modelBuilder.Entity<CLOStatistic>(entity =>
        {
            entity.HasKey(e => e.id_clo_stat).HasName("PK__CLOStati__B5D0D8968F968936");

            entity.HasOne(d => d.id_cloNavigation).WithMany(p => p.CLOStatistics)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__CLOStatis__id_cl__02084FDA");
        });

        modelBuilder.Entity<CivilServant>(entity =>
        {
            entity.HasOne(d => d.id_yearNavigation).WithMany(p => p.CivilServants).HasConstraintName("FK_CivilServants_Year");
        });

        modelBuilder.Entity<Course>(entity =>
        {
            entity.HasOne(d => d.id_deparmentNavigation).WithMany(p => p.Courses).HasConstraintName("FK_Course_Deparment");

            entity.HasOne(d => d.id_programNavigation).WithMany(p => p.Courses).HasConstraintName("FK_Course_Program");
        });

        modelBuilder.Entity<Deparment>(entity =>
        {
            entity.HasOne(d => d.id_facultyNavigation).WithMany(p => p.Deparments).HasConstraintName("FK_Deparment_Faculty");
        });

        modelBuilder.Entity<Faculty>(entity =>
        {
            entity.HasOne(d => d.id_yearNavigation).WithMany(p => p.Faculties).HasConstraintName("FK_Faculty_Year");
        });

        modelBuilder.Entity<FunctionUser>(entity =>
        {
            entity.HasOne(d => d.id_type_usersNavigation).WithMany(p => p.FunctionUsers).HasConstraintName("FK_FunctionUsers_TypeUsers");
        });

        modelBuilder.Entity<Syllabus>(entity =>
        {
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

        modelBuilder.Entity<SyllabusAssessment>(entity =>
        {
            entity.HasKey(e => e.id_assessment).HasName("PK__Syllabus__0A74B827A8EABFB7");

            entity.HasOne(d => d.id_syllabusNavigation).WithMany(p => p.SyllabusAssessments)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__SyllabusA__id_sy__76969D2E");
        });

        modelBuilder.Entity<SyllabusSection>(entity =>
        {
            entity.HasKey(e => e.id_section).HasName("PK__Syllabus__3A8312DE091036DE");

            entity.HasOne(d => d.id_syllabusNavigation).WithMany(p => p.SyllabusSections)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__SyllabusS__id_sy__6D0D32F4");

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

            entity.Property(e => e.is_default).HasDefaultValue(true);
        });

        modelBuilder.Entity<SyllabusTemplateSection>(entity =>
        {
            entity.HasKey(e => e.id_template_section).HasName("PK__Syllabus__83EAD1E85B9F7048");

            entity.Property(e => e.is_required).HasDefaultValue(1);

            entity.HasOne(d => d.id_templateNavigation).WithMany(p => p.SyllabusTemplateSections)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__SyllabusT__id_te__6A30C649");
        });

        modelBuilder.Entity<TrainingProgram>(entity =>
        {
            entity.HasKey(e => e.id_program).HasName("PK_Program");

            entity.HasOne(d => d.id_deparmentNavigation).WithMany(p => p.TrainingPrograms).HasConstraintName("FK_Program_Deparment");

            entity.HasOne(d => d.id_facultyNavigation).WithMany(p => p.TrainingPrograms).HasConstraintName("FK_Program_Faculty");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasOne(d => d.id_type_usersNavigation).WithMany(p => p.Users).HasConstraintName("FK_Users_TypeUsers");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}

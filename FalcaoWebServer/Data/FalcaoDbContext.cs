#nullable enable
using FalcaoPwaShared.Models.Auth;
using FalcaoPwaShared.Models.Tbl;
using FalcaoPwaShared.Models.Uvw;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace FalcaoWebServer.Data
{
    public class FalcaoDbContext : DbContext
    {
        public FalcaoDbContext(DbContextOptions<FalcaoDbContext> opts) : base(opts) { }

        // === Tabelas do sistema de autenticação ===
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Role> Roles { get; set; } = null!;
        public DbSet<UserRole> UserRoles { get; set; } = null!;
        public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;
        public DbSet<EmailPin> EmailPins { get; set; } = null!;
        public DbSet<TempPassword> TempPasswords { get; set; } = null!;
        public DbSet<Rota> Rotas { get; set; } = null!;
        public DbSet<Perfil> Perfis { get; set; } = null!;
        public DbSet<Menu> Menus { get; set; } = null!;

        // === Tabelas (TBL) ===
        public DbSet<TblBairro> TblBairro { get; set; } = null!;
        public DbSet<TblCep> TblCep { get; set; } = null!;
        public DbSet<TblCidade> TblCidade { get; set; } = null!;
        public DbSet<TblEstado> TblEstado { get; set; } = null!;
        public DbSet<TblLogradouro> TblLogradouro { get; set; } = null!;
        public DbSet<TblParametro> TblParametro { get; set; } = null!;
        public DbSet<TblPessoa> TblPessoa { get; set; } = null!;
        public DbSet<TblPessoaEmail> TblPessoaEmail { get; set; } = null!;
        public DbSet<TblPessoaFisica> TblPessoaFisica { get; set; } = null!;
        public DbSet<TblPessoaJuridica> TblPessoaJuridica { get; set; } = null!;
        public DbSet<TblPessoaCliente> TblPessoaCliente { get; set; } = null!;


        // === Views (Uvw) ===
        public DbSet<UvwBairro> UvwBairro { get; set; } = null!;
        public DbSet<UvwCep> UvwCep { get; set; } = null!;
        public DbSet<UvwCidade> UvwCidade { get; set; } = null!;
        public DbSet<UvwEstado> UvwEstado { get; set; } = null!;
        public DbSet<UvwLogradouro> UvwLogradouro { get; set; } = null!;
        public DbSet<UvwParametro> UvwParametro { get; set; } = null!;
        public DbSet<UvwPessoa> UvwPessoa { get; set; } = null!;
        public DbSet<UvwPessoaCliente> UvwPessoaCliente { get; set; } = null!;
        public DbSet<UvwPessoaEmail> UvwPessoaEmail { get; set; } = null!;
        public DbSet<UvwPessoaFisica> UvwPessoaFisica { get; set; } = null!;
        public DbSet<UvwPessoaJuridica> UvwPessoaJuridica { get; set; } = null!;
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // === Tabelas com chave (mantidas do seu código original) ===
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(u => u.Id);

                entity.Property(u => u.TipoUsuario)
                        .HasConversion<string>()
                        .HasColumnType("enum('Ceo','Administrador','Vendedor','Cliente','Operador')")
                        .HasMaxLength(20)
                        .IsRequired();
            });

            modelBuilder.Entity<Role>().ToTable("Roles").HasKey(r => r.Id);
            modelBuilder.Entity<UserRole>().ToTable("UserRoles").HasKey(ur => new { ur.UserId, ur.RoleId });
            modelBuilder.Entity<RefreshToken>().ToTable("RefreshTokens").HasKey(rt => rt.Id);
            modelBuilder.Entity<TempPassword>().ToTable("TempPasswords").HasKey(tp => tp.Id);

            // TBLs principais — definindo chaves e conversões de enums que são armazenados como enum(...) no MySQL
            modelBuilder.Entity<TblBairro>().HasKey(e => e.ID_Bairro);

            modelBuilder.Entity<TblCep>(entity =>
            {
                entity.HasKey(e => e.ID_Cep);

                // Colunas do tipo enum(...) no MySQL — persistir o enum como string
                entity.Property(e => e.TipoCep)
                        .HasConversion<string>()
                        .HasColumnType("enum('Definitivo','Provisorio','Unico')")
                        .IsRequired();

                entity.Property(e => e.Estatus)
                        .HasConversion<string>()
                        .HasColumnType("enum('Ativo','Inativo','Bloqueado')")
                        .IsRequired()
                        .HasDefaultValue(FalcaoPwaShared.Enums.EstatusEnum.Ativo);

                entity.Property(e => e.Resultado)
                        .HasConversion<string>()
                        .HasColumnType("enum('OK','NaoEncontrado','ProblemaPesquisa','CorreioForaOperacao')")
                        .IsRequired()
                        .HasDefaultValue(FalcaoPwaShared.Enums.ResultadoEnum.OK);
            });

            modelBuilder.Entity<TblCidade>().HasKey(e => e.ID_Cidade);


            modelBuilder.Entity<TblEstado>().HasKey(e => e.ID_Estado);
            modelBuilder.Entity<TblLogradouro>().HasKey(e => e.ID_Logradouro);
            modelBuilder.Entity<TblParametro>().HasKey(e => e.ID_Parametro);
            modelBuilder.Entity<TblPessoa>(entity =>
            {
                entity.HasKey(e => e.ID_Pessoa);

                // Converte o enum para string ao persistir — EF passará "Ativo" / "Inativo" / "Bloqueado"
                entity.Property(e => e.Estatus)
                        .HasConversion<string>()
                        .HasColumnType("enum('Ativo','Inativo','Bloqueado')")
                        .IsRequired()
                        .HasDefaultValue(FalcaoPwaShared.Enums.EstatusEnum.Ativo);
            });
            modelBuilder.Entity<TblPessoaCliente>().HasKey(e => e.ID_PessoaCliente);
            modelBuilder.Entity<TblPessoaFisica>().HasKey(e => e.ID_PessoaFisica);
            modelBuilder.Entity<TblPessoaEmail>(entity =>
            {
                entity.HasKey(e => e.ID_PessoaEmail);
                entity.Property(e => e.Setor)
                        .HasConversion<string>()
                        .HasMaxLength(50);
                entity.Property(e => e.SelecionadoParaImprimir)
                        .HasConversion<string>()
                        .HasMaxLength(10);
            });
            modelBuilder.Entity<TblPessoaJuridica>().HasKey(e => e.ID_PessoaJuridica);

            // Rota
            modelBuilder.Entity<Rota>(entity =>
            {
                entity.ToTable("rotas");
                entity.HasKey(r => r.Id);

                entity.Property(r => r.Nome)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(r => r.Caminho)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(r => r.Icone)
                    .HasMaxLength(50);

                entity.Property(r => r.Nome)
                    .HasMaxLength(200);

                entity.Property(r => r.OrdemGlobal)
                    .HasDefaultValue(0);

                entity.Property(r => r.Ativo)
                    .HasDefaultValue(true);

                entity.Property(r => r.CreatedAt)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(r => r.UpdatedAt)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP")
                    .ValueGeneratedOnAddOrUpdate();

                // Indexes
                entity.HasIndex(r => r.Caminho).IsUnique();
                entity.HasIndex(r => r.OrdemGlobal);
                entity.HasIndex(r => r.Ativo);
            });

            // Perfil
            modelBuilder.Entity<Perfil>(entity =>
            {
                entity.ToTable("perfis");
                entity.HasKey(p => p.Id);

                entity.Property(p => p.Codigo)
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(p => p.Descricao)
                    .HasMaxLength(100);

                entity.Property(p => p.NivelAcesso)
                    .HasDefaultValue(1);

                entity.Property(p => p.Ativo)
                    .HasDefaultValue(true);

                entity.Property(p => p.CreatedAt)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(p => p.UpdatedAt)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP")
                    .ValueGeneratedOnAddOrUpdate();

                // Indexes
                entity.HasIndex(p => p.Codigo).IsUnique();
                entity.HasIndex(p => p.NivelAcesso);
                entity.HasIndex(p => p.Ativo);
            });

            // Menu (chave composta)
            modelBuilder.Entity<Menu>(entity =>
            {
                entity.ToTable("menus");
                entity.HasKey(m => new { m.PerfilId, m.RotaId });

                entity.Property(m => m.Ordem)
                    .IsRequired(false);

                entity.Property(m => m.Ativo)
                    .HasDefaultValue(true);

                entity.Property(m => m.CreatedAt)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                // Relacionamentos
                entity.HasOne(m => m.Perfil)
                    .WithMany()
                    .HasForeignKey(m => m.PerfilId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(m => m.Rota)
                    .WithMany()
                    .HasForeignKey(m => m.RotaId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Indexes
                entity.HasIndex(m => m.Ordem);
                entity.HasIndex(m => m.Ativo);
            });


            // -------------------------
            // === VIEWS (Uvw) ===
            // -------------------------
            // NOTE: For views where we need Include()/relationships, we can define a logical PK
            // only if the underlying view guarantees uniqueness for that column.
            // Otherwise keep HasNoKey().

            // UvwCep
            modelBuilder.Entity<UvwCep>(entity =>
            {
                entity.ToView("UvwCep");
                entity.HasNoKey();
                entity.Property(e => e.TipoCep).HasConversion<string>();
                entity.Property(e => e.Estatus).HasConversion<string>();
                entity.Property(e => e.Resultado).HasConversion<string>();
            });
            // UvwParametro
            modelBuilder.Entity<UvwParametro>(entity =>
            {
                entity.ToView("UvwParametro");
                entity.HasNoKey();
            });
            // UvwPessoa
            modelBuilder.Entity<UvwPessoa>(entity =>
            {
                entity.ToView("UvwPessoa");
                entity.HasNoKey();
                entity.Property(e => e.Estatus).HasConversion<string>();
            });
            // UvwPessoaCliente
            modelBuilder.Entity<UvwPessoaCliente>(entity =>
            {
                entity.HasKey(e => e.ID_PessoaCliente);
                entity.ToView("UvwPessoaCliente");
                entity.Property(e => e.Estatus).HasConversion<string>();
            });
            // UvwPessoaEmail
            modelBuilder.Entity<UvwPessoaEmail>(entity =>
            {
                entity.HasKey(e => e.ID_PessoaEmail);
                entity.ToView("UvwPessoaEmail");
                entity.Property(e => e.SelecionadoParaImprimir).HasConversion<string>();
                entity.Property(e => e.Setor).HasConversion<string>();
            });
            MapView<UvwBairro>(modelBuilder, "UvwBairro");
            MapView<UvwCidade>(modelBuilder, "UvwCidade");
            MapView<UvwEstado>(modelBuilder, "UvwEstado");
            MapView<UvwLogradouro>(modelBuilder, "UvwLogradouro");
            MapView<UvwPessoaFisica>(modelBuilder, "UvwPessoaFisica");
            MapView<UvwPessoaJuridica>(modelBuilder, "UvwPessoaJuridica");

            base.OnModelCreating(modelBuilder);
        }

        // Helper genérico para views simples (keyless)
        private static void MapView<T>(ModelBuilder builder, string viewName) where T : class
        {
            builder.Entity<T>(entity =>
            {
                entity.ToView(viewName);
                entity.HasNoKey();
            });
        }
    }
}

#nullable enable
using CanindePwaShared.Enums;
using CanindePwaShared.Models.Auth;
using CanindePwaShared.Models.Tbl;
using CanindePwaShared.Models.Uvw;
using Microsoft.EntityFrameworkCore;

namespace CanindeWebServer.Data
{
    /// <summary>
    /// ProjetoDbContext — mapeamento EF Core do banco MySQL "caninde" (BANCO EM PRODUÇÃO).
    ///
    /// ═══════════════════════════════════════════════════════════════════════
    /// REGRAS DESTE ARQUIVO (não negociáveis)
    /// ═══════════════════════════════════════════════════════════════════════
    /// 1. O banco já existe e está em produção. O EF só LÊ e GRAVA DADOS.
    ///    NUNCA usar Migrations, EnsureCreated, Migrate ou qualquer comando que altere estrutura.
    ///    Tabelas existentes NÃO são alteradas. Novas etapas do projeto usam TABELAS NOVAS.
    /// 2. O mapeamento segue a estrutura REAL, obtida com SHOW CREATE TABLE em 2026-11-19.
    ///    Se uma coluna não existe no banco, a propriedade correspondente é IGNORADA aqui.
    /// 3. Enums do C# NÃO são alterados para caber no banco: a tradução é feita por
    ///    EnumConverters (ex.: C# Inativo ↔ banco 'Desativado').
    ///
    /// LEGENDA DOS COMENTÁRIOS
    ///   [VERIFICADO]   confirmado contra a estrutura real do banco (2026-11-19)
    ///   [SCRIPT JWT]   conforme __ScriptJWT_CANINDE.sql — ainda NÃO conferido no banco vivo
    ///   [ENTIDADE ?]   depende da classe de entidade, que precisa ser conferida
    ///   [DECISÃO]      depende de decisão de negócio ainda pendente
    ///
    /// NOMES DE TABELA/VIEW: gravados em minúsculas, exatamente como o banco devolve
    /// (lower_case_table_names do servidor). Se o servidor de produção for Linux com
    /// lower_case_table_names=0, conferir a caixa dos nomes antes de publicar.
    /// </summary>
    public class ProjetoDbContext : DbContext
    {
        public ProjetoDbContext(DbContextOptions<ProjetoDbContext> opts) : base(opts) { }

        // === Autenticação (tabelas do JWT/Cookie do AspNet.Core) ===
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

        // === Perfis de pessoa (tblcdpessoa*) ===
        public DbSet<TblCdPessoaParceiroFrete> TblCdPessoaParceiroFrete { get; set; } = null!;
        public DbSet<TblCdPessoaCliente> TblCdPessoaCliente { get; set; } = null!;
        public DbSet<TblCdPessoaEmbarcador> TblCdPessoaEmbarcador { get; set; } = null!;
        public DbSet<TblCdPessoaCorretor> TblCdPessoaCorretor { get; set; } = null!;
        public DbSet<TblCdPessoaAgenciador> TblCdPessoaAgenciador { get; set; } = null!;
        public DbSet<TblCdPessoaMotorista> TblCdPessoaMotorista { get; set; } = null!;
        public DbSet<TblCdPessoaFornecedor> TblCdPessoaFornecedor { get; set; } = null!;
        public DbSet<TblCdPessoaProdutorRural> TblCdPessoaProdutorRural { get; set; } = null!;
        public DbSet<TblCdPessoaProdutorRuralContaBancaria> TblCdPessoaProdutorRuralContaBancaria { get; set; } = null!;
        public DbSet<TblCdPessoaProdutorRuralPropriedade> TblCdPessoaProdutorRuralPropriedade { get; set; } = null!;
        public DbSet<TblCdPropriedade> TblCdPropriedade { get; set; } = null!;

        // === Views (Uvw) — SOMENTE LEITURA ===
        public DbSet<UvwBairro> UvwBairro { get; set; } = null!;
        public DbSet<UvwCep> UvwCep { get; set; } = null!;
        public DbSet<UvwCidade> UvwCidade { get; set; } = null!;
        public DbSet<UvwEstado> UvwEstado { get; set; } = null!;
        public DbSet<UvwLogradouro> UvwLogradouro { get; set; } = null!;
        public DbSet<UvwParametro> UvwParametro { get; set; } = null!;
        public DbSet<UvwPessoa> UvwPessoa { get; set; } = null!;
        public DbSet<UvwPessoaEmail> UvwPessoaEmail { get; set; } = null!;
        public DbSet<UvwPessoaFisica> UvwPessoaFisica { get; set; } = null!;
        public DbSet<UvwPessoaJuridica> UvwPessoaJuridica { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ============================================================
            // 1. AUTENTICAÇÃO  [SCRIPT JWT]
            // Tabelas criadas por __ScriptJWT_CANINDE.sql (users, roles, userroles, refreshtokens,
            // temppasswords, emailpins, rotas, perfis, menus). Os nomes Users/Roles/UserRoles/
            // RefreshTokens/TempPasswords funcionam hoje porque o servidor ignora maiúsculas.
            // ============================================================
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(u => u.Id);

                // [DECISÃO] O UsuarioEnum tem Financeiro e Site, mas a coluna só aceita
                // 'Ceo','Administrador','Operador'. Gravar esses dois papéis falha até a coluna
                // ser ampliada. Não alterado aqui (regra: não mexer em tabelas existentes).
                entity.Property(u => u.TipoUsuario)
                      .HasConversion<string>()
                      .HasColumnType("enum('Ceo','Administrador','Operador')")
                      .IsRequired();
            });

            modelBuilder.Entity<Role>().ToTable("Roles").HasKey(r => r.Id);
            modelBuilder.Entity<UserRole>().ToTable("UserRoles").HasKey(ur => new { ur.UserId, ur.RoleId });
            modelBuilder.Entity<RefreshToken>().ToTable("RefreshTokens").HasKey(rt => rt.Id);
            modelBuilder.Entity<TempPassword>().ToTable("TempPasswords").HasKey(tp => tp.Id);

            // ============================================================
            // 2. ENDEREÇO  [VERIFICADO]
            // Uma pessoa aponta para UM CEP (tblpessoa.ID_Cep). O mesmo CEP serve várias pessoas.
            // Estrutura: tblcep → tbllogradouro, tblbairro, tblcidade, tblestado.
            // tblpessoacep e tblcidadeestado NÃO existem no banco atual: não usar.
            // ============================================================
            modelBuilder.Entity<TblLogradouro>(entity =>
            {
                entity.ToTable("tbllogradouro");
                entity.HasKey(e => e.ID_Logradouro);
                entity.Property("Descricao").IsRequired().HasMaxLength(100);   // varchar(100)
            });

            // CORREÇÃO: esta configuração tinha desaparecido do arquivo. Sem chave definida
            // (ID_Bairro não é reconhecida por convenção) o EF recusa montar o modelo.
            modelBuilder.Entity<TblBairro>(entity =>
            {
                entity.ToTable("tblbairro");
                entity.HasKey(e => e.ID_Bairro);
                entity.Property("Descricao").IsRequired().HasMaxLength(50);    // varchar(50)
            });

            modelBuilder.Entity<TblCidade>(entity =>
            {
                entity.ToTable("tblcidade");
                entity.HasKey(e => e.ID_Cidade);
                entity.Property("Descricao").IsRequired().HasMaxLength(50);    // varchar(50)
            });

            // Os 27 estados já estão cadastrados: a API só CONSULTA por UF, nunca cria.
            modelBuilder.Entity<TblEstado>(entity =>
            {
                entity.ToTable("tblestado");
                entity.HasKey(e => e.ID_Estado);
                entity.Property(e => e.Descricao).IsRequired().HasMaxLength(25); // varchar(25)
                entity.Property(e => e.Uf).IsRequired().HasMaxLength(2);         // varchar(2)
            });

            modelBuilder.Entity<TblCep>(entity =>
            {
                entity.ToTable("tblcep");
                entity.HasKey(e => e.ID_Cep);

                // varchar(9), SEM índice único no banco: podem existir CEPs repetidos.
                entity.Property(e => e.Cep).IsRequired().HasMaxLength(9);

                // Banco: 'NaoCadastrado'|'Provisorio'|'PorCidade'|'PorRua'
                // C#:    Provisorio | Unico (=PorCidade) | Definitivo (=PorRua)
                entity.Property(e => e.TipoCep)
                      .HasConversion(EnumConverters.TipoCep)
                      .HasColumnType("enum('NaoCadastrado','Provisorio','PorCidade','PorRua')")
                      .IsRequired();

                // Banco: 'Ativo'|'Desativado'|'Bloqueado'  ↔  C#: Ativo|Inativo|Bloqueado
                entity.Property(e => e.Estatus)
                      .HasConversion(EnumConverters.Estatus)
                      .HasColumnType("enum('Ativo','Desativado','Bloqueado')")
                      .IsRequired()
                      .HasDefaultValue(EstatusEnum.Ativo);

                // Banco: varchar(45) NULL — todos os CEPs atuais têm Resultado nulo.
                // [ENTIDADE ?] a propriedade em TblCep.cs precisa ser ResultadoEnum? (anulável).
                entity.Property(e => e.Resultado)
                      .HasConversion<string>()
                      .HasMaxLength(45);
            });

            modelBuilder.Entity<TblParametro>().HasKey(e => e.ID_Parametro);   // estrutura não conferida

            // ============================================================
            // 3. PESSOA  [VERIFICADO]
            // tblpessoa guarda o que é comum; os dados específicos ficam em tblpessoafisica OU
            // tblpessoajuridica. CodigoPessoaTipo: 1 = física (2.713 pessoas), 2 = jurídica (138).
            // A pessoa NÃO tem e-mail próprio: o e-mail fica em tblpessoaemail e a pessoa aponta
            // para ele por ID_PessoaEmailCadastro (int, default 0, sem chave estrangeira).
            // A tabela NÃO tem created_at/updated_at.
            // ============================================================
            modelBuilder.Entity<TblPessoa>(entity =>
            {
                entity.ToTable("tblpessoa");
                entity.HasKey(e => e.ID_Pessoa);

                entity.Property(e => e.Numero).HasMaxLength(10);              // varchar(10) NULL
                entity.Property(e => e.ComplementoNumero).HasMaxLength(45);   // varchar(45) NULL
                entity.Property(e => e.DddFixo).HasMaxLength(2);              // varchar(2)  NULL
                entity.Property(e => e.Fixo).HasMaxLength(20);                // varchar(20) NULL
                entity.Property(e => e.DddCelular).HasMaxLength(2);           // varchar(2)  NULL
                entity.Property(e => e.Celular).HasMaxLength(20);             // varchar(20) NULL
                entity.Property(e => e.Contato).HasMaxLength(45);             // varchar(45) NULL

                // Banco: 'Ativo'|'Desativado'|'Bloqueado' (NOT NULL, default 'Ativo')
                entity.Property(e => e.Estatus)
                      .HasConversion(EnumConverters.Estatus)
                      .HasColumnType("enum('Ativo','Desativado','Bloqueado')")
                      .IsRequired()
                      .HasDefaultValue(EstatusEnum.Ativo);

                // [ENTIDADE ?] TblPessoa.cs deve ter ID_PessoaEmailCadastro (int) e NÃO ter Email,
                // Foto, CreatedAt, UpdatedAt (colunas inexistentes). Se tiver, ignorar com
                // entity.Ignore("NomeDaPropriedade") ou remover da classe.
            });

            // Pessoa física — UNIQUE no Cpf. NÃO existem: Foto, created_at, updated_at.
            modelBuilder.Entity<TblPessoaFisica>(entity =>
            {
                entity.ToTable("tblpessoafisica");
                entity.HasKey(e => e.ID_PessoaFisica);
                entity.Property(e => e.Nome).IsRequired().HasMaxLength(100);  // varchar(100)
                entity.Property(e => e.Cpf).IsRequired().HasMaxLength(20);    // varchar(20), UNIQUE
                entity.Property(e => e.Rg).HasMaxLength(30);                  // varchar(30) NULL

                // Defesa: colunas que existiam no script antigo e não existem no banco real.
                entity.Ignore("Foto");
                entity.Ignore("CreatedAt");
                entity.Ignore("UpdatedAt");
            });

            // Pessoa jurídica — UNIQUE no Cnpj. NÃO existem: Logotipo, created_at, updated_at.
            modelBuilder.Entity<TblPessoaJuridica>(entity =>
            {
                entity.ToTable("tblpessoajuridica");
                entity.HasKey(e => e.ID_PessoaJuridica);
                entity.Property(e => e.RazaoSocial).IsRequired().HasMaxLength(100); // varchar(100)
                entity.Property(e => e.Cnpj).IsRequired().HasMaxLength(20);         // varchar(20), UNIQUE
                entity.Property(e => e.InscricaoEstadual).HasMaxLength(30);         // varchar(30) NULL

                entity.Ignore("Logotipo");
                entity.Ignore("CreatedAt");
                entity.Ignore("UpdatedAt");
            });

            // E-mail da pessoa. Sem chave estrangeira para tblpessoa e sem UNIQUE (ID_Pessoa, Setor):
            // a aplicação é quem garante a consistência (um e-mail por setor).
            modelBuilder.Entity<TblPessoaEmail>(entity =>
            {
                entity.ToTable("tblpessoaemail");
                entity.HasKey(e => e.ID_PessoaEmail);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(255);   // coluna "email" varchar(255)

                // Banco: 'Cadastro','Administrativo','Financeiro','Logistica','Compra','Venda'.
                // [DECISÃO] O SetorEnum precisa dos 6 valores (faltam Logistica, Compra, Venda);
                // hoje já existem e-mails de Logistica (4) e Venda (10) no banco.
                entity.Property(e => e.Setor)
                      .HasConversion<string>()
                      .HasMaxLength(50);

                // Banco: 'Sim'|'Nao', default 'Nao'. Atenção: o default do C# (SimNaoEnum) é Sim.
                // Sempre informar o valor ao gravar.
                entity.Property(e => e.SelecionadoParaImprimir)
                      .HasConversion<string>()
                      .HasColumnType("enum('Sim','Nao')")
                      .IsRequired();
            });

            // ============================================================
            // 4. PERFIS DE PESSOA  [VERIFICADO]
            // O perfil é a EXISTÊNCIA de uma linha em tblcdpessoa<perfil> para aquela pessoa.
            // Pessoa sem linha em nenhuma das 8 tabelas = pessoa sem perfil (estado válido).
            // Regra de negócio (validada no service, o banco não impõe): nenhum perfil, um
            // perfil, ou exatamente Embarcador + Agenciador.
            // NENHUMA tabela de perfil tem created_at/updated_at e nenhuma tem ID_MobileUsuario.
            // ============================================================
            MapPerfil<TblCdPessoaCliente>(modelBuilder, "tblcdpessoacliente", "ID_PessoaCliente");
            MapPerfil<TblCdPessoaMotorista>(modelBuilder, "tblcdpessoamotorista", "ID_PessoaMotorista");
            MapPerfil<TblCdPessoaFornecedor>(modelBuilder, "tblcdpessoafornecedor", "ID_PessoaFornecedor");
            MapPerfil<TblCdPessoaProdutorRural>(modelBuilder, "tblcdpessoaprodutorrural", "ID_PessoaProdutorRural");

            // [DECISÃO] Estes 4 têm ID_Usuario NOT NULL com chave estrangeira para tblusuario
            // (usuário do Windows Forms). Como a tabela não pode ser alterada, a API só consegue
            // gravá-los informando um ID_Usuario válido. Ver decisão pendente.
            MapPerfil<TblCdPessoaEmbarcador>(modelBuilder, "tblcdpessoaembarcador", "ID_PessoaEmbarcador");
            MapPerfil<TblCdPessoaAgenciador>(modelBuilder, "tblcdpessoaagenciador", "ID_PessoaAgenciador");
            MapPerfil<TblCdPessoaCorretor>(modelBuilder, "tblcdpessoacorretor", "ID_PessoaCorretor");
            MapPerfil<TblCdPessoaParceiroFrete>(modelBuilder, "tblcdpessoaparceirofrete", "ID_PessoaParceiroFrete");

            // Motorista: só ID_PessoaMotorista e ID_Pessoa. NÃO tem ID_PessoaAgenciador.
            modelBuilder.Entity<TblCdPessoaMotorista>().Ignore("ID_PessoaAgenciador");

            modelBuilder.Entity<TblCdPessoaFornecedor>()
                        .Property(e => e.NomeFantasia).IsRequired().HasMaxLength(45);  // varchar(45) NOT NULL

            modelBuilder.Entity<TblCdPessoaProdutorRural>()
                        .Property(e => e.Pis).HasMaxLength(20);                        // varchar(20) NULL

            // ------------------------------------------------------------
            // Conta bancária do produtor rural [VERIFICADO]
            // Colunas: ID_Banco, ID_Cidade, ID_Estado (separados), Agencia(10), Conta(20),
            //          TipoConta enum('Corrente','Poupança'), Compensacao(5) NULL,
            //          Estatus enum('Ativo','Desativado','Bloqueado').
            // [ENTIDADE ?] conferir Estatus/TipoConta na entidade antes de aplicar conversores.
            // ------------------------------------------------------------
            MapPerfil<TblCdPessoaProdutorRuralContaBancaria>(modelBuilder,
                "tblcdpessoaprodutorruralcontabancaria", "ID_PessoaProdutorRuralContaBancaria");
            modelBuilder.Entity<TblCdPessoaProdutorRuralContaBancaria>().Ignore("ID_CidadeEstado");

            // Vínculo produtor × propriedade [VERIFICADO]: só os 3 IDs. Sem Estatus e sem datas.
            MapPerfil<TblCdPessoaProdutorRuralPropriedade>(modelBuilder,
                "tblcdpessoaprodutorruralpropriedade", "ID_PessoaProdutorRuralPropriedade");
            modelBuilder.Entity<TblCdPessoaProdutorRuralPropriedade>().Ignore("Estatus");

            // Propriedade [VERIFICADO]: ID_Cidade, ID_Estado, NomePropriedade(100), Ipr(45, UNIQUE),
            // Caepf(20) NULL, Car(255) NULL, Estatus enum(...Desativado...), Sustentavel enum Sim/Nao.
            // [ENTIDADE ?] conferir Estatus/Sustentavel na entidade.
            MapPerfil<TblCdPropriedade>(modelBuilder, "tblcdpropriedade", "ID_Propriedade");
            modelBuilder.Entity<TblCdPropriedade>().Ignore("ID_CidadeEstado");
            modelBuilder.Entity<TblCdPropriedade>().Ignore("EnderecoPropriedade");

            // ============================================================
            // 5. ROTAS / PERFIS DE ACESSO / MENUS  [SCRIPT JWT]
            // "perfis" aqui = perfis de ACESSO ao sistema (CEO/ADMIN/OPERADOR), diferentes dos
            // perfis de PESSOA (Embarcador, Agenciador...).
            // ============================================================
            modelBuilder.Entity<Rota>(entity =>
            {
                entity.ToTable("rotas");
                entity.HasKey(r => r.Id);

                entity.Property(r => r.Nome).IsRequired().HasMaxLength(50);
                entity.Property(r => r.Caminho).IsRequired().HasMaxLength(100);
                entity.Property(r => r.Icone).HasMaxLength(50);

                // CORREÇÃO: no script a coluna se chama "Ordem"; sem isto o EF consultaria
                // "OrdemGlobal" (coluna inexistente).
                entity.Property(r => r.OrdemGlobal).HasColumnName("Ordem").HasDefaultValue(0);

                entity.Property(r => r.Ativo).HasDefaultValue(true);
                entity.Property(r => r.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(r => r.UpdatedAt)
                      .HasDefaultValueSql("CURRENT_TIMESTAMP")
                      .ValueGeneratedOnAddOrUpdate();

                entity.HasIndex(r => r.Caminho).IsUnique();
                entity.HasIndex(r => r.OrdemGlobal);
                entity.HasIndex(r => r.Ativo);
            });

            modelBuilder.Entity<Perfil>(entity =>
            {
                entity.ToTable("perfis");
                entity.HasKey(p => p.Id);

                entity.Property(p => p.Codigo).IsRequired().HasMaxLength(20);
                entity.Property(p => p.Descricao).HasMaxLength(100);
                entity.Property(p => p.NivelAcesso).HasDefaultValue(1);
                entity.Property(p => p.Ativo).HasDefaultValue(true);
                entity.Property(p => p.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(p => p.UpdatedAt)
                      .HasDefaultValueSql("CURRENT_TIMESTAMP")
                      .ValueGeneratedOnAddOrUpdate();

                entity.HasIndex(p => p.Codigo).IsUnique();
                entity.HasIndex(p => p.NivelAcesso);
                entity.HasIndex(p => p.Ativo);
            });

            modelBuilder.Entity<Menu>(entity =>
            {
                entity.ToTable("menus");
                entity.HasKey(m => new { m.PerfilId, m.RotaId });

                entity.Property(m => m.Ordem).IsRequired(false);
                entity.Property(m => m.Ativo).HasDefaultValue(true);
                entity.Property(m => m.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasOne(m => m.Perfil)
                      .WithMany()
                      .HasForeignKey(m => m.PerfilId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(m => m.Rota)
                      .WithMany()
                      .HasForeignKey(m => m.RotaId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(m => m.Ordem);
                entity.HasIndex(m => m.Ativo);
            });

            // ============================================================
            // 6. VIEWS (somente leitura)  [VERIFICADO nas views do script]
            // O EF não grava em view: toda escrita vai para as tabelas.
            // ============================================================
            modelBuilder.Entity<UvwCep>(entity =>
            {
                entity.ToView("uvwcep");
                entity.HasNoKey();
                // A view repassa os valores crus de tblcep: mesmas traduções da tabela.
                entity.Property(e => e.TipoCep).HasConversion(EnumConverters.TipoCep);
                entity.Property(e => e.Estatus).HasConversion(EnumConverters.Estatus);
                entity.Property(e => e.Resultado).HasConversion<string>();   // [ENTIDADE ?] anulável
            });

            modelBuilder.Entity<UvwParametro>(entity =>
            {
                entity.ToView("uvwparametro");
                entity.HasNoKey();
            });

            // UvwPessoa NÃO tem chave: nunca usar FindAsync nela; consultar com
            // FirstOrDefaultAsync(p => p.ID_Pessoa == id).
            modelBuilder.Entity<UvwPessoa>(entity =>
            {
                entity.ToView("uvwpessoa");
                entity.HasNoKey();
                entity.Property(e => e.Estatus).HasConversion(EnumConverters.Estatus);
                // [ENTIDADE ?] a view devolve TipoCep cru ('PorRua'...). Se UvwPessoa.TipoCep for
                // TipoCepEnum, acrescentar: entity.Property(e => e.TipoCep).HasConversion(EnumConverters.TipoCep);
            });

            modelBuilder.Entity<UvwPessoaEmail>(entity =>
            {
                entity.HasKey(e => e.ID_PessoaEmail);
                entity.ToView("uvwpessoaemail");
                entity.Property(e => e.SelecionadoParaImprimir).HasConversion<string>();
                entity.Property(e => e.Setor).HasConversion<string>();   // exige os 6 setores no SetorEnum
            });

            MapView<UvwBairro>(modelBuilder, "uvwbairro");
            MapView<UvwCidade>(modelBuilder, "uvwcidade");
            MapView<UvwEstado>(modelBuilder, "uvwestado");
            MapView<UvwLogradouro>(modelBuilder, "uvwlogradouro");
            MapView<UvwPessoaFisica>(modelBuilder, "uvwpessoafisica");
            MapView<UvwPessoaJuridica>(modelBuilder, "uvwpessoajuridica");
        }

        /// <summary>
        /// Mapeia uma tabela de perfil/sub-tabela do cadastro de pessoa.
        /// Tabela, chave primária autoincremento e IGNORA as colunas que existiam no script antigo
        /// e NÃO existem no banco real (created_at, updated_at, ID_MobileUsuario).
        /// Ignore(string) é inofensivo se a propriedade nem existir na classe.
        /// </summary>
        private static void MapPerfil<T>(ModelBuilder modelBuilder, string tabela, string chavePrimaria) where T : class
        {
            modelBuilder.Entity<T>(entity =>
            {
                entity.ToTable(tabela);
                entity.HasKey(chavePrimaria);
                entity.Property(chavePrimaria).ValueGeneratedOnAdd();

                entity.Ignore("CreatedAt");
                entity.Ignore("UpdatedAt");
                entity.Ignore("ID_MobileUsuario");
            });
        }

        /// <summary>Mapeia uma entidade como view (sem chave primária, somente leitura).</summary>
        private static void MapView<T>(ModelBuilder modelBuilder, string viewName) where T : class
        {
            modelBuilder.Entity<T>(entity =>
            {
                entity.ToView(viewName);
                entity.HasNoKey();
            });
        }

        /*
         * ═══════════════════════════════════════════════════════════════════════
         * PENDÊNCIAS DE CONFERÊNCIA (dependem de arquivos de entidade ainda não revisados)
         * ═══════════════════════════════════════════════════════════════════════
         *  1. TblCep.cs / UvwCep.cs      : Resultado deve ser ResultadoEnum? (anulável).
         *  2. TblPessoa.cs                : precisa de ID_PessoaEmailCadastro; sem Email/Foto/datas.
         *  3. TblCdPessoa* (perfis)       : remover CreatedAt/UpdatedAt das classes (já ignorados aqui).
         *  4. Embarcador/Agenciador/Corretor/ParceiroFrete: ID_Usuario é NOT NULL no banco.
         *  5. ContaBancaria/Propriedade   : Estatus e TipoCep/Sustentavel — tipo na entidade (string
         *                                   ou enum?) define se entram EnumConverters.Estatus etc.
         *  6. UvwPessoa.TipoCep           : se for enum, aplicar EnumConverters.TipoCep.
         *  7. SetorEnum                   : acrescentar Logistica, Compra, Venda (só adição).
         *  8. Rotas                       : conferir no banco vivo (colunas do script JWT).
         */
    }
}

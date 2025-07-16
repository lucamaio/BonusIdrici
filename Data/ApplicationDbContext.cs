using Microsoft.EntityFrameworkCore;
using Atto; // Assicurati che Atto.Atto sia commentato se non lo usi
using Dichiarante;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using BonusIdrici2.Models; // Assicurati di avere questo per Ente e FileUpload
using System.Linq; // Necessario per SequenceEqual, Aggregate (se scommenti CfMembri)
using System; // Necessario per HashCode (se scommenti CfMembri)


namespace BonusIdrici2.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Se Atti non è più usato, puoi eliminarlo o lasciarlo commentato
        // public DbSet<Atto.Atto> Atti { get; set; }
        public DbSet<Dichiarante.Dichiarante> Dichiaranti { get; set; }
        public DbSet<Ente> Enti { get; set; }
        public DbSet<FileUpload> FileUploads { get; set; } // Già presente, ottimo!

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Configurazione per la classe Dichiarante
            modelBuilder.Entity<Dichiarante.Dichiarante>(entity =>
            {
                entity.HasKey(d => d.IdDichiarante);

                // Se CfMembri è una lista di stringhe che vuoi salvare come stringa delimitata,
                // assicurati che il tipo di dato di CfMembri sia List<string> nel tuo modello Dichiarante.
                // E che tu voglia abilitare questa conversione.
                // entity.Property(d => d.CfMembri)
                //     .HasConversion(
                //         v => string.Join(";", v),
                //         v => v.Split(";", StringSplitOptions.RemoveEmptyEntries).ToList()
                //     )
                //     .Metadata.SetValueComparer(new ValueComparer<List<string>>(
                //         (c1, c2) => c1.SequenceEqual(c2),
                //         c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                //         c => c.ToList()
                //     ));
            });

            // Configurazione per la classe Ente
            modelBuilder.Entity<Ente>(entity =>
            {
                entity.ToTable("enti"); // Assicurati che il nome della tabella sia "enti"
                entity.HasKey(e => e.id); // La chiave primaria è 'id'
                // Aggiungi qui altre mappature se i nomi delle colonne in Ente non corrispondono
                // esempio: entity.Property(e => e.nome).HasColumnName("nome_ente");
            });


            // --- INIZIO: NUOVA CONFIGURAZIONE PER FILEUPLOAD ---
           modelBuilder.Entity<FileUpload>(entity =>
            {
                entity.ToTable("fileuploads"); // Mappa la classe FileUpload alla tabella 'fileuploads' nel DB

                // Mappatura esplicita delle proprietà alle colonne del DB (snake_case)
                entity.HasKey(f => f.Id); // Specifica che 'Id' è la chiave primaria

                entity.Property(f => f.NomeFile).HasColumnName("nome_file").IsRequired().HasMaxLength(255);
                entity.Property(f => f.PercorsoFile).HasColumnName("percorso_file").IsRequired().HasMaxLength(512);
                entity.Property(f => f.DataInizio).HasColumnName("data_inizio_validita"); // Nullable per default
                entity.Property(f => f.DataFine).HasColumnName("data_fine_validita");     // Nullable per default
                entity.Property(f => f.DataCaricamento).HasColumnName("data_caricamento").IsRequired(); // Se l'hai reso non nullable nel modello
                entity.Property(f => f.IdEnte).HasColumnName("id_ente").IsRequired();

                // RIMOVI QUESTE RIGHE:
                // entity.Property(f => f.Stato).HasColumnName("stato").HasMaxLength(50);
                // entity.Property(f => f.NoteErrore).HasColumnName("note_errore");

                // Configurazione della chiave esterna
                // entity.HasOne(f => f.Ente)
                //       .WithMany()
                //       .HasForeignKey(f => f.IdEnte)
                //       .HasConstraintName("fk_fileuploads_id_ente")
                //       .OnDelete(DeleteBehavior.Restrict);
            });
            // --- FINE: NUOVA CONFIGURAZIONE PER FILEUPLOAD ---

            // Configurazione per la classe Atto (se decidi di scommentarla e usarla)
            // modelBuilder.Entity<Atto.Atto>(entity =>
            // {
            //     entity.HasKey(a => a.id); // 'id' rimane la chiave primaria auto-incrementante
            //     entity.Property(a => a.OriginalCsvId).IsRequired();
            //     // Aggiungi altre mappature per Atto se i nomi delle colonne nel DB non corrispondono
            // });

            base.OnModelCreating(modelBuilder); // Chiamata importante, deve essere l'ultima
        }
    }
}
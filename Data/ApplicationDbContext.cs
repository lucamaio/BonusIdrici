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

        public DbSet<UtenzaIdrica> UtenzeIdriche { get; set; }
        public DbSet<FileUpload> FileUploads { get; set; }
        public object UtenzeIdrica { get; internal set; }

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

                entity.Property(f => f.NomeFile).HasColumnName("nome").IsRequired().HasMaxLength(255);
                entity.Property(f => f.PercorsoFile).HasColumnName("percorso").IsRequired().HasMaxLength(512);
                entity.Property(f => f.DataInizio).HasColumnName("data_inizio"); // Nullable per default
                entity.Property(f => f.DataFine).HasColumnName("data_fine");     // Nullable per default
                entity.Property(f => f.DataCaricamento).HasColumnName("data_caricamento").IsRequired(); // Se l'hai reso non nullable nel modello
                entity.Property(f => f.IdEnte).HasColumnName("id_ente").IsRequired();

            });

                modelBuilder.Entity<BonusIdrici2.Models.UtenzaIdrica>(entity =>
            {
                entity.ToTable("utenzeidriche"); // Mappa la classe FileUpload alla tabella 'fileuploads' nel DB

                // Mappatura esplicita delle proprietà alle colonne del DB (snake_case)
                entity.HasKey(f => f.id); 
                entity.Property(f => f.idAcquedotto).HasColumnName("idAcquedotto").IsRequired();
                entity.Property(f => f.stato).HasColumnName("stato").IsRequired();
                entity.Property(f => f.periodoIniziale).HasColumnName("periodo_iniziale").IsRequired();
                entity.Property(f => f.periodoFinale).HasColumnName("periodo_finale"); // Nullable per default
                entity.Property(f => f.matricolaContatore).HasColumnName("matricola_contatore").IsRequired().HasMaxLength(50);
                entity.Property(f => f.indirizzoUbicazione).HasColumnName("indirizzo_ubicazione").IsRequired().HasMaxLength(255);
                entity.Property(f => f.numeroCivico).HasColumnName("numero_civico").IsRequired().HasMaxLength(10);
                entity.Property(f => f.subUbicazione).HasColumnName("sub_ubicazione").HasMaxLength(20);
                entity.Property(f => f.scalaUbicazione).HasColumnName("scala_ubicazione").HasMaxLength(50);
                entity.Property(f => f.piano).HasColumnName("piano").HasMaxLength(20);
                entity.Property(f => f.interno).HasColumnName("interno").HasMaxLength(20);
                entity.Property(f => f.tipoUtenza).HasColumnName("tipo_utenza").HasMaxLength(100); // Esempio: "Domestica", "Non Domestica"
                entity.Property(f => f.cognome).HasColumnName("cognome").IsRequired().HasMaxLength(100);
                entity.Property(f => f.nome).HasColumnName("nome").IsRequired().HasMaxLength(100);
                entity.Property(f => f.codiceFiscale).HasColumnName("codice_fiscale").IsRequired().HasMaxLength(16);
                entity.Property(f => f.IdEnte).HasColumnName("id_ente").IsRequired();

            });

            base.OnModelCreating(modelBuilder); // Chiamata importante, deve essere l'ultima
        }
    }
}
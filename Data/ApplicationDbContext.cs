using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using BonusIdrici2.Models;
using System.Linq;
using System;
using Atto;
using Dichiarante;

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
        // public DbSet<FileUpload> FileUploads { get; set; }
        public object UtenzeIdrica { get; internal set; }
        
         public DbSet<Report> Reports { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Configurazione per la classe Dichiarante
            modelBuilder.Entity<Dichiarante.Dichiarante>(entity =>
            {
                entity.ToTable("dichiaranti");
                entity.HasKey(d => d.IdDichiarante);
                entity.Property(f => f.Nome).HasColumnName("nome").IsRequired().HasMaxLength(125);
                entity.Property(f => f.Cognome).HasColumnName("cognome").IsRequired().HasMaxLength(125);
                entity.Property(f => f.CodiceFiscale).HasColumnName("codiceFiscale").IsRequired().HasMaxLength(16);
                entity.Property(f => f.DataNascita).HasColumnName("dataNascita").IsRequired();
                entity.Property(f => f.IndirizzoResidenza).HasColumnName("IndirizzoResidenza").IsRequired().HasMaxLength(255);
                entity.Property(f => f.NumeroCivico).HasColumnName("NumeroCivico").IsRequired().HasMaxLength(255);
                entity.Property(f => f.ComuneNascita).HasColumnName("ComuneNascita").IsRequired().HasMaxLength(255);
                entity.Property(f => f.Sesso).HasColumnName("Sesso").IsRequired().HasMaxLength(1);
                entity.Property(f => f.NomeEnte).HasColumnName("NomeEnte").IsRequired().HasMaxLength(250);
                entity.Property(f => f.NumeroComponenti).HasColumnName("NumeroComponenti").IsRequired();
                entity.Property(f => f.CodiceFamiglia).HasColumnName("CodiceFamiglia").IsRequired();
                entity.Property(f => f.Parentela).HasColumnName("Parentela").IsRequired().HasMaxLength(128);
                entity.Property(f => f.CodiceFiscaleIntestatarioScheda).HasColumnName("CodiceFiscaleIntestatarioScheda").IsRequired().HasMaxLength(16);
            });

            // Configurazione per la classe Ente
            modelBuilder.Entity<Ente>(entity =>
            {
                entity.ToTable("enti"); // Assicurati che il nome della tabella sia "enti"
                entity.HasKey(f => f.id); // La chiave primaria è 'id'
                entity.Property(f => f.nome).HasColumnName("nome").IsRequired();
                entity.Property(f => f.istat).HasColumnName("istat").IsRequired();
                entity.Property(f => f.partitaIva).HasColumnName("partita_iva").IsRequired();
                entity.Property(f => f.CodiceFiscale).HasColumnName("CodiceFiscale").IsRequired().HasMaxLength(16);
                entity.Property(f => f.Cap).HasColumnName("cap").IsRequired().HasMaxLength(10);
                entity.Property(f => f.Provincia).HasColumnName("provincia").IsRequired().HasMaxLength(50);
                entity.Property(f => f.Regione).HasColumnName("regione").IsRequired().HasMaxLength(50);
            });


            // --- INIZIO: NUOVA CONFIGURAZIONE PER FILEUPLOAD ---
            //    modelBuilder.Entity<FileUpload>(entity =>
            //     {
            //         entity.ToTable("fileuploads"); // Mappa la classe FileUpload alla tabella 'fileuploads' nel DB

            //         // Mappatura esplicita delle proprietà alle colonne del DB (snake_case)
            //         entity.HasKey(f => f.Id); // Specifica che 'Id' è la chiave primaria

            //         entity.Property(f => f.NomeFile).HasColumnName("nome").IsRequired().HasMaxLength(255);
            //         entity.Property(f => f.PercorsoFile).HasColumnName("percorso").IsRequired().HasMaxLength(512);
            //         entity.Property(f => f.DataInizio).HasColumnName("data_inizio"); // Nullable per default
            //         entity.Property(f => f.DataFine).HasColumnName("data_fine");     // Nullable per default
            //         entity.Property(f => f.DataCaricamento).HasColumnName("data_caricamento").IsRequired(); // Se l'hai reso non nullable nel modello
            //         entity.Property(f => f.IdEnte).HasColumnName("id_ente").IsRequired();

            //     });

            modelBuilder.Entity<BonusIdrici2.Models.UtenzaIdrica>(entity =>
        {
            entity.ToTable("utenzeidriche"); // Mappa la classe FileUpload alla tabella 'fileuploads' nel DB

            // Mappatura esplicita delle proprietà alle colonne del DB (snake_case)
            entity.HasKey(f => f.id);
            entity.Property(f => f.idAcquedotto).HasColumnName("idAcquedotto").IsRequired();
            entity.Property(f => f.stato).HasColumnName("stato").IsRequired();
            entity.Property(f => f.periodoIniziale).HasColumnName("periodo_iniziale");
            entity.Property(f => f.periodoFinale).HasColumnName("periodo_finale");
            entity.Property(f => f.matricolaContatore).HasColumnName("matricola_contatore").IsRequired().HasMaxLength(50);
            entity.Property(f => f.indirizzoUbicazione).HasColumnName("indirizzo_ubicazione").IsRequired().HasMaxLength(255);
            entity.Property(f => f.numeroCivico).HasColumnName("numero_civico").IsRequired().HasMaxLength(10);
            entity.Property(f => f.subUbicazione).HasColumnName("sub_ubicazione").HasMaxLength(20);
            entity.Property(f => f.scalaUbicazione).HasColumnName("scala_ubicazione").HasMaxLength(50);
            entity.Property(f => f.piano).HasColumnName("piano").HasMaxLength(20);
            entity.Property(f => f.interno).HasColumnName("interno").HasMaxLength(20);
            entity.Property(f => f.tipoUtenza).HasColumnName("tipo_utenza").HasMaxLength(100);
            entity.Property(f => f.cognome).HasColumnName("cognome").IsRequired().HasMaxLength(100);
            entity.Property(f => f.nome).HasColumnName("nome").IsRequired().HasMaxLength(100);
            entity.Property(f => f.codiceFiscale).HasColumnName("codice_fiscale").IsRequired().HasMaxLength(16);
            entity.Property(f => f.IdEnte).HasColumnName("id_ente").IsRequired();
        });

            modelBuilder.Entity<BonusIdrici2.Models.Report>(entity =>
       {
           entity.ToTable("reports"); // Mappa la classe FileUpload alla tabella 'fileuploads' nel DB

           // Mappatura esplicita delle proprietà alle colonne del DB (snake_case)
           entity.HasKey(f => f.id);
           entity.Property(f => f.idAto).HasColumnName("idAto").IsRequired();
           entity.Property(f => f.codiceBonus).HasColumnName("codice_bonus").IsRequired();
           entity.Property(f => f.esitoStr).HasColumnName("esito_str").IsRequired();
           entity.Property(f => f.esito).HasColumnName("esito").IsRequired();
           entity.Property(f => f.idFornitura).HasColumnName("idFornitura");
           entity.Property(f => f.codiceFiscale).HasColumnName("codice_fiscale").IsRequired();
           entity.Property(f => f.numeroComponenti).HasColumnName("numero_componenti");
           entity.Property(f => f.nomeDichiarante).HasColumnName("nome_dichiarante");
           entity.Property(f => f.cognomeDichiarante).HasColumnName("cognome_dichiarante");
           entity.Property(f => f.annoValidita).HasColumnName("anno_validita");
           entity.Property(f => f.indirizzoAbitazione).HasColumnName("indirizzo_abitazione");
           entity.Property(f => f.numeroCivico).HasColumnName("numero_civico");
           entity.Property(f => f.istat).HasColumnName("istat");
           entity.Property(f => f.capAbitazione).HasColumnName("cap_abitazione");
           entity.Property(f => f.provinciaAbitazione).HasColumnName("provincia_abitazione");
           entity.Property(f => f.presenzaPod).HasColumnName("presenza_pod");
           entity.Property(f => f.dataInizioValidita).HasColumnName("data_inizio_validita").IsRequired();
           entity.Property(f => f.dataFineValidita).HasColumnName("data_fine_validita").IsRequired();
           entity.Property(f => f.dataCreazione).HasColumnName("data_creazione");
           entity.Property(f => f.IdEnte).HasColumnName("id_ente").IsRequired();
       });


            base.OnModelCreating(modelBuilder);
        }
    }
}
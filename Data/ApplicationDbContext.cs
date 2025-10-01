using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Models;
using System.Linq;
using System;

namespace Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        public DbSet<Dichiarante> Dichiaranti { get; set; }
        public DbSet<Ente> Enti { get; set; }

        public DbSet<UtenzaIdrica> UtenzeIdriche { get; set; }        
        public DbSet<Report> Reports { get; set; }
        public DbSet<Toponimo> Toponomi { get; set; }

        public DbSet<User> Users { get; set; }
        public DbSet<UserEnte> UserEnti { get; set; }
        public DbSet<Ruolo> Ruoli { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Configurazione per la classe Dichiarante
            modelBuilder.Entity<Dichiarante>(entity =>
            {
                entity.ToTable("dichiaranti");
                entity.HasKey(d => d.id);
                entity.Property(f => f.Nome).HasColumnName("nome").IsRequired().HasMaxLength(125);
                entity.Property(f => f.Cognome).HasColumnName("cognome").IsRequired().HasMaxLength(125);
                entity.Property(f => f.CodiceFiscale).HasColumnName("codiceFiscale").IsRequired();
                entity.Property(f => f.DataNascita).HasColumnName("dataNascita").IsRequired();
                entity.Property(f => f.IndirizzoResidenza).HasColumnName("IndirizzoResidenza").IsRequired().HasMaxLength(250);
                entity.Property(f => f.NumeroCivico).HasColumnName("NumeroCivico").IsRequired().HasMaxLength(250);
                entity.Property(f => f.ComuneNascita).HasColumnName("ComuneNascita").HasMaxLength(250);
                entity.Property(f => f.Sesso).HasColumnName("Sesso").IsRequired().HasMaxLength(1);
                entity.Property(f => f.NumeroComponenti).HasColumnName("NumeroComponenti");
                entity.Property(f => f.CodiceAbitante).HasColumnName("CodiceAbitante");
                entity.Property(f => f.CodiceFamiglia).HasColumnName("CodiceFamiglia");
                entity.Property(f => f.Parentela).HasColumnName("Parentela").HasMaxLength(128);
                entity.Property(f => f.CodiceFiscaleIntestatarioScheda).HasColumnName("CodiceFiscaleIntestatarioScheda").HasMaxLength(16);
                entity.Property(f => f.data_creazione).HasColumnName("data_creazione");
                entity.Property(f => f.data_aggiornamento).HasColumnName("data_aggiornamento");
                entity.Property(f => f.data_cancellazione).HasColumnName("data_cancellazione");
                entity.Property(f => f.IdEnte).HasColumnName("idEnte").IsRequired();
                entity.Property(f => f.IdUser).HasColumnName("idUser").IsRequired();
            });

            // Configurazione per la classe Ente
            modelBuilder.Entity<Ente>(entity =>
            {
                entity.ToTable("enti"); // Assicurati che il nome della tabella sia "enti"
                entity.HasKey(f => f.id); // La chiave primaria è 'id'
                entity.Property(f => f.nome).HasColumnName("nome").IsRequired();
                entity.Property(f => f.istat).HasColumnName("istat").IsRequired();
                entity.Property(f => f.partitaIva).HasColumnName("partita_iva").IsRequired();
                entity.Property(f => f.CodiceFiscale).HasColumnName("codice_fiscale").HasMaxLength(11);
                entity.Property(f => f.Cap).HasColumnName("cap").IsRequired().IsRequired().HasMaxLength(5);
                entity.Property(f => f.Provincia).HasColumnName("provincia").IsRequired().HasMaxLength(2);
                entity.Property(f => f.Regione).HasColumnName("regione").HasMaxLength(50);
                entity.Property(f => f.Piranha).HasColumnName("piranha").IsRequired();
                entity.Property(f => f.Selene).HasColumnName("selene").IsRequired();
                entity.Property(f => f.DataCreazione).HasColumnName("data_creazione");
                entity.Property(f => f.DataAggiornamento).HasColumnName("data_aggiornamento");
                entity.Property(f => f.IdUser).HasColumnName("idUser").IsRequired();
            });


            modelBuilder.Entity<UtenzaIdrica>(entity =>
        {
            entity.ToTable("utenzeidriche"); // Mappa la classe uTENZE Idrica alla tabella 'utenzeidriche' nel DB

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
            entity.Property(f => f.nome).HasColumnName("nome").HasMaxLength(100);
            entity.Property(f => f.sesso).HasColumnName("sesso").HasMaxLength(1);
            entity.Property(f => f.DataNascita).HasColumnName("data_nascita");
            entity.Property(f => f.codiceFiscale).HasColumnName("codice_fiscale").HasMaxLength(16);
            entity.Property(f => f.partitaIva).HasColumnName("partita_iva");
            entity.Property(f => f.IdDichiarante).HasColumnName("id_dichiarante");
            entity.Property(f => f.data_creazione).HasColumnName("data_creazione");
            entity.Property(f => f.data_aggiornamento).HasColumnName("data_aggiornamento");
            entity.Property(f => f.IdEnte).HasColumnName("id_ente").IsRequired();
            entity.Property(f => f.IdUser).HasColumnName("id_user").IsRequired();
            entity.Property(f => f.idToponimo).HasColumnName("id_toponimo");
        });

            modelBuilder.Entity<Report>(entity =>
       {
           entity.ToTable("reports"); // Mappa la classe Report alla tabella 'reports' nel DB

           // Mappatura esplicita delle proprietà alle colonne del DB (snake_case)
           entity.HasKey(f => f.id);
           entity.Property(f => f.idAto).HasColumnName("idAto").IsRequired();
           entity.Property(f => f.codiceBonus).HasColumnName("codice_bonus").IsRequired();
           entity.Property(f => f.esitoStr).HasColumnName("esito_str").IsRequired();
           entity.Property(f => f.esito).HasColumnName("esito").IsRequired();
           entity.Property(f => f.idFornitura).HasColumnName("idFornitura");
           entity.Property(f => f.codiceFiscaleRichiedente).HasColumnName("codice_fiscale").IsRequired();
           entity.Property(f => f.codiceFiscaleUtenzaTrovata).HasColumnName("codice_fiscale_trovato");
           entity.Property(f => f.idUtenza).HasColumnName("id_utenza");
           entity.Property(f => f.numeroComponenti).HasColumnName("numero_componenti");
           entity.Property(f => f.nomeDichiarante).HasColumnName("nome_dichiarante");
           entity.Property(f => f.cognomeDichiarante).HasColumnName("cognome_dichiarante");
           entity.Property(f => f.idDichiarante).HasColumnName("id_dichiarante");
           entity.Property(f => f.annoValidita).HasColumnName("anno_validita");
           entity.Property(f => f.indirizzoAbitazione).HasColumnName("indirizzo_abitazione");
           entity.Property(f => f.numeroCivico).HasColumnName("numero_civico");
           entity.Property(f => f.istat).HasColumnName("istat");
           entity.Property(f => f.capAbitazione).HasColumnName("cap_abitazione");
           entity.Property(f => f.provinciaAbitazione).HasColumnName("provincia_abitazione");
           entity.Property(f => f.presenzaPod).HasColumnName("presenza_pod");
           entity.Property(f => f.note).HasColumnName("note");
           entity.Property(f => f.incongruenze).HasColumnName("incongruenze");
           entity.Property(f => f.serie).HasColumnName("serie").IsRequired();
           entity.Property(f => f.mc).HasColumnName("mc");
           entity.Property(f => f.dataInizioValidita).HasColumnName("data_inizio_validita").IsRequired();
           entity.Property(f => f.dataFineValidita).HasColumnName("data_fine_validita").IsRequired();
           entity.Property(f => f.DataAggiornamento).HasColumnName("data_aggiornamento");
           entity.Property(f => f.DataCreazione).HasColumnName("data_creazione");
           entity.Property(f => f.IdEnte).HasColumnName("id_ente").IsRequired();
           entity.Property(f => f.IdUser).HasColumnName("id_user").IsRequired();
       });

            // Configurazione per la classe Ente
            modelBuilder.Entity<Toponimo>(entity =>
            {
                entity.ToTable("toponomi"); // Assicurati che il nome della tabella sia "enti"
                entity.HasKey(f => f.id); // La chiave primaria è 'id'
                entity.Property(f => f.denominazione).HasColumnName("denominazione").IsRequired().HasMaxLength(255);
                entity.Property(f => f.normalizzazione).HasColumnName("normalizzazione").HasMaxLength(255);
                entity.Property(f => f.data_creazione).HasColumnName("data_creazione");
                entity.Property(f => f.data_aggiornamento).HasColumnName("data_aggiornamento");
                entity.Property(f => f.IdEnte).HasColumnName("id_ente").IsRequired();
            });

            // Configurazione per la clase User

            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("users"); // Assicurati che il nome della tabella sia "enti"
                entity.HasKey(f => f.id); // La chiave primaria è 'id'
                entity.Property(f => f.Email).HasColumnName("email").IsRequired().HasMaxLength(255);
                entity.Property(f => f.Password).HasColumnName("password").HasMaxLength(255);
                entity.Property(f => f.Cognome).HasColumnName("cognome");
                entity.Property(f => f.Nome).HasColumnName("nome");
                entity.Property(f => f.Username).HasColumnName("username").HasMaxLength(255);
                entity.Property(f => f.dataCreazione).HasColumnName("data_creazione");
                entity.Property(f => f.dataAggiornamento).HasColumnName("data_aggiornamento");
                entity.Property(f => f.idRuolo).HasColumnName("id_ruolo").IsRequired();
            });

            // Configurazione per la clase UsersEnti

           modelBuilder.Entity<UserEnte>(entity =>
            {
                entity.ToTable("users_enti");

                entity.HasKey(e => new { e.idUser, e.idEnte }); // Chiave composta

                entity.Property(f => f.idUser).HasColumnName("id_user").IsRequired();
                entity.Property(f => f.idEnte).HasColumnName("id_ente").IsRequired();
            });

            // Configurazione per la classe Ruolo

            modelBuilder.Entity<Ruolo>(entity =>
            {
                entity.ToTable("ruoli");
                entity.HasKey(f => f.id);
                entity.Property(f => f.nome).HasColumnName("nome").IsRequired().HasMaxLength(255);
                entity.Property(f => f.dataCreazione).HasColumnName("data_creazione");
            });


            base.OnModelCreating(modelBuilder);
        }
    }
}
﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using WordWhisperer.Core.Data;

#nullable disable

namespace WordWhisperer.Api.Data.Migrations
{
    [DbContext(typeof(DatabaseContext))]
    partial class DatabaseContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "9.0.3");

            modelBuilder.Entity("WordWhisperer.Core.Data.Models.Favorite", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("AddedAt")
                        .HasColumnType("TEXT");

                    b.Property<string>("Notes")
                        .HasColumnType("TEXT");

                    b.Property<string>("Tags")
                        .HasColumnType("TEXT");

                    b.Property<int>("WordId")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("AddedAt");

                    b.HasIndex("WordId");

                    b.ToTable("Favorites");
                });

            modelBuilder.Entity("WordWhisperer.Core.Data.Models.History", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("AccentUsed")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("Timestamp")
                        .HasColumnType("TEXT");

                    b.Property<int>("WordId")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("Timestamp");

                    b.HasIndex("WordId");

                    b.ToTable("History");
                });

            modelBuilder.Entity("WordWhisperer.Core.Data.Models.Setting", b =>
                {
                    b.Property<string>("Key")
                        .HasColumnType("TEXT");

                    b.Property<string>("Description")
                        .HasColumnType("TEXT");

                    b.Property<string>("Value")
                        .HasColumnType("TEXT");

                    b.HasKey("Key");

                    b.ToTable("Settings");
                });

            modelBuilder.Entity("WordWhisperer.Core.Data.Models.Word", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int>("AccessCount")
                        .HasColumnType("INTEGER");

                    b.Property<string>("AudioPath")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("TEXT");

                    b.Property<string>("Definition")
                        .HasColumnType("TEXT");

                    b.Property<bool>("HasMultiplePron")
                        .HasColumnType("INTEGER");

                    b.Property<string>("IpaPhonetic")
                        .HasColumnType("TEXT");

                    b.Property<bool>("IsGenerated")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("LastAccessedAt")
                        .HasColumnType("TEXT");

                    b.Property<string>("PartOfSpeech")
                        .HasColumnType("TEXT");

                    b.Property<string>("Phonetic")
                        .HasColumnType("TEXT");

                    b.Property<string>("Source")
                        .HasColumnType("TEXT");

                    b.Property<string>("WordText")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("WordText")
                        .IsUnique();

                    b.ToTable("Words");
                });

            modelBuilder.Entity("WordWhisperer.Core.Data.Models.WordVariant", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("AudioPath")
                        .HasColumnType("TEXT");

                    b.Property<string>("IpaPhonetic")
                        .HasColumnType("TEXT");

                    b.Property<string>("Phonetic")
                        .HasColumnType("TEXT");

                    b.Property<string>("Variant")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<int>("WordId")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("WordId");

                    b.ToTable("WordVariants");
                });

            modelBuilder.Entity("WordWhisperer.Core.Data.Models.Favorite", b =>
                {
                    b.HasOne("WordWhisperer.Core.Data.Models.Word", "Word")
                        .WithMany("Favorites")
                        .HasForeignKey("WordId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Word");
                });

            modelBuilder.Entity("WordWhisperer.Core.Data.Models.History", b =>
                {
                    b.HasOne("WordWhisperer.Core.Data.Models.Word", "Word")
                        .WithMany("History")
                        .HasForeignKey("WordId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Word");
                });

            modelBuilder.Entity("WordWhisperer.Core.Data.Models.WordVariant", b =>
                {
                    b.HasOne("WordWhisperer.Core.Data.Models.Word", "Word")
                        .WithMany("Variants")
                        .HasForeignKey("WordId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Word");
                });

            modelBuilder.Entity("WordWhisperer.Core.Data.Models.Word", b =>
                {
                    b.Navigation("Favorites");

                    b.Navigation("History");

                    b.Navigation("Variants");
                });
#pragma warning restore 612, 618
        }
    }
}

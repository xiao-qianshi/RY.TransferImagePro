﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using RY.TransferImagePro.Data;

namespace RY.TransferImagePro.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20210621184313_init_0")]
    partial class init_0
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "3.1.10");

            modelBuilder.Entity("RY.TransferImagePro.Domain.Entity.ImageInformation", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("CreateTime")
                        .HasColumnType("TEXT");

                    b.Property<string>("FileExtension")
                        .HasColumnType("TEXT")
                        .HasMaxLength(10);

                    b.Property<string>("FileName")
                        .HasColumnType("TEXT")
                        .HasMaxLength(40);

                    b.Property<long>("FileSize")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("HasUploaded")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Location")
                        .HasColumnType("TEXT")
                        .HasMaxLength(200);

                    b.Property<DateTime>("UploadTime")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("ImageInformations");
                });
#pragma warning restore 612, 618
        }
    }
}

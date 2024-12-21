﻿// <auto-generated />
using System;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

using WASalesTax.Models;

namespace WASalesTax.Migrations
{
    [DbContext(typeof(WashingtonStateContext))]
    [Migration("20210307232251_InitialCreate")]
    partial class InitialCreate
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "5.0.3");

            modelBuilder.Entity("WASalesTax.Models.AddressRange", b =>
                {
                    b.Property<int>("AddressRangeId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int?>("AddressRangeLowerBound")
                        .HasColumnType("INTEGER");

                    b.Property<int?>("AddressRangeUpperBound")
                        .HasColumnType("INTEGER");

                    b.Property<string>("CEZName")
                        .HasColumnType("TEXT");

                    b.Property<int>("LocationCode")
                        .HasColumnType("INTEGER");

                    b.Property<char>("OddOrEven")
                        .HasColumnType("TEXT");

                    b.Property<string>("PTBAName")
                        .HasColumnType("TEXT");

                    b.Property<string>("Period")
                        .HasColumnType("TEXT");

                    b.Property<char>("RTA")
                        .HasColumnType("TEXT");

                    b.Property<string>("State")
                        .HasColumnType("TEXT");

                    b.Property<string>("Street")
                        .HasColumnType("TEXT");

                    b.Property<string>("ZipCode")
                        .HasColumnType("TEXT");

                    b.Property<string>("ZipCodePlus4")
                        .HasColumnType("TEXT");

                    b.HasKey("AddressRangeId");

                    b.ToTable("AddressRanges");
                });

            modelBuilder.Entity("WASalesTax.Models.ShortZip", b =>
                {
                    b.Property<int>("ShortZipId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("EffectiveEndDate")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("EffectiveStartDate")
                        .HasColumnType("TEXT");

                    b.Property<string>("Local")
                        .HasColumnType("TEXT");

                    b.Property<int>("LocationCode")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Plus4LowerBound")
                        .HasColumnType("TEXT");

                    b.Property<string>("Plus4UpperBound")
                        .HasColumnType("TEXT");

                    b.Property<string>("State")
                        .HasColumnType("TEXT");

                    b.Property<string>("TotalRate")
                        .HasColumnType("TEXT");

                    b.Property<string>("Zip")
                        .HasColumnType("TEXT");

                    b.HasKey("ShortZipId");

                    b.ToTable("ZipCodes");
                });

            modelBuilder.Entity("WASalesTax.Models.TaxRate", b =>
                {
                    b.Property<int>("LocationCode")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("EffectiveDate")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("ExpirationDate")
                        .HasColumnType("TEXT");

                    b.Property<double>("Local")
                        .HasColumnType("REAL");

                    b.Property<string>("Name")
                        .HasColumnType("TEXT");

                    b.Property<double>("RTA")
                        .HasColumnType("REAL");

                    b.Property<double>("Rate")
                        .HasColumnType("REAL");

                    b.Property<double>("State")
                        .HasColumnType("REAL");

                    b.HasKey("LocationCode");

                    b.ToTable("TaxRates");
                });
#pragma warning restore 612, 618
        }
    }
}

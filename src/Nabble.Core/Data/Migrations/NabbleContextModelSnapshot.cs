using System;
using Microsoft.Data.Entity;
using Microsoft.Data.Entity.Infrastructure;
using Microsoft.Data.Entity.Metadata;
using Microsoft.Data.Entity.Migrations;
using Nabble.Core.Data;

namespace Nabble.Core.Data.Migrations
{
    [DbContext(typeof(NabbleContext))]
    partial class NabbleContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
            modelBuilder
                .HasAnnotation("ProductVersion", "7.0.0-rc1-16348");

            modelBuilder.Entity("Nabble.Core.Data.Entities.Badge", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("BadgeIdentifier");

                    b.Property<DateTime>("Created");

                    b.HasKey("Id");

                    b.HasIndex("BadgeIdentifier")
                        .IsUnique();
                });

            modelBuilder.Entity("Nabble.Core.Data.Entities.Project", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("AccountName");

                    b.Property<DateTime>("DateTime");

                    b.Property<string>("ProjectName");

                    b.HasKey("Id");

                    b.HasIndex("AccountName", "ProjectName")
                        .IsUnique();
                });

            modelBuilder.Entity("Nabble.Core.Data.Entities.Request", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<DateTime>("DateTime");

                    b.HasKey("Id");
                });
        }
    }
}

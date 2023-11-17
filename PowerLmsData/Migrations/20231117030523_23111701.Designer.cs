﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using PowerLmsServer.EfData;

#nullable disable
#pragma warning disable CS1591 // 缺少对公共可见类型或成员的 XML 注释

namespace PowerLmsData.Migrations
{
    [DbContext(typeof(PowerLmsUserDbContext))]
    [Migration("20231117030523_23111701")]
    partial class _23111701
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "6.0.24")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder, 1L, 1);

            modelBuilder.Entity("PowerLms.Data.Account", b =>
                {
                    b.Property<Guid>("Id")
                        .HasColumnType("uniqueidentifier")
                        .HasColumnOrder(0);

                    b.Property<DateTime>("CreateUtc")
                        .HasColumnType("datetime2")
                        .HasComment("创建该对象的世界时间");

                    b.Property<string>("CurrentLanguageTag")
                        .HasMaxLength(12)
                        .HasColumnType("varchar(12)")
                        .HasComment("使用的首选语言标准缩写。如:zh-CN");

                    b.Property<string>("DisplayName")
                        .HasMaxLength(64)
                        .HasColumnType("nvarchar(64)")
                        .HasComment("用户的显示名");

                    b.Property<Guid?>("GenderCode")
                        .HasColumnType("uniqueidentifier")
                        .HasComment("性别编码");

                    b.Property<Guid?>("IncumbencyCode")
                        .HasColumnType("uniqueidentifier")
                        .HasComment("在职状态编码");

                    b.Property<DateTime>("LastModifyDateTimeUtc")
                        .HasColumnType("datetime2");

                    b.Property<string>("LoginName")
                        .HasMaxLength(64)
                        .HasColumnType("nvarchar(64)")
                        .HasComment("登录名");

                    b.Property<int?>("NodeNum")
                        .HasColumnType("int");

                    b.Property<Guid?>("OrgId")
                        .HasColumnType("uniqueidentifier")
                        .HasComment("当前使用的组织机构Id。在登陆后要首先设置");

                    b.Property<byte[]>("PwdHash")
                        .HasMaxLength(32)
                        .HasColumnType("varbinary(32)")
                        .HasComment("密码的Hash值");

                    b.Property<Guid?>("QualificationsCode")
                        .HasColumnType("uniqueidentifier")
                        .HasComment("学历编码");

                    b.Property<byte>("State")
                        .HasColumnType("tinyint")
                        .HasComment("用户状态。0是正常使用用户，1是锁定用户。");

                    b.Property<TimeSpan>("Timeout")
                        .HasColumnType("time");

                    b.Property<byte[]>("Timestamp")
                        .IsConcurrencyToken()
                        .ValueGeneratedOnAddOrUpdate()
                        .HasColumnType("rowversion");

                    b.Property<Guid?>("Token")
                        .HasColumnType("uniqueidentifier")
                        .HasComment("最近使用的Token");

                    b.Property<Guid?>("WorkingStatusCode")
                        .HasColumnType("uniqueidentifier")
                        .HasComment("工作状态编码");

                    b.HasKey("Id");

                    b.HasIndex("LoginName");

                    b.HasIndex("Token");

                    b.ToTable("Accounts");
                });

            modelBuilder.Entity("PowerLms.Data.AccountPlOrganization", b =>
                {
                    b.Property<Guid>("UserId")
                        .HasColumnType("uniqueidentifier")
                        .HasComment("用户Id");

                    b.Property<Guid>("OrgId")
                        .HasColumnType("uniqueidentifier")
                        .HasComment("直属组织机构Id");

                    b.HasKey("UserId", "OrgId");

                    b.ToTable("AccountPlOrganizations");

                    b.HasComment("账号所属组织机构多对多表");
                });

            modelBuilder.Entity("PowerLms.Data.Multilingual", b =>
                {
                    b.Property<Guid>("Id")
                        .HasColumnType("uniqueidentifier")
                        .HasColumnOrder(0)
                        .HasComment("主键。");

                    b.Property<string>("Key")
                        .HasMaxLength(64)
                        .HasColumnType("nvarchar(64)")
                        .HasComment("键值字符串。如:未登录.登录.标题。");

                    b.Property<string>("LanguageTag")
                        .IsRequired()
                        .HasMaxLength(12)
                        .HasColumnType("varchar(12)")
                        .HasComment("主键，也是语言的标准缩写名。");

                    b.Property<string>("Text")
                        .HasColumnType("nvarchar(max)")
                        .HasComment("内容。");

                    b.HasKey("Id");

                    b.HasIndex("LanguageTag", "Key")
                        .IsUnique()
                        .HasFilter("[Key] IS NOT NULL");

                    b.ToTable("Multilinguals");
                });

            modelBuilder.Entity("PowerLms.Data.PlOrganization", b =>
                {
                    b.Property<Guid>("Id")
                        .HasColumnType("uniqueidentifier")
                        .HasColumnOrder(0);

                    b.Property<string>("Description")
                        .HasColumnType("nvarchar(max)")
                        .HasComment("组织机构描述");

                    b.Property<Guid?>("MerchantId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<int>("Otc")
                        .HasColumnType("int")
                        .HasComment("机构类型，1商户，2公司，4下属机构");

                    b.Property<Guid?>("ParentId")
                        .HasColumnType("uniqueidentifier")
                        .HasComment("所属组织机构Id。没有父的组织机构是顶层节点即\"商户\"。");

                    b.Property<string>("ShortcutCode")
                        .HasMaxLength(8)
                        .HasColumnType("char(8)")
                        .HasComment("快捷输入码。服务器不使用。8个ASCII字符不足的尾部填充空格（写入时可不填充，但读回后会自动加入）。");

                    b.HasKey("Id");

                    b.HasIndex("ParentId");

                    b.ToTable("PlOrganizations");
                });

            modelBuilder.Entity("PowerLms.Data.SimpleDataDic", b =>
                {
                    b.Property<Guid>("Id")
                        .HasColumnType("uniqueidentifier")
                        .HasColumnOrder(0);

                    b.Property<string>("Code")
                        .IsRequired()
                        .HasMaxLength(32)
                        .HasColumnType("varchar(32)")
                        .HasComment("编码，对本系统有一定意义的编码");

                    b.Property<string>("CustomsCode")
                        .HasColumnType("nvarchar(max)")
                        .HasComment("海关码。项目类型决定有大量与海关的EDI行为，交换使用的码。");

                    b.Property<Guid?>("DataDicId")
                        .HasColumnType("uniqueidentifier")
                        .HasComment("所属数据字典的的Id");

                    b.Property<string>("DisplayName")
                        .HasColumnType("nvarchar(max)")
                        .HasComment("显示的名称");

                    b.Property<string>("ShortName")
                        .HasMaxLength(32)
                        .HasColumnType("nvarchar(32)")
                        .HasComment("缩写名");

                    b.Property<string>("ShortcutName")
                        .HasMaxLength(8)
                        .HasColumnType("char(8)")
                        .HasComment("快捷输入名");

                    b.HasKey("Id");

                    b.ToTable("SimpleDataDics");
                });

            modelBuilder.Entity("PowerLms.Data.SystemResource", b =>
                {
                    b.Property<Guid>("Id")
                        .HasColumnType("uniqueidentifier")
                        .HasColumnOrder(0);

                    b.Property<string>("DisplayName")
                        .HasColumnType("nvarchar(max)")
                        .HasComment("显示的名称");

                    b.Property<string>("Name")
                        .HasMaxLength(32)
                        .HasColumnType("nvarchar(32)")
                        .HasComment("编码，对本系统有一定意义的编码");

                    b.Property<Guid?>("ParentId")
                        .HasColumnType("uniqueidentifier")
                        .HasComment("父资源的Id。可能分类用");

                    b.Property<string>("Remark")
                        .HasColumnType("nvarchar(max)")
                        .HasComment("说明");

                    b.HasKey("Id");

                    b.HasIndex("Name")
                        .IsUnique()
                        .HasFilter("[Name] IS NOT NULL");

                    b.ToTable("SystemResources");
                });

            modelBuilder.Entity("PowerLms.Data.PlOrganization", b =>
                {
                    b.HasOne("PowerLms.Data.PlOrganization", "Parent")
                        .WithMany("Children")
                        .HasForeignKey("ParentId");

                    b.OwnsOne("PowerLms.Data.PlOwnedName", "Name", b1 =>
                        {
                            b1.Property<Guid>("PlOrganizationId")
                                .HasColumnType("uniqueidentifier");

                            b1.Property<string>("DisplayName")
                                .HasColumnType("nvarchar(max)")
                                .HasComment("显示名，有时它是昵称或简称(系统内)的意思");

                            b1.Property<string>("Name")
                                .HasColumnType("nvarchar(max)")
                                .HasComment("正式名称，拥有相对稳定性");

                            b1.Property<string>("ShortName")
                                .HasMaxLength(32)
                                .HasColumnType("nvarchar(32)")
                                .HasComment("正式简称，对正式的组织机构通常简称也是规定的");

                            b1.HasKey("PlOrganizationId");

                            b1.ToTable("PlOrganizations");

                            b1.WithOwner()
                                .HasForeignKey("PlOrganizationId");
                        });

                    b.OwnsOne("PowerLms.Data.PlSimpleOwnedAddress", "Address", b1 =>
                        {
                            b1.Property<Guid>("PlOrganizationId")
                                .HasColumnType("uniqueidentifier");

                            b1.Property<string>("Fax")
                                .HasMaxLength(28)
                                .HasColumnType("nvarchar(28)")
                                .HasComment("传真");

                            b1.Property<string>("FullAddress")
                                .HasColumnType("nvarchar(max)")
                                .HasComment("详细地址");

                            b1.Property<string>("Tel")
                                .HasMaxLength(28)
                                .HasColumnType("nvarchar(28)")
                                .HasComment("电话");

                            b1.HasKey("PlOrganizationId");

                            b1.ToTable("PlOrganizations");

                            b1.WithOwner()
                                .HasForeignKey("PlOrganizationId");
                        });

                    b.Navigation("Address");

                    b.Navigation("Name");

                    b.Navigation("Parent");
                });

            modelBuilder.Entity("PowerLms.Data.PlOrganization", b =>
                {
                    b.Navigation("Children");
                });
#pragma warning restore 612, 618
        }
    }
}

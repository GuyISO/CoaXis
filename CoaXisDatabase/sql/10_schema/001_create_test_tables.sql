/*
目的: テスト用の3テーブルを作成する
対象データベース:
実行順: 001
実行前提: 対象データベースへ接続済みで、同名テーブルが存在しないこと

依存関係:
- TestMaster: 依存なし
- TestEntity: ParentIdで自身を参照
- TestProperty: TestEntityとTestMasterを参照
*/

CREATE TABLE dbo.TestMaster
(
    Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_TestMaster_Id DEFAULT NEWID(),
    Name NVARCHAR(200) NOT NULL,

    CONSTRAINT PK_TestMaster PRIMARY KEY (Id)
);
GO

CREATE TABLE dbo.TestEntity
(
    Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_TestEntity_Id DEFAULT NEWID(),
    CreatedBy NVARCHAR(100) NOT NULL,
    CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_TestEntity_CreatedAt DEFAULT SYSDATETIME(),
    DeletedBy NVARCHAR(100) NULL,
    DeletedAt DATETIME2 NULL,
    ParentId UNIQUEIDENTIFIER NULL,

    CONSTRAINT PK_TestEntity PRIMARY KEY (Id),
    CONSTRAINT FK_TestEntity_Parent FOREIGN KEY (ParentId)
        REFERENCES dbo.TestEntity(Id)
);
GO

CREATE TABLE dbo.TestProperty
(
    Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_TestProperty_Id DEFAULT NEWID(),
    CreatedBy NVARCHAR(100) NOT NULL,
    CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_TestProperty_CreatedAt DEFAULT SYSDATETIME(),
    DeletedBy NVARCHAR(100) NULL,
    DeletedAt DATETIME2 NULL,
    TestEntityId UNIQUEIDENTIFIER NOT NULL,
    Name NVARCHAR(200) NOT NULL,
    Value NVARCHAR(MAX) NULL,
    TestMasterId UNIQUEIDENTIFIER NULL,

    CONSTRAINT PK_TestProperty PRIMARY KEY (Id),
    CONSTRAINT FK_TestProperty_Entity FOREIGN KEY (TestEntityId)
        REFERENCES dbo.TestEntity(Id),
    CONSTRAINT FK_TestProperty_Master FOREIGN KEY (TestMasterId)
        REFERENCES dbo.TestMaster(Id)
);
GO

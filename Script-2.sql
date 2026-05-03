-- DROP SCHEMA dbo;

CREATE SCHEMA dbo;
-- HisDB.dbo.T_Inbound definition

-- Drop table

-- DROP TABLE HisDB.dbo.T_Inbound;

CREATE TABLE HisDB.dbo.T_Inbound (
	InboundID int IDENTITY(1,1) NOT NULL,
	MedicineID int NOT NULL,
	BatchNumber nvarchar(50) COLLATE Chinese_PRC_CI_AS NULL,
	Quantity int NOT NULL,
	OperatorID int NOT NULL,
	InboundTime datetime DEFAULT getdate() NULL,
	ExpiryDate datetime NOT NULL,
	CONSTRAINT PK__T_Inboun__B4DB7A9556FCBB52 PRIMARY KEY (InboundID)
);


-- HisDB.dbo.T_Medicine definition

-- Drop table

-- DROP TABLE HisDB.dbo.T_Medicine;

CREATE TABLE HisDB.dbo.T_Medicine (
	MedicineID int IDENTITY(1,1) NOT NULL,
	MedicineName nvarchar(100) COLLATE Chinese_PRC_CI_AS NOT NULL,
	PyCode nvarchar(50) COLLATE Chinese_PRC_CI_AS NOT NULL,
	Spec nvarchar(50) COLLATE Chinese_PRC_CI_AS NOT NULL,
	Unit nvarchar(10) COLLATE Chinese_PRC_CI_AS NOT NULL,
	Price decimal(10,2) DEFAULT 0 NOT NULL,
	Stock int DEFAULT 0 NOT NULL,
	Manufacturer nvarchar(100) COLLATE Chinese_PRC_CI_AS NULL,
	IsSpecial bit DEFAULT 0 NULL,
	MinStock int NULL,
	CONSTRAINT PK__T_Medici__4F2128F09FE5A558 PRIMARY KEY (MedicineID)
);


-- HisDB.dbo.T_Medicine_Batch definition

-- Drop table

-- DROP TABLE HisDB.dbo.T_Medicine_Batch;

CREATE TABLE HisDB.dbo.T_Medicine_Batch (
	BatchID int IDENTITY(1,1) NOT NULL,
	MedicineID int NOT NULL,
	BatchNo nvarchar(50) COLLATE Chinese_PRC_CI_AS NOT NULL,
	ProductionDate date NULL,
	ExpiryDate date NOT NULL,
	BatchStock int NOT NULL,
	InPrice decimal(18,2) NULL,
	CreateTime datetime DEFAULT getdate() NULL,
	CONSTRAINT PK__T_Medici__5D55CE38FC622A85 PRIMARY KEY (BatchID)
);


-- HisDB.dbo.T_Patient definition

-- Drop table

-- DROP TABLE HisDB.dbo.T_Patient;

CREATE TABLE HisDB.dbo.T_Patient (
	PatientID int IDENTITY(1,1) NOT NULL,
	PatientName nvarchar(20) COLLATE Chinese_PRC_CI_AS NOT NULL,
	Gender nvarchar(4) COLLATE Chinese_PRC_CI_AS NOT NULL,
	Age int NOT NULL,
	CardID nvarchar(20) COLLATE Chinese_PRC_CI_AS NULL,
	Phone nvarchar(20) COLLATE Chinese_PRC_CI_AS NULL,
	CreateTime datetime DEFAULT getdate() NULL,
	CONSTRAINT PK__T_Patien__970EC346521344DE PRIMARY KEY (PatientID)
);


-- HisDB.dbo.T_Prescription_Detail definition

-- Drop table

-- DROP TABLE HisDB.dbo.T_Prescription_Detail;

CREATE TABLE HisDB.dbo.T_Prescription_Detail (
	DetailID int IDENTITY(1,1) NOT NULL,
	PrescriptionID int NOT NULL,
	MedicineID int NOT NULL,
	MedicineName nvarchar(100) COLLATE Chinese_PRC_CI_AS NOT NULL,
	Price decimal(10,2) NOT NULL,
	Quantity int NOT NULL,
	[Usage] nvarchar(100) COLLATE Chinese_PRC_CI_AS NULL,
	Frequency nvarchar(50) COLLATE Chinese_PRC_CI_AS NULL,
	CONSTRAINT PK__T_Prescr__135C314DC9B81D20 PRIMARY KEY (DetailID)
);


-- HisDB.dbo.T_Prescription_Main definition

-- Drop table

-- DROP TABLE HisDB.dbo.T_Prescription_Main;

CREATE TABLE HisDB.dbo.T_Prescription_Main (
	PrescriptionID int IDENTITY(1,1) NOT NULL,
	PrescriptionNo nvarchar(50) COLLATE Chinese_PRC_CI_AS NOT NULL,
	PatientID int NOT NULL,
	PatientName nvarchar(20) COLLATE Chinese_PRC_CI_AS NOT NULL,
	PatientGender nvarchar(2) COLLATE Chinese_PRC_CI_AS NULL,
	PatientAge int NULL,
	DoctorID int NOT NULL,
	DoctorName nvarchar(20) COLLATE Chinese_PRC_CI_AS NOT NULL,
	TotalAmount decimal(10,2) DEFAULT 0 NOT NULL,
	Status int DEFAULT 0 NOT NULL,
	CreateTime datetime DEFAULT getdate() NULL,
	CONSTRAINT PK__T_Prescr__401308129B860D16 PRIMARY KEY (PrescriptionID)
);


-- HisDB.dbo.T_Rule_Incompatibility definition

-- Drop table

-- DROP TABLE HisDB.dbo.T_Rule_Incompatibility;

CREATE TABLE HisDB.dbo.T_Rule_Incompatibility (
	RuleID int IDENTITY(1,1) NOT NULL,
	MedicineID_A int NOT NULL,
	MedicineID_B int NOT NULL,
	RiskLevel nvarchar(20) COLLATE Chinese_PRC_CI_AS NOT NULL,
	Description nvarchar(200) COLLATE Chinese_PRC_CI_AS NOT NULL,
	CreateTime datetime DEFAULT getdate() NULL,
	CONSTRAINT PK__T_Rule_I__110458C2BFE1EF4F PRIMARY KEY (RuleID)
);


-- HisDB.dbo.T_Stock_Log definition

-- Drop table

-- DROP TABLE HisDB.dbo.T_Stock_Log;

CREATE TABLE HisDB.dbo.T_Stock_Log (
	LogID int IDENTITY(1,1) NOT NULL,
	MedicineID int NOT NULL,
	BatchID int NOT NULL,
	ChangeQty int NOT NULL,
	AfterStock int NOT NULL,
	OpType nvarchar(20) COLLATE Chinese_PRC_CI_AS NOT NULL,
	OperatorID int NOT NULL,
	PrescriptionID int NULL,
	CreateTime datetime DEFAULT getdate() NULL,
	CONSTRAINT PK__T_Stock_Log PRIMARY KEY (LogID)
);
 CREATE NONCLUSTERED INDEX IX_StockLog_BatchID ON HisDB.dbo.T_Stock_Log (  BatchID ASC  )  
	 WITH (  PAD_INDEX = OFF ,FILLFACTOR = 100  ,SORT_IN_TEMPDB = OFF , IGNORE_DUP_KEY = OFF , STATISTICS_NORECOMPUTE = OFF , ONLINE = OFF , ALLOW_ROW_LOCKS = ON , ALLOW_PAGE_LOCKS = ON  )
	 ON [PRIMARY ] ;
 CREATE NONCLUSTERED INDEX IX_StockLog_MedicineID ON HisDB.dbo.T_Stock_Log (  MedicineID ASC  )  
	 WITH (  PAD_INDEX = OFF ,FILLFACTOR = 100  ,SORT_IN_TEMPDB = OFF , IGNORE_DUP_KEY = OFF , STATISTICS_NORECOMPUTE = OFF , ONLINE = OFF , ALLOW_ROW_LOCKS = ON , ALLOW_PAGE_LOCKS = ON  )
	 ON [PRIMARY ] ;
 CREATE NONCLUSTERED INDEX IX_StockLog_Time ON HisDB.dbo.T_Stock_Log (  CreateTime ASC  )  
	 WITH (  PAD_INDEX = OFF ,FILLFACTOR = 100  ,SORT_IN_TEMPDB = OFF , IGNORE_DUP_KEY = OFF , STATISTICS_NORECOMPUTE = OFF , ONLINE = OFF , ALLOW_ROW_LOCKS = ON , ALLOW_PAGE_LOCKS = ON  )
	 ON [PRIMARY ] ;


-- HisDB.dbo.T_User definition

-- Drop table

-- DROP TABLE HisDB.dbo.T_User;

CREATE TABLE HisDB.dbo.T_User (
	UserID int IDENTITY(1,1) NOT NULL,
	UserName nvarchar(50) COLLATE Chinese_PRC_CI_AS NOT NULL,
	Password nvarchar(50) COLLATE Chinese_PRC_CI_AS NOT NULL,
	RealName nvarchar(20) COLLATE Chinese_PRC_CI_AS NOT NULL,
	RoleType nvarchar(20) COLLATE Chinese_PRC_CI_AS NOT NULL,
	CreateTime datetime DEFAULT getdate() NULL,
	CONSTRAINT PK__T_User__1788CCAC350FDDBF PRIMARY KEY (UserID)
);
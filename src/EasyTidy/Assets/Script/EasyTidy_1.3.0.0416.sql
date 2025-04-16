ALTER TABLE TaskOrchestration ADD COLUMN AIIdentify TEXT DEFAULT "00000000-0000-0000-0000-000000000000";
ALTER TABLE TaskOrchestration ADD COLUMN UserDefinePromptsJson TEXT DEFAULT NULL;
ALTER TABLE TaskOrchestration ADD COLUMN Argument TEXT NULL;

PRAGMA foreign_keys = false;

-- ----------------------------
-- Table structure for AIService
-- ----------------------------
DROP TABLE IF EXISTS "AIService";
CREATE TABLE "AIService" (
  "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
  "Name" TEXT,
  "Identify" TEXT NOT NULL,
  "Type" INTEGER NOT NULL,
  "IsEnabled" INTEGER NOT NULL,
  "Url" TEXT,
  "AppID" TEXT,
  "AppKey" TEXT,
  "Model" TEXT,
  "Temperature" REAL DEFAULT 0.8,
  "IsDefault" INTEGER DEFAULT 0
);

PRAGMA foreign_keys = true;
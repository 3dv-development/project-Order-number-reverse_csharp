# 受注番号採番システム (C# Edition)

3Dビジュアル社の受注番号を自動採番・管理するWebアプリケーション（C#/ASP.NET Core版）

## 概要

Python版の受注番号採番システムをC#で再実装したバージョンです。
同じPostgreSQLデータベースを使用し、Python版と同じ機能を提供します。

## 技術スタック

- **フレームワーク**: ASP.NET Core 8.0 (MVC)
- **ORM**: Entity Framework Core 8.0
- **データベース**: PostgreSQL (Renderホスティング)
- **言語**: C# 12
- **フロントエンド**: Bootstrap 5, Razor Pages

## 機能

### 採番機能
- 7桁の受注番号を自動採番（YYCCNNN形式）
- カテゴリ別の連番管理
- ユニーク性チェック
- 採番完了時の自動メール送信

### 検索・閲覧機能
- カテゴリ別検索
- キーワード検索
- 最新順での表示
- 詳細情報の表示

### 編集機能
- 既存案件の編集
- 編集履歴の記録・表示

### 認証機能
- 社員番号によるログイン
- セッション管理
- 管理者権限

### Board API連携
- Board案件情報の取得
- 受注番号の自動登録

## カテゴリ

- **02**: 設計
- **03**: トレーニング・たよれーる・データ販売
- **04**: 製品販売
- **06**: システム受託
- **07**: システム小規模開発
- **08**: 付帯業務

## セットアップ

### 必要要件

- .NET 8.0 SDK以上
- PostgreSQLデータベース（Renderでホスティング済み）
- Visual Studio 2022またはVisual Studio Code

### 1. リポジトリのクローン

```bash
git clone https://github.com/your-org/project-Order-number-reverse_csharp.git
cd project-Order-number-reverse_csharp
```

### 2. 依存パッケージの復元

```bash
dotnet restore
```

### 3. 環境変数の設定

`appsettings.json`をコピーして`appsettings.Development.json`を作成：

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=project_numbers;Username=postgres;Password=yourpassword"
  },
  "BoardApi": {
    "ApiKey": "your-api-key",
    "ApiToken": "your-api-token"
  },
  "Email": {
    "Username": "your-email@example.com",
    "Password": "your-password"
  }
}
```

本番環境では環境変数`DATABASE_URL`を使用します。

### 4. データベースマイグレーション

開発環境で新規にデータベースを作成する場合：

```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```

**注意**: 本番環境では既存のPython版と同じデータベースを使用するため、マイグレーションは不要です。

### 5. アプリケーションの起動

```bash
dotnet run
```

ブラウザで `https://localhost:5001` にアクセス

## プロジェクト構造

```
project-Order-number-reverse_csharp/
├── Controllers/           # MVCコントローラー
│   ├── HomeController.cs
│   ├── AccountController.cs
│   └── ProjectController.cs
├── Models/                # データモデル
│   ├── Project.cs
│   ├── EditHistory.cs
│   └── Employee.cs
├── Views/                 # Razorビュー
│   ├── Shared/
│   ├── Home/
│   ├── Account/
│   └── Project/
├── Services/              # ビジネスロジック
│   ├── ProjectService.cs
│   ├── EmailService.cs
│   └── BoardApiService.cs
├── Data/                  # データベースコンテキスト
│   └── ApplicationDbContext.cs
├── wwwroot/               # 静的ファイル
│   ├── css/
│   └── js/
├── Program.cs             # エントリーポイント
├── appsettings.json       # 設定ファイル
└── README.md
```

## デプロイ（Render）

### 1. Renderにログイン

https://dashboard.render.com/

### 2. 新しいWeb Serviceを作成

- Gitリポジトリを接続
- Environment: `.NET`
- Build Command: `dotnet publish -c Release -o out`
- Start Command: `cd out && dotnet ProjectOrderNumberSystem.dll`

### 3. 環境変数を設定

Renderのダッシュボードで以下を設定：

```
DATABASE_URL=postgresql://project_user:password@host/database
ASPNETCORE_ENVIRONMENT=Production
BoardApi__ApiKey=your-api-key
BoardApi__ApiToken=your-api-token
Email__Username=your-email
Email__Password=your-password
```

### 4. デプロイ

Gitプッシュで自動デプロイされます。

## 開発者向け情報

### 保守担当

- **担当者**: suzuki.kosei@3dv.co.jp
- **リポジトリ**: 別途管理

### Python版との互換性

- 同じPostgreSQLデータベースを使用
- データモデルは完全互換
- Python版からC#版への移行はシームレス

### ログイン情報

- 初期パスワードは社員番号と同じ
- 管理者アカウント: `admin` または `2024`

## トラブルシューティング

### データベース接続エラー

```bash
# 接続文字列を確認
echo $DATABASE_URL

# PostgreSQL接続テスト
psql $DATABASE_URL
```

### ビルドエラー

```bash
# パッケージを再復元
dotnet clean
dotnet restore
dotnet build
```

## ライセンス

© 2025 3Dビジュアル株式会社

---

**作成日**: 2025年10月15日
**バージョン**: 1.0.0 (C# Edition)

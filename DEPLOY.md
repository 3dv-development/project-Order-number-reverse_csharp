# デプロイ手順書

## Renderへのデプロイ

### 前提条件

1. Renderアカウントを持っていること
2. Gitリポジトリが作成されていること
3. PostgreSQLデータベースが既に設定されていること（Python版と共有）

### 手順

#### 1. Gitリポジトリの準備

```bash
cd C:\Users\h.hasebe\project-Order-number-reverse_csharp

git init
git add .
git commit -m "Initial commit - C# version"

# リモートリポジトリを追加（GitHubなど）
git remote add origin https://github.com/your-org/project-Order-number-reverse_csharp.git
git push -u origin main
```

#### 2. Renderでの新規Web Service作成

1. https://dashboard.render.com/ にアクセス
2. 「New +」→「Web Service」をクリック
3. Gitリポジトリを接続

#### 3. サービス設定

**Basic Settings:**
- Name: `project-order-number-csharp`（任意）
- Region: `Singapore` または `Oregon`
- Branch: `main`
- Runtime: `Docker` または `.NET`

**Build Settings:**
- Build Command:
  ```bash
  dotnet publish -c Release -o out
  ```

- Start Command:
  ```bash
  cd out && dotnet ProjectOrderNumberSystem.dll
  ```

**Environment Variables:**

以下の環境変数を設定：

```
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:5001

DATABASE_URL=postgresql://project_user:Inj7VQ3JHamRu9BGD2VBNmibBfQBvuym@dpg-d3jl9e9r0fns738bgh20-a/project_numbers

BoardApi__ApiKey=rSpvXP3Qar8BxUh5B4AeR5vmQyGMiOBJ7Xv3xywl
BoardApi__ApiToken=fea92a922aa101c92225d3bd1f88b662ab303aa8

Email__SmtpServer=smtp.gmail.com
Email__SmtpPort=587
Email__Username=(メールアドレス)
Email__Password=(アプリパスワード)
Email__DefaultSender=noreply@3dv.co.jp
Email__NotificationEmail=3dvall@3dv.co.jp
```

**注意**: 階層的な設定には `__`（アンダースコア2つ）を使用します。

#### 4. デプロイ実行

1. 「Create Web Service」をクリック
2. 自動的にビルドとデプロイが開始されます
3. ログを確認してエラーがないことを確認

#### 5. 動作確認

デプロイ完了後、RenderのURLにアクセス：

```
https://project-order-number-csharp.onrender.com
```

ログインページが表示されれば成功です。

### トラブルシューティング

#### ビルドエラー

```bash
# ローカルで確認
dotnet clean
dotnet restore
dotnet build -c Release
dotnet publish -c Release -o out
```

#### データベース接続エラー

環境変数`DATABASE_URL`が正しく設定されているか確認：

```bash
# Renderのシェルで確認
echo $DATABASE_URL
```

#### ポートエラー

Renderはポート10000を使用します。`ASPNETCORE_URLS`環境変数を確認：

```
ASPNETCORE_URLS=http://+:10000
```

### Dockerデプロイ（オプション）

Dockerを使用する場合は、以下のDockerfileを作成：

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

COPY *.csproj ./
RUN dotnet restore

COPY . ./
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/out .

ENV ASPNETCORE_URLS=http://+:10000
EXPOSE 10000

ENTRYPOINT ["dotnet", "ProjectOrderNumberSystem.dll"]
```

### 継続的デプロイ

Gitへのpush時に自動デプロイされます：

```bash
git add .
git commit -m "Update feature"
git push origin main
```

Renderが自動的に検知してデプロイを開始します。

### ロールバック

Renderのダッシュボードから以前のデプロイバージョンに戻すことができます：

1. Renderダッシュボード → サービスを選択
2. 「Events」タブ → 以前のデプロイを選択
3. 「Rollback to this deploy」をクリック

## ローカル開発環境

### 起動方法

```bash
dotnet run
```

または Visual Studio で F5 キーを押下

### データベース接続

ローカル開発では、`appsettings.Development.json`を作成：

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=project_numbers;Username=postgres;Password=postgres"
  }
}
```

### ホットリロード

```bash
dotnet watch run
```

ファイルを編集すると自動的に再起動されます。

## 管理者向け

### 保守担当者

- **氏名**: 鈴木 光星
- **メール**: suzuki.kosei@3dv.co.jp
- **権限**: リポジトリ管理、デプロイ権限

### バックアップ

データベースはPython版と共有のため、Python版のバックアップ手順に従ってください。

### モニタリング

Renderのログを確認：

```bash
# Renderダッシュボード → Logs
```

### スケーリング

必要に応じてRenderのプランをアップグレードしてください。

---

**最終更新**: 2025年10月15日

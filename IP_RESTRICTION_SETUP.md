# IP制限機能セットアップガイド

## 概要

受注番号採番システムに、特定のIPアドレスからのみアクセスを許可するセキュリティ機能を追加しました。

## 機能説明

- **許可されたIPアドレス**: 220.157.175.227
- **アクセス拒否時**: HTTP 403（Forbidden）エラーを返します
- **ローカル開発環境**: localhost（127.0.0.1、::1）からのアクセスは常に許可されます

## ファイル構成

### 1. Middleware/IpRestrictionMiddleware.cs
IP制限を実装するミドルウェアクラス

**主な機能**:
- クライアントIPアドレスの取得（X-Forwarded-Forヘッダー対応）
- 許可IPリストとの照合
- ローカルホストアクセスの自動許可
- 詳細なログ出力

### 2. appsettings.json / appsettings.Production.json
IP制限の設定

```json
{
  "IpRestriction": {
    "Enabled": true,
    "AllowedIps": [
      "220.157.175.227"
    ]
  }
}
```

### 3. Program.cs
ミドルウェアの登録（HTTPリクエストパイプライン）

## Render環境での設定

### 環境変数の設定（オプション）

Renderダッシュボードで以下の環境変数を設定できます：

#### IP制限を有効/無効にする
```
IpRestriction__Enabled=true
```

#### 許可IPアドレスを追加する
```
IpRestriction__AllowedIps__0=220.157.175.227
IpRestriction__AllowedIps__1=xxx.xxx.xxx.xxx
```

**注意**: ASP.NET Coreの環境変数では、階層構造を `__`（アンダースコア2つ）で表現します。

### 設定手順

1. Renderダッシュボードにログイン
2. 対象のWeb Serviceを選択
3. 「Environment」タブを開く
4. 上記の環境変数を追加
5. 「Save Changes」をクリック
6. 自動的に再デプロイされます

## ローカル開発環境での設定

### IP制限を無効化する場合

開発中にIP制限を無効化したい場合は、`appsettings.Development.json`を作成：

```json
{
  "IpRestriction": {
    "Enabled": false
  }
}
```

### ローカルテスト

ローカル環境（localhost、127.0.0.1）からのアクセスは、IP制限が有効でも常に許可されます。

## 追加のIPアドレスを許可する方法

### 1. appsettings.json を編集

```json
{
  "IpRestriction": {
    "Enabled": true,
    "AllowedIps": [
      "220.157.175.227",
      "xxx.xxx.xxx.xxx",
      "yyy.yyy.yyy.yyy"
    ]
  }
}
```

### 2. Gitにコミット＆プッシュ

```bash
git add appsettings.json appsettings.Production.json
git commit -m "Update allowed IP addresses"
git push
```

### 3. Render環境変数で上書き（推奨）

セキュリティのため、本番環境のIPリストは環境変数で管理することを推奨します：

```
IpRestriction__AllowedIps__0=220.157.175.227
IpRestriction__AllowedIps__1=xxx.xxx.xxx.xxx
```

## トラブルシューティング

### アクセスが拒否される場合

1. **自社のグローバルIPアドレスを確認**
   - https://www.cman.jp/network/support/go_access.cgi にアクセス
   - 表示されたIPアドレスを許可リストに追加

2. **Renderのログを確認**
   ```
   アクセス元IP: xxx.xxx.xxx.xxx
   許可されていないIPアドレスからのアクセスをブロック: xxx.xxx.xxx.xxx
   ```

3. **環境変数が正しく設定されているか確認**
   - Renderダッシュボードの「Environment」タブで確認

### IP制限を一時的に無効化する

緊急時にIP制限を無効化する場合：

```
IpRestriction__Enabled=false
```

環境変数を設定して再デプロイします。

## セキュリティ上の注意

- **IPアドレスの管理**: 許可IPリストは機密情報として扱ってください
- **環境変数の使用**: 本番環境では環境変数での設定を推奨します
- **ログの監視**: 不正アクセスの試行がないか定期的にログを確認してください
- **動的IP**: 会社のIPアドレスが動的に変わる場合は、VPNやプロキシの使用を検討してください

## ログの確認

Renderのログで以下の情報を確認できます：

```
[INFO] アクセス元IP: 220.157.175.227
[INFO] 許可されたIPアドレスからのアクセス: 220.157.175.227
```

または

```
[WARNING] 許可されていないIPアドレスからのアクセスをブロック: xxx.xxx.xxx.xxx
```

---

**作成日**: 2025年10月30日
**バージョン**: 1.0.0

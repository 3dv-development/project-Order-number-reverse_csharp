# Renderスリープ防止設定ガイド

## 概要

Renderの無料プランでは、15分間アクセスがないとサービスがスリープ状態になります。このガイドでは、外部監視サービスを使用してサービスを常時稼働させる方法を説明します。

## ヘルスチェックエンドポイント

IP制限を実装しているため、専用のヘルスチェックエンドポイントを用意しています：

```
https://project-order-number-reverse-csharp.onrender.com/Home/Health
```

このエンドポイントの特徴：
- ✅ IP制限から除外されている
- ✅ 認証不要でアクセス可能
- ✅ 軽量なJSON応答
- ✅ データベース接続は必要なし

**応答例：**
```json
{
  "status": "healthy",
  "timestamp": "2025-10-30T10:00:00Z",
  "service": "ProjectOrderNumberSystem"
}
```

## 推奨監視サービス

### 1. UptimeRobot（推奨）

無料で最大50個のモニターを設定でき、5分間隔でチェックできます。

#### 設定手順

1. **アカウント作成**
   - https://uptimerobot.com/ にアクセス
   - 無料アカウントを作成

2. **モニターの追加**
   - 「Add New Monitor」をクリック
   - Monitor Type: `HTTP(s)`
   - Friendly Name: `受注番号採番システム`
   - URL: `https://project-order-number-reverse-csharp.onrender.com/Home/Health`
   - Monitoring Interval: `5 minutes`（無料プランの最短間隔）

3. **アラート設定（オプション）**
   - Email Alert Contactsでメールアドレスを登録
   - サービスダウン時に通知を受け取れます

4. **保存**
   - 「Create Monitor」をクリック

**結果：**
- 5分ごとにヘルスチェックエンドポイントにアクセス
- サービスがスリープしないよう維持

---

### 2. Cron-job.org

無料で50個のジョブまで設定可能で、1分間隔でチェックできます。

#### 設定手順

1. **アカウント作成**
   - https://cron-job.org/ にアクセス
   - 無料アカウントを作成

2. **Cronjobの作成**
   - 「Create cronjob」をクリック
   - Title: `受注番号採番システム Keep Alive`
   - Address: `https://project-order-number-reverse-csharp.onrender.com/Home/Health`
   - Schedule: `Every 5 minutes`（推奨）
   - Request method: `GET`

3. **詳細設定（オプション）**
   - Expected Response: `200`
   - Notifications: ダウン時のメール通知を有効化

4. **保存**
   - 「Create」をクリック

---

### 3. Render Cron Jobs（有料オプション）

Renderの公式機能ですが、Cron Jobsは有料プランが必要です。

---

## 監視間隔の推奨設定

| サービス | 推奨間隔 | 理由 |
|---------|---------|------|
| UptimeRobot | 5分 | 無料プランの最短間隔、15分のタイムアウトに対して十分 |
| Cron-job.org | 5分 | サーバー負荷を抑えつつスリープを防止 |

**注意：** 1分間隔は可能ですが、サーバーリソースを考慮して5分間隔を推奨します。

---

## 動作確認

### ブラウザでテスト

ヘルスチェックエンドポイントに直接アクセス：

```
https://project-order-number-reverse-csharp.onrender.com/Home/Health
```

**期待される応答：**
```json
{
  "status": "healthy",
  "timestamp": "2025-10-30T10:00:00Z",
  "service": "ProjectOrderNumberSystem"
}
```

### curlでテスト

コマンドラインからテスト：

```bash
curl https://project-order-number-reverse-csharp.onrender.com/Home/Health
```

---

## トラブルシューティング

### ヘルスチェックが失敗する

**原因1: サービスが起動中**
- 初回アクセス時は起動に1〜2分かかります
- 監視サービスのタイムアウトを60秒以上に設定

**原因2: IP制限の設定ミス**
- ヘルスチェックエンドポイントがIP制限から除外されているか確認
- Renderのログで「ヘルスチェックエンドポイントへのアクセス」が表示されるか確認

### サービスがまだスリープする

**監視間隔を確認**
- 15分以内の間隔で設定されているか確認
- UptimeRobot/Cron-jobのダッシュボードで実行履歴を確認

**監視が正常に動作しているか確認**
- 監視サービスのステータスページで成功率を確認
- 200 OKのレスポンスが返っているか確認

---

## セキュリティ上の注意

### ヘルスチェックエンドポイントの安全性

- ✅ 認証情報は不要
- ✅ システムの状態のみを返す
- ✅ データベースアクセスなし
- ✅ 機密情報は含まれない

### IP制限との関係

- メインシステムは220.157.175.227からのみアクセス可能
- ヘルスチェックエンドポイント（/Home/Health）のみ除外
- セキュリティと可用性のバランスを保持

---

## コスト

### 無料プランの比較

| サービス | 無料プラン | モニター数 | 最短間隔 | アラート |
|---------|-----------|-----------|----------|---------|
| UptimeRobot | あり | 50個 | 5分 | Email/SMS |
| Cron-job.org | あり | 50個 | 1分 | Email |
| Render Cron | なし | - | - | - |

**推奨：** UptimeRobot（使いやすさと信頼性のバランスが良い）

---

## 実装の確認

以下のファイルでヘルスチェック機能が実装されています：

1. **Controllers/HomeController.cs**
   - `Health()` アクションメソッド
   - IP制限なしでアクセス可能

2. **Middleware/IpRestrictionMiddleware.cs**
   - `/Home/Health` パスをIP制限から除外
   - ログ出力で動作確認可能

---

## まとめ

1. ✅ ヘルスチェックエンドポイントを実装
2. ✅ IP制限から除外
3. ✅ UptimeRobotまたはCron-job.orgで監視設定
4. ✅ 5分間隔で定期アクセス
5. ✅ Renderサービスが常時稼働

**設定完了後、Renderサービスはスリープせず常時稼働します！**

---

**作成日**: 2025年10月30日
**バージョン**: 1.0.0

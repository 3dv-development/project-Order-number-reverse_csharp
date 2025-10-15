// グローバルJavaScript
console.log('受注番号採番システム (C# Edition) - 読み込み完了');

// アラートの自動非表示
document.addEventListener('DOMContentLoaded', function() {
    const alerts = document.querySelectorAll('.alert');
    alerts.forEach(alert => {
        setTimeout(() => {
            const bsAlert = new bootstrap.Alert(alert);
            bsAlert.close();
        }, 5000); // 5秒後に自動非表示
    });
});

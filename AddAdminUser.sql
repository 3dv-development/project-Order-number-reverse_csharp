-- リモートデータベースに管理者ユーザーを追加するSQL
-- 社員番号2024の管理者ユーザーを追加（既に存在する場合はスキップ）

-- 既存チェック
DO $$
BEGIN
    -- 社員番号2024が存在するかチェック
    IF NOT EXISTS (SELECT 1 FROM employees WHERE employee_id = '2024') THEN
        -- 存在しない場合は追加
        INSERT INTO employees (employee_id, name, email, is_active, role)
        VALUES ('2024', '管理者', 'admin@3dv.co.jp', true, 'admin');
        RAISE NOTICE '社員番号2024の管理者ユーザーを追加しました';
    ELSE
        RAISE NOTICE '社員番号2024のユーザーは既に存在します';
    END IF;
END $$;

-- 確認
SELECT * FROM employees WHERE employee_id = '2024';

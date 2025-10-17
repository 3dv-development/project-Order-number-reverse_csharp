-- Check existing data in remote database
SELECT 'Employees' as table_name, COUNT(*) as count FROM employees
UNION ALL
SELECT 'Projects', COUNT(*) FROM projects
UNION ALL
SELECT 'Edit Histories', COUNT(*) FROM edit_histories;

-- Show all employees
SELECT * FROM employees ORDER BY employee_id;

-- Show recent projects
SELECT id, project_number, title, status, created_at FROM projects ORDER BY created_at DESC LIMIT 10;

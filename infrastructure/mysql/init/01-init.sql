-- WO Property Management System - MySQL 初始化脚本
-- 此脚本在 MySQL 容器首次启动时自动执行
-- 字符集: utf8mb4

-- 创建数据库（如果不存在）
CREATE DATABASE IF NOT EXISTS wo_property
    CHARACTER SET utf8mb4
    COLLATE utf8mb4_unicode_ci;

USE wo_property;

-- 设置默认字符集
SET NAMES utf8mb4;
SET CHARACTER SET utf8mb4;

-- 创建应用用户（如果不存在）
-- 注意：MySQL 8.0 需要使用 CREATE USER IF NOT EXISTS
CREATE USER IF NOT EXISTS 'woproperty'@'%';
ALTER USER 'woproperty'@'%' IDENTIFIED BY 'WOProperty2026!';
GRANT ALL PRIVILEGES ON wo_property.* TO 'woproperty'@'%';
FLUSH PRIVILEGES;

-- 日志：记录初始化完成
SELECT 'WO Property MySQL initialization complete' AS status;

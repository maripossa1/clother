-- *******************************************************************
-- 1. إنشاء قاعدة البيانات (تنفيذ لمرة واحدة)
-- *******************************************************************
-- يسقط (يحذف) قاعدة البيانات إذا كانت موجودة، ثم ينشئها جديدة
DROP DATABASE IF EXISTS clothing_store;
CREATE DATABASE clothing_store;

-- الاتصال بقاعدة البيانات الجديدة لإنشاء الجداول
\c clothing_store; 


-- *******************************************************************
-- 2. إنشاء جدول المنتجات (products)
-- نستخدم أسماء الأعمدة التي تم إنشاؤها لديك أولاً
-- *******************************************************************
CREATE TABLE products (
    id SERIAL PRIMARY KEY,
    -- الاسم الفعلي الذي تم إنشاؤه:
    name VARCHAR(255) NOT NULL,
    price numeric(10,2) NOT NULL, -- السعر النهائي (TTC)
    stock_quantity INTEGER NOT NULL DEFAULT 0, -- الكمية في المخزون
    size VARCHAR(50), 
    color VARCHAR(100), 
    category VARCHAR(100), 
    barcode VARCHAR(100) UNIQUE, 
    created_at TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    tva_rate numeric(5,2) DEFAULT 20.00,
    price_without_tva numeric(10,2) -- السعر بدون ضريبة (HT)
);


-- *******************************************************************
-- 3. إنشاء جدول المبيعات (sales)
-- *******************************************************************
CREATE TABLE sales (
    id SERIAL PRIMARY KEY,
    barcode VARCHAR(100),
    product_name VARCHAR(255) NOT NULL,
    quantity_sold INTEGER NOT NULL,
    unit_price numeric(10,2) NOT NULL, -- سعر الوحدة (TTC)
    total_amount numeric(10,2) NOT NULL, -- الإجمالي (TTC)
    sale_date TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    payment_method VARCHAR(50) DEFAULT 'CASH',
    category VARCHAR(100),
    tva_rate numeric(5,2) DEFAULT 20.00,
    unit_price_without_tva numeric(10,2), -- سعر الوحدة (HT)
    total_amount_without_tva numeric(10,2) -- الإجمالي (HT)
);


-- *******************************************************************
-- 4. أوامر التعديل (ALTER TABLE) لضمان التوافق مع الكود القديم
-- هذا الجزء هو ما يجعل الكود الثابت يعمل دون تعديل
-- *******************************************************************

-- جدول products: تغيير الأسماء لتطابق الكود الثابت
ALTER TABLE products RENAME COLUMN name TO product_name; -- (name -> product_name)
ALTER TABLE products RENAME COLUMN price TO price_ttc; -- (price -> price_ttc)
ALTER TABLE products RENAME COLUMN price_without_tva TO price_ht; -- (price_without_tva -> price_ht)
ALTER TABLE products RENAME COLUMN stock_quantity TO quantity; -- (stock_quantity -> quantity)

-- جدول sales: تغيير الأسماء لتطابق الكود الثابت
ALTER TABLE sales RENAME COLUMN quantity_sold TO quantity; -- (quantity_sold -> quantity)
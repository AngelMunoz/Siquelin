-- MIGRONDI:NAME=add-name-column_1708811304117.sql
-- MIGRONDI:TIMESTAMP=1708811304117
-- ---------- MIGRONDI:UP ----------

-- We're adding new columns to the database to store the name of the item.
-- and attempt to backfill

alter table work_days add column item_name text;

UPDATE work_days SET item_name = CAST(id AS TEXT) where item_name is null;

alter table shift_items add column item_name text;

UPDATE shift_items SET item_name = CAST(id AS TEXT) where item_name is null;

-- ---------- MIGRONDI:DOWN ----------
-- Add your SQL rollback code below. You can delete this line but do not delete the comment above.



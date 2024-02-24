-- MIGRONDI:NAME=initial-types_1708665113196.sql
-- MIGRONDI:TIMESTAMP=1708665113196
-- ---------- MIGRONDI:UP ----------

-- Add the SQLite SQL tables for the initial types

create table work_days(
    id integer primary key autoincrement,
    date text not null,
    unique(date)
);

create table shift_items(
    id integer primary key autoincrement,
    work_day_id integer not null,
    start_time text not null,
    end_time text not null,

    foreign key(work_day_id) references work_days(id)
      ON DELETE CASCADE
);

-- ---------- MIGRONDI:DOWN ----------



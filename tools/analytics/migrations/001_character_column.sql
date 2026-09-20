-- Shared runs table, one row per uploaded run from ANY character mod.
-- `character` = the uploading mod's manifest id ("TheWitch", "TheAugur"), written by
-- Common/Data/RunAnalytics.cs. Existing rows predate the column and are all Witch runs.
--
-- NOT applied automatically. Run in the Supabase SQL editor BEFORE shipping a client that
-- sends the column (PostgREST rejects inserts with unknown columns -> every upload 400s).

alter table public.runs
    add column if not exists character text not null default 'TheWitch';

create index if not exists runs_character_idx on public.runs (character);

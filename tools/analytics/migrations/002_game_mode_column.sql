-- Game mode of each uploaded run: 'STANDARD' or 'BLITZ' (the vanilla metrics strings), written by
-- Common/Data/RunAnalytics.cs from the run's GameMode. Custom, Daily and Dread runs are never
-- uploaded. Existing rows predate the column and are all Standard runs.
--
-- NOT applied automatically. Run in the Supabase SQL editor BEFORE shipping a client that
-- sends the column (PostgREST rejects inserts with unknown columns -> every upload 400s).

alter table public.runs
    add column if not exists game_mode text not null default 'STANDARD';

create index if not exists runs_game_mode_idx on public.runs (game_mode);

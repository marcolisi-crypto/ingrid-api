create extension if not exists "pgcrypto";

create table if not exists dealerships (
  id uuid primary key default gen_random_uuid(),
  code text not null unique,
  name text not null,
  timezone text not null default 'America/Toronto',
  status text not null default 'active',
  created_at_utc timestamptz not null default now(),
  updated_at_utc timestamptz not null default now()
);

create table if not exists customers (
  id uuid primary key default gen_random_uuid(),
  first_name text not null default '',
  last_name text not null default '',
  email text not null default '',
  preferred_language text not null default '',
  status text not null default 'active',
  created_at_utc timestamptz not null default now(),
  updated_at_utc timestamptz not null default now()
);

create table if not exists customer_phones (
  id uuid primary key default gen_random_uuid(),
  customer_id uuid not null references customers(id) on delete cascade,
  e164_phone text not null unique,
  phone_type text not null default 'mobile',
  is_primary boolean not null default true,
  created_at_utc timestamptz not null default now()
);

create table if not exists vehicles (
  id uuid primary key default gen_random_uuid(),
  vin text not null default '',
  year int,
  make text not null default '',
  model text not null default '',
  trim text not null default '',
  mileage int,
  status text not null default 'active',
  created_at_utc timestamptz not null default now(),
  updated_at_utc timestamptz not null default now()
);

create index if not exists idx_vehicles_vin on vehicles(vin);

create table if not exists customer_vehicles (
  id uuid primary key default gen_random_uuid(),
  customer_id uuid not null references customers(id) on delete cascade,
  vehicle_id uuid not null references vehicles(id) on delete cascade,
  relationship_type text not null default 'owner',
  is_primary boolean not null default false,
  created_at_utc timestamptz not null default now(),
  unique (customer_id, vehicle_id)
);

create table if not exists conversations (
  id uuid primary key default gen_random_uuid(),
  customer_id uuid references customers(id),
  vehicle_id uuid references vehicles(id),
  channel text not null,
  external_key text not null,
  status text not null default 'open',
  last_message_preview text not null default '',
  last_message_at_utc timestamptz,
  message_count int not null default 0,
  created_at_utc timestamptz not null default now(),
  updated_at_utc timestamptz not null default now(),
  unique (channel, external_key)
);

create table if not exists timeline_events (
  id uuid primary key default gen_random_uuid(),
  customer_id uuid references customers(id),
  vehicle_id uuid references vehicles(id),
  conversation_id uuid references conversations(id),
  event_type text not null,
  title text not null,
  body text not null default '',
  department text not null default '',
  source_system text not null default 'ingrid',
  source_id text not null default '',
  occurred_at_utc timestamptz not null,
  created_at_utc timestamptz not null default now()
);

create index if not exists idx_timeline_events_customer_id on timeline_events(customer_id);
create index if not exists idx_timeline_events_vehicle_id on timeline_events(vehicle_id);
create index if not exists idx_timeline_events_occurred_at_utc on timeline_events(occurred_at_utc desc);

create table if not exists calls (
  id uuid primary key default gen_random_uuid(),
  customer_id uuid references customers(id),
  vehicle_id uuid references vehicles(id),
  conversation_id uuid references conversations(id),
  call_sid text not null unique,
  parent_call_sid text not null default '',
  direction text not null default '',
  from_phone text not null default '',
  to_phone text not null default '',
  status text not null default '',
  recording_url text not null default '',
  recording_sid text not null default '',
  transcript text not null default '',
  detected_language text not null default '',
  detected_department text not null default '',
  notes text not null default '',
  started_at_utc timestamptz,
  ended_at_utc timestamptz,
  created_at_utc timestamptz not null default now(),
  updated_at_utc timestamptz not null default now()
);

create table if not exists notes (
  id uuid primary key default gen_random_uuid(),
  customer_id uuid references customers(id),
  vehicle_id uuid references vehicles(id),
  call_sid text not null default '',
  body text not null,
  note_type text not null default 'internal',
  created_at_utc timestamptz not null default now(),
  updated_at_utc timestamptz not null default now()
);

create index if not exists idx_notes_call_sid on notes(call_sid);

create table if not exists tasks (
  id uuid primary key default gen_random_uuid(),
  customer_id uuid references customers(id),
  vehicle_id uuid references vehicles(id),
  title text not null,
  description text not null default '',
  status text not null default 'open',
  priority text not null default 'normal',
  due_at_utc timestamptz,
  created_at_utc timestamptz not null default now(),
  updated_at_utc timestamptz not null default now()
);

create table if not exists appointments (
  id uuid primary key default gen_random_uuid(),
  customer_id uuid references customers(id),
  vehicle_id uuid references vehicles(id),
  first_name text not null default '',
  last_name text not null default '',
  phone text not null default '',
  email text not null default '',
  make text not null default '',
  model text not null default '',
  year text not null default '',
  vin text not null default '',
  service text not null default '',
  advisor text not null default '',
  date text not null default '',
  time text not null default '',
  transport text not null default '',
  notes text not null default '',
  status text not null default 'scheduled',
  scheduled_start_utc timestamptz,
  created_at_utc timestamptz not null default now(),
  updated_at_utc timestamptz not null default now()
);

create table if not exists repair_orders (
  id uuid primary key default gen_random_uuid(),
  customer_id uuid references customers(id),
  vehicle_id uuid references vehicles(id),
  appointment_id uuid references appointments(id),
  repair_order_number text not null unique,
  status text not null default 'open',
  advisor text not null default '',
  complaint text not null default '',
  odometer_in int,
  transport_option text not null default '',
  notes text not null default '',
  promise_at_utc timestamptz,
  opened_at_utc timestamptz not null default now(),
  closed_at_utc timestamptz,
  labor_subtotal numeric(12,2) not null default 0,
  parts_subtotal numeric(12,2) not null default 0,
  fees_subtotal numeric(12,2) not null default 0,
  payments_applied numeric(12,2) not null default 0,
  total_estimate numeric(12,2) not null default 0,
  balance_due numeric(12,2) not null default 0,
  created_at_utc timestamptz not null default now(),
  updated_at_utc timestamptz not null default now()
);

create index if not exists idx_repair_orders_customer_id on repair_orders(customer_id);
create index if not exists idx_repair_orders_vehicle_id on repair_orders(vehicle_id);
create index if not exists idx_repair_orders_status on repair_orders(status);

create table if not exists repair_order_estimate_lines (
  id uuid primary key default gen_random_uuid(),
  repair_order_id uuid not null references repair_orders(id) on delete cascade,
  line_type text not null default 'labor',
  op_code text not null default '',
  description text not null default '',
  quantity numeric(12,2) not null default 1,
  unit_price numeric(12,2) not null default 0,
  department text not null default 'service',
  status text not null default 'open',
  created_at_utc timestamptz not null default now(),
  updated_at_utc timestamptz not null default now()
);

create index if not exists idx_ro_estimate_lines_repair_order_id on repair_order_estimate_lines(repair_order_id);

create table if not exists repair_order_part_lines (
  id uuid primary key default gen_random_uuid(),
  repair_order_id uuid not null references repair_orders(id) on delete cascade,
  part_number text not null default '',
  description text not null default '',
  quantity numeric(12,2) not null default 1,
  unit_price numeric(12,2) not null default 0,
  status text not null default 'requested',
  source text not null default 'stock',
  created_at_utc timestamptz not null default now(),
  updated_at_utc timestamptz not null default now()
);

create index if not exists idx_ro_part_lines_repair_order_id on repair_order_part_lines(repair_order_id);

create table if not exists technician_clock_events (
  id uuid primary key default gen_random_uuid(),
  repair_order_id uuid not null references repair_orders(id) on delete cascade,
  technician_name text not null default '',
  event_type text not null default 'clock_in',
  labor_op_code text not null default '',
  notes text not null default '',
  occurred_at_utc timestamptz not null default now(),
  created_at_utc timestamptz not null default now()
);

create index if not exists idx_technician_clock_events_repair_order_id on technician_clock_events(repair_order_id);
create index if not exists idx_technician_clock_events_occurred_at_utc on technician_clock_events(occurred_at_utc desc);

create table if not exists accounting_entries (
  id uuid primary key default gen_random_uuid(),
  repair_order_id uuid not null references repair_orders(id) on delete cascade,
  entry_type text not null default 'estimate',
  description text not null default '',
  amount numeric(12,2) not null default 0,
  status text not null default 'open',
  created_at_utc timestamptz not null default now(),
  updated_at_utc timestamptz not null default now()
);

create index if not exists idx_accounting_entries_repair_order_id on accounting_entries(repair_order_id);

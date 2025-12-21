/*
  ordersystem – db schema (mysql)
  author: <doplň>
  date: <doplň>

  splňuje:
  - 5+ tabulek (customers, products, orders, order_items, payments)
  - m:n vazba (orders <-> products přes order_items)
  - datové typy: float, bool, "enum" ekvivalent přes check, string, datetime
*/

use ordersystem;

-- zákazníci
create table if not exists customers (
  id int auto_increment primary key,
  first_name varchar(80) not null,
  last_name varchar(80) not null,
  email varchar(255) not null unique,
  phone varchar(30) null,
  created_at datetime not null default current_timestamp
);

-- produkty
create table if not exists products (
  id int auto_increment primary key,
  name varchar(160) not null,
  price decimal(10,2) not null,
  stock int not null default 0,
  is_active tinyint(1) not null default 1, -- bool
  rating float null,                        -- float
  created_at datetime not null default current_timestamp,
  constraint uq_products_name unique (name)
);

-- objednávky
create table if not exists orders (
  id int auto_increment primary key,
  customer_id int not null,
  state varchar(20) not null check (state in ('new','paid')), -- enum ekvivalent
  note varchar(500) null,
  created_at datetime not null default current_timestamp,
  constraint fk_orders_customer foreign key (customer_id)
    references customers(id)
    on delete restrict on update cascade
);

-- položky objednávky (m:n)
create table if not exists order_items (
  order_id int not null,
  product_id int not null,
  quantity int not null,
  unit_price decimal(10,2) not null,
  primary key (order_id, product_id),
  constraint fk_items_order foreign key (order_id)
    references orders(id)
    on delete cascade on update cascade,
  constraint fk_items_product foreign key (product_id)
    references products(id)
    on delete restrict on update cascade
);

-- platby
create table if not exists payments (
  id int auto_increment primary key,
  order_id int not null,
  amount decimal(10,2) not null,
  paid tinyint(1) not null default 0, -- bool
  paid_at datetime null,
  provider varchar(60) null,
  constraint fk_payments_order foreign key (order_id)
    references orders(id)
    on delete cascade on update cascade
);

-- seed (pár záznamů pro rychlý start)
insert into customers(first_name,last_name,email,phone) values
('Jan','Novák','jan.novak@example.com','777111222')
on duplicate key update email=email;

insert into products(name,price,stock,is_active,rating) values('Tričko',399.00,50,1,4.2),
('Mikina',899.00,20,1,4.6),
('Čepice',249.00,35,1,3.9)
on duplicate key update name=name;
